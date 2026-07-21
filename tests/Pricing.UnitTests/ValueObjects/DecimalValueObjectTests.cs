namespace Pricing.UnitTests.ValueObjects;

using System.Text.Json;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class DecimalValueObjectTests
{
    [Theory]
    [InlineData(-9999999.99999999)]
    [InlineData(0)]
    [InlineData(9999999.99999999)]
    public void MoneyAcceptsMappedBoundaries(decimal value) => Assert.Equal(value, new Money(value).Value);

    [Fact]
    public void MoneyRejectsExcessScale() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(0.000000001m));

    [Fact]
    public void MoneyRejectsExcessMagnitude() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(10_000_000m));

    [Theory]
    [InlineData(-999.9999)]
    [InlineData(0)]
    [InlineData(999.9999)]
    public void PercentageAcceptsMappedBoundaries(decimal value) =>
        Assert.Equal(value, new Percentage(value).Value);

    [Fact]
    public void PercentageRejectsExcessScale() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Percentage(0.00001m));

    [Fact]
    public void PercentageRejectsExcessMagnitude() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Percentage(1_000m));

    [Theory]
    [InlineData(-9999999)]
    [InlineData(0)]
    [InlineData(9999999)]
    public void QuantityAcceptsMappedBoundaries(decimal value) => Assert.Equal(value, new Quantity(value).Value);

    [Fact]
    public void QuantityRejectsFractionalValues() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Quantity(1.1m));

    [Fact]
    public void DecimalValueObjectsRoundTripThroughJson()
    {
        Assert.Equal(new Money(-12.34567891m), RoundTrip(new Money(-12.34567891m)));
        Assert.Equal(new Percentage(-12.3456m), RoundTrip(new Percentage(-12.3456m)));
        Assert.Equal(new Quantity(-12m), RoundTrip(new Quantity(-12m)));
    }

    private static T RoundTrip<T>(T value) where T : struct =>
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value));
}
