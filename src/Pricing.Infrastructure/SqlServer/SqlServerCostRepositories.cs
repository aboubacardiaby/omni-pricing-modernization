namespace Pricing.Infrastructure.SqlServer;

using System.Collections.Immutable;
using System.Globalization;
using Pricing.Application.CostSelection;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

internal static class SqlServerCostMapping
{
    internal static object Parameters(PricingContext context) => new
    {
        Division = context.Request.Division.Value,
        Account = context.Request.Account.Value,
        Customer = context.Customer.Customer?.Value,
        Vendor = context.Request.Vendor.Value,
        Product = context.Request.Product.Value,
        UnitOfMeasure = context.Request.UnitOfMeasure.Value,
        PricingDate = context.Request.PricingDate.ToDateTime(TimeOnly.MinValue),
        SpecialBypass = context.Request.IsSpecialContract ? "B" : null,
    };

    internal static DateOnly Date(DateTime value) => DateOnly.FromDateTime(value);
    internal static DateOnly? Date(DateTime? value) => value is null ? null : DateOnly.FromDateTime(value.Value);
    internal static PricingDateRange Range(DateTime effective, DateTime? expiration) => new(Date(effective), Date(expiration));
    internal static bool Yes(string? value) => string.Equals(value?.Trim(), "Y", StringComparison.OrdinalIgnoreCase);
}

/// <summary>CCG01/03/04/06/09/27 adapter for A6U01 9037/9040 and MIN_INDV.</summary>
public sealed class SqlServerIndividualCostContractRepository(ISqlServerQueryExecutor executor)
    : IIndividualCostContractRepository
{
    private static readonly SqlServerQuery Query = new("cost.individual-contracts", """
        SELECT c.I_CONTRACT AS Contract, c.C_CNT_TYPE AS ContractType,
          l.A_CNT_LN_UNIT_COST AS UnitCost, RTRIM(l.C_CNT_LN_UM) AS UnitOfMeasure,
          c.D_CNT_START AS ContractEffective, c.D_CNT_PROT_END AS ContractExpiration,
          l.D_CNT_LINE_START AS LineEffective, l.D_CNT_LINE_EXPIRE AS LineExpiration,
          a.D_ASGN_EFFECT AS AssignmentEffective, a.D_ASGN_EXPIRE AS AssignmentExpiration,
          v.D_IND_VND_CNT_EFF AS VendorEffective, v.D_IND_VND_CNT_EXP AS VendorExpiration,
          CASE WHEN x.I_ACCOUNT IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS IsExcluded,
          COALESCE((SELECT TOP (1) s.A_CNT_LN_SUGG_SELL FROM dbo.CCG13 s WHERE s.I_CONTRACT=c.I_CONTRACT AND s.L_CNT_LINE=l.L_CNT_LINE),
                   (SELECT TOP (1) s.A_CNT_LN_SUGG_SELL FROM dbo.CCG14 s WHERE s.I_CONTRACT=c.I_CONTRACT AND s.L_CNT_LINE=l.L_CNT_LINE),
                   (SELECT TOP (1) s.A_CNT_LN_SUGG_SELL FROM dbo.CCG15 s WHERE s.I_CONTRACT=c.I_CONTRACT AND s.L_CNT_LINE=l.L_CNT_LINE),
                   (SELECT TOP (1) s.A_CNT_LN_SUGG_SELL FROM dbo.CCG16 s WHERE s.I_CONTRACT=c.I_CONTRACT AND s.L_CNT_LINE=l.L_CNT_LINE)) AS SuggestedSellPrice
        FROM dbo.CCG01 c
        JOIN dbo.CCG03 l ON l.I_CONTRACT=c.I_CONTRACT
        JOIN dbo.CCG04 d ON d.I_CONTRACT=c.I_CONTRACT AND d.I_DIVISION=@Division
        JOIN dbo.CCG06 a ON a.I_CONTRACT=c.I_CONTRACT AND a.I_CUSTOMER=@Customer
        JOIN dbo.CCG09 v ON v.I_CONTRACT=c.I_CONTRACT AND v.I_DIVISION=d.I_DIVISION
        LEFT JOIN dbo.CC_CNT_PRT_FLG p ON p.I_CONTRACT=c.I_CONTRACT
        LEFT JOIN dbo.CCG25 x ON x.I_CONTRACT=c.I_CONTRACT AND x.I_ACCOUNT=(SELECT TOP (1) I_ACCOUNT FROM dbo.CUG03 WHERE I_DIVISION=@Division AND S_ACCOUNT=@Account)
          AND x.C_SHIP_TO_SUFFIX=COALESCE(@ShipTo,'') AND x.D_EFFECT<=@PricingDate
          AND (x.D_EXPIRE>=@PricingDate OR x.D_EXPIRE IS NULL)
        WHERE l.I_VENDOR=@Vendor AND l.I_VND_PRODUCT=@Product
          AND c.D_CNT_START<=@PricingDate AND c.D_CNT_PROT_END>=@PricingDate
          AND l.D_CNT_LINE_START<=@PricingDate AND (l.D_CNT_LINE_EXPIRE>=@PricingDate OR l.D_CNT_LINE_EXPIRE IS NULL)
          AND a.D_ASGN_EFFECT<=@PricingDate AND (a.D_ASGN_EXPIRE>=@PricingDate OR a.D_ASGN_EXPIRE IS NULL)
          AND v.D_IND_VND_CNT_EFF<=@PricingDate AND (v.D_IND_VND_CNT_EXP>=@PricingDate OR v.D_IND_VND_CNT_EXP IS NULL)
          AND (@SpecialBypass IS NULL OR c.F_BYPASS=@SpecialBypass)
        ORDER BY p.F_CNT_PRIORITY, c.I_CONTRACT
        """);

    public async ValueTask<ImmutableArray<IndividualCostContractCandidate>> FindCandidatesAsync(
        PricingContext context, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = new
            {
                Division = context.Request.Division.Value,
                Account = context.Request.Account.Value,
                Customer = context.Customer.Customer?.Value,
                Vendor = context.Request.Vendor.Value,
                Product = context.Request.Product.Value,
                UnitOfMeasure = context.Request.UnitOfMeasure.Value,
                PricingDate = context.Request.PricingDate.ToDateTime(TimeOnly.MinValue),
                ShipTo = context.Request.ShipTo,
                SpecialBypass = context.Request.IsSpecialContract ? "B" : null,
            };
            IReadOnlyList<Row> rows = await executor.QueryAsync<Row>(Query, parameters, cancellationToken).ConfigureAwait(false);
            IReadOnlyDictionary<string, decimal> factors = await SqlServerCostUomConverter.LoadAsync(executor, context, cancellationToken).ConfigureAwait(false);
            return rows.Select(row => Map(row, context.Request.UnitOfMeasure, factors)).Where(x => x is not null).Select(x => x!).ToImmutableArray();
        }
        catch (SqlServerAccessException exception)
        {
            throw new IndividualCostContractRepositoryException("Individual contract SQL lookup failed.", "120", "90", exception.IsTransient, exception);
        }
    }

    private static IndividualCostContractCandidate? Map(Row row, UnitOfMeasure targetUom, IReadOnlyDictionary<string, decimal> factors)
    {
        if (!SqlServerCostUomConverter.TryNormalize(row.UnitCost, row.UnitOfMeasure, targetUom, factors, out Money normalizedCost)) return null;
        Money? normalizedSuggested = null;
        if (row.SuggestedSellPrice is { } suggested)
        {
            if (!SqlServerCostUomConverter.TryNormalize(suggested, row.UnitOfMeasure, targetUom, factors, out Money convertedSuggested)) return null;
            normalizedSuggested = convertedSuggested;
        }
        PricingDateRange contractDates = SqlServerCostMapping.Range(row.ContractEffective, row.ContractExpiration);
        PricingDateRange lineDates = SqlServerCostMapping.Range(row.LineEffective, row.LineExpiration);
        return new IndividualCostContractCandidate(new ContractId(row.Contract.ToString(CultureInfo.InvariantCulture)), row.ContractType?.Trim() ?? "INDIVIDUAL",
            normalizedCost, targetUom,
            [contractDates, lineDates, SqlServerCostMapping.Range(row.AssignmentEffective, row.AssignmentExpiration),
             SqlServerCostMapping.Range(row.VendorEffective, row.VendorExpiration)], row.IsExcluded,
            new RuleProvenance("individual-customer-cost-contract", "CCG01/03/04/06/09/27", "INDIVIDUAL", lineDates),
            normalizedSuggested);
    }

    private sealed record Row(int Contract, string? ContractType, decimal UnitCost, string UnitOfMeasure,
        DateTime ContractEffective, DateTime? ContractExpiration, DateTime LineEffective, DateTime? LineExpiration,
        DateTime AssignmentEffective, DateTime? AssignmentExpiration, DateTime VendorEffective, DateTime? VendorExpiration,
        bool IsExcluded, decimal? SuggestedSellPrice);
}

/// <summary>CCG01/03/05 buying-group adapter preserving context priority and ancestry.</summary>
public sealed class SqlServerBuyingGroupCostContractRepository(ISqlServerQueryExecutor executor)
    : IBuyingGroupCostContractRepository
{
    private static readonly SqlServerQuery Query = new("cost.buying-group-contracts", """
        SELECT c.I_CONTRACT AS Contract, l.A_CNT_LN_UNIT_COST AS UnitCost, RTRIM(l.C_CNT_LN_UM) AS UnitOfMeasure,
          g.I_BUY_GROUP AS BuyingGroupId, c.D_CNT_START AS ContractEffective, c.D_CNT_PROT_END AS ContractExpiration,
          l.D_CNT_LINE_START AS LineEffective, l.D_CNT_LINE_EXPIRE AS LineExpiration,
          RTRIM(g.F_GRP_CNT_FEES) AS GroupContractFees
        FROM dbo.CCG01 c JOIN dbo.CCG03 l ON l.I_CONTRACT=c.I_CONTRACT
        JOIN dbo.CCG05 g ON g.I_CONTRACT=c.I_CONTRACT
        WHERE l.I_VENDOR=@Vendor AND l.I_VND_PRODUCT=@Product
          AND c.D_CNT_START<=@PricingDate AND c.D_CNT_PROT_END>=@PricingDate
          AND l.D_CNT_LINE_START<=@PricingDate AND (l.D_CNT_LINE_EXPIRE>=@PricingDate OR l.D_CNT_LINE_EXPIRE IS NULL)
        ORDER BY c.I_CONTRACT
        """);

    public async ValueTask<BuyingGroupCostSearchData> FindCandidatesAsync(PricingContext context, CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<Row> rows = await executor.QueryAsync<Row>(Query, SqlServerCostMapping.Parameters(context), cancellationToken).ConfigureAwait(false);
            IReadOnlyDictionary<string, decimal> factors = await SqlServerCostUomConverter.LoadAsync(executor, context, cancellationToken).ConfigureAwait(false);
            return new BuyingGroupCostSearchData(Build(context, rows, factors, BuyingGroupScope.Account, "CUG11"),
                Build(context, rows, factors, BuyingGroupScope.Customer, "CUG07"));
        }
        catch (SqlServerAccessException exception)
        {
            throw new BuyingGroupCostContractRepositoryException("Buying-group contract SQL lookup failed.", "210", "90", exception.IsTransient, exception);
        }
    }

    private static BuyingGroupScopeCandidates Build(PricingContext context, IReadOnlyList<Row> rows, IReadOnlyDictionary<string, decimal> factors, BuyingGroupScope scope, string source)
    {
        var memberships = context.Customer.BuyingGroupMemberships.Where(x => string.Equals(x.Source, source, StringComparison.Ordinal));
        ImmutableArray<BuyingGroupPriorityBranch> branches = memberships.OrderBy(x => x.Priority).Select(m =>
        {
            Row? child = rows.FirstOrDefault(x => x.BuyingGroupId == m.BuyingGroupId);
            ImmutableArray<BuyingGroupCostCandidate> parents = m.ParentBuyingGroupId is { } parent
                ? rows.Where(x => x.BuyingGroupId == parent).Select(x => Map(x, m, context.Request.UnitOfMeasure, factors, scope, BuyingGroupHierarchyLevel.Parent)).Where(x => x is not null).Select(x => x!).ToImmutableArray()
                : [];
            return new BuyingGroupPriorityBranch(m.Priority,
                child is null ? null : Map(child, m, context.Request.UnitOfMeasure, factors, scope, BuyingGroupHierarchyLevel.Child), parents);
        }).ToImmutableArray();
        return new BuyingGroupScopeCandidates(scope, [], [], branches);
    }

    private static BuyingGroupCostCandidate? Map(Row row, BuyingGroupMembership membership, UnitOfMeasure targetUom, IReadOnlyDictionary<string, decimal> factors, BuyingGroupScope scope, BuyingGroupHierarchyLevel level)
    {
        if (!SqlServerCostUomConverter.TryNormalize(row.UnitCost, row.UnitOfMeasure, targetUom, factors, out Money normalizedCost)) return null;
        PricingDateRange lineDates = SqlServerCostMapping.Range(row.LineEffective, row.LineExpiration);
        return new BuyingGroupCostCandidate(new ContractId(row.Contract.ToString(CultureInfo.InvariantCulture)), normalizedCost, targetUom,
            row.BuyingGroupId, membership.Priority, scope, BuyingGroupSelectionPath.Priority, level, true, false,
            [membership.EffectiveDates, SqlServerCostMapping.Range(row.ContractEffective, row.ContractExpiration), lineDates],
            new RuleProvenance("buying-group-cost-contract", "CCG01/03/05", level.ToString().ToUpperInvariant(), lineDates),
            string.Equals(row.GroupContractFees, "Y", StringComparison.Ordinal));
    }

    private sealed record Row(int Contract, decimal UnitCost, string UnitOfMeasure, int BuyingGroupId,
        DateTime ContractEffective, DateTime? ContractExpiration, DateTime LineEffective, DateTime? LineExpiration,
        string? GroupContractFees);
}

/// <summary>Extracted VNG03 A6U01 7105/7575 fallback query.</summary>
public sealed class SqlServerAcquisitionCostRepository(ISqlServerQueryExecutor executor) : IAcquisitionCostRepository
{
    private static readonly SqlServerQuery Query = new("cost.acquisition", """
        SELECT RTRIM(C_VND_PRC_UM) AS UnitOfMeasure, RTRIM(C_VND_PRC_LEVEL) AS PriceLevel,
          D_VND_PRC_LIST_EFF AS EffectiveDate, D_VND_PRC_EXPIRE AS ExpirationDate,
          A_VND_PRC_DEALER AS DealerCost, A_VND_PRC_ACQ_COST AS AcquisitionCost
        FROM dbo.VNG03 WHERE I_VENDOR=@Vendor AND I_VND_PRODUCT=@Product
          AND D_VND_PRC_LIST_EFF<=@PricingDate AND D_VND_PRC_ACTIVE<=@PricingDate
          AND (D_VND_PRC_EXPIRE>=@PricingDate OR D_VND_PRC_EXPIRE IS NULL)
        ORDER BY D_VND_PRC_LIST_EFF DESC, C_VND_PRC_LEVEL DESC
        """);

    public async ValueTask<AcquisitionCostData> FindAsync(PricingContext context, CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<Row> rows = await executor.QueryAsync<Row>(Query, SqlServerCostMapping.Parameters(context), cancellationToken).ConfigureAwait(false);
            IReadOnlyDictionary<string, decimal> factors = await SqlServerCostUomConverter.LoadAsync(executor, context, cancellationToken).ConfigureAwait(false);
            Row? directRow = rows.FirstOrDefault(x => string.Equals(x.UnitOfMeasure.Trim(), context.Request.UnitOfMeasure.Value, StringComparison.Ordinal));
            AcquisitionPriceListCandidate? direct = directRow is null ? null : Map(directRow, context.Request.UnitOfMeasure, factors);
            ImmutableArray<AcquisitionPriceListCandidate> fallback = rows.Where(x => !ReferenceEquals(x, directRow))
                .Select(x => Map(x, context.Request.UnitOfMeasure, factors)).Where(x => x is not null).Select(x => x!).ToImmutableArray();
            return new AcquisitionCostData(direct, fallback);
        }
        catch (SqlServerAccessException exception)
        {
            throw new AcquisitionCostRepositoryException("VNG03 acquisition-cost SQL lookup failed.", "130", "90", exception.IsTransient, exception);
        }
    }

    private static AcquisitionPriceListCandidate? Map(Row row, UnitOfMeasure targetUom, IReadOnlyDictionary<string, decimal> factors)
    {
        if (!SqlServerCostUomConverter.TryNormalize(row.DealerCost, row.UnitOfMeasure, targetUom, factors, out Money dealer)) return null;
        if (!SqlServerCostUomConverter.TryNormalize(row.AcquisitionCost, row.UnitOfMeasure, targetUom, factors, out Money acquisition)) return null;
        PricingDateRange dates = new(SqlServerCostMapping.Date(row.EffectiveDate), SqlServerCostMapping.Date(row.ExpirationDate));
        return new AcquisitionPriceListCandidate(dealer, acquisition,
            targetUom, row.PriceLevel.Trim(), SqlServerCostMapping.Date(row.EffectiveDate),
            new RuleProvenance("acquisition-dealer-cost-fallback", "VNG03", "DEFAULT", dates));
    }

    private sealed record Row(string UnitOfMeasure, string PriceLevel, DateTime EffectiveDate, DateTime? ExpirationDate,
        decimal DealerCost, decimal AcquisitionCost);
}
