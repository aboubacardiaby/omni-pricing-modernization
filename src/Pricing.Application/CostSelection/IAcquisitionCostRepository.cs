namespace Pricing.Application.CostSelection;

using System.Collections.Immutable;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>Loads normalized VNG03 prices for the ordered UOM.</summary>
/// <remarks>
/// Implementations perform A6U01 7110/7065 conversion and COBOL-equivalent rounding before
/// returning monetary values. The rounding policy itself is introduced by T046.
/// </remarks>
public interface IAcquisitionCostRepository
{
    ValueTask<AcquisitionCostData> FindAsync(PricingContext context, CancellationToken cancellationToken);
}

public sealed record AcquisitionCostData(
    AcquisitionPriceListCandidate? DirectMatch,
    ImmutableArray<AcquisitionPriceListCandidate> FallbackCandidates);

public sealed record AcquisitionPriceListCandidate(
    Money DealerCost,
    Money AcquisitionCost,
    UnitOfMeasure UnitOfMeasure,
    string PriceLevel,
    DateOnly PriceListEffectiveDate,
    RuleProvenance Provenance);

public sealed class AcquisitionCostRepositoryException : Exception
{
    public AcquisitionCostRepositoryException(
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
