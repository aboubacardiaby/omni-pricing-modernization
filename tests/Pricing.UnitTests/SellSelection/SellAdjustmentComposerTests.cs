namespace Pricing.UnitTests.SellSelection;

using System.Collections.Immutable;
using Pricing.Application.SellSelection;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class SellAdjustmentComposerTests
{
    [Fact]
    public void ComposesAll7195BucketsAndItemizedProvenance()
    {
        SellAdjustmentSet adjustments = new(
            Component("Inventory", 1m), Component("Risk", 2m), Component("Delivery", 3m),
            Component("Finance", 4m), Component("Non-contract", 5m), Component("Prepay", 2m),
            Component("JIT sell", 1.5m), Component("JIT cost", 0.5m), Component("PANDAC", 2m), Component("SurgiTrak", 3m));

        SellAdjustmentCompositionResult result = SellAdjustmentComposer.Compose(new(Base(100m), adjustments, "P"));

        Assert.Equal(20m, result.TotalAdjustment.Value);
        Assert.Equal(120m, result.TotalSell.Value);
        Assert.Equal(11, result.Components.Length);
        Assert.Equal(-2m, result.Components.Single(component => component.Name == "Prepay").Amount.Value);
        Assert.All(result.Components, component => Assert.NotNull(component.Provenance));
    }

    [Theory]
    [InlineData("R")]
    [InlineData("A")]
    public void JitRateAndAccountModesExcludeJitFromSell(string code)
    {
        var adjustments = new SellAdjustmentSet(JitSell: Component("JIT sell", 3m), JitCost: Component("JIT cost", 4m));
        SellAdjustmentCompositionResult result = SellAdjustmentComposer.Compose(new(Base(10m), adjustments, code));
        Assert.Equal(0m, result.TotalAdjustment.Value);
        Assert.DoesNotContain(result.Components, component => component.Name.StartsWith("JIT", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(true, false, 3)]
    [InlineData(false, true, 2)]
    [InlineData(true, true, 0)]
    [InlineData(false, false, 5)]
    public void MonthlyBillingControlsPandacAndSurgiTrakFoldIn(bool pandacMonthly, bool surgiMonthly, decimal expected)
    {
        var adjustments = new SellAdjustmentSet(Pandac: Component("PANDAC", 2m), SurgiTrak: Component("SurgiTrak", 3m));
        SellAdjustmentCompositionResult result = SellAdjustmentComposer.Compose(new(Base(10m), adjustments, "", pandacMonthly, surgiMonthly));
        Assert.Equal(expected, result.TotalAdjustment.Value);
    }

    [Fact]
    public void ZeroAndNegativeAdjustmentsRetainCobolSignedArithmetic()
    {
        var adjustments = new SellAdjustmentSet(InventoryClass: Component("Zero", 0m), RiskPremium: Component("Credit", -2.25m));
        SellAdjustmentCompositionResult result = SellAdjustmentComposer.Compose(new(Base(10m), adjustments, ""));
        Assert.Equal(-2.25m, result.TotalAdjustment.Value);
        Assert.Equal(7.75m, result.TotalSell.Value);
    }

    [Fact]
    public void PropagatesBaseCalculationFailureWithoutAdjusting()
    {
        SellPriceCalculationResult failed = Base(10m) with
        {
            Error = new MissingDataPricingError("SELL_FAILED", "Sell failed."),
        };
        SellAdjustmentCompositionResult result = SellAdjustmentComposer.Compose(
            new(failed, new SellAdjustmentSet(RiskPremium: Component("Risk", 2m)), ""));
        Assert.True(result.IsFailure);
        Assert.Equal(10m, result.TotalSell.Value);
        Assert.Equal(0m, result.TotalAdjustment.Value);
    }

    [Fact]
    public void RejectsWrongComponentTypeAndNegativePrepayInput()
    {
        PriceComponent wrong = Component("Wrong", 1m) with { Type = PriceComponentType.Fee };
        Assert.Throws<ArgumentException>(() => SellAdjustmentComposer.Compose(new(Base(10m), new SellAdjustmentSet(RiskPremium: wrong), "")));
        Assert.Throws<ArgumentException>(() => SellAdjustmentComposer.Compose(
            new(Base(10m), new SellAdjustmentSet(PrepayDeduction: Component("Prepay", -1m)), "")));
    }

    [Fact]
    public void RoundsTotalAtCobolComputeStage()
    {
        var adjustments = new SellAdjustmentSet(RiskPremium: Component("Risk", 0.00000001m));
        SellAdjustmentCompositionResult result = SellAdjustmentComposer.Compose(new(Base(1.00000001m), adjustments, ""));
        Assert.Equal(1.00000002m, result.TotalSell.Value);
    }

    private static PriceComponent Component(string name, decimal amount) => new(
        name, PriceComponentType.SellAdjustment, new Money(amount), new RuleProvenance(name, $"A6U01 {name}", "ACCOUNT", null));

    private static SellPriceCalculationResult Base(decimal amount)
    {
        var provenance = new RuleProvenance("Base sell", "A6U01 7090/7205", "ACCOUNT", null);
        var money = new Money(amount);
        return new(money, "C(+)", 0.1m, ImmutableArray.Create(new PriceComponent("Base sell", PriceComponentType.BaseSell, money, provenance)), provenance);
    }
}
