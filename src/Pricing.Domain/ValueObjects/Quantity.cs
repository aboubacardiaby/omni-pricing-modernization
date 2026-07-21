namespace Pricing.Domain.ValueObjects;

using System.Text.Json.Serialization;

/// <summary>Represents a signed product quantity.</summary>
/// <remarks>COBOL: OMGPR.CPY, OMGPR-Q-ORD-LIN-ORDERED, PIC S9(7) COMP.</remarks>
public readonly record struct Quantity
{
    public const int MaximumScale = 0;
    public const decimal MaximumMagnitude = 9_999_999m;

    [JsonConstructor]
    public Quantity(decimal value)
    {
        Value = DecimalValue.RequireMagnitude(
            DecimalValue.RequireScale(value, MaximumScale, nameof(value)),
            MaximumMagnitude,
            nameof(value));
    }

    public decimal Value { get; }

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
