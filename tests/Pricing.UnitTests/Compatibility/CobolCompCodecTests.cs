namespace Pricing.UnitTests.Compatibility;

using Pricing.Compatibility.Cobol;
using Xunit;

public sealed class CobolCompCodecTests
{
    [Theory]
    [InlineData(CobolByteOrder.BigEndian, "00BC614E")]
    [InlineData(CobolByteOrder.LittleEndian, "4E61BC00")]
    public void ContractVectorRoundTrips(CobolByteOrder byteOrder, string expectedHex)
    {
        byte[] encoded = CobolCompCodec.EncodeSigned(12345678, 4, byteOrder);

        Assert.Equal(expectedHex, Convert.ToHexString(encoded));
        Assert.Equal(12345678, CobolCompCodec.DecodeSigned(encoded, byteOrder));
    }

    [Theory]
    [InlineData(CobolByteOrder.BigEndian, "FF439EB2")]
    [InlineData(CobolByteOrder.LittleEndian, "B29E43FF")]
    public void NegativeContractVectorRoundTrips(CobolByteOrder byteOrder, string expectedHex)
    {
        byte[] encoded = CobolCompCodec.EncodeSigned(-12345678, 4, byteOrder);

        Assert.Equal(expectedHex, Convert.ToHexString(encoded));
        Assert.Equal(-12345678, CobolCompCodec.DecodeSigned(encoded, byteOrder));
    }

    [Fact]
    public void RejectsOverflow() =>
        Assert.Throws<OverflowException>(() => CobolCompCodec.EncodeSigned(short.MaxValue + 1L, 2, CobolByteOrder.BigEndian));

    [Fact]
    public void RejectsInvalidLength() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CobolCompCodec.EncodeSigned(1, 1, CobolByteOrder.BigEndian));

    [Theory]
    [InlineData(CobolByteOrder.BigEndian, 8_388_607, "7FFFFF")]
    [InlineData(CobolByteOrder.BigEndian, -8_388_608, "800000")]
    [InlineData(CobolByteOrder.LittleEndian, 8_388_607, "FFFF7F")]
    [InlineData(CobolByteOrder.LittleEndian, -8_388_608, "000080")]
    public void ThreeByteCompRoundTrips(CobolByteOrder byteOrder, int value, string expectedHex)
    {
        byte[] encoded = CobolCompCodec.EncodeSigned(value, 3, byteOrder);

        Assert.Equal(expectedHex, Convert.ToHexString(encoded));
        Assert.Equal(value, CobolCompCodec.DecodeSigned(encoded, byteOrder));
    }
}
