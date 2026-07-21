namespace Pricing.Domain.ValueObjects;

using System.Text.Json.Serialization;

/// <summary>Represents a signed percentage value without applying a calculation policy.</summary>
/// <remarks>COBOL: OMGPR.CPY percentage fields use signed PIC clauses up to S9(3)V9(4).</remarks>
public readonly record struct Percentage
{
    public const int MaximumScale = 4;
    public const decimal MaximumMagnitude = 999.9999m;

    [JsonConstructor]
    public Percentage(decimal value)
    {
        Value = DecimalValue.RequireMagnitude(
            DecimalValue.RequireScale(value, MaximumScale, nameof(value)),
            MaximumMagnitude,
            nameof(value));
    }

    public decimal Value { get; }

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
