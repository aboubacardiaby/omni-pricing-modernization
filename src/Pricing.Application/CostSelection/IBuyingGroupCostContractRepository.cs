namespace Pricing.Application.CostSelection;

using System.Collections.Immutable;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>Loads the account/customer buying-group search structure used by A6U01 0240.</summary>
public interface IBuyingGroupCostContractRepository
{
    ValueTask<BuyingGroupCostSearchData> FindCandidatesAsync(
        PricingContext context,
        CancellationToken cancellationToken);
}

public sealed record BuyingGroupCostSearchData(
    BuyingGroupScopeCandidates Account,
    BuyingGroupScopeCandidates Customer);

public sealed record BuyingGroupScopeCandidates(
    BuyingGroupScope Scope,
    ImmutableArray<BuyingGroupCostCandidate> ProductCategoryOverrides,
    ImmutableArray<BuyingGroupCostCandidate> VendorOverrides,
    ImmutableArray<BuyingGroupPriorityBranch> PriorityBranches);

public sealed record BuyingGroupPriorityBranch(
    int Priority,
    BuyingGroupCostCandidate? Child,
    ImmutableArray<BuyingGroupCostCandidate> Parents);

public sealed record BuyingGroupCostCandidate(
    ContractId Contract,
    Money NormalizedUnitCost,
    UnitOfMeasure NormalizedUnitOfMeasure,
    long BuyingGroupId,
    int Priority,
    BuyingGroupScope Scope,
    BuyingGroupSelectionPath SelectionPath,
    BuyingGroupHierarchyLevel HierarchyLevel,
    bool IsMembershipEligible,
    bool IsExcluded,
    ImmutableArray<PricingDateRange> EligibilityDates,
    RuleProvenance Provenance,
    bool HasContractFees = false);

public enum BuyingGroupScope
{
    Account,
    Customer,
}

public enum BuyingGroupSelectionPath
{
    ProductCategoryOverride,
    VendorOverride,
    Priority,
}

public enum BuyingGroupHierarchyLevel
{
    Child,
    Parent,
}

public sealed class BuyingGroupCostContractRepositoryException : Exception
{
    public BuyingGroupCostContractRepositoryException(
        string message,
        string? legacyErrorCode = null,
        string? legacySeverityCode = null,
        bool isTransient = false,
        Exception? innerException = null)
        : base(message, innerException)
    {
        LegacyErrorCode = legacyErrorCode;
        LegacySeverityCode = legacySeverityCode;
        IsTransient = isTransient;
    }

    public string? LegacyErrorCode { get; }
    public string? LegacySeverityCode { get; }
    public bool IsTransient { get; }
}
