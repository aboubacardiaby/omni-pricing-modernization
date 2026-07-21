namespace Pricing.UnitTests.ValueObjects;

using System.Text.Json;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class UnitOfMeasureTests
{
    [Theory]
    [InlineData("EA")]
    [InlineData("BOX")]
    public void AcceptsMappedUomLengths(string value) => Assert.Equal(value, new UnitOfMeasure(value).Value);

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("EACH")]
    public void RejectsInvalidUom(string value) =>
        Assert.ThrowsAny<ArgumentException>(() => new UnitOfMeasure(value));

    [Fact]
    public void EqualityIsValueBasedAndCaseSensitive()
    {
        Assert.Equal(new UnitOfMeasure("EA"), new UnitOfMeasure("EA"));
        Assert.NotEqual(new UnitOfMeasure("EA"), new UnitOfMeasure("ea"));
    }

    [Fact]
    public void RoundTripsThroughJson()
    {
        var expected = new UnitOfMeasure("EA");
        Assert.Equal(expected, JsonSerializer.Deserialize<UnitOfMeasure>(JsonSerializer.Serialize(expected)));
    }
}
