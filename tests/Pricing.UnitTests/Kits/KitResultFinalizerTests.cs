namespace Pricing.UnitTests.Kits;

using Pricing.Application.Kits;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class KitResultFinalizerTests
{
    [Fact]
    public void AppliesOnlyConfirmedAlternateUomConversionsWithCobolTruncation()
    {
        Scenario scenario = CreateScenario(
            requestedUom: "CS",
            componentExpiration: new DateOnly(2028, 10, 31));

        KitFinalizationResult finalized = KitResultFinalizer.Create(
            scenario.Request with { AlternateUnitOfMeasureFactor = 1.5m });

        Assert.Equal(1.5m, finalized.AppliedConversionFactor);
        Assert.Equal(15m, finalized.ConvertedRollup.ComponentCost.Value);
        Assert.Equal(9m, finalized.ConvertedRollup.SelectedBaseCost.Value);
        Assert.Equal(3.35185183m, finalized.ConvertedRollup.Rebates.Value);
        Assert.Equal(.75m, finalized.ConvertedRollup.VendorCostAdjustments.Value);
        Assert.Equal(.375m, finalized.ConvertedRollup.Freight.Value);
        Assert.Equal(.15m, finalized.ConvertedRollup.Jit.Value);
        Assert.Equal(1.5m, finalized.ConvertedRollup.Overhead.Value);
        Assert.Equal(.9m, finalized.ConvertedRollup.CostAdjustments.Value);
        Assert.Equal(.2m, finalized.ConvertedRollup.SellAdjustments.Value);
        Assert.Equal(2m, finalized.ConvertedRollup.ThirdPartyCost.Value);
        Assert.Equal(3m, finalized.ConvertedRollup.ThirdPartySell.Value);
        Assert.Equal(24m, finalized.PricingResult.Cost?.Value);
        Assert.Equal(27m, finalized.PricingResult.SellPrice?.Value);

        PriceComponent precise = Assert.Single(
            finalized.PricingResult.Components,
            component => component.Name == "Precise rebate");
        Assert.Equal(1.85185183m, precise.Amount.Value);
    }

    [Fact]
    public void SameUomRequiresNoFactorAndLeavesRollupUnchanged()
    {
        Scenario scenario = CreateScenario("EA", new DateOnly(2028, 10, 31));

        KitFinalizationResult finalized = KitResultFinalizer.Create(scenario.Request);

        Assert.Equal(1m, finalized.AppliedConversionFactor);
        Assert.Same(scenario.Rollup, finalized.ConvertedRollup);
        Assert.Empty(finalized.PricingResult.Errors);
    }

    [Fact]
    public void MissingAlternateFactorProducesTypedErrorAndPreservesUnconvertedValues()
    {
        Scenario scenario = CreateScenario("CS", new DateOnly(2028, 10, 31));

        KitFinalizationResult finalized = KitResultFinalizer.Create(scenario.Request);

        MissingDataPricingError error = Assert.IsType<MissingDataPricingError>(Assert.Single(finalized.PricingResult.Errors));
        Assert.Equal("KIT_ALTERNATE_UOM_FACTOR_MISSING", error.Code);
        Assert.Equal(scenario.Rollup.TotalCost, finalized.PricingResult.Cost);
        Assert.Equal(1m, finalized.AppliedConversionFactor);
    }

    [Fact]
    public void ChoosesEarliestComponentOrRootFeeExpirationWithProvenance()
    {
        Scenario scenario = CreateScenario("EA", new DateOnly(2028, 10, 31));

        KitFinalizationResult finalized = KitResultFinalizer.Create(scenario.Request);

        Assert.Equal(new DateOnly(2028, 6, 30), finalized.PricingResult.ExpirationDate);
        Assert.Contains(
            finalized.PricingResult.Provenance,
            provenance => provenance.Source.Contains("OMGEXPL-TP-COST-EXP-DATE", StringComparison.Ordinal));
        Assert.Contains(
            finalized.PricingResult.Provenance,
            provenance => provenance.Source.Contains("A6U01", StringComparison.Ordinal));
    }

    [Fact]
    public void PropagatesComponentTraversalAndRollupErrorsAndComponentWarnings()
    {
        Scenario scenario = CreateScenario("EA", new DateOnly(2028, 10, 31), componentError: true);
        KitComponentPricingResult componentPricing = scenario.ComponentPricing with
        {
            TraversalError = new ValidationPricingError("KIT_CYCLE_DETECTED", "Cycle"),
        };
        KitRollupResult rollup = scenario.Rollup with
        {
            Error = new ValidationPricingError("KIT_ROLLUP_AMOUNT_EXCEEDED", "Too large", "3000"),
        };

        KitFinalizationResult finalized = KitResultFinalizer.Create(
            scenario.Request with { ComponentPricing = componentPricing, Rollup = rollup });

        Assert.Equal(
            ["COMPONENT_FAILED", "KIT_CYCLE_DETECTED", "KIT_ROLLUP_AMOUNT_EXCEEDED"],
            finalized.PricingResult.Errors.Select(error => error.Code));
        Assert.Equal("COMPONENT_WARNING", Assert.Single(finalized.PricingResult.Warnings).Code);
    }

    private static Scenario CreateScenario(
        string requestedUom,
        DateOnly componentExpiration,
        bool componentError = false)
    {
        RuleProvenance provenance = new("Component", "A6U01", "Component", null);
        KitProductNumber product = new("1234COMP0001");
        KitExplosionItem item = new(
            1,
            null,
            product,
            new UnitOfMeasure("EA"),
            new Quantity(1m),
            KitExplosionItemType.Component,
            1);
        PricingError[] errors = componentError
            ? [new MissingDataPricingError("COMPONENT_FAILED", "Component failed")]
            : [];
        var componentResult = new PricingResult(
            ProductType.Regular,
            new Money(10m),
            new Money(15m),
            componentExpiration,
            null,
            null,
            [
                new PriceComponent("Base cost", PriceComponentType.BaseCost, new Money(6m), provenance),
                new PriceComponent("Contract rebate", PriceComponentType.Rebate, new Money(1m), provenance),
                new PriceComponent("Precise rebate", PriceComponentType.Rebate, new Money(1.23456789m), provenance),
                new PriceComponent("Vendor cost adjustment", PriceComponentType.CostAdjustment, new Money(.5m), provenance),
                new PriceComponent("Cost adjustment", PriceComponentType.CostAdjustment, new Money(.4m), provenance),
                new PriceComponent("Inbound freight", PriceComponentType.Fee, new Money(.25m), provenance),
                new PriceComponent("JIT service", PriceComponentType.Fee, new Money(.1m), provenance),
                new PriceComponent("Risk", PriceComponentType.SellAdjustment, new Money(.2m), provenance),
            ],
            [provenance],
            [new PricingWarning("COMPONENT_WARNING", "Warning")],
            [.. errors]);
        var priced = new PricedKitComponent(item, item.Quantity, 0, [new KitProductNumber("1234ROOT0001")], componentResult);
        KitExplosion explosion = new(
            new KitProductNumber("1234ROOT0001"),
            new UnitOfMeasure("EA"),
            "O",
            string.Empty,
            string.Empty,
            string.Empty,
            null,
            Fee(1m, new DateOnly(2028, 9, 30)),
            Fee(2m, new DateOnly(2028, 6, 30)),
            Fee(3m, new DateOnly(2028, 12, 31)),
            [item]);
        var componentPricing = new KitComponentPricingResult(
            [new ExplodedKitNode(explosion.PackProduct, new Quantity(1m), 0, explosion)],
            [priced],
            null,
            componentError ? priced : null);
        KitRollupResult rollup = KitRollupCalculator.Calculate(componentPricing);
        PricingRequest request = new(
            new DivisionId("01"),
            new AccountNumber("123456"),
            new VendorId("1234"),
            new ProductId("ROOT0001"),
            new Quantity(1m),
            new UnitOfMeasure(requestedUom),
            "001",
            "001",
            new DateOnly(2028, 1, 1),
            PricingRequestType.Full);
        return new Scenario(
            new KitFinalizationRequest(request, new UnitOfMeasure("EA"), null, componentPricing, rollup),
            componentPricing,
            rollup);
    }

    private static KitFeeWindow Fee(decimal amount, DateOnly expiration) =>
        new(new Money(amount), new DateOnly(2028, 1, 1), expiration);

    private sealed record Scenario(
        KitFinalizationRequest Request,
        KitComponentPricingResult ComponentPricing,
        KitRollupResult Rollup);
}
