namespace Pricing.Application.CostAdjustments;

using System.Collections.Immutable;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>Loads the applicable vendor-cost-adjustment waterfall in COBOL evaluation order.</summary>
public interface IVendorCostAdjustmentRepository
{
    ValueTask<ImmutableArray<VendorCostAdjustmentCandidate>> FindAsync(
        PricingContext context,
        bool hasCostContract,
        CancellationToken cancellationToken);
}

public sealed record VendorCostAdjustmentCandidate(
    int EvaluationOrder,
    decimal Percentage,
    string SourceCode,
    PricingDateRange EffectiveDates,
    RuleProvenance Provenance,
    bool IsExempt = false);

public sealed class VendorCostAdjustmentRepositoryException : Exception
{
    public VendorCostAdjustmentRepositoryException(
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
