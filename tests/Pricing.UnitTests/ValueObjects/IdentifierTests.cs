namespace Pricing.UnitTests.ValueObjects;

using System.Text.Json;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class IdentifierTests
{
    [Fact]
    public void IdentifiersAcceptMappedMaximumLengths()
    {
        Assert.Equal("01", new DivisionId("01").Value);
        Assert.Equal("123456", new AccountNumber("123456").Value);
        Assert.Equal("1234", new VendorId("1234").Value);
        Assert.Equal("12345678", new ProductId("12345678").Value);
        Assert.Equal(new string('C', 20), new ContractId(new string('C', 20)).Value);
        Assert.Equal(9_999_999_999L, new CustomerNumber(9_999_999_999L).Value);
        Assert.Equal(-9_999_999_999L, new CustomerNumber(-9_999_999_999L).Value);
    }

    [Fact]
    public void IdentifiersRejectBlankOrOversizedValues()
    {
        Assert.Throws<ArgumentException>(() => new DivisionId(" "));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DivisionId("001"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AccountNumber("1234567"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VendorId("12345"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProductId("123456789"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContractId(new string('C', 21)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CustomerNumber(10_000_000_000L));
    }

    [Fact]
    public void IdentifierEqualityIsValueBased()
    {
        Assert.Equal(new ProductId("ABC123"), new ProductId("ABC123"));
        Assert.NotEqual(new ProductId("ABC123"), new ProductId("abc123"));
    }

    [Fact]
    public void IdentifiersRoundTripThroughJson()
    {
        Assert.Equal(new DivisionId("01"), RoundTrip(new DivisionId("01")));
        Assert.Equal(new AccountNumber("123456"), RoundTrip(new AccountNumber("123456")));
        Assert.Equal(new VendorId("1234"), RoundTrip(new VendorId("1234")));
        Assert.Equal(new ProductId("ABC123"), RoundTrip(new ProductId("ABC123")));
        Assert.Equal(new ContractId("CONTRACT-1"), RoundTrip(new ContractId("CONTRACT-1")));
        Assert.Equal(new CustomerNumber(123), RoundTrip(new CustomerNumber(123)));
    }

    private static T RoundTrip<T>(T value) where T : struct =>
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value));
}
