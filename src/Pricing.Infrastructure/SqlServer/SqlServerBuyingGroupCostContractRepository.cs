namespace Pricing.Infrastructure.SqlServer;

using System.Collections.Immutable;
using Pricing.Application.CostSelection;
using Pricing.Application.Rounding;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>Loads the A6U01 MIN_GRP contract cursor for active customer memberships.</summary>
/// <remarks>COBOL: A6U01 0240/0315 and MIN_GRP/MIN_GRP_CONT cursor declarations.</remarks>
public sealed class SqlServerBuyingGroupCostContractRepository(ISqlServerQueryExecutor queries) :
    IBuyingGroupCostContractRepository
{
    private static readonly SqlServerQuery Contracts = new("contract.min-grp", """
        SELECT CCG01.I_CONTRACT AS Contract,
               CCG03.C_CNT_LN_UM AS UnitOfMeasure,
               CCG03.A_CNT_LN_UNIT_COST AS UnitCost,
               CCG05.F_GRP_CNT_ELIG_LIS AS MembershipEligible,
               CCG01.D_CNT_START AS HeaderEffective,
               CCG01.D_CNT_PROT_END AS HeaderExpiration,
               CCG03.D_CNT_LINE_START AS LineEffective,
               CCG03.D_CNT_LINE_EXPIRE AS LineExpiration,
               CCG05.D_GRP_FLG_EFF AS GroupEffective,
               CCG05.D_GRP_FLG_EXP AS GroupExpiration,
               CCG10.D_GRP_VND_CNT_EFF AS VendorEffective,
               CCG10.D_GRP_VND_CNT_EXP AS VendorExpiration
        FROM dbo.CCG01
        JOIN dbo.CCG03 ON CCG03.I_CONTRACT = CCG01.I_CONTRACT
        JOIN dbo.CCG05 ON CCG05.I_CONTRACT = CCG01.I_CONTRACT
        JOIN dbo.CCG10 ON CCG10.I_CONTRACT = CCG01.I_CONTRACT
                      AND CCG10.I_VENDOR = CCG01.I_VENDOR
        WHERE CCG05.I_BUY_GROUP = @BuyingGroup
          AND CCG01.I_VENDOR = COALESCE(
              (SELECT TOP (1) I_PARENT_VENDOR FROM dbo.VNG01 WHERE I_VENDOR = @Vendor), @Vendor)
          AND CCG03.I_VENDOR = @Vendor
          AND CCG03.I_VND_PRODUCT = @Product
          AND CCG01.C_CNT_TYPE = @ContractType
          AND CCG01.F_BYPASS = 'N'
          AND CCG01.D_CNT_START <= @PricingDate
          AND CCG01.D_CNT_PROT_END >= @PricingDate
          AND CCG03.D_CNT_LINE_START <= @PricingDate
          AND (CCG03.D_CNT_LINE_EXPIRE >= @PricingDate OR CCG03.D_CNT_LINE_EXPIRE IS NULL)
          AND CCG05.D_GRP_FLG_EFF <= @PricingDate
          AND (CCG05.D_GRP_FLG_EXP >= @PricingDate OR CCG05.D_GRP_FLG_EXP IS NULL)
          AND CCG10.D_GRP_VND_CNT_EFF <= @PricingDate
          AND (CCG10.D_GRP_VND_CNT_EXP >= @PricingDate OR CCG10.D_GRP_VND_CNT_EXP IS NULL)
        ORDER BY CCG01.I_CONTRACT, CCG03.L_CNT_LINE;
        """);
    private static readonly SqlServerQuery Factors = new("contract.group-vng05", """
        SELECT C_VD_PRD_ALT_UM AS UnitOfMeasure, A_VD_PRD_ALT_UMF AS ConversionFactor
        FROM dbo.VNG05 WHERE I_VENDOR = @Vendor AND I_VND_PRODUCT = @Product;
        """);
    private static readonly SqlServerQuery Overrides = new("contract.group-overrides", """
        SELECT 'ACCOUNT_PRODUCT' AS OverrideSource, I_BUY_GROUP AS BuyingGroupId, C_OVERRIDE_TYPE AS OverrideType
          FROM dbo.CUG13
         WHERE I_ACCOUNT = (SELECT TOP (1) I_ACCOUNT FROM dbo.CUG03 WHERE S_ACCOUNT=@Account AND I_DIVISION=@Division)
           AND I_VENDOR=@Vendor AND S_PROD_CATEGORY=@ProductCategory
           AND D_AC_CA_BG_OVR_EFF<=@PricingDate AND (D_AC_CA_BG_OVR_EXP>=@PricingDate OR D_AC_CA_BG_OVR_EXP IS NULL)
        UNION ALL
        SELECT 'ACCOUNT_VENDOR', I_BUY_GROUP, C_OVERRIDE_TYPE FROM dbo.CUG12
         WHERE I_ACCOUNT = (SELECT TOP (1) I_ACCOUNT FROM dbo.CUG03 WHERE S_ACCOUNT=@Account AND I_DIVISION=@Division)
           AND I_VENDOR=@Vendor AND D_AC_VN_BG_OVR_EFF<=@PricingDate
           AND (D_AC_VN_BG_OVR_EXP>=@PricingDate OR D_AC_VN_BG_OVR_EXP IS NULL)
        UNION ALL
        SELECT 'CUSTOMER_PRODUCT', I_BUY_GROUP, C_OVERRIDE_TYPE FROM dbo.CUG09
         WHERE I_CUSTOMER=@Customer AND I_VENDOR=@Vendor AND S_PROD_CATEGORY=@ProductCategory
           AND D_CU_CA_BG_OVR_EFF<=@PricingDate AND (D_CU_CA_BG_OVR_EXP>=@PricingDate OR D_CU_CA_BG_OVR_EXP IS NULL)
        UNION ALL
        SELECT 'CUSTOMER_VENDOR', I_BUY_GROUP, C_OVERRIDE_TYPE FROM dbo.CUG08
         WHERE I_CUSTOMER=@Customer AND I_VENDOR=@Vendor AND D_CU_VN_BG_OVR_EFF<=@PricingDate
           AND (D_CU_VN_BG_OVR_EXP>=@PricingDate OR D_CU_VN_BG_OVR_EXP IS NULL);
        """);
    private static readonly SqlServerQuery Exclusion = new("contract.ccg25-exclusion", """
        SELECT TOP (1) 1 FROM dbo.CCG25
         WHERE I_ACCOUNT=(SELECT TOP (1) I_ACCOUNT FROM dbo.CUG03 WHERE S_ACCOUNT=@Account AND I_DIVISION=@Division)
           AND I_CONTRACT=@Contract
           AND C_SHIP_TO_SUFFIX IN ('000', @ShipTo)
           AND D_EFFECT<=@PricingDate AND (D_EXPIRE>=@PricingDate OR D_EXPIRE IS NULL);
        """);

    private readonly ISqlServerQueryExecutor queries = queries ?? throw new ArgumentNullException(nameof(queries));

    public async ValueTask<BuyingGroupCostSearchData> FindCandidatesAsync(
        PricingContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            IReadOnlyList<FactorRow> factorRows = await queries.QueryAsync<FactorRow>(Factors,
                new { Vendor = context.Request.Vendor.Value, Product = context.Request.Product.Value }, cancellationToken)
                .ConfigureAwait(false);
            Dictionary<string, decimal> factors = factorRows
                .Where(row => !string.IsNullOrWhiteSpace(row.UnitOfMeasure) && row.ConversionFactor > 0m)
                .ToDictionary(row => row.UnitOfMeasure!.Trim(), row => row.ConversionFactor, StringComparer.Ordinal);
            factors[context.Product.BaseUnitOfMeasure.Value] = 1m;

            IReadOnlyList<OverrideRow> overrides = await queries.QueryAsync<OverrideRow>(Overrides, new
            {
                Account = context.Request.Account.Value, Division = context.Request.Division.Value,
                Customer = context.Customer.Customer?.Value ?? 0, Vendor = context.Request.Vendor.Value,
                ProductCategory = context.Product.ProductCategory, PricingDate = context.Request.PricingDate.ToDateTime(TimeOnly.MinValue),
            }, cancellationToken).ConfigureAwait(false);

            BuyingGroupScopeCandidates account = await Scope(context, factors, BuyingGroupScope.Account,
                context.Customer.BuyingGroupMemberships.Where(m => m.Source.Contains("CUG10", StringComparison.Ordinal)),
                overrides.Where(row => row.OverrideSource.StartsWith("ACCOUNT", StringComparison.Ordinal)), cancellationToken);
            BuyingGroupScopeCandidates customer = await Scope(context, factors, BuyingGroupScope.Customer,
                context.Customer.BuyingGroupMemberships.Where(m => !m.Source.Contains("CUG10", StringComparison.Ordinal)),
                overrides.Where(row => row.OverrideSource.StartsWith("CUSTOMER", StringComparison.Ordinal)), cancellationToken);
            return new BuyingGroupCostSearchData(account, customer);
        }
        catch (SqlServerAccessException exception)
        {
            throw new BuyingGroupCostContractRepositoryException(
                $"SQL Server buying-group contract lookup '{exception.Operation}' failed.", "24", "70",
                exception.IsTransient, exception);
        }
    }

    private async ValueTask<BuyingGroupScopeCandidates> Scope(
        PricingContext context, IReadOnlyDictionary<string, decimal> factors, BuyingGroupScope scope,
        IEnumerable<BuyingGroupMembership> memberships, IEnumerable<OverrideRow> overrides,
        CancellationToken cancellationToken)
    {
        BuyingGroupMembership[] membershipArray = memberships.ToArray();
        var productOverrides = ImmutableArray.CreateBuilder<BuyingGroupCostCandidate>();
        var vendorOverrides = ImmutableArray.CreateBuilder<BuyingGroupCostCandidate>();
        foreach (OverrideRow item in overrides.Where(item => item.OverrideType?.Trim() != "B"))
        {
            BuyingGroupMembership? membership = membershipArray.FirstOrDefault(value => value.BuyingGroupId == item.BuyingGroupId);
            if (membership is null) continue;
            BuyingGroupSelectionPath path = item.OverrideSource.EndsWith("PRODUCT", StringComparison.Ordinal)
                ? BuyingGroupSelectionPath.ProductCategoryOverride : BuyingGroupSelectionPath.VendorOverride;
            BuyingGroupCostCandidate? candidate = await Candidate(context, factors, scope, item.BuyingGroupId,
                membership.Priority, BuyingGroupHierarchyLevel.Child, path, cancellationToken);
            if (candidate is not null) (path == BuyingGroupSelectionPath.ProductCategoryOverride ? productOverrides : vendorOverrides).Add(candidate);
        }
        var branches = ImmutableArray.CreateBuilder<BuyingGroupPriorityBranch>();
        foreach (BuyingGroupMembership membership in membershipArray.OrderBy(item => item.Priority))
        {
            BuyingGroupCostCandidate? child = await Candidate(context, factors, scope, membership.BuyingGroupId,
                membership.Priority, BuyingGroupHierarchyLevel.Child, BuyingGroupSelectionPath.Priority, cancellationToken);
            var parents = ImmutableArray.CreateBuilder<BuyingGroupCostCandidate>();
            if (membership.ParentBuyingGroupId is long parentId)
            {
                BuyingGroupCostCandidate? parent = await Candidate(context, factors, scope, parentId,
                    membership.Priority, BuyingGroupHierarchyLevel.Parent, BuyingGroupSelectionPath.Priority, cancellationToken);
                if (parent is not null) parents.Add(parent);
            }
            branches.Add(new BuyingGroupPriorityBranch(membership.Priority, child, parents.ToImmutable()));
        }
        return new BuyingGroupScopeCandidates(scope, productOverrides.ToImmutable(), vendorOverrides.ToImmutable(), branches.ToImmutable());
    }

    private async ValueTask<BuyingGroupCostCandidate?> Candidate(
        PricingContext context, IReadOnlyDictionary<string, decimal> factors, BuyingGroupScope scope,
        long group, int priority, BuyingGroupHierarchyLevel hierarchy, BuyingGroupSelectionPath path,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Row> rows = await queries.QueryAsync<Row>(Contracts, new
        {
            BuyingGroup = group, Vendor = context.Request.Vendor.Value, Product = context.Request.Product.Value,
            PricingDate = context.Request.PricingDate.ToDateTime(TimeOnly.MinValue), ContractType = "G",
        }, cancellationToken).ConfigureAwait(false);
        Row? row = rows.OrderBy(candidate => candidate.UnitCost).FirstOrDefault();
        if (row is null) return null;
        int? excluded = await queries.QuerySingleOrDefaultAsync<int?>(Exclusion, new
        {
            Account = context.Request.Account.Value, Division = context.Request.Division.Value,
            Contract = row.Contract, ShipTo = string.IsNullOrWhiteSpace(context.Request.ShipTo) ? "000" : context.Request.ShipTo,
            PricingDate = context.Request.PricingDate.ToDateTime(TimeOnly.MinValue),
        }, cancellationToken).ConfigureAwait(false);
        string fileUom = row.UnitOfMeasure?.Trim() ?? context.Product.BaseUnitOfMeasure.Value;
        decimal up = Factor(context.Request.UnitOfMeasure.Value, context.Product.BaseUnitOfMeasure.Value, factors);
        decimal down = Factor(fileUom, context.Product.BaseUnitOfMeasure.Value, factors);
        var dates = ImmutableArray.Create(Range(row.HeaderEffective, row.HeaderExpiration),
            Range(row.LineEffective, row.LineExpiration), Range(row.GroupEffective, row.GroupExpiration),
            Range(row.VendorEffective, row.VendorExpiration));
        return new BuyingGroupCostCandidate(new ContractId(row.Contract.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Money(CobolRoundingPolicy.RoundIntermediate(row.UnitCost * up / down)), context.Request.UnitOfMeasure,
            group, priority, scope, path, hierarchy,
            row.MembershipEligible?.Trim() is not "N", excluded is not null, dates,
            new RuleProvenance("buying-group-cost-contract", "CCG01/03/05/10", hierarchy.ToString(), dates[0]));
    }

    private static decimal Factor(string uom, string baseUom, IReadOnlyDictionary<string, decimal> factors) =>
        StringComparer.Ordinal.Equals(uom, baseUom) ? 1m : factors.TryGetValue(uom, out decimal value) ? value
        : throw new BuyingGroupCostContractRepositoryException($"UOM '{uom}' was not found in VNG05.", "96", "60");
    private static PricingDateRange Range(DateTime effective, DateTime? expiration) => new(
        DateOnly.FromDateTime(effective), expiration is null ? null : DateOnly.FromDateTime(expiration.Value));
    private sealed class FactorRow { public string? UnitOfMeasure { get; init; } public decimal ConversionFactor { get; init; } }
    private sealed class OverrideRow { public string OverrideSource { get; init; } = string.Empty; public long BuyingGroupId { get; init; } public string? OverrideType { get; init; } }
    private sealed class Row
    {
        public long Contract { get; init; } public string? UnitOfMeasure { get; init; }
        public decimal UnitCost { get; init; } public string? MembershipEligible { get; init; }
        public DateTime HeaderEffective { get; init; } public DateTime? HeaderExpiration { get; init; }
        public DateTime LineEffective { get; init; } public DateTime? LineExpiration { get; init; }
        public DateTime GroupEffective { get; init; } public DateTime? GroupExpiration { get; init; }
        public DateTime VendorEffective { get; init; } public DateTime? VendorExpiration { get; init; }
    }
}
