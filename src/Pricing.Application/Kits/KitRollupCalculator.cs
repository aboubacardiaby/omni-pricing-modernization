namespace Pricing.Application.Kits;

using System.Collections.Immutable;
using Pricing.Application.Rounding;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

public sealed record KitRollupLine(
    KitProductNumber ComponentProduct,
    string Name,
    PriceComponentType Type,
    Quantity EffectiveQuantity,
    Money UnitAmount,
    Money ExtendedAmount,
    RuleProvenance Provenance);

public sealed record KitRollupResult(
    Quantity TotalComponentQuantity,
    Money ComponentCost,
    Money SelectedBaseCost,
    Money Rebates,
    Money CostAdjustments,
    Money Freight,
    Money Jit,
    Money VendorCostAdjustments,
    Money SellAdjustments,
    Money Overhead,
    Money ThirdPartyCost,
    Money ThirdPartySell,
    Money TotalCost,
    Money CostBasisForSell,
    Money ComponentSell,
    Money TotalSell,
    ImmutableArray<KitRollupLine> Lines,
    ImmutableArray<PriceComponent> Components,
    PricingError? Error = null)
{
    public bool IsSuccess => Error is null;
}

/// <summary>Rolls regular component results into the named A6O011U 3000/3100 kit buckets.</summary>
public static class KitRollupCalculator
{
    public static KitRollupResult Calculate(KitComponentPricingResult input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var accumulator = new Accumulator();

        foreach (PricedKitComponent component in input.Components)
        {
            decimal quantity = component.EffectiveQuantity.Value;
            accumulator.Quantity += quantity;
            accumulator.ComponentCost += Extended(component.PricingResult.Cost, quantity);
            accumulator.ComponentSell += Extended(component.PricingResult.SellPrice, quantity);

            foreach (PriceComponent priceComponent in component.PricingResult.Components)
            {
                decimal extended = Round(priceComponent.Amount.Value * quantity);
                accumulator.Lines.Add(new KitRollupLine(
                    component.ExplosionItem.Product,
                    priceComponent.Name,
                    priceComponent.Type,
                    component.EffectiveQuantity,
                    priceComponent.Amount,
                    MoneyOf(extended),
                    priceComponent.Provenance));
                accumulator.Components.Add(priceComponent with { Amount = MoneyOf(extended) });
                AccumulateBucket(accumulator, priceComponent, extended);
            }

        }

        // A6O011U 3100 consumes the one OMGEXPL header returned for the requested top pack.
        // A6O015U's blank rollup-switch behavior is blocked, so nested node headers are not inferred.
        if (!input.ExplodedKits.IsEmpty)
        {
            KitExplosion root = input.ExplodedKits[0].Explosion;
            accumulator.Overhead = root.Overhead.Amount.Value;
            accumulator.ThirdPartyCost = root.ThirdPartyCost.Amount.Value;
            accumulator.ThirdPartySell = root.ThirdPartySell.Amount.Value;
            AddHeaderComponent(accumulator, "Kit overhead", PriceComponentType.CostAdjustment, root.Overhead, "OMGEXPL-OH-FEE");
            AddHeaderComponent(accumulator, "Kit third-party cost fee", PriceComponentType.Fee, root.ThirdPartyCost, "OMGEXPL-TP-FEE-COST");
            AddHeaderComponent(accumulator, "Kit third-party sell fee", PriceComponentType.Fee, root.ThirdPartySell, "OMGEXPL-TP-FEE-SELL");
        }

        decimal headerFees = accumulator.Overhead + accumulator.ThirdPartyCost + accumulator.ThirdPartySell;
        decimal totalCost = Round(accumulator.ComponentCost + headerFees);
        decimal costBasisForSell = accumulator.ThirdPartySell > 0m
            ? Round(totalCost - accumulator.ThirdPartySell)
            : totalCost;
        decimal totalSell = Round(accumulator.ComponentSell + accumulator.ThirdPartySell);

        try
        {
            return new KitRollupResult(
                new Quantity(accumulator.Quantity),
                MoneyOf(accumulator.ComponentCost),
                MoneyOf(accumulator.SelectedBaseCost),
                MoneyOf(accumulator.Rebates),
                MoneyOf(accumulator.CostAdjustments),
                MoneyOf(accumulator.Freight),
                MoneyOf(accumulator.Jit),
                MoneyOf(accumulator.VendorCostAdjustments),
                MoneyOf(accumulator.SellAdjustments),
                MoneyOf(accumulator.Overhead),
                MoneyOf(accumulator.ThirdPartyCost),
                MoneyOf(accumulator.ThirdPartySell),
                MoneyOf(totalCost),
                MoneyOf(costBasisForSell),
                MoneyOf(accumulator.ComponentSell),
                MoneyOf(totalSell),
                accumulator.Lines.ToImmutable(),
                accumulator.Components.ToImmutable());
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return Failed(accumulator, exception.Message);
        }
    }

    private static void AccumulateBucket(Accumulator accumulator, PriceComponent component, decimal extended)
    {
        if (component.Type == PriceComponentType.BaseCost)
        {
            accumulator.SelectedBaseCost += extended;
            return;
        }

        if (component.Type == PriceComponentType.Rebate)
        {
            accumulator.Rebates += extended;
            return;
        }

        if (component.Type == PriceComponentType.CostAdjustment)
        {
            accumulator.CostAdjustments += extended;
            if (component.Name.Equals("Vendor cost adjustment", StringComparison.Ordinal))
            {
                accumulator.VendorCostAdjustments += extended;
            }

            return;
        }

        if (component.Type == PriceComponentType.SellAdjustment)
        {
            accumulator.SellAdjustments += extended;
            return;
        }

        if (component.Name.Equals("Inbound freight", StringComparison.Ordinal))
        {
            accumulator.Freight += extended;
        }
        else if (IsJit(component.Name))
        {
            accumulator.Jit += extended;
        }
    }

    private static bool IsJit(string name) =>
        name.StartsWith("JIT ", StringComparison.Ordinal)
        || name is "Print label" or "Apply label" or "Extra delivery" or "Low-UOM/break-bulk";

    private static void AddHeaderComponent(
        Accumulator accumulator,
        string name,
        PriceComponentType type,
        KitFeeWindow fee,
        string source)
    {
        if (fee.Amount.Value == 0m)
        {
            return;
        }

        var provenance = new RuleProvenance(
            name,
            $"A6O011U 3100-MOVE-COSTS/{source}",
            "Kit",
            fee.EffectiveDate is { } effective
                ? new PricingDateRange(effective, fee.ExpirationDate)
                : null);
        accumulator.Components.Add(new PriceComponent(name, type, fee.Amount, provenance));
    }

    private static KitRollupResult Failed(Accumulator accumulator, string message) => new(
        new Quantity(0m),
        new Money(0m),
        new Money(0m),
        new Money(0m),
        new Money(0m),
        new Money(0m),
        new Money(0m),
        new Money(0m),
        new Money(0m),
        new Money(0m),
        new Money(0m),
        new Money(0m),
        new Money(0m),
        new Money(0m),
        new Money(0m),
        new Money(0m),
        accumulator.Lines.ToImmutable(),
        accumulator.Components.ToImmutable(),
        new ValidationPricingError(
            "KIT_ROLLUP_AMOUNT_EXCEEDED",
            message,
            "3000",
            "kitRollup"));

    private static decimal Extended(Money? value, decimal quantity) =>
        value is null ? 0m : Round(value.Value.Value * quantity);

    private static decimal Round(decimal value) =>
        CobolRoundingPolicy.RoundIntermediate(value);

    private static Money MoneyOf(decimal value) => new(Round(value));

    private sealed class Accumulator
    {
        public decimal Quantity { get; set; }
        public decimal ComponentCost { get; set; }
        public decimal SelectedBaseCost { get; set; }
        public decimal Rebates { get; set; }
        public decimal CostAdjustments { get; set; }
        public decimal Freight { get; set; }
        public decimal Jit { get; set; }
        public decimal VendorCostAdjustments { get; set; }
        public decimal SellAdjustments { get; set; }
        public decimal Overhead { get; set; }
        public decimal ThirdPartyCost { get; set; }
        public decimal ThirdPartySell { get; set; }
        public decimal ComponentSell { get; set; }
        public ImmutableArray<KitRollupLine>.Builder Lines { get; } = ImmutableArray.CreateBuilder<KitRollupLine>();
        public ImmutableArray<PriceComponent>.Builder Components { get; } = ImmutableArray.CreateBuilder<PriceComponent>();
    }
}
