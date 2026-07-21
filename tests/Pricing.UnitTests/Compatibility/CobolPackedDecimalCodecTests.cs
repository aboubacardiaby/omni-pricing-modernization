namespace Pricing.UnitTests.Compatibility;

using Pricing.Compatibility.Cobol;
using Xunit;

public sealed class CobolPackedDecimalCodecTests
{
    [Theory]
    [InlineData("123.45678901", "0012345678901C")]
    [InlineData("-123.45678901", "0012345678901D")]
    [InlineData("0.00000000", "0000000000000C")]
    [InlineData("99999.99999999", "9999999999999C")]
    public void SignedUnitCostVectorsRoundTrip(string value, string expectedHex)
    {
        decimal logical = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        byte[] encoded = CobolPackedDecimalCodec.Encode(logical, 13, 8, true);

        Assert.Equal(expectedHex, Convert.ToHexString(encoded));
        Assert.Equal(logical, CobolPackedDecimalCodec.Decode(encoded, 13, 8, true));
    }

    [Theory]
    [InlineData("0.1500", "01500F")]
    [InlineData("9.9999", "99999F")]
    public void UnsignedPercentageVectorsRoundTrip(string value, string expectedHex)
    {
        decimal logical = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        byte[] encoded = CobolPackedDecimalCodec.Encode(logical, 5, 4, false);

        Assert.Equal(expectedHex, Convert.ToHexString(encoded));
        Assert.Equal(logical, CobolPackedDecimalCodec.Decode(encoded, 5, 4, false));
    }

    [Fact]
    public void RejectsScaleLoss() =>
        Assert.Throws<OverflowException>(() => CobolPackedDecimalCodec.Encode(1.234m, 5, 2, true));

    [Fact]
    public void RejectsPrecisionOverflow() =>
        Assert.Throws<OverflowException>(() => CobolPackedDecimalCodec.Encode(1000m, 3, 0, true));

    [Fact]
    public void RejectsInvalidDigitNibble() =>
        Assert.Throws<FormatException>(() => CobolPackedDecimalCodec.Decode(new byte[] { 0x1A, 0x2C }, 3, 0, true));

    [Fact]
    public void RejectsInvalidSignNibble() =>
        Assert.Throws<FormatException>(() => CobolPackedDecimalCodec.Decode(new byte[] { 0x12, 0x3B }, 3, 0, true));
}
