namespace Pricing.UnitTests.Compatibility;

using System.Collections.Immutable;
using Pricing.Compatibility.Cobol;
using Pricing.Compatibility.Omgpr;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class OmgprEncoderTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EncodesResultAndPreservesRequestAndOpaqueTail(bool mainframe)
    {
        OmgprEncodingProfile profile = mainframe
            ? OmgprEncodingProfile.MainframeEbcdic
            : OmgprEncodingProfile.MicroFocusAsciiNative;
        byte[] originalBytes = CreateRecord(profile);
        DecodedOmgprRecord original = new OmgprDecoder(profile).Decode(originalBytes);
        PricingResult result = CreateResult(
            new Money(123.45678901m),
            new Money(456.78901234m),
            new DateOnly(2027, 1, 31),
            ImmutableArray<PricingError>.Empty);

        byte[] encoded = new OmgprEncoder(profile).Encode(original, result);
        DecodedOmgprRecord roundTripped = new OmgprDecoder(profile).Decode(encoded);

        Assert.Equal(1789, encoded.Length);
        Assert.Equal(original.Request, roundTripped.Request);
        Assert.Equal(original.OpaqueTrailingBytes, roundTripped.OpaqueTrailingBytes);
        Assert.Equal("0012345678901C", Convert.ToHexString(encoded.AsSpan(916, 7)));
        Assert.Equal("0045678901234C", Convert.ToHexString(encoded.AsSpan(909, 7)));
        Assert.Equal("01/31/2027", CobolAlphanumericCodec.Decode(encoded.AsSpan(1254, 10), profile.CharacterSet));
    }

    [Fact]
    public void MapsConfirmedLegacyErrorFields()
    {
        OmgprEncodingProfile profile = OmgprEncodingProfile.MainframeEbcdic;
        DecodedOmgprRecord original = new OmgprDecoder(profile).Decode(CreateRecord(profile));
        PricingResult result = CreateResult(
            null,
            null,
            null,
            [new DependencyPricingError("DB2", "DB ERROR", "602", false, "70")]);

        byte[] encoded = new OmgprEncoder(profile).Encode(original, result);

        Assert.Equal("Y", CobolAlphanumericCodec.Decode(encoded.AsSpan(308, 1), profile.CharacterSet));
        Assert.Equal("DB ERROR", CobolAlphanumericCodec.Decode(encoded.AsSpan(309, 76), profile.CharacterSet, true));
        Assert.Equal(602, CobolCompCodec.DecodeSigned(encoded.AsSpan(1305, 2), profile.CompByteOrder));
        Assert.Equal("070", CobolAlphanumericCodec.Decode(encoded.AsSpan(1307, 3), profile.CharacterSet));
    }

    [Fact]
    public void ClearsLegacyErrorFieldsWhenResultHasNoErrors()
    {
        OmgprEncodingProfile profile = OmgprEncodingProfile.MainframeEbcdic;
        byte[] bytes = CreateRecord(profile);
        WriteText(bytes, 308, 1, "Y", profile);
        WriteText(bytes, 309, 76, "OLD ERROR", profile);
        CobolCompCodec.EncodeSigned(602, 2, profile.CompByteOrder).CopyTo(bytes, 1305);
        WriteText(bytes, 1307, 3, "070", profile);
        DecodedOmgprRecord original = new OmgprDecoder(profile).Decode(bytes);

        byte[] encoded = new OmgprEncoder(profile).Encode(original, CreateResult(null, null, null, []));

        Assert.Equal(" ", CobolAlphanumericCodec.Decode(encoded.AsSpan(308, 1), profile.CharacterSet));
        Assert.Equal(string.Empty, CobolAlphanumericCodec.Decode(encoded.AsSpan(309, 76), profile.CharacterSet, true));
        Assert.Equal(0, CobolCompCodec.DecodeSigned(encoded.AsSpan(1305, 2), profile.CompByteOrder));
        Assert.Equal("000", CobolAlphanumericCodec.Decode(encoded.AsSpan(1307, 3), profile.CharacterSet));
    }

    [Fact]
    public void RejectsInvalidLegacyErrorCode()
    {
        OmgprEncodingProfile profile = OmgprEncodingProfile.MainframeEbcdic;
        DecodedOmgprRecord original = new OmgprDecoder(profile).Decode(CreateRecord(profile));
        PricingResult result = CreateResult(
            null,
            null,
            null,
            [new ValidationPricingError("BAD", "Bad", "ABC", LegacySeverityCode: "70")]);

        Assert.Throws<FormatException>(() => new OmgprEncoder(profile).Encode(original, result));
    }

    [Fact]
    public void RejectsErrorWithoutIndependentLegacySeverity()
    {
        OmgprEncodingProfile profile = OmgprEncodingProfile.MainframeEbcdic;
        DecodedOmgprRecord original = new OmgprDecoder(profile).Decode(CreateRecord(profile));
        PricingResult result = CreateResult(null, null, null, [new ValidationPricingError("BAD", "Bad", "602")]);

        Assert.Throws<InvalidOperationException>(() => new OmgprEncoder(profile).Encode(original, result));
    }

    private static PricingResult CreateResult(
        Money? cost,
        Money? sell,
        DateOnly? expiration,
        ImmutableArray<PricingError> errors) =>
        new(
            ProductType.Regular,
            cost,
            sell,
            expiration,
            null,
            null,
            [],
            [],
            [],
            errors);

    private static byte[] CreateRecord(OmgprEncodingProfile profile)
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
        WriteText(source, 140, 1, "P", profile);
        CobolPackedDecimalCodec.Encode(0m, 13, 8, true).CopyTo(source, 775);
        CobolPackedDecimalCodec.Encode(0m, 5, 4, false).CopyTo(source, 968);
        Enumerable.Range(1, 16).Select(value => (byte)value).ToArray().CopyTo(source, 1773);
        return source;
    }

    private static void WriteText(byte[] destination, int offset, int length, string value, OmgprEncodingProfile profile) =>
        CobolAlphanumericCodec.Encode(value, length, profile.CharacterSet).CopyTo(destination, offset);
}
