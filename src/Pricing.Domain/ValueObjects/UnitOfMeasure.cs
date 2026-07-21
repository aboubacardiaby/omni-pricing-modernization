namespace Pricing.Domain.ValueObjects;

using System.Text.Json.Serialization;

/// <summary>Represents an OMNI unit-of-measure code.</summary>
/// <remarks>
/// COBOL: OMGPR-C-ORD-LIN-CUST-UOM and base UOM are PIC X(2); alternate/label UOM fields are PIC X(3).
/// </remarks>
public readonly record struct UnitOfMeasure
{
    public const int MaximumLength = 3;

    [JsonConstructor]
    public UnitOfMeasure(string value) => Value = StringValue.Require(value, MaximumLength, nameof(value));

    public string Value { get; }

    public override string ToString() => Value;
}
