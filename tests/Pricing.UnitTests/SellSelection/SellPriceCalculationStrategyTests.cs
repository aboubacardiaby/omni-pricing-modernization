namespace Pricing.UnitTests.SellSelection;

using Pricing.Application.SellSelection;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class SellPriceCalculationStrategyTests
{
    [Theory]
    [InlineData("01", "3", 15)]
    [InlineData("02", "3", 14)]
    [InlineData("03", "3", 13)]
    [InlineData("99", "3", 13)]
    [InlineData("01", "4", 13.5)]
    public async Task ListMethodsUseBusinessSpecificBasis(string business, string method, decimal expected)
    {
        SellPriceCalculationResult result = await Dispatcher().CalculateAsync(method, Input(business: business, percentage: 0.1m), default);
        Assert.Equal(expected, result.SellPrice.Value);
    }

    [Theory]
    [InlineData("1", 12.5)]
    [InlineData("2", 12)]
    public async Task CostMethodsApplyConfirmedFormula(string method, decimal expected)
    {
        SellPriceCalculationResult result = await Dispatcher().CalculateAsync(method, Input(percentage: 0.2m), default);
        Assert.Equal(expected, result.SellPrice.Value);
        Assert.Equal(0.2m, result.AppliedPercentage);
    }

    [Fact]
    public async Task GrossMarginClampsOneHundredPercentAndRoundsToEightPlaces()
    {
        SellPriceCalculationResult result = await Dispatcher().CalculateAsync("1", Input(percentage: 1m, totalCost: 0.00000001m), default);
        Assert.Equal(0.0001m, result.SellPrice.Value);
        Assert.Equal(0.9999m, result.AppliedPercentage);
    }

    [Theory]
    [InlineData("5", 12)]
    [InlineData("6", 13.2)]
    [InlineData("7", 10.8)]
    public async Task SuggestedMethodsApplyConfirmedFormula(string method, decimal expected)
    {
        SellPriceCalculationResult result = await Dispatcher().CalculateAsync(method, Input(percentage: 0.1m, suggested: 12m, costContract: true, suggestedAvailable: true), default);
        Assert.Equal(expected, result.SellPrice.Value);
    }

    [Theory]
    [InlineData("5")]
    [InlineData("6")]
    [InlineData("7")]
    public async Task SuggestedMethodsFallBackForZeroOrUnavailableValues(string method)
    {
        SellPriceCalculationResult result = await Dispatcher().CalculateAsync(method, Input(percentage: 0m, suggested: 0m), default);
        Assert.Equal(13m, result.SellPrice.Value);
        Assert.Equal("LIST-DEF", result.MethodCode);
    }

    [Fact]
    public async Task StatedPriceConvertsAndRoundsAtComputeStage()
    {
        SellPriceCalculationResult result = await Dispatcher().CalculateAsync("8", Input(stated: 10m, conversionUp: 1m, conversionDown: 3m), default);
        Assert.Equal(3.33333333m, result.SellPrice.Value);
    }

    [Fact]
    public async Task InvalidStatedDenominatorReturnsTypedError()
    {
        SellPriceCalculationResult result = await Dispatcher().CalculateAsync("8", Input(stated: 10m, conversionDown: 0m), default);
        Assert.IsType<ValidationPricingError>(result.Error);
    }

    [Fact]
    public async Task PercentageSourceFailureIsPreserved()
    {
        var error = new MissingDataPricingError("SELL_PERCENTAGE_MISSING", "Missing percentage.", "602", LegacySeverityCode: "70");
        SellPriceCalculationInput input = Input(percentage: 0.1m) with { PercentageResolutionError = error };
        SellPriceCalculationResult result = await Dispatcher().CalculateAsync("1", input, default);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public async Task NegativeCostPlusPercentageIsPreservedLikeCobol()
    {
        SellPriceCalculationResult result = await Dispatcher().CalculateAsync("2", Input(percentage: -0.1m), default);
        Assert.Equal(9m, result.SellPrice.Value);
    }

    private static SellPriceCalculationDispatcher Dispatcher() => new(
    [
        new GrossMarginCalculationStrategy(), new CostPlusCalculationStrategy(), new ListPriceCalculationStrategy(),
        new CostDiscountCalculationStrategy(), new SuggestedSellCalculationStrategy(),
        new SuggestedSellMarkupCalculationStrategy(), new SuggestedSellMarkdownCalculationStrategy(),
        new StatedPriceCalculationStrategy(),
    ]);

    private static SellPriceCalculationInput Input(
        string business = "03", decimal? percentage = null, decimal totalCost = 10m, decimal suggested = 12m,
        bool costContract = false, bool suggestedAvailable = false, decimal? stated = null,
        decimal conversionUp = 1m, decimal conversionDown = 1m)
    {
        PricingContext context = Context();
        var provenance = new RuleProvenance("sell-test", "A6U01 7090", "test", null);
        var selection = new SellArrangementSelection("A", "TEST", provenance);
        var basis = new SellCalculationBasis(new Money(totalCost), new Money(10m), new Money(15m), new Money(14m), new Money(13m), new Money(suggested), new UnitOfMeasure("EA"));
        return new(context, selection, basis, business, percentage is null ? null : new Percentage(percentage.Value), costContract, suggestedAvailable,
            stated is null ? null : new Money(stated.Value), conversionUp, conversionDown);
    }

    private static PricingContext Context()
    {
        var request = new PricingRequest(new DivisionId("01"), new AccountNumber("123456"), new VendorId("V001"),
            new ProductId("P0000001"), new Quantity(1m), new UnitOfMeasure("EA"), null, null, new DateOnly(2026, 7, 21), PricingRequestType.Full);
        var product = new ProductInformation(request.Vendor, request.Product, ProductType.Regular, request.UnitOfMeasure, null, null, 1234, "A");
        return new(request, product, new CustomerInformation(request.Account, new CustomerNumber(42), []), null, null);
    }
}
