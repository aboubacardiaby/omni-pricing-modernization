namespace Pricing.UnitTests.Kits;

using Pricing.Application.Kits;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class KitRollupCalculatorTests
{
    [Fact]
    public void RollsQuantitiesCostsRebatesAdjustmentsFeesAndSellIntoNamedBuckets()
    {
        RuleProvenance provenance = Provenance("Component");
        PricedKitComponent first = Priced(
            "COMP0001",
            2m,
            10m,
            15m,
            Component("Base cost", PriceComponentType.BaseCost, 8m, provenance),
            Component("Contract rebate", PriceComponentType.Rebate, 1m, provenance),
            Component("Vendor cost adjustment", PriceComponentType.CostAdjustment, .5m, provenance),
            Component("Inbound freight", PriceComponentType.Fee, .25m, provenance),
            Component("JIT service", PriceComponentType.Fee, .1m, provenance),
            Component("Risk", PriceComponentType.SellAdjustment, .2m, provenance));
        PricedKitComponent second = Priced(
            "COMP0002",
            3m,
            4m,
            6m,
            Component("Base cost", PriceComponentType.BaseCost, 3m, provenance),
            Component("Price protection", PriceComponentType.Rebate, -1m, provenance),
            Component("Cost adjustment", PriceComponentType.CostAdjustment, .4m, provenance));
        KitComponentPricingResult input = Input(
            [first, second],
            ExplosionNode("ROOT0001", 1m, 2m, 3m),
            ExplosionNode("SUB00001", 99m, 99m, 99m));

        KitRollupResult result = KitRollupCalculator.Calculate(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(5m, result.TotalComponentQuantity.Value);
        Assert.Equal(32m, result.ComponentCost.Value);
        Assert.Equal(25m, result.SelectedBaseCost.Value);
        Assert.Equal(-1m, result.Rebates.Value);
        Assert.Equal(2.2m, result.CostAdjustments.Value);
        Assert.Equal(1m, result.VendorCostAdjustments.Value);
        Assert.Equal(.5m, result.Freight.Value);
        Assert.Equal(.2m, result.Jit.Value);
        Assert.Equal(.4m, result.SellAdjustments.Value);
        Assert.Equal(1m, result.Overhead.Value);
        Assert.Equal(2m, result.ThirdPartyCost.Value);
        Assert.Equal(3m, result.ThirdPartySell.Value);
        Assert.Equal(38m, result.TotalCost.Value);
        Assert.Equal(35m, result.CostBasisForSell.Value);
        Assert.Equal(48m, result.ComponentSell.Value);
        Assert.Equal(51m, result.TotalSell.Value);
    }

    [Fact]
    public void ProducesQuantityExtendedExplainableLinesInComponentOrder()
    {
        RuleProvenance firstSource = Provenance("First");
        RuleProvenance secondSource = Provenance("Second");
        KitComponentPricingResult input = Input(
            [
                Priced("COMP0001", 2m, 1m, 2m, Component("Inbound freight", PriceComponentType.Fee, .5m, firstSource)),
                Priced("COMP0002", 3m, 1m, 2m, Component("JIT label", PriceComponentType.Fee, .25m, secondSource)),
            ],
            ExplosionNode("ROOT0001", 0m, 0m, 0m));

        KitRollupResult result = KitRollupCalculator.Calculate(input);

        Assert.Collection(
            result.Lines,
            line =>
            {
                Assert.Equal("1234COMP0001", line.ComponentProduct.Value);
                Assert.Equal(1m, line.ExtendedAmount.Value);
                Assert.Same(firstSource, line.Provenance);
            },
            line =>
            {
                Assert.Equal("1234COMP0002", line.ComponentProduct.Value);
                Assert.Equal(.75m, line.ExtendedAmount.Value);
                Assert.Same(secondSource, line.Provenance);
            });
    }

    [Fact]
    public void AddsOnlyRootExplosionHeaderFeesBecauseLegacyNestedRollupIsBlocked()
    {
        KitComponentPricingResult input = Input(
            [],
            ExplosionNode("ROOT0001", 1m, 2m, 3m),
            ExplosionNode("SUB00001", 10m, 20m, 30m));

        KitRollupResult result = KitRollupCalculator.Calculate(input);

        Assert.Equal(6m, result.TotalCost.Value);
        Assert.Equal(3m, result.CostBasisForSell.Value);
        Assert.Equal(3m, result.TotalSell.Value);
        Assert.Equal(
            ["Kit overhead", "Kit third-party cost fee", "Kit third-party sell fee"],
            result.Components.Select(component => component.Name));
    }

    [Fact]
    public void ZeroThirdPartySellDoesNotReduceCostBasisOrCreateComponent()
    {
        KitComponentPricingResult input = Input([], ExplosionNode("ROOT0001", 1m, 2m, 0m));

        KitRollupResult result = KitRollupCalculator.Calculate(input);

        Assert.Equal(result.TotalCost, result.CostBasisForSell);
        Assert.DoesNotContain(result.Components, component => component.Name == "Kit third-party sell fee");
    }

    private static KitComponentPricingResult Input(
        PricedKitComponent[] components,
        params ExplodedKitNode[] explosions) =>
        new([.. explosions], [.. components], null, null);

    private static PricedKitComponent Priced(
        string product,
        decimal quantity,
        decimal cost,
        decimal sell,
        params PriceComponent[] components)
    {
        KitExplosionItem item = Item(product, quantity);
        var result = new PricingResult(
            ProductType.Regular,
            new Money(cost),
            new Money(sell),
            null,
            null,
            null,
            [.. components],
            [],
            [],
            []);
        return new(item, item.Quantity, 0, [Product("ROOT0001")], result);
    }

    private static ExplodedKitNode ExplosionNode(string product, decimal overhead, decimal thirdPartyCost, decimal thirdPartySell)
    {
        KitProductNumber pack = Product(product);
        KitExplosion explosion = new(
            pack,
            new UnitOfMeasure("KT"),
            "O",
            string.Empty,
            string.Empty,
            string.Empty,
            null,
            Fee(overhead),
            Fee(thirdPartyCost),
            Fee(thirdPartySell),
            []);
        return new(pack, new Quantity(1m), 0, explosion);
    }

    private static KitFeeWindow Fee(decimal amount) => new(
        new Money(amount),
        new DateOnly(2028, 1, 1),
        new DateOnly(2028, 12, 31));

    private static KitExplosionItem Item(string product, decimal quantity) => new(
        1,
        null,
        Product(product),
        new UnitOfMeasure("EA"),
        new Quantity(quantity),
        KitExplosionItemType.Component,
        1);

    private static PriceComponent Component(
        string name,
        PriceComponentType type,
        decimal amount,
        RuleProvenance provenance) =>
        new(name, type, new Money(amount), provenance);

    private static RuleProvenance Provenance(string name) =>
        new(name, "A6U01", "Component", null);

    private static KitProductNumber Product(string product) => new("1234" + product);
}
