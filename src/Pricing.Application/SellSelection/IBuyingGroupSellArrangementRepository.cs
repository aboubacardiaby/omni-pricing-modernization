namespace Pricing.Application.SellSelection;

using System.Collections.Immutable;
using Pricing.Domain.Models;

/// <summary>Loads eligible subgroup and parent-group sell arrangements.</summary>
public interface IBuyingGroupSellArrangementRepository
{
    ValueTask<SellArrangementSelection?> FindSubgroupAsync(
        PricingContext context,
        SellCostCascade cascade,
        SellArrangementLevel level,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns parent matches with parent depth; application code owns within-parent COBOL order.
    /// Membership/tier resolution mirrors A6U01 7585/7590/7580.
    /// </summary>
    ValueTask<ImmutableArray<ParentSellArrangementCandidate>> FindParentCandidatesAsync(
        PricingContext context,
        SellCostCascade cascade,
        CancellationToken cancellationToken);
}

public sealed record ParentSellArrangementCandidate(
    int ParentDepth,
    SellArrangementLevel Level,
    SellArrangementSelection Selection);

public sealed class BuyingGroupSellArrangementRepositoryException : Exception
{
    public BuyingGroupSellArrangementRepositoryException(
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
