namespace Pricing.Domain.ValueObjects;

using System.Text.Json.Serialization;

/// <summary>Represents a signed monetary amount without applying a rounding policy.</summary>
/// <remarks>COBOL: OMGPR.CPY monetary fields use signed PIC clauses up to S9(7)V9(8).</remarks>
public readonly record struct Money
{
    public const int MaximumScale = 8;
    public const decimal MaximumMagnitude = 9_999_999.99999999m;

    [JsonConstructor]
    public Money(decimal value)
    {
        Value = DecimalValue.RequireMagnitude(
            DecimalValue.RequireScale(value, MaximumScale, nameof(value)),
            MaximumMagnitude,
            nameof(value));
    }

    public decimal Value { get; }

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
