namespace Pricing.Application.SellSelection;

using Pricing.Domain.Models;

/// <summary>Loads a corporate match and the same-level group override attempted after it.</summary>
public interface ICorporateSellArrangementRepository
{
    ValueTask<CorporateSellArrangementMatch> FindAsync(
        PricingContext context,
        SellArrangementLevel level,
        CancellationToken cancellationToken);
}

public sealed record CorporateSellArrangementMatch(
    SellArrangementSelection? CorporateSelection,
    SellArrangementSelection? GroupOverrideSelection);

public sealed class CorporateSellArrangementRepositoryException : Exception
{
    public CorporateSellArrangementRepositoryException(
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
