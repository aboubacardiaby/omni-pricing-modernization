namespace Pricing.UnitTests.ValueObjects;

using System.Text.Json;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class PricingDateRangeTests
{
    private static readonly DateOnly EffectiveDate = new(2026, 7, 20);

    [Fact]
    public void IncludesBothBoundaries()
    {
        var range = new PricingDateRange(EffectiveDate, EffectiveDate.AddDays(1));

        Assert.True(range.Contains(EffectiveDate));
        Assert.True(range.Contains(EffectiveDate.AddDays(1)));
        Assert.False(range.Contains(EffectiveDate.AddDays(-1)));
        Assert.False(range.Contains(EffectiveDate.AddDays(2)));
    }

    [Fact]
    public void NullExpirationIsOpenEnded()
    {
        var range = new PricingDateRange(EffectiveDate);

        Assert.True(range.Contains(DateOnly.MaxValue));
    }

    [Fact]
    public void RejectsExpirationBeforeEffectiveDate() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PricingDateRange(EffectiveDate, EffectiveDate.AddDays(-1)));

    [Fact]
    public void RoundTripsThroughJson()
    {
        var expected = new PricingDateRange(EffectiveDate, EffectiveDate.AddDays(1));
        Assert.Equal(expected, JsonSerializer.Deserialize<PricingDateRange>(JsonSerializer.Serialize(expected)));
    }
}
