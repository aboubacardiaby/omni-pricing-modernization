namespace Pricing.UnitTests.Compatibility;

using Pricing.Compatibility.Cobol;
using Xunit;

public sealed class CobolAlphanumericCodecTests
{
    [Theory]
    [InlineData(CobolCharacterSet.EbcdicCodePage037, "E5F0F0F1")]
    [InlineData(CobolCharacterSet.Windows1252, "56303031")]
    public void VendorVectorRoundTrips(CobolCharacterSet characterSet, string expectedHex)
    {
        byte[] encoded = CobolAlphanumericCodec.Encode("V001", 4, characterSet);

        Assert.Equal(expectedHex, Convert.ToHexString(encoded));
        Assert.Equal("V001", CobolAlphanumericCodec.Decode(encoded, characterSet));
    }

    [Fact]
    public void PadsAndOptionallyTrimsSpaces()
    {
        byte[] encoded = CobolAlphanumericCodec.Encode("DB ERROR", 10, CobolCharacterSet.EbcdicCodePage037);

        Assert.Equal("C4C240C5D9D9D6D94040", Convert.ToHexString(encoded));
        Assert.Equal("DB ERROR", CobolAlphanumericCodec.Decode(encoded, CobolCharacterSet.EbcdicCodePage037, true));
    }

    [Fact]
    public void LowValuesAreBinaryZeros() =>
        Assert.Equal("00000000", Convert.ToHexString(CobolAlphanumericCodec.Encode(string.Empty, 4, CobolCharacterSet.Windows1252, true)));

    [Fact]
    public void RejectsTextOverflow() =>
        Assert.Throws<OverflowException>(() => CobolAlphanumericCodec.Encode("ABCDE", 4, CobolCharacterSet.Windows1252));
}
