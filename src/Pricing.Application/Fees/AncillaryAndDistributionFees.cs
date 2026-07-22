namespace Pricing.Application.Fees;

using System.Collections.Immutable;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

public sealed record AncillaryJitFeeInput(
    JitFeeResult Jit,
    RuleProvenance LabelProvenance,
    RuleProvenance ApplicationProvenance,
    RuleProvenance ExtraDeliveryProvenance);

public sealed record AncillaryJitFeeResult(
    Money Label,
    Money Application,
    Money ExtraDelivery,
    ImmutableArray<PriceComponent> Components);

/// <summary>
/// Exposes the A6U01 7225 label/application/extra-delivery outputs as distinct explainable fees.
/// Label/application billing frequency is intentionally ignored because A6U01 never reads it.
/// </summary>
public static class AncillaryJitFeeComposer
{
    public static AncillaryJitFeeResult Compose(AncillaryJitFeeInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var components = ImmutableArray.CreateBuilder<PriceComponent>();
        Add("Print label", input.Jit.LabelFee, input.LabelProvenance);
        Add("Apply label", input.Jit.ApplyLabelFee, input.ApplicationProvenance);
        Add("Extra delivery", input.Jit.ExtraDeliveryFee, input.ExtraDeliveryProvenance);
        return new(input.Jit.LabelFee, input.Jit.ApplyLabelFee, input.Jit.ExtraDeliveryFee, components.ToImmutable());

        void Add(string name, Money amount, RuleProvenance provenance)
        {
            if (amount.Value != 0m)
                components.Add(new PriceComponent(name, PriceComponentType.Fee, amount, provenance));
        }
    }
}

public enum DistributionCategory
{
    GroupSanctioned,
    GroupNonSanctioned,
    Individual,
    NonContract,
    Customer,
}

public sealed record DistributionFeeInput(
    DistributionCategory Category,
    Money SellPrice,
    Money TotalCost,
    string? BillingFrequency,
    RuleProvenance Provenance);

public sealed record DistributionFeeResult(
    DistributionCategory Category,
    Money Amount,
    Money LineSellPrice,
    bool BilledSeparately,
    PriceComponent Component);

/// <summary>
/// Implements A6U01 7095 distribution amount and monthly reversal after T044 classifies the bucket.
/// </summary>
public static class DistributionFeeEngine
{
    public static DistributionFeeResult Calculate(DistributionFeeInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        Money amount = new(decimal.Round(input.SellPrice.Value - input.TotalCost.Value, Money.MaximumScale, MidpointRounding.AwayFromZero));
        bool monthly = input.BillingFrequency is "MA" or "MM";
        Money lineSell = monthly ? new Money(input.SellPrice.Value - amount.Value) : input.SellPrice;
        var component = new PriceComponent($"Distribution {Code(input.Category)}", PriceComponentType.Markup, amount, input.Provenance);
        return new(input.Category, amount, lineSell, monthly, component);
    }

    public static string Code(DistributionCategory category) => category switch
    {
        DistributionCategory.GroupSanctioned => "GS",
        DistributionCategory.GroupNonSanctioned => "GN",
        DistributionCategory.Individual => "MI",
        DistributionCategory.NonContract => "MN",
        DistributionCategory.Customer => "MC",
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown distribution category."),
    };
}
