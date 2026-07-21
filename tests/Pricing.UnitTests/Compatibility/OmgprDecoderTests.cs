namespace Pricing.UnitTests.Compatibility;

using Pricing.Compatibility.Cobol;
using Pricing.Compatibility.Omgpr;
using Pricing.Domain.Models;
using Xunit;

public sealed class OmgprDecoderTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DecodesPricingRequestUsingConfiguredProfile(bool mainframe)
    {
        OmgprEncodingProfile profile = mainframe
            ? OmgprEncodingProfile.MainframeEbcdic
            : OmgprEncodingProfile.MicroFocusAsciiNative;
        byte[] source = CreateRecord(profile, "P");

        DecodedOmgprRecord decoded = new OmgprDecoder(profile).Decode(source);

        Assert.Equal("01", decoded.Request.Division.Value);
        Assert.Equal("123456", decoded.Request.Account.Value);
        Assert.Equal("V001", decoded.Request.Vendor.Value);
        Assert.Equal("P0000001", decoded.Request.Product.Value);
        Assert.Equal(25m, decoded.Request.Quantity.Value);
        Assert.Equal("EA", decoded.Request.UnitOfMeasure.Value);
        Assert.Equal("001", decoded.Request.ShipTo);
        Assert.Equal("002", decoded.Request.BillTo);
        Assert.Equal(new DateOnly(2026, 7, 18), decoded.Request.PricingDate);
        Assert.Equal(PricingRequestType.Full, decoded.Request.RequestType);
        Assert.Equal(Enumerable.Range(1, 16).Select(value => (byte)value), decoded.OpaqueTrailingBytes);
    }

    [Theory]
    [InlineData("C", PricingRequestType.CostOnly)]
    [InlineData("S", PricingRequestType.SellOnly)]
    [InlineData("J", PricingRequestType.Jit)]
    [InlineData(" ", PricingRequestType.Full)]
    public void MapsRequestSwitch(string requestSwitch, PricingRequestType expected)
    {
        byte[] source = CreateRecord(OmgprEncodingProfile.MainframeEbcdic, requestSwitch);

        Assert.Equal(expected, new OmgprDecoder(OmgprEncodingProfile.MainframeEbcdic).Decode(source).Request.RequestType);
    }

    [Theory]
    [InlineData(1788)]
    [InlineData(1790)]
    public void RejectsWrongRecordLength(int length) =>
        Assert.Throws<ArgumentException>(() =>
            new OmgprDecoder(OmgprEncodingProfile.MainframeEbcdic).Decode(new byte[length]));

    [Fact]
    public void RejectsUnknownRequestSwitch()
    {
        byte[] source = CreateRecord(OmgprEncodingProfile.MainframeEbcdic, "X");

        Assert.Throws<FormatException>(() => new OmgprDecoder(OmgprEncodingProfile.MainframeEbcdic).Decode(source));
    }

    [Theory]
    [InlineData("Y", true)]
    [InlineData("N", false)]
    [InlineData(" ", false)]
    public void MapsSpecialContractFlag(string flag, bool expected)
    {
        byte[] source = CreateRecord(OmgprEncodingProfile.MainframeEbcdic, "P");
        WriteText(source, 151, 1, flag, OmgprEncodingProfile.MainframeEbcdic);

        Assert.Equal(expected, new OmgprDecoder(OmgprEncodingProfile.MainframeEbcdic)
            .Decode(source).Request.IsSpecialContract);
    }

    [Fact]
    public void RejectsUnknownSpecialContractFlag()
    {
        byte[] source = CreateRecord(OmgprEncodingProfile.MainframeEbcdic, "P");
        WriteText(source, 151, 1, "X", OmgprEncodingProfile.MainframeEbcdic);

        Assert.Throws<FormatException>(() =>
            new OmgprDecoder(OmgprEncodingProfile.MainframeEbcdic).Decode(source));
    }

    [Fact]
    public void RejectsInvalidPricingDate()
    {
        byte[] source = CreateRecord(OmgprEncodingProfile.MainframeEbcdic, "P");
        WriteText(source, 32, 10, "not-a-date", OmgprEncodingProfile.MainframeEbcdic);

        Assert.Throws<FormatException>(() => new OmgprDecoder(OmgprEncodingProfile.MainframeEbcdic).Decode(source));
    }

    [Fact]
    public void DecodesAllT016RepresentativeNumericFieldsFromCentralizedOffsets()
    {
        OmgprEncodingProfile profile = OmgprEncodingProfile.MainframeEbcdic;
        byte[] source = CreateRecord(profile, "P");
        CobolCompCodec.EncodeSigned(12_345_678, 4, profile.CompByteOrder).CopyTo(source, 523);
        CobolPackedDecimalCodec.Encode(123.45678901m, 13, 8, true).CopyTo(source, 775);
        CobolPackedDecimalCodec.Encode(0.1500m, 5, 4, false).CopyTo(source, 968);
        CobolCompCodec.EncodeSigned(602, 2, profile.CompByteOrder).CopyTo(source, 1305);

        OmgprRepresentativeFields fields = new OmgprDecoder(profile).Decode(source).RepresentativeFields;

        Assert.Equal(12_345_678, fields.ContractNumber);
        Assert.Equal(123.45678901m, fields.ContractLineUnitCost);
        Assert.Equal(0.1500m, fields.PricingPercentage);
        Assert.Equal(602, fields.ErrorNumber);
    }

    private static byte[] CreateRecord(OmgprEncodingProfile profile, string requestSwitch)
    {
        byte[] source = CobolAlphanumericCodec.Encode(string.Empty, 1789, profile.CharacterSet);
        WriteText(source, 0, 2, "01", profile);
        WriteText(source, 2, 6, "123456", profile);
        WriteText(source, 8, 4, "V001", profile);
        WriteText(source, 12, 8, "P0000001", profile);
        CobolCompCodec.EncodeSigned(25, profile.QuantityCompLength, profile.CompByteOrder).CopyTo(source, 20);
        WriteText(source, 24, 2, "EA", profile);
        WriteText(source, 26, 3, "001", profile);
        WriteText(source, 29, 3, "002", profile);
        WriteText(source, 32, 10, "07/18/2026", profile);
        WriteText(source, 140, 1, requestSwitch, profile);
        CobolPackedDecimalCodec.Encode(0m, 13, 8, true).CopyTo(source, 775);
        CobolPackedDecimalCodec.Encode(0m, 5, 4, false).CopyTo(source, 968);
        Enumerable.Range(1, 16).Select(value => (byte)value).ToArray().CopyTo(source, 1773);
        return source;
    }

    private static void WriteText(byte[] destination, int offset, int length, string value, OmgprEncodingProfile profile) =>
        CobolAlphanumericCodec.Encode(value, length, profile.CharacterSet).CopyTo(destination, offset);
}
