namespace Pricing.UnitTests.Fees;

using Pricing.Application.Fees;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class AncillaryAndDistributionFeeTests
{
    [Fact]
    public void ExposesLabelApplicationAndExtraDeliveryAsDistinctComponents()
    {
        JitFeeResult jit = Jit(label: 2m, application: 3m, extraDelivery: 4m);
        AncillaryJitFeeResult result = AncillaryJitFeeComposer.Compose(new(jit, Provenance("label"), Provenance("application"), Provenance("delivery")));
        Assert.Equal(2m, result.Label.Value);
        Assert.Equal(3m, result.Application.Value);
        Assert.Equal(4m, result.ExtraDelivery.Value);
        Assert.Equal(["Print label", "Apply label", "Extra delivery"], result.Components.Select(component => component.Name));
        Assert.All(result.Components, component => Assert.Equal(PriceComponentType.Fee, component.Type));
    }

    [Fact]
    public void OmitsZeroAncillaryComponentsWithoutChangingAmounts()
    {
        AncillaryJitFeeResult result = AncillaryJitFeeComposer.Compose(
            new(Jit(label: 0m, application: 3m, extraDelivery: 0m), Provenance("label"), Provenance("application"), Provenance("delivery")));
        Assert.Single(result.Components);
        Assert.Equal("Apply label", result.Components[0].Name);
    }

    [Theory]
    [InlineData(DistributionCategory.GroupSanctioned, "GS")]
    [InlineData(DistributionCategory.GroupNonSanctioned, "GN")]
    [InlineData(DistributionCategory.Individual, "MI")]
    [InlineData(DistributionCategory.NonContract, "MN")]
    [InlineData(DistributionCategory.Customer, "MC")]
    public void DistributionPreservesAllCobolBucketCodes(DistributionCategory category, string code)
    {
        DistributionFeeResult result = DistributionFeeEngine.Calculate(Input(category));
        Assert.Equal(code, DistributionFeeEngine.Code(category));
        Assert.Equal($"Distribution {code}", result.Component.Name);
        Assert.Equal(PriceComponentType.Markup, result.Component.Type);
    }

    [Fact]
    public void DistributionDecomposesMarginAlreadyEmbeddedInSellPrice()
    {
        DistributionFeeResult result = DistributionFeeEngine.Calculate(Input(DistributionCategory.GroupSanctioned));
        Assert.Equal(25m, result.Amount.Value);
        Assert.Equal(125m, result.LineSellPrice.Value);
        Assert.False(result.BilledSeparately);
    }

    [Theory]
    [InlineData("MA")]
    [InlineData("MM")]
    public void MonthlyDistributionSubtractsEmbeddedMarginBackOut(string billingFrequency)
    {
        DistributionFeeResult result = DistributionFeeEngine.Calculate(Input(DistributionCategory.Customer) with { BillingFrequency = billingFrequency });
        Assert.Equal(25m, result.Amount.Value);
        Assert.Equal(100m, result.LineSellPrice.Value);
        Assert.True(result.BilledSeparately);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("DL")]
    [InlineData("DS")]
    public void NonMonthlyDistributionRemainsInLine(string? billingFrequency)
    {
        DistributionFeeResult result = DistributionFeeEngine.Calculate(Input(DistributionCategory.Individual) with { BillingFrequency = billingFrequency });
        Assert.Equal(125m, result.LineSellPrice.Value);
        Assert.False(result.BilledSeparately);
    }

    [Fact]
    public void DistributionPreservesNegativeAndZeroMargin()
    {
        Assert.Equal(0m, DistributionFeeEngine.Calculate(Input(DistributionCategory.NonContract) with { SellPrice = new Money(100m) }).Amount.Value);
        Assert.Equal(-10m, DistributionFeeEngine.Calculate(Input(DistributionCategory.NonContract) with { SellPrice = new Money(90m) }).Amount.Value);
    }

    private static DistributionFeeInput Input(DistributionCategory category) =>
        new(category, new Money(125m), new Money(100m), null, Provenance("distribution"));

    private static JitFeeResult Jit(decimal label, decimal application, decimal extraDelivery) => new(
        new Money(0m), new Money(label), new Money(application), new Money(0m), new Money(0m), new Money(extraDelivery),
        new Money(0m), new Money(0m), new Money(0m), false, false, []);

    private static RuleProvenance Provenance(string name) => new(name, $"A6U01 {name}", "ACCOUNT", null);
}
