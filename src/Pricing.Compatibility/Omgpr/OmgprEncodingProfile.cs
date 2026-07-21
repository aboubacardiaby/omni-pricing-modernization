namespace Pricing.Compatibility.Omgpr;

using Pricing.Compatibility.Cobol;

public sealed record OmgprEncodingProfile(
    CobolCharacterSet CharacterSet,
    CobolByteOrder CompByteOrder,
    string PricingDateFormat,
    int QuantityCompLength = 4)
{
    public static OmgprEncodingProfile MainframeEbcdic { get; } =
        new(CobolCharacterSet.EbcdicCodePage037, CobolByteOrder.BigEndian, "MM/dd/yyyy");

    public static OmgprEncodingProfile MicroFocusAsciiNative { get; } =
        new(CobolCharacterSet.Windows1252, CobolByteOrder.LittleEndian, "MM/dd/yyyy", 3);
}
