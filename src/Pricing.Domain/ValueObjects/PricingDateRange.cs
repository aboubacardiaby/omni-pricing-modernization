namespace Pricing.Domain.ValueObjects;

using System.Text.Json.Serialization;

/// <summary>Represents an inclusive pricing effective-date range.</summary>
/// <remarks>COBOL: OMGPR.CPY date fields are PIC X(10); boundary inclusion is explicit in A6U01 SQL.</remarks>
public readonly record struct PricingDateRange
{
    [JsonConstructor]
    public PricingDateRange(DateOnly effectiveDate, DateOnly? expirationDate = null)
    {
        if (expirationDate is { } expiration && expiration < effectiveDate)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expirationDate),
                expirationDate,
                "Expiration date cannot precede the effective date.");
        }

        EffectiveDate = effectiveDate;
        ExpirationDate = expirationDate;
    }

    public DateOnly EffectiveDate { get; }
    public DateOnly? ExpirationDate { get; }

    public bool Contains(DateOnly date) => date >= EffectiveDate && (ExpirationDate is null || date <= ExpirationDate);
}
