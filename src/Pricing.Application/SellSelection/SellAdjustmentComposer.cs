namespace Pricing.Application.SellSelection;

using System.Collections.Immutable;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>Named outputs populated by the A6U01 7195 adjustment lookup/calculation paragraphs.</summary>
public sealed record SellAdjustmentSet(
    PriceComponent? InventoryClass = null,
    PriceComponent? RiskPremium = null,
    PriceComponent? Delivery = null,
    PriceComponent? FinanceCharge = null,
    PriceComponent? NonContract = null,
    PriceComponent? PrepayDeduction = null,
    PriceComponent? JitSell = null,
    PriceComponent? JitCost = null,
    PriceComponent? Pandac = null,
    PriceComponent? SurgiTrak = null);

public sealed record SellAdjustmentInput(
    SellPriceCalculationResult BaseCalculation,
    SellAdjustmentSet Adjustments,
    string JitServiceFeeCode,
    bool PandacBilledMonthly = false,
    bool SurgiTrakBilledMonthly = false);

public sealed record SellAdjustmentCompositionResult(
    Money BaseSell,
    Money TotalAdjustment,
    Money TotalSell,
    ImmutableArray<PriceComponent> Components,
    PricingError? Error = null)
{
    public bool IsFailure => Error is not null;
}

/// <summary>
/// Composes the A6U01 7195 sell-adjustment buckets after price-lock reconciliation.
/// Lookup/formula engines supply the named buckets; this class preserves COBOL fold-in order.
/// </summary>
public static class SellAdjustmentComposer
{
    public static SellAdjustmentCompositionResult Compose(SellAdjustmentInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        Validate(input.Adjustments);

        if (input.BaseCalculation.Error is not null)
        {
            return new(
                input.BaseCalculation.SellPrice,
                new Money(0m),
                input.BaseCalculation.SellPrice,
                input.BaseCalculation.Components,
                input.BaseCalculation.Error);
        }

        var included = ImmutableArray.CreateBuilder<PriceComponent>();
        included.AddRange(input.BaseCalculation.Components);
        decimal adjustment = 0m;

        Add(input.Adjustments.InventoryClass);
        Add(input.Adjustments.RiskPremium);
        Add(input.Adjustments.Delivery);
        Add(input.Adjustments.FinanceCharge);
        Add(input.Adjustments.NonContract);
        AddDeduction(input.Adjustments.PrepayDeduction);

        // A6U01 7195 excludes both JIT buckets for R/A; other modes fold both into sell.
        if (!StringComparer.Ordinal.Equals(input.JitServiceFeeCode, "R") &&
            !StringComparer.Ordinal.Equals(input.JitServiceFeeCode, "A"))
        {
            Add(input.Adjustments.JitSell);
            Add(input.Adjustments.JitCost);
        }

        adjustment = Round(adjustment);
        if (!input.PandacBilledMonthly)
        {
            Add(input.Adjustments.Pandac);
            adjustment = Round(adjustment);
        }

        if (!input.SurgiTrakBilledMonthly)
        {
            Add(input.Adjustments.SurgiTrak);
            adjustment = Round(adjustment);
        }

        Money totalAdjustment = new(adjustment);
        Money totalSell = new(Round(input.BaseCalculation.SellPrice.Value + adjustment));
        return new(input.BaseCalculation.SellPrice, totalAdjustment, totalSell, included.ToImmutable());

        void Add(PriceComponent? component)
        {
            if (component is null)
            {
                return;
            }

            included.Add(component);
            adjustment += component.Amount.Value;
        }

        void AddDeduction(PriceComponent? component)
        {
            if (component is null)
            {
                return;
            }

            Money deduction = new(-component.Amount.Value);
            included.Add(component with { Amount = deduction });
            adjustment += deduction.Value;
        }
    }

    private static decimal Round(decimal value) =>
        decimal.Round(value, Money.MaximumScale, MidpointRounding.AwayFromZero);

    private static void Validate(SellAdjustmentSet adjustments)
    {
        foreach (PriceComponent? component in Enumerate(adjustments))
        {
            if (component is not null && component.Type != PriceComponentType.SellAdjustment)
            {
                throw new ArgumentException($"Adjustment component '{component.Name}' must have SellAdjustment type.", nameof(adjustments));
            }
        }

        if (adjustments.PrepayDeduction?.Amount.Value < 0m)
        {
            throw new ArgumentException("Prepay deduction must be supplied as a non-negative amount.", nameof(adjustments));
        }
    }

    private static IEnumerable<PriceComponent?> Enumerate(SellAdjustmentSet value)
    {
        yield return value.InventoryClass;
        yield return value.RiskPremium;
        yield return value.Delivery;
        yield return value.FinanceCharge;
        yield return value.NonContract;
        yield return value.PrepayDeduction;
        yield return value.JitSell;
        yield return value.JitCost;
        yield return value.Pandac;
        yield return value.SurgiTrak;
    }
}
