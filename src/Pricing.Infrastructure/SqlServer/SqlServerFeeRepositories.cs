namespace Pricing.Infrastructure.SqlServer;

using System.Collections.Immutable;
using Pricing.Application.Fees;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>VNG31/VN_CID_VN_FREIGHT(VNG40)/BGG10/ING01/VNG20/VNG19 adapters for A6U01 0205, 7270/7820/7895, and 9650–9691.</summary>
public sealed class SqlServerFreightRepository(ISqlServerQueryExecutor executor) : IFreightRepository
{
    private static readonly SqlServerQuery Account = Query("freight.account", """
        SELECT TOP (1) P_AC_VN_FREIGHT AS CostPercentage, P_AC_VN_SELL_FREIGHT AS SellPercentage,
          CAST(NULL AS decimal(19,8)) AS DirectAmount, D_AC_VN_FREIGHT_EF AS EffectiveDate,
          D_AC_VN_FREIGHT_EX AS ExpirationDate
        FROM dbo.VNG31 WHERE I_ACCOUNT=@Account AND I_VENDOR=@Vendor
          AND D_AC_VN_FREIGHT_EF<=@PricingDate AND (D_AC_VN_FREIGHT_EX>=@PricingDate OR D_AC_VN_FREIGHT_EX IS NULL)
        ORDER BY D_AC_VN_FREIGHT_EF DESC
        """);
    private static readonly SqlServerQuery Customer = Query("freight.customer", """
        SELECT TOP (1) P_CID_VN_FREIGHT AS CostPercentage, P_CID_VN_SELL_FREIGHT AS SellPercentage,
          CAST(NULL AS decimal(19,8)) AS DirectAmount, D_CID_VN_FREIGHT_EF AS EffectiveDate,
          D_CID_VN_FREIGHT_EX AS ExpirationDate
        FROM dbo.VN_CID_VN_FREIGHT WHERE CUSTOMER_NBR=@Customer AND I_VENDOR=@Vendor
          AND D_CID_VN_FREIGHT_EF<=@PricingDate AND (D_CID_VN_FREIGHT_EX>=@PricingDate OR D_CID_VN_FREIGHT_EX IS NULL)
        ORDER BY D_CID_VN_FREIGHT_EF DESC
        """);
    private static readonly SqlServerQuery BuyingGroup = Query("freight.buying-group", """
        SELECT TOP (1) P_INFRT_BG AS CostPercentage, P_INFRT_BG AS SellPercentage,
          CAST(NULL AS decimal(19,8)) AS DirectAmount, D_INFRT_BG_EFF AS EffectiveDate,
          D_INFRT_BG_EXP AS ExpirationDate
        FROM dbo.BGG10 WHERE I_BUY_GROUP=@BuyingGroup AND I_VENDOR IN (@Vendor,'0000')
          AND D_INFRT_BG_EFF<=@PricingDate AND (D_INFRT_BG_EXP>=@PricingDate OR D_INFRT_BG_EXP IS NULL)
        ORDER BY CASE WHEN I_VENDOR=@Vendor THEN 0 ELSE 1 END, D_INFRT_BG_EFF DESC
        """);
    private static readonly SqlServerQuery Product = Query("freight.product", """
        SELECT TOP (1) CAST(0 AS decimal(19,8)) AS CostPercentage, CAST(0 AS decimal(19,8)) AS SellPercentage,
          A_DIV_INV_FREIGHT AS DirectAmount, @PricingDate AS EffectiveDate, CAST(NULL AS datetime) AS ExpirationDate
        FROM dbo.ING01 WHERE I_DIVISION=@Division AND I_VENDOR=@Vendor AND I_VND_PRODUCT=@Product
          AND A_DIV_INV_FREIGHT > 0
        """);
    private static readonly SqlServerQuery Division = Query("freight.division-vendor", """
        SELECT TOP (1) P_IN_FREIGHT AS CostPercentage, P_IN_SELL_FREIGHT AS SellPercentage,
          CAST(NULL AS decimal(19,8)) AS DirectAmount, D_IN_FREIGHT_EFF AS EffectiveDate, D_IN_FREIGHT_EXP AS ExpirationDate
        FROM dbo.VNG20 WHERE I_DIVISION=@Division AND I_VENDOR=@Vendor
          AND D_IN_FREIGHT_EFF<=@PricingDate AND (D_IN_FREIGHT_EXP>=@PricingDate OR D_IN_FREIGHT_EXP IS NULL)
        ORDER BY D_IN_FREIGHT_EFF DESC
        """);
    private static readonly SqlServerQuery Corporate = Query("freight.corporate-vendor", """
        SELECT TOP (1) P_IN_FREIGHT AS CostPercentage, P_IN_SELL_FREIGHT AS SellPercentage,
          CAST(NULL AS decimal(19,8)) AS DirectAmount, D_IN_FREIGHT_EFF AS EffectiveDate, D_IN_FREIGHT_EXP AS ExpirationDate
        FROM dbo.VNG19 WHERE I_VENDOR=@Vendor
          AND D_IN_FREIGHT_EFF<=@PricingDate AND (D_IN_FREIGHT_EXP>=@PricingDate OR D_IN_FREIGHT_EXP IS NULL)
        ORDER BY D_IN_FREIGHT_EFF DESC
        """);

    public ValueTask<FreightLookupResult> FindAccountAsync(PricingContext context, CancellationToken cancellationToken) =>
        FindAsync(Account, context, FreightSource.Account, cancellationToken);
    public ValueTask<FreightLookupResult> FindCustomerAsync(PricingContext context, CancellationToken cancellationToken) =>
        context.Customer.Customer is null ? ValueTask.FromResult(new FreightLookupResult(null)) : FindAsync(Customer, context, FreightSource.Customer, cancellationToken);
    public async ValueTask<FreightLookupResult> FindBuyingGroupAsync(PricingContext context, CancellationToken cancellationToken)
    {
        foreach (BuyingGroupMembership membership in context.Customer.BuyingGroupMemberships.OrderBy(x => x.Priority))
        {
            FreightLookupResult result = await FindAsync(BuyingGroup, context, FreightSource.BuyingGroup, cancellationToken, membership.BuyingGroupId).ConfigureAwait(false);
            if (result.Error is not null || result.Candidate is not null) return result;
        }
        return new(null);
    }
    public ValueTask<FreightLookupResult> FindProductAsync(PricingContext context, CancellationToken cancellationToken) =>
        FindAsync(Product, context, FreightSource.Product, cancellationToken);
    public ValueTask<FreightLookupResult> FindDivisionVendorAsync(PricingContext context, CancellationToken cancellationToken) =>
        FindAsync(Division, context, FreightSource.DivisionVendor, cancellationToken);
    public ValueTask<FreightLookupResult> FindCorporateVendorAsync(PricingContext context, CancellationToken cancellationToken) =>
        FindAsync(Corporate, context, FreightSource.CorporateVendor, cancellationToken);

    private async ValueTask<FreightLookupResult> FindAsync(
        SqlServerQuery query, PricingContext context, FreightSource source,
        CancellationToken cancellationToken, long? group = null)
    {
        try
        {
            IReadOnlyList<Row> rows = await executor.QueryAsync<Row>(query, Parameters(context, group), cancellationToken).ConfigureAwait(false);
            if (rows.Count == 0) return new(null);
            Row row = rows[0];
            PricingDateRange dates = SqlServerCostMapping.Range(row.EffectiveDate, row.ExpirationDate);
            return new(new FreightCandidate(source, row.CostPercentage, row.SellPercentage,
                row.DirectAmount is { } amount ? new Money(amount) : null, null,
                new RuleProvenance("inbound-freight", $"A6U01 0205; {query.Operation}", source.ToString(), dates),
                dates, group));
        }
        catch (SqlServerAccessException exception)
        {
            return new(null, Dependency("FREIGHT_LOOKUP_FAILED", query.Operation, exception));
        }
    }

    private static SqlServerQuery Query(string name, string sql) => new(name, sql);
    private static object Parameters(PricingContext context, long? group = null) => new
    {
        Account = context.Request.Account.Value, Customer = context.Customer.Customer?.Value,
        Division = context.Request.Division.Value, Vendor = context.Request.Vendor.Value,
        Product = context.Request.Product.Value, BuyingGroup = group,
        PricingDate = context.Request.PricingDate.ToDateTime(TimeOnly.MinValue),
    };
    private sealed record Row(decimal CostPercentage, decimal SellPercentage, decimal? DirectAmount,
        DateTime EffectiveDate, DateTime? ExpirationDate);

    internal static DependencyPricingError Dependency(string code, string source, SqlServerAccessException exception) =>
        new(code, $"{source} SQL lookup failed.", null, exception.IsTransient, "90");
}

/// <summary>VNG05 adapter for A6U01 0245/7872–7898.</summary>
/// <remarks>BG_LOW_UOM_VEND_EXCL is an orphaned legacy code with no table in the migrated schema; group/vendor exclusion always resolves false.</remarks>
public sealed class SqlServerLowUomRepository(ISqlServerQueryExecutor executor) : ILowUomRepository
{
    private static readonly SqlServerQuery Alternate = new("low-uom.alternates", """
        SELECT RTRIM(C_VD_PRD_ALT_UM) AS UnitOfMeasure, A_VD_PRD_ALT_UMF AS ConversionFactor,
          RTRIM(F_ALT_UOM_DESIGNATOR) AS Designator
        FROM dbo.VNG05 WHERE I_VENDOR=@Vendor AND I_VND_PRODUCT=@Product
        ORDER BY C_VD_PRD_ALT_UM
        """);

    public async ValueTask<AlternateUomLookupResult> LoadAlternateUomsAsync(
        PricingContext context, CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<Row> rows = await executor.QueryAsync<Row>(
                Alternate, SqlServerCostMapping.Parameters(context), cancellationToken).ConfigureAwait(false);
            return new(rows.Select(row => new AlternateUomCandidate(new UnitOfMeasure(row.UnitOfMeasure.Trim()),
                row.ConversionFactor, row.Designator.Trim() switch
                {
                    "L" => LowUomDesignator.LowUnitOfMeasure,
                    "B" => LowUomDesignator.BreakBulk,
                    _ => LowUomDesignator.None,
                })).ToImmutableArray());
        }
        catch (SqlServerAccessException exception)
        {
            return new([], SqlServerFreightRepository.Dependency("LOW_UOM_LOOKUP_FAILED", Alternate.Operation, exception));
        }
    }

    public ValueTask<bool> IsGroupVendorExcludedAsync(
        PricingContext context, long buyingGroupId, DateOnly effectiveDate, CancellationToken cancellationToken) =>
        ValueTask.FromResult(false);

    private sealed record Row(string UnitOfMeasure, decimal ConversionFactor, string Designator);
}

/// <summary>CUTFEE_PRICE/CUG29 adapter for A6U01 0165/0170/7705/7706.</summary>
public sealed class SqlServerPandacRepository(ISqlServerQueryExecutor executor) : IPandacRepository
{
    private static readonly SqlServerQuery NewSpecific = new("pandac.new-specific", """
        SELECT TOP (1) FEE_PCT AS Rate, FEE_AMT AS Amount, RTRIM(FEE_TYPE) AS FeeType,
          RTRIM(BILLING_FREQ) AS BillingFrequency, D_FEE_EFFECTIVE AS EffectiveDate, D_FEE_EXPIRE AS ExpirationDate
        FROM dbo.CUTFEE_PRICE WHERE COMPANY_ID='OM' AND CUST_ID=@CustomerId
          AND SHIP_TO_SUFFIX=@ShipTo AND FEE_SHORT_CODE='PN'
          AND D_FEE_EFFECTIVE<=@PricingDate AND (D_FEE_EXPIRE>=@PricingDate OR D_FEE_EXPIRE IS NULL)
        ORDER BY D_FEE_EFFECTIVE DESC
        """);
    private static readonly SqlServerQuery Legacy = new("pandac.legacy", """
        SELECT TOP (1) P_PANDAC_ADJ AS Rate, CAST(0 AS decimal(19,8)) AS Amount,
          'C' AS FeeType, CAST(NULL AS varchar(2)) AS BillingFrequency,
          D_PANDAC_ADJ_EFF AS EffectiveDate, D_PANDAC_ADJ_EXP AS ExpirationDate
        FROM dbo.CUG29 WHERE I_ACCOUNT=@Account AND C_SHIP_TO_SUFFIX=@ShipTo
          AND D_PANDAC_ADJ_EFF<=@PricingDate AND (D_PANDAC_ADJ_EXP>=@PricingDate OR D_PANDAC_ADJ_EXP IS NULL)
        ORDER BY D_PANDAC_ADJ_EFF DESC
        """);

    public ValueTask<PandacLookupResult> FindNewSpecificAsync(PricingContext context, CancellationToken cancellationToken) =>
        FindAsync(NewSpecific, context, PandacSource.NewSpecificShipTo, cancellationToken);
    public ValueTask<PandacLookupResult> FindLegacySpecificAsync(PricingContext context, CancellationToken cancellationToken) =>
        FindAsync(Legacy, context, PandacSource.LegacySpecificShipTo, cancellationToken);
    public ValueTask<PandacLookupResult> FindDefaultAsync(PricingContext context, CancellationToken cancellationToken) =>
        FindAsync(Legacy, context, PandacSource.DefaultShipTo, cancellationToken, "000");

    private async ValueTask<PandacLookupResult> FindAsync(
        SqlServerQuery query, PricingContext context, PandacSource source,
        CancellationToken cancellationToken, string? shipTo = null)
    {
        try
        {
            IReadOnlyList<Row> rows = await executor.QueryAsync<Row>(query, new
            {
                Account = context.Request.Account.Value,
                CustomerId = context.Request.Division.Value + context.Request.Account.Value,
                ShipTo = shipTo ?? context.Request.ShipTo ?? "000",
                PricingDate = context.Request.PricingDate.ToDateTime(TimeOnly.MinValue),
            }, cancellationToken).ConfigureAwait(false);
            if (rows.Count == 0) return new(null);
            Row row = rows[0];
            PricingDateRange dates = SqlServerCostMapping.Range(row.EffectiveDate, row.ExpirationDate);
            PandacFeeBasis basis = row.FeeType.Trim() switch
            {
                "S" or "P" => PandacFeeBasis.SellPercentage,
                "F" or "A" => PandacFeeBasis.Flat,
                _ => PandacFeeBasis.CostPercentage,
            };
            return new(new PandacCandidate(source, basis, row.Rate, new Money(row.Amount), true,
                new RuleProvenance("pandac", $"A6U01 7705/7706; {query.Operation}", source.ToString(), dates),
                dates, row.BillingFrequency?.Trim()));
        }
        catch (SqlServerAccessException exception)
        {
            return new(null, SqlServerFreightRepository.Dependency("PANDAC_LOOKUP_FAILED", query.Operation, exception));
        }
    }

    private sealed record Row(decimal Rate, decimal Amount, string FeeType,
        string? BillingFrequency, DateTime EffectiveDate, DateTime? ExpirationDate);
}

/// <summary>VNG21 surcharge adapter for A6U01 0215/0250/7210.</summary>
/// <remarks>VNG17/32/33/34/35/37/39 are orphaned legacy codes with no table in the migrated schema.</remarks>
public sealed class SqlServerSurchargeRepository(ISqlServerQueryExecutor executor) : ISurchargeRepository
{
    private static readonly SqlServerQuery Query = new("surcharge.waterfall", """
        SELECT TOP (1) Percentage, EffectiveDate, ExpirationDate FROM (
          SELECT P_BG_VN_SURCHG AS Percentage, D_BG_VN_SURCHG_EFF AS EffectiveDate, D_BG_VN_SURCHG_EXP AS ExpirationDate
          FROM dbo.VNG21 WHERE @Table='VNG21' AND I_BUY_GROUP=@BuyingGroup AND I_VENDOR=@LookupVendor
        ) x WHERE EffectiveDate<=@PricingDate AND (ExpirationDate>=@PricingDate OR ExpirationDate IS NULL)
        ORDER BY EffectiveDate DESC
        """);

    public async ValueTask<SurchargeLookupResult> FindAsync(
        PricingContext context, SurchargeLevel level, CancellationToken cancellationToken)
    {
        (string table, bool defaultVendor, long? group) = Resolve(context, level);
        try
        {
            IReadOnlyList<Row> rows = await executor.QueryAsync<Row>(Query, new
            {
                Table = table, Account = context.Request.Account.Value,
                Customer = context.Customer.Customer?.Value, Division = context.Request.Division.Value,
                LookupVendor = defaultVendor ? "0000" : context.Request.Vendor.Value,
                Category = context.Product.ProductCategory, BuyingGroup = group,
                PricingDate = context.Request.PricingDate.ToDateTime(TimeOnly.MinValue),
            }, cancellationToken).ConfigureAwait(false);
            if (rows.Count == 0) return new(null);
            Row row = rows[0];
            PricingDateRange dates = SqlServerCostMapping.Range(row.EffectiveDate, row.ExpirationDate);
            return new(new SurchargeCandidate(level, row.Percentage, Code(level),
                new RuleProvenance("category-surcharge", $"A6U01 0215/0250; {table}", level.ToString(), dates)));
        }
        catch (SqlServerAccessException exception)
        {
            return new(null, SqlServerFreightRepository.Dependency("SURCHARGE_LOOKUP_FAILED", table, exception));
        }
    }

    private static (string Table, bool DefaultVendor, long? Group) Resolve(PricingContext context, SurchargeLevel level)
    {
        bool category = level.ToString().Contains("Category", StringComparison.Ordinal);
        bool fallback = level.ToString().Contains("Default", StringComparison.Ordinal);
        string table = level switch
        {
            SurchargeLevel.AccountCategoryVendor or SurchargeLevel.AccountCategoryDefault => "VNG35",
            SurchargeLevel.CustomerCategoryVendor or SurchargeLevel.CustomerCategoryDefault => "VNG37",
            SurchargeLevel.DivisionCategoryVendor or SurchargeLevel.DivisionCategoryDefault => "VNG33",
            SurchargeLevel.CorporateCategoryVendor or SurchargeLevel.CorporateCategoryDefault => "VNG32",
            SurchargeLevel.BuyingGroup => category ? "VNG34" : "VNG21",
            SurchargeLevel.AccountVendor or SurchargeLevel.AccountDefault => "VNG17",
            SurchargeLevel.CustomerVendor or SurchargeLevel.CustomerDefault => "VNG39",
            _ => "VNG21",
        };
        long? group = context.Customer.BuyingGroupMemberships.OrderBy(x => x.Priority)
            .Select(x => (long?)x.BuyingGroupId).FirstOrDefault();
        return (table, fallback, group);
    }
    private static string Code(SurchargeLevel level) => ((int)level + 1).ToString("00", System.Globalization.CultureInfo.InvariantCulture);
    private sealed record Row(decimal Percentage, DateTime EffectiveDate, DateTime? ExpirationDate);
}
