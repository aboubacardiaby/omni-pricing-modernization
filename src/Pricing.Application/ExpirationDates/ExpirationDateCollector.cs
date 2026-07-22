namespace Pricing.Application.ExpirationDates;

using System.Collections.Immutable;
using Pricing.Domain.Models;

/// <summary>A possible price expiration and the component/data source that supplied it.</summary>
public sealed record ExpirationDateSource(
    string Component,
    DateOnly? ExpirationDate,
    RuleProvenance Provenance)
{
    public string Component { get; } = string.IsNullOrWhiteSpace(Component)
        ? throw new ArgumentException("An expiration-date component is required.", nameof(Component))
        : Component;
}

/// <summary>The closest valid expiration and the sources considered while resolving it.</summary>
public sealed record ExpirationDateCollectionResult(
    DateOnly? ExpirationDate,
    ImmutableArray<ExpirationDateSource> ValidSources,
    ImmutableArray<ExpirationDateSource> SelectedSources,
    PricingError? Error)
{
    public bool IsSuccess => Error is null;
}

/// <summary>
/// Collects component expirations and returns the first date that can invalidate the calculated price.
/// </summary>
/// <remarks>
/// A6U01 7695-ADD-EXP-DATE-ARRA-010 accumulates up to 50 dates and reports legacy error 145
/// on overflow. A6U01 7720-PRO-EXPIRE-DATES-010 scans those dates and returns the earliest.
/// </remarks>
public sealed class ExpirationDateCollector
{
    public const int MaximumCandidateCount = 50;

    public static ExpirationDateCollectionResult Collect(
        DateOnly pricingDate,
        IEnumerable<ExpirationDateSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        ImmutableArray<ExpirationDateSource>.Builder valid = ImmutableArray.CreateBuilder<ExpirationDateSource>();
        foreach (ExpirationDateSource source in sources)
        {
            ArgumentNullException.ThrowIfNull(source);

            // Nullable DB2 expiration fields never reach COBOL paragraph 7695. Rows expiring
            // before the inclusive pricing date are likewise not valid contributors.
            if (source.ExpirationDate is not { } expiration || expiration < pricingDate)
            {
                continue;
            }

            if (valid.Count == MaximumCandidateCount)
            {
                return new ExpirationDateCollectionResult(
                    null,
                    valid.ToImmutable(),
                    [],
                    new ValidationPricingError(
                        "EXPIRATION_DATE_LIMIT_EXCEEDED",
                        "The number of expiration dates exceeds the supported limit of 50.",
                        "145"));
            }

            valid.Add(source);
        }

        if (valid.Count == 0)
        {
            return new ExpirationDateCollectionResult(null, [], [], null);
        }

        DateOnly closest = valid.Min(static source => source.ExpirationDate!.Value);
        ImmutableArray<ExpirationDateSource> validSources = valid.ToImmutable();
        ImmutableArray<ExpirationDateSource> selected = validSources
            .Where(source => source.ExpirationDate == closest)
            .ToImmutableArray();

        return new ExpirationDateCollectionResult(closest, validSources, selected, null);
    }
}
