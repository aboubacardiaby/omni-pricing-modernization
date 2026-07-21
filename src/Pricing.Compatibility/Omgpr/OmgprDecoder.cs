namespace Pricing.Compatibility.Omgpr;

using System.Globalization;
using Pricing.Compatibility.Cobol;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

public sealed class OmgprDecoder
{
    private readonly OmgprEncodingProfile profile;

    public OmgprDecoder(OmgprEncodingProfile profile) =>
        this.profile = profile ?? throw new ArgumentNullException(nameof(profile));

    public DecodedOmgprRecord Decode(ReadOnlySpan<byte> source)
    {
        if (source.Length != OmgprLayout.RecordLength)
        {
            throw new ArgumentException(
                $"OMGPR record must be exactly {OmgprLayout.RecordLength} bytes; received {source.Length}.",
                nameof(source));
        }

        PricingRequest request = new(
            new DivisionId(ReadRequiredText(source, OmgprLayout.Input.Division, "division")),
            new AccountNumber(ReadRequiredText(source, OmgprLayout.Input.Account, "account")),
            new VendorId(ReadRequiredText(source, OmgprLayout.Input.Vendor, "vendor")),
            new ProductId(ReadRequiredText(source, OmgprLayout.Input.Product, "product")),
            new Quantity(CobolCompCodec.DecodeSigned(
                OmgprLayout.Input.Quantity(profile.QuantityCompLength).Read(source),
                profile.CompByteOrder)),
            new UnitOfMeasure(ReadRequiredText(source, OmgprLayout.Input.UnitOfMeasure, "unit of measure")),
            ReadOptionalText(source, OmgprLayout.Input.ShipToSuffix),
            ReadOptionalText(source, OmgprLayout.Input.BillToSuffix),
            ReadPricingDate(source),
            ReadRequestType(source),
            ReadSpecialContractFlag(source));

        OmgprRepresentativeFields representativeFields = new(
            CobolCompCodec.DecodeSigned(OmgprLayout.Output.ContractNumber.Read(source), profile.CompByteOrder),
            CobolPackedDecimalCodec.Decode(OmgprLayout.Output.ContractLineUnitCost.Read(source), 13, 8, true),
            CobolPackedDecimalCodec.Decode(OmgprLayout.Output.PricingPercentage.Read(source), 5, 4, false),
            CobolCompCodec.DecodeSigned(OmgprLayout.Output.ErrorNumber.Read(source), profile.CompByteOrder));

        return new DecodedOmgprRecord(request, representativeFields, source);
    }

    private string ReadRequiredText(ReadOnlySpan<byte> source, OmgprLayout.Field field, string name)
    {
        string value = ReadOptionalText(source, field) ?? string.Empty;
        return string.IsNullOrWhiteSpace(value)
            ? throw new FormatException($"OMGPR {name} is required.")
            : value;
    }

    private string? ReadOptionalText(ReadOnlySpan<byte> source, OmgprLayout.Field field)
    {
        ReadOnlySpan<byte> bytes = field.Read(source);
        if (IsLowValues(bytes)) return null;

        string value = CobolAlphanumericCodec.Decode(bytes, profile.CharacterSet, true);
        return value.Length == 0 ? null : value;
    }

    private DateOnly ReadPricingDate(ReadOnlySpan<byte> source)
    {
        string value = ReadRequiredText(source, OmgprLayout.Input.PricingDate, "pricing date");
        return DateOnly.TryParseExact(
            value,
            profile.PricingDateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateOnly date)
            ? date
            : throw new FormatException(
                $"OMGPR pricing date '{value}' does not match configured format '{profile.PricingDateFormat}'.");
    }

    private PricingRequestType ReadRequestType(ReadOnlySpan<byte> source)
    {
        string value = ReadOptionalText(source, OmgprLayout.Input.PricingRequestSwitch) ?? " ";
        return value switch
        {
            " " or "P" => PricingRequestType.Full,
            "C" => PricingRequestType.CostOnly,
            "S" => PricingRequestType.SellOnly,
            "J" => PricingRequestType.Jit,
            _ => throw new FormatException($"Unknown OMGPR pricing request switch '{value}'."),
        };
    }

    private bool ReadSpecialContractFlag(ReadOnlySpan<byte> source)
    {
        string value = ReadOptionalText(source, OmgprLayout.Input.SpecialContractFlag) ?? " ";
        return value switch
        {
            " " or "N" => false,
            "Y" => true,
            _ => throw new FormatException($"Unknown OMGPR special-contract flag '{value}'."),
        };
    }

    private static bool IsLowValues(ReadOnlySpan<byte> source)
    {
        foreach (byte value in source)
        {
            if (value != 0) return false;
        }

        return true;
    }
}
