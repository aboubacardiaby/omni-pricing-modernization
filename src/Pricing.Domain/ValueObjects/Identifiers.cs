namespace Pricing.Domain.ValueObjects;

using System.Text.Json.Serialization;

/// <summary>OMNI division identifier.</summary>
/// <remarks>COBOL: OMGPR.CPY, OMGPR-I-DIVISION, PIC X(2).</remarks>
public readonly record struct DivisionId
{
    public const int MaximumLength = 2;
    [JsonConstructor]
    public DivisionId(string value) => Value = StringValue.Require(value, MaximumLength, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

/// <summary>OMNI display account number.</summary>
/// <remarks>COBOL: OMGPR.CPY, OMGPR-S-ACCOUNT, PIC X(6).</remarks>
public readonly record struct AccountNumber
{
    public const int MaximumLength = 6;
    [JsonConstructor]
    public AccountNumber(string value) => Value = StringValue.Require(value, MaximumLength, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

/// <summary>OMNI vendor identifier.</summary>
/// <remarks>COBOL: OMGPR.CPY, OMGPR-I-VENDOR, PIC X(4).</remarks>
public readonly record struct VendorId
{
    public const int MaximumLength = 4;
    [JsonConstructor]
    public VendorId(string value) => Value = StringValue.Require(value, MaximumLength, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

/// <summary>Vendor product identifier.</summary>
/// <remarks>COBOL: OMGPR.CPY, OMGPR-I-VND-PRODUCT, PIC X(8).</remarks>
public readonly record struct ProductId
{
    public const int MaximumLength = 8;
    [JsonConstructor]
    public ProductId(string value) => Value = StringValue.Require(value, MaximumLength, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

/// <summary>Concatenated vendor/product identifier used by the legacy kit explosion interface.</summary>
/// <remarks>COBOL: OMGEXPL-PACK-PRODNO and component product numbers are PIC X(12).</remarks>
public readonly record struct KitProductNumber
{
    public const int MaximumLength = 12;
    [JsonConstructor]
    public KitProductNumber(string value) => Value = StringValue.Require(value, MaximumLength, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

/// <summary>Contract identifier.</summary>
/// <remarks>COBOL: OMGPR.CPY, OMGPR-I-CONTRACT, PIC X(20).</remarks>
public readonly record struct ContractId
{
    public const int MaximumLength = 20;
    [JsonConstructor]
    public ContractId(string value) => Value = StringValue.Require(value, MaximumLength, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

/// <summary>Numeric customer identifier.</summary>
/// <remarks>COBOL: OMGPR.CPY, OMGPR-CUSTOMER-NBR, PIC S9(10) COMP-3.</remarks>
public readonly record struct CustomerNumber
{
    public const long MaximumMagnitude = 9_999_999_999;

    [JsonConstructor]
    public CustomerNumber(long value)
    {
        Value = value is >= -MaximumMagnitude and <= MaximumMagnitude
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Customer number exceeds PIC S9(10).");
    }

    public long Value { get; }
    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
