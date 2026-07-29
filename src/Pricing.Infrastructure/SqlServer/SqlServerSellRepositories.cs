namespace Pricing.Infrastructure.SqlServer;

using System.Collections.Immutable;
using Pricing.Application.SellSelection;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>SAG01/05/09 and SA_CID_SELL_ASSN adapter for A6U01 0180/7075/0185.</summary>
public sealed class SqlServerAccountCustomerSellArrangementRepository(ISqlServerQueryExecutor executor)
    : IAccountCustomerSellArrangementRepository
{
    public async ValueTask<SellArrangementSelection?> FindAsync(
        PricingContext context, SellArrangementScope scope, SellArrangementLevel level,
        CancellationToken cancellationToken)
    {
        if (level == SellArrangementLevel.SpecialServiceCode)
        {
            return null;
        }

        try
        {
            IReadOnlyList<SellArrangementRow> rows = await executor.QueryAsync<SellArrangementRow>(
                SqlServerSellQueries.AccountCustomer,
                SqlServerSellQueries.Parameters(context, scope, level),
                cancellationToken).ConfigureAwait(false);
            return rows.Count == 0 ? null : SqlServerSellQueries.Map(rows[0], scope.ToString(), level);
        }
        catch (SqlServerAccessException exception)
        {
            throw new SellArrangementRepositoryException(
                "Account/customer sell-arrangement SQL lookup failed.", "33", "90",
                exception.IsTransient, exception);
        }
    }
}

/// <summary>SAG02/03/05 adapter preserving subgroup priority and nearest-parent order.</summary>
public sealed class SqlServerBuyingGroupSellArrangementRepository(ISqlServerQueryExecutor executor)
    : IBuyingGroupSellArrangementRepository
{
    public async ValueTask<SellArrangementSelection?> FindSubgroupAsync(
        PricingContext context, SellCostCascade cascade, SellArrangementLevel level,
        CancellationToken cancellationToken)
    {
        foreach (BuyingGroupMembership membership in context.Customer.BuyingGroupMemberships.OrderBy(x => x.Priority))
        {
            SellArrangementSelection? selection = await FindAsync(
                context, membership.BuyingGroupId, membership.Priority, 0, level, cancellationToken).ConfigureAwait(false);
            if (selection is not null)
            {
                return selection;
            }
        }
        return null;
    }

    public async ValueTask<ImmutableArray<ParentSellArrangementCandidate>> FindParentCandidatesAsync(
        PricingContext context, SellCostCascade cascade, CancellationToken cancellationToken)
    {
        var result = ImmutableArray.CreateBuilder<ParentSellArrangementCandidate>();
        foreach (BuyingGroupMembership membership in context.Customer.BuyingGroupMemberships.OrderBy(x => x.Priority))
        {
            if (membership.ParentBuyingGroupId is not { } parent)
            {
                continue;
            }
            foreach (SellArrangementLevel level in ParentLevels(cascade))
            {
                SellArrangementSelection? selection = await FindAsync(
                    context, parent, membership.Priority, 1, level, cancellationToken).ConfigureAwait(false);
                if (selection is not null)
                {
                    result.Add(new ParentSellArrangementCandidate(1, level, selection));
                }
            }
        }
        return result.ToImmutable();
    }

    private async ValueTask<SellArrangementSelection?> FindAsync(
        PricingContext context, long group, int priority, int depth, SellArrangementLevel level,
        CancellationToken cancellationToken)
    {
        if (level == SellArrangementLevel.SpecialServiceCode)
        {
            return null;
        }
        try
        {
            IReadOnlyList<SellArrangementRow> rows = await executor.QueryAsync<SellArrangementRow>(
                SqlServerSellQueries.BuyingGroup,
                SqlServerSellQueries.Parameters(context, SellArrangementScope.Account, level, group),
                cancellationToken).ConfigureAwait(false);
            return rows.Count == 0
                ? null
                : SqlServerSellQueries.Map(rows[0], depth == 0 ? "SUBGROUP" : "PARENT", level,
                    group, priority, depth);
        }
        catch (SqlServerAccessException exception)
        {
            throw new BuyingGroupSellArrangementRepositoryException(
                "Buying-group sell-arrangement SQL lookup failed.", "90", "90",
                exception.IsTransient, exception);
        }
    }

    private static IEnumerable<SellArrangementLevel> ParentLevels(SellCostCascade cascade) =>
        cascade == SellCostCascade.GroupContract
            ? [SellArrangementLevel.Product, SellArrangementLevel.VendorContract,
               SellArrangementLevel.ProductCategory, SellArrangementLevel.SpecialServiceCode,
               SellArrangementLevel.Vendor, SellArrangementLevel.Default]
            : [SellArrangementLevel.Product, SellArrangementLevel.ProductCategory,
               SellArrangementLevel.SpecialServiceCode, SellArrangementLevel.Vendor,
               SellArrangementLevel.Default];
}

/// <summary>Corporate 01/666666 assignment plus A6U01 7240/7250/7255 group override.</summary>
public sealed class SqlServerCorporateSellArrangementRepository(
    ISqlServerQueryExecutor executor,
    IBuyingGroupSellArrangementRepository groups) : ICorporateSellArrangementRepository
{
    public async ValueTask<CorporateSellArrangementMatch> FindAsync(
        PricingContext context, SellArrangementLevel level, CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<SellArrangementRow> rows = await executor.QueryAsync<SellArrangementRow>(
                SqlServerSellQueries.AccountCustomer,
                SqlServerSellQueries.Parameters(context, SellArrangementScope.Account, level,
                    account: "666666", division: "01"),
                cancellationToken).ConfigureAwait(false);
            SellArrangementSelection? corporate = rows.Count == 0
                ? null
                : SqlServerSellQueries.Map(rows[0], "CORPORATE", level);
            SellArrangementSelection? groupOverride = corporate is null
                ? null
                : await groups.FindSubgroupAsync(context, SellCostCascade.IndividualContract, level, cancellationToken)
                    .ConfigureAwait(false);
            if (corporate is not null && groupOverride is null)
            {
                ImmutableArray<ParentSellArrangementCandidate> parents = await groups
                    .FindParentCandidatesAsync(context, SellCostCascade.IndividualContract, cancellationToken)
                    .ConfigureAwait(false);
                groupOverride = parents.Where(candidate => candidate.Level == level)
                    .OrderBy(candidate => candidate.ParentDepth)
                    .Select(candidate => candidate.Selection)
                    .FirstOrDefault();
            }
            return new CorporateSellArrangementMatch(corporate, groupOverride);
        }
        catch (SqlServerAccessException exception)
        {
            throw new CorporateSellArrangementRepositoryException(
                "Corporate sell-arrangement SQL lookup failed.", "33", "90",
                exception.IsTransient, exception);
        }
    }
}

/// <summary>CUG31 adapter for A6U01 7765 and 9475-SQL-SELECT-010.</summary>
public sealed class SqlServerPriceLockRepository(ISqlServerQueryExecutor executor) : IPriceLockRepository
{
    private static readonly SqlServerQuery Query = new("sell.price-lock", """
        SELECT TOP (1) A_LP_TOTAL_SELL AS TotalSell, A_LP_TOTAL_COST AS TotalCost,
          A_LP_TOT_SELL_ADJ AS TotalSellAdjustment, A_LP_TOT_COST_ADJ AS TotalCostAdjustment,
          A_LP_UNADJ_UNT_CST AS UnadjustedUnitCost, RTRIM(C_LP_SELL_PRC_METH) AS SellMethodCode,
          P_LP_SELL_PRC_PCT AS SellPercentage, D_LOCKED_PRICE_EFF AS EffectiveDate,
          D_LOCKED_PRICE_EXP AS ExpirationDate
        FROM dbo.CUG31
        WHERE I_ACCOUNT=@Account AND I_VENDOR=@Vendor AND I_VND_PRODUCT=@Product
          AND D_LOCKED_PRICE_EFF<=@PricingDate
          AND (D_LOCKED_PRICE_EXP>=@PricingDate OR D_LOCKED_PRICE_EXP IS NULL)
        ORDER BY D_LOCKED_PRICE_EFF DESC
        """);

    public async ValueTask<PriceLockRecord?> FindEffectiveAsync(
        PricingContext context, CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<Row> rows = await executor.QueryAsync<Row>(
                Query, SqlServerCostMapping.Parameters(context), cancellationToken).ConfigureAwait(false);
            if (rows.Count == 0)
            {
                return null;
            }
            Row row = rows[0];
            PricingDateRange dates = SqlServerCostMapping.Range(row.EffectiveDate, row.ExpirationDate);
            return new PriceLockRecord(new(row.TotalSell), new(row.TotalCost), new(row.TotalSellAdjustment),
                new(row.TotalCostAdjustment), new(row.UnadjustedUnitCost), row.SellMethodCode?.Trim(),
                row.SellPercentage is { } percentage ? new Percentage(percentage) : null, dates,
                new RuleProvenance("price-lock", "A6U01 7765/9475; CUG31", "ACCOUNT", dates));
        }
        catch (SqlServerAccessException exception)
        {
            throw new SellArrangementRepositoryException(
                "Price-lock SQL lookup failed.", "146", "90", exception.IsTransient, exception);
        }
    }

    private sealed record Row(decimal TotalSell, decimal TotalCost, decimal TotalSellAdjustment,
        decimal TotalCostAdjustment, decimal UnadjustedUnitCost, string? SellMethodCode,
        decimal? SellPercentage, DateTime EffectiveDate, DateTime? ExpirationDate);
}

internal sealed record SellArrangementRow(int BaseSell, DateTime EffectiveDate, DateTime? ExpirationDate,
    string MethodCode, decimal? Percentage, decimal? StatedPrice, string? StatedUom);

internal static class SqlServerSellQueries
{
    internal static readonly SqlServerQuery AccountCustomer = new("sell.account-customer", """
        WITH assigned AS (
          SELECT TOP (1) h.I_BAS_SELL AS BaseSell, h.D_SELL_ARR_EFF AS EffectiveDate,
            CASE WHEN h.D_SELL_ARR_EXP IS NULL OR a.AssignmentExpiration<h.D_SELL_ARR_EXP
                 THEN a.AssignmentExpiration ELSE h.D_SELL_ARR_EXP END AS ExpirationDate
          FROM dbo.SAG05 h
          JOIN (
            SELECT I_BAS_SELL, D_SELL_ASGN_EFFECT AS AssignmentEffective,
              D_SELL_ASGN_EXPIRE AS AssignmentExpiration
            FROM dbo.SAG09 WHERE @Scope='Account'
              AND I_ACCOUNT=(SELECT TOP (1) I_ACCOUNT FROM dbo.CUG03
                WHERE I_DIVISION=@Division AND S_ACCOUNT=@Account)
            UNION ALL
            SELECT I_BAS_SELL, D_SELL_ASGN_EFFECT, D_SELL_ASGN_EXPIRE
            FROM dbo.SA_CID_SELL_ASSN WHERE @Scope='CustomerNumber' AND CUSTOMER_NBR=@Customer
          ) a ON a.I_BAS_SELL=h.I_BAS_SELL
          WHERE h.D_SELL_ARR_START<=@PricingDate
            AND (h.D_SELL_ARR_EXP>=@PricingDate OR h.D_SELL_ARR_EXP IS NULL)
            AND a.AssignmentEffective<=@PricingDate
            AND (a.AssignmentExpiration>=@PricingDate OR a.AssignmentExpiration IS NULL)
        )
        SELECT TOP (1) a.BaseSell, a.EffectiveDate, a.ExpirationDate,
          d.MethodCode, d.Percentage, d.StatedPrice, d.StatedUom
        FROM assigned a CROSS APPLY (
          SELECT '1', g.P_SELL_GRSSMGN, NULL, NULL FROM dbo.SAG23 g
            WHERE @Level='Product' AND g.I_BAS_SELL=a.BaseSell AND g.I_VENDOR=@Vendor AND g.I_VND_PRODUCT=@Product
          UNION ALL SELECT '2', p.P_SELL_CST_PLS, NULL, NULL FROM dbo.SAG24 p
            WHERE @Level='Product' AND p.I_BAS_SELL=a.BaseSell AND p.I_VENDOR=@Vendor AND p.I_VND_PRODUCT=@Product
          UNION ALL SELECT '4', p.P_SELL_LIST_LESS, NULL, NULL FROM dbo.SAG25 p
            WHERE @Level='Product' AND p.I_BAS_SELL=a.BaseSell AND p.I_VENDOR=@Vendor AND p.I_VND_PRODUCT=@Product
          UNION ALL SELECT '8', NULL, p.A_SELL_PROD_PRC, RTRIM(p.C_SELL_PROD_UM) FROM dbo.SAG26 p
            WHERE @Level='Product' AND p.I_BAS_SELL=a.BaseSell AND p.I_VENDOR=@Vendor AND p.I_VND_PRODUCT=@Product
          UNION ALL SELECT '1', p.P_SELL_GRSSMGN, NULL, NULL FROM dbo.SAG19 p
            WHERE @Level='VendorContract' AND p.I_BAS_SELL=a.BaseSell AND p.I_CONTRACT=@Contract
          UNION ALL SELECT '2', p.P_SELL_CST_PLS, NULL, NULL FROM dbo.SAG20 p
            WHERE @Level='VendorContract' AND p.I_BAS_SELL=a.BaseSell AND p.I_CONTRACT=@Contract
          UNION ALL SELECT '5', p.P_SELL_CONT_SUGG, NULL, NULL FROM dbo.SAG21 p
            WHERE @Level='VendorContract' AND p.I_BAS_SELL=a.BaseSell AND p.I_CONTRACT=@Contract
          UNION ALL SELECT '4', p.P_SELL_LIST_LESS, NULL, NULL FROM dbo.SAG22 p
            WHERE @Level='VendorContract' AND p.I_BAS_SELL=a.BaseSell AND p.I_CONTRACT=@Contract
          UNION ALL SELECT '1', p.P_SELL_GRSSMGN, NULL, NULL FROM dbo.SAG16 p
            WHERE @Level='ProductCategory' AND p.I_BAS_SELL=a.BaseSell AND p.I_VENDOR=@Vendor AND p.S_PROD_CATEGORY=@Category
          UNION ALL SELECT '2', p.P_SELL_CST_PLS, NULL, NULL FROM dbo.SAG17 p
            WHERE @Level='ProductCategory' AND p.I_BAS_SELL=a.BaseSell AND p.I_VENDOR=@Vendor AND p.S_PROD_CATEGORY=@Category
          UNION ALL SELECT '4', p.P_SELL_LIST_LESS, NULL, NULL FROM dbo.SAG18 p
            WHERE @Level='ProductCategory' AND p.I_BAS_SELL=a.BaseSell AND p.I_VENDOR=@Vendor AND p.S_PROD_CATEGORY=@Category
          UNION ALL SELECT '1', p.P_SELL_GRSSMGN, NULL, NULL FROM dbo.SAG13 p
            WHERE @Level='Vendor' AND p.I_BAS_SELL=a.BaseSell AND p.I_VENDOR=@Vendor
          UNION ALL SELECT '2', p.P_SELL_CST_PLS, NULL, NULL FROM dbo.SAG14 p
            WHERE @Level='Vendor' AND p.I_BAS_SELL=a.BaseSell AND p.I_VENDOR=@Vendor
          UNION ALL SELECT '4', p.P_SELL_LIST_LESS, NULL, NULL FROM dbo.SAG15 p
            WHERE @Level='Vendor' AND p.I_BAS_SELL=a.BaseSell AND p.I_VENDOR=@Vendor
          UNION ALL SELECT '1', p.P_SELL_GRSSMGN, NULL, NULL FROM dbo.SAG11 p
            WHERE @Level='Default' AND p.I_BAS_SELL=a.BaseSell
          UNION ALL SELECT '2', p.P_SELL_CST_PLS, NULL, NULL FROM dbo.SAG12 p
            WHERE @Level='Default' AND p.I_BAS_SELL=a.BaseSell
        ) d(MethodCode,Percentage,StatedPrice,StatedUom)
        """);

    internal static readonly SqlServerQuery BuyingGroup = new("sell.buying-group", """
        WITH assigned AS (
          SELECT TOP (1) h.I_BAS_SELL AS BaseSell, h.D_SELL_ARR_EFF AS EffectiveDate,
            h.D_SELL_ARR_EXP AS ExpirationDate
          FROM dbo.SAG02 a JOIN dbo.SAG05 h ON h.I_BAS_SELL=a.I_BAS_SELL
          WHERE a.I_BUY_GROUP=@BuyingGroup AND h.D_SELL_ARR_START<=@PricingDate
            AND (h.D_SELL_ARR_EXP>=@PricingDate OR h.D_SELL_ARR_EXP IS NULL)
          ORDER BY a.Q_PREF_TIER_LEVEL DESC, a.D_BG_TIER_START DESC
        )
        SELECT TOP (1) a.BaseSell, a.EffectiveDate, a.ExpirationDate,
          d.MethodCode, d.Percentage, d.StatedPrice, d.StatedUom
        FROM assigned a CROSS APPLY (
          SELECT '1', g.P_SELL_GRSSMGN, NULL, NULL FROM dbo.SAG23 g
            WHERE @Level='Product' AND g.I_BAS_SELL=a.BaseSell AND g.I_VENDOR=@Vendor AND g.I_VND_PRODUCT=@Product
          UNION ALL SELECT '2', p.P_SELL_CST_PLS, NULL, NULL FROM dbo.SAG24 p
            WHERE @Level='Product' AND p.I_BAS_SELL=a.BaseSell AND p.I_VENDOR=@Vendor AND p.I_VND_PRODUCT=@Product
          UNION ALL SELECT '4', p.P_SELL_LIST_LESS, NULL, NULL FROM dbo.SAG25 p
            WHERE @Level='Product' AND p.I_BAS_SELL=a.BaseSell AND p.I_VENDOR=@Vendor AND p.I_VND_PRODUCT=@Product
          UNION ALL SELECT '8', NULL, p.A_SELL_PROD_PRC, RTRIM(p.C_SELL_PROD_UM) FROM dbo.SAG26 p
            WHERE @Level='Product' AND p.I_BAS_SELL=a.BaseSell AND p.I_VENDOR=@Vendor AND p.I_VND_PRODUCT=@Product
          UNION ALL SELECT '1', p.P_SELL_GRSSMGN, NULL, NULL FROM dbo.SAG16 p
            WHERE @Level='ProductCategory' AND p.I_BAS_SELL=a.BaseSell AND p.I_VENDOR=@Vendor AND p.S_PROD_CATEGORY=@Category
          UNION ALL SELECT '2', p.P_SELL_CST_PLS, NULL, NULL FROM dbo.SAG17 p
            WHERE @Level='ProductCategory' AND p.I_BAS_SELL=a.BaseSell AND p.I_VENDOR=@Vendor AND p.S_PROD_CATEGORY=@Category
          UNION ALL SELECT '4', p.P_SELL_LIST_LESS, NULL, NULL FROM dbo.SAG18 p
            WHERE @Level='ProductCategory' AND p.I_BAS_SELL=a.BaseSell AND p.I_VENDOR=@Vendor AND p.S_PROD_CATEGORY=@Category
          UNION ALL SELECT '1', p.P_SELL_GRSSMGN, NULL, NULL FROM dbo.SAG13 p
            WHERE @Level='Vendor' AND p.I_BAS_SELL=a.BaseSell AND p.I_VENDOR=@Vendor
          UNION ALL SELECT '2', p.P_SELL_CST_PLS, NULL, NULL FROM dbo.SAG14 p
            WHERE @Level='Vendor' AND p.I_BAS_SELL=a.BaseSell AND p.I_VENDOR=@Vendor
          UNION ALL SELECT '4', p.P_SELL_LIST_LESS, NULL, NULL FROM dbo.SAG15 p
            WHERE @Level='Vendor' AND p.I_BAS_SELL=a.BaseSell AND p.I_VENDOR=@Vendor
          UNION ALL SELECT '1', p.P_SELL_GRSSMGN, NULL, NULL FROM dbo.SAG11 p WHERE @Level='Default' AND p.I_BAS_SELL=a.BaseSell
          UNION ALL SELECT '2', p.P_SELL_CST_PLS, NULL, NULL FROM dbo.SAG12 p WHERE @Level='Default' AND p.I_BAS_SELL=a.BaseSell
        ) d(MethodCode,Percentage,StatedPrice,StatedUom)
        """);

    internal static object Parameters(PricingContext context, SellArrangementScope scope,
        SellArrangementLevel level, long? buyingGroup = null, string? account = null, string? division = null) => new
    {
        Scope = scope.ToString(),
        Division = division ?? context.Request.Division.Value,
        Account = account ?? context.Request.Account.Value,
        Customer = context.Customer.Customer?.Value,
        Vendor = context.Request.Vendor.Value,
        Product = context.Request.Product.Value,
        Contract = context.ContractSelection?.Contract.Value,
        Category = context.Product.ProductCategory,
        Level = level.ToString(),
        BuyingGroup = buyingGroup,
        PricingDate = context.Request.PricingDate.ToDateTime(TimeOnly.MinValue),
    };

    internal static SellArrangementSelection Map(SellArrangementRow row, string scope,
        SellArrangementLevel level, long? group = null, int? priority = null, int? depth = null)
    {
        PricingDateRange dates = SqlServerCostMapping.Range(row.EffectiveDate, row.ExpirationDate);
        return new SellArrangementSelection(row.BaseSell.ToString(System.Globalization.CultureInfo.InvariantCulture),
            level.ToString(), new RuleProvenance("sell-arrangement",
                $"A6U01 0180/7075/0185; SAG{Table(level)}", scope, dates),
            group, null, priority, null, depth, row.MethodCode.Trim(), row.Percentage,
            row.StatedPrice is { } stated ? new Money(stated) : null,
            row.StatedUom is { Length: > 0 } uom ? new UnitOfMeasure(uom.Trim()) : null);
    }

    private static string Table(SellArrangementLevel level) => level switch
    {
        SellArrangementLevel.Product => "23-26",
        SellArrangementLevel.VendorContract => "19-22",
        SellArrangementLevel.ProductCategory => "16-18",
        SellArrangementLevel.Vendor => "13-15",
        _ => "11-12",
    };
}
