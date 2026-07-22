namespace Pricing.UnitTests.SellSelection;

using System.Collections.Immutable;
using Pricing.Application.SellSelection;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class PriceLockServiceTests
{
    [Fact]
    public async Task MissingRowLeavesFreshCalculationUnlocked()
    {
        var repository = new StubRepository(null);
        SellPriceCalculationResult current = Calculation(12m);
        PriceLockResult result = await new PriceLockService(repository).ApplyAsync(Input(current, "2", 0.2m), default);

        Assert.Same(current, result.Calculation);
        Assert.False(result.LockRecordFound);
        Assert.False(result.IsPriceLocked);
        Assert.False(result.BypassSurcharge);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("4")]
    [InlineData("6")]
    [InlineData("7")]
    public async Task UnchangedPercentageMethodUsesFrozenPrice(string method)
    {
        PriceLockResult result = await Apply(Record(method, 0.2m), Input(Calculation(12m), method, 0.2m));

        Assert.True(result.IsPriceLocked);
        Assert.Equal(18m, result.Calculation.SellPrice.Value);
        Assert.True(result.BypassSurcharge);
        Assert.True(result.BypassVendorCostAdjustment);
        Assert.Equal("CUG31", result.Calculation.Provenance.Source);
    }

    [Theory]
    [InlineData("3")]
    [InlineData("5")]
    [InlineData("8")]
    [InlineData("")]
    public async Task UnchangedPriceMethodUsesFrozenPrice(string method)
    {
        PriceLockResult result = await Apply(Record(method, null), Input(Calculation(18m), method, 0m));
        Assert.True(result.IsPriceLocked);
        Assert.Equal(18m, result.Calculation.SellPrice.Value);
    }

    [Fact]
    public async Task PercentageChangeInvalidatesPercentageMethod()
    {
        SellPriceCalculationResult current = Calculation(12m);
        PriceLockResult result = await Apply(Record("2", 0.1m), Input(current, "2", 0.2m));
        Assert.False(result.IsPriceLocked);
        Assert.Same(current, result.Calculation);
    }

    [Fact]
    public async Task PriceChangeInvalidatesPriceMethodButPercentageDoesNot()
    {
        PriceLockResult result = await Apply(Record("3", 0.9m), Input(Calculation(17m), "3", -0.4m));
        Assert.False(result.IsPriceLocked);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task CostOrMethodChangeInvalidatesAnyLock(bool costChanged, bool methodChanged)
    {
        PriceLockInput input = Input(Calculation(12m), methodChanged ? "4" : "2", 0.2m) with
        {
            CurrentUnadjustedUnitCost = new Money(costChanged ? 11m : 10m),
        };
        PriceLockResult result = await Apply(Record("2", 0.2m), input);
        Assert.False(result.IsPriceLocked);
    }

    [Fact]
    public async Task BaseUomAmountsAreConvertedAndRoundedBeforeComparison()
    {
        PriceLockInput input = Input(Calculation(9m), "3", 0m) with
        {
            CurrentUnadjustedUnitCost = new Money(5m),
            ConversionUpFactor = 1m,
            ConversionDownFactor = 2m,
        };
        PriceLockResult result = await Apply(Record("3", null, totalSell: 20m, sellAdjustment: 2m, cost: 10m), input);
        Assert.True(result.IsPriceLocked);
        Assert.Equal(9m, result.Calculation.SellPrice.Value);
    }

    [Fact]
    public async Task NonPositiveConversionDenominatorReturnsTypedFailure()
    {
        PriceLockInput input = Input(Calculation(12m), "2", 0.2m) with { ConversionDownFactor = 0m };
        PriceLockResult result = await Apply(Record("2", 0.2m), input);
        Assert.IsType<ValidationPricingError>(result.Error);
        Assert.False(result.IsPriceLocked);
    }

    [Fact]
    public async Task CancellationIsForwardedToRepository()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var repository = new StubRepository(Record("2", 0.2m));
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await new PriceLockService(repository).ApplyAsync(Input(Calculation(12m), "2", 0.2m), source.Token));
    }

    private static async Task<PriceLockResult> Apply(PriceLockRecord record, PriceLockInput input) =>
        await new PriceLockService(new StubRepository(record)).ApplyAsync(input, default);

    private static PriceLockRecord Record(
        string method,
        decimal? percentage,
        decimal totalSell = 20m,
        decimal sellAdjustment = 2m,
        decimal cost = 10m) => new(
            new Money(totalSell), new Money(15m), new Money(sellAdjustment), new Money(1m), new Money(cost), method,
            percentage is null ? null : new Percentage(percentage.Value),
            new PricingDateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
            new RuleProvenance("Locked sell price", "CUG31", "ACCOUNT", null));

    private static PriceLockInput Input(SellPriceCalculationResult calculation, string method, decimal percentage) =>
        new(Context(), calculation, new Money(10m), method, new Percentage(percentage));

    private static SellPriceCalculationResult Calculation(decimal amount)
    {
        var provenance = new RuleProvenance("Fresh sell", "A6U01 7090", "ACCOUNT", null);
        var money = new Money(amount);
        return new(money, "C(+)", 0.2m, ImmutableArray.Create(new PriceComponent("Fresh", PriceComponentType.BaseSell, money, provenance)), provenance);
    }

    private static PricingContext Context()
    {
        var request = new PricingRequest(new DivisionId("01"), new AccountNumber("123456"), new VendorId("V001"),
            new ProductId("P0000001"), new Quantity(1m), new UnitOfMeasure("EA"), null, null, new DateOnly(2026, 7, 21), PricingRequestType.Full);
        var product = new ProductInformation(request.Vendor, request.Product, ProductType.Regular, request.UnitOfMeasure, null, null, 1234, "A");
        return new(request, product, new CustomerInformation(request.Account, new CustomerNumber(42), []), null, null);
    }

    private sealed class StubRepository(PriceLockRecord? record) : IPriceLockRepository
    {
        public ValueTask<PriceLockRecord?> FindEffectiveAsync(PricingContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(record);
        }
    }
}
