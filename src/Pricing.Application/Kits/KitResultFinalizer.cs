namespace Pricing.Application.Kits;

using System.Collections.Immutable;
using Pricing.Application.ExpirationDates;
using Pricing.Application.Rounding;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

public sealed record KitFinalizationRequest(
    PricingRequest Request,
    UnitOfMeasure BaseUnitOfMeasure,
    decimal? AlternateUnitOfMeasureFactor,
    KitComponentPricingResult ComponentPricing,
    KitRollupResult Rollup);

public sealed record KitFinalizationResult(
    PricingResult PricingResult,
    KitRollupResult ConvertedRollup,
    decimal AppliedConversionFactor);

/// <summary>Applies A6O011U 4200/4300 behavior and emits the final explainable kit result.</summary>
public static class KitResultFinalizer
{
    public static KitFinalizationResult Create(KitFinalizationRequest input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var errors = ImmutableArray.CreateBuilder<PricingError>();
        var warnings = ImmutableArray.CreateBuilder<PricingWarning>();
        AddComponentOutcomes(input.ComponentPricing, errors, warnings);
        if (input.ComponentPricing.TraversalError is { } traversalError)
        {
            errors.Add(traversalError);
        }

        if (input.Rollup.Error is { } rollupError)
        {
            errors.Add(rollupError);
        }

        decimal factor = ResolveFactor(input, errors);
        KitRollupResult converted = factor == 1m ? input.Rollup : Convert(input.Rollup, factor);

        ImmutableArray<ExpirationDateSource> expirationSources = ExpirationSources(input);
        ExpirationDateCollectionResult expiration = ExpirationDateCollector.Collect(
            input.Request.PricingDate,
            expirationSources);
        if (expiration.Error is { } expirationError)
        {
            errors.Add(expirationError);
        }

        ImmutableArray<RuleProvenance> provenance = converted.Components
            .Select(component => component.Provenance)
            .Concat(expirationSources.Select(source => source.Provenance))
            .Distinct()
            .ToImmutableArray();
        var result = new PricingResult(
            ProductType.Kit,
            converted.TotalCost,
            converted.TotalSell,
            expiration.ExpirationDate,
            null,
            null,
            converted.Components,
            provenance,
            warnings.ToImmutable(),
            errors.ToImmutable());
        return new KitFinalizationResult(result, converted, factor);
    }

    private static decimal ResolveFactor(
        KitFinalizationRequest input,
        ImmutableArray<PricingError>.Builder errors)
    {
        if (input.Request.UnitOfMeasure == input.BaseUnitOfMeasure)
        {
            return 1m;
        }

        if (input.AlternateUnitOfMeasureFactor is not > 0m)
        {
            errors.Add(new MissingDataPricingError(
                "KIT_ALTERNATE_UOM_FACTOR_MISSING",
                $"No positive conversion factor exists from kit base UOM '{input.BaseUnitOfMeasure}' to requested UOM '{input.Request.UnitOfMeasure}'.",
                Entity: "alternateUnitOfMeasure"));
            return 1m;
        }

        return input.AlternateUnitOfMeasureFactor.Value;
    }

    private static KitRollupResult Convert(KitRollupResult value, decimal factor)
    {
        ImmutableArray<PriceComponent> components = value.Components
            .Select(component => ShouldConvert(component)
                ? component with { Amount = Scale(component.Amount, factor) }
                : component)
            .ToImmutableArray();
        ImmutableArray<KitRollupLine> lines = value.Lines
            .Select(line => ShouldConvert(line.Name, line.Type)
                ? line with { ExtendedAmount = Scale(line.ExtendedAmount, factor) }
                : line)
            .ToImmutableArray();

        return value with
        {
            ComponentCost = Scale(value.ComponentCost, factor),
            SelectedBaseCost = Scale(value.SelectedBaseCost, factor),
            Rebates = Scale(value.Rebates, factor),
            Freight = Scale(value.Freight, factor),
            Jit = Scale(value.Jit, factor),
            VendorCostAdjustments = Scale(value.VendorCostAdjustments, factor),
            Overhead = Scale(value.Overhead, factor),
            TotalCost = Scale(value.TotalCost, factor),
            CostBasisForSell = Scale(value.CostBasisForSell, factor),
            ComponentSell = Scale(value.ComponentSell, factor),
            TotalSell = Scale(value.TotalSell, factor),
            Lines = lines,
            Components = components,
        };
    }

    private static bool ShouldConvert(PriceComponent component) => ShouldConvert(component.Name, component.Type);

    private static bool ShouldConvert(string name, PriceComponentType type)
    {
        if (name is "Kit third-party cost fee" or "Kit third-party sell fee")
        {
            return false;
        }

        if (type == PriceComponentType.SellAdjustment)
        {
            return false;
        }

        return type != PriceComponentType.CostAdjustment
            || name is "Vendor cost adjustment" or "Kit overhead";
    }

    private static Money Scale(Money value, decimal factor) =>
        new(CobolRoundingPolicy.TruncateIntermediate(value.Value * factor));

    private static void AddComponentOutcomes(
        KitComponentPricingResult componentPricing,
        ImmutableArray<PricingError>.Builder errors,
        ImmutableArray<PricingWarning>.Builder warnings)
    {
        foreach (PricedKitComponent component in componentPricing.Components)
        {
            warnings.AddRange(component.PricingResult.Warnings);
            errors.AddRange(component.PricingResult.Errors);
        }
    }

    private static ImmutableArray<ExpirationDateSource> ExpirationSources(KitFinalizationRequest input)
    {
        var sources = ImmutableArray.CreateBuilder<ExpirationDateSource>();
        foreach (PricedKitComponent component in input.ComponentPricing.Components)
        {
            if (component.PricingResult.ExpirationDate is not { } expiration)
            {
                continue;
            }

            sources.Add(new ExpirationDateSource(
                $"Component {component.ExplosionItem.Product.Value}",
                expiration,
                new RuleProvenance(
                    "Kit component expiration",
                    "A6O011U 2000-PROCESS-PRICE/A6U01",
                    component.ExplosionItem.Product.Value,
                    null)));
        }

        if (!input.ComponentPricing.ExplodedKits.IsEmpty)
        {
            KitExplosion root = input.ComponentPricing.ExplodedKits[0].Explosion;
            AddFee("Kit overhead", root.Overhead, "OMGEXPL-OH-EXP-DATE");
            AddFee("Kit third-party cost", root.ThirdPartyCost, "OMGEXPL-TP-COST-EXP-DATE");
            AddFee("Kit third-party sell", root.ThirdPartySell, "OMGEXPL-TP-SELL-EXP-DATE");
        }

        return sources.ToImmutable();

        void AddFee(string component, KitFeeWindow fee, string source)
        {
            if (fee.ExpirationDate is not { } expiration)
            {
                return;
            }

            sources.Add(new ExpirationDateSource(
                component,
                expiration,
                new RuleProvenance(
                    "Kit fee expiration",
                    $"A6O011U 4200-PROCESS-EXP-DATE/{source}",
                    "Kit",
                    fee.EffectiveDate is { } effective
                        ? new PricingDateRange(effective, expiration)
                        : null)));
        }
    }
}
