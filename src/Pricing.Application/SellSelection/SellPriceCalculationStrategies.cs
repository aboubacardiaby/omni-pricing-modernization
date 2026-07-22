namespace Pricing.Application.SellSelection;

using System.Collections.Immutable;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>Method identifiers from A6U01 working-storage and paragraph 7090.</summary>
public static class SellPriceMethodCodes
{
    public const string GrossMargin = "1";
    public const string CostPlus = "2";
    public const string ListPrice = "3";
    public const string CostDiscount = "4";
    public const string SuggestedSell = "5";
    public const string SuggestedSellMarkup = "6";
    public const string SuggestedSellMarkdown = "7";
    public const string StatedPrice = "8";
}

/// <summary>Dispatches the selected A6U01 7090 method and applies the HC last-word override.</summary>
public sealed class SellPriceCalculationDispatcher
{
    private readonly Dictionary<string, ISellPriceCalculationStrategy> strategies;
    private readonly ListPriceCalculationStrategy listPrice;

    public SellPriceCalculationDispatcher(IEnumerable<ISellPriceCalculationStrategy> strategies)
    {
        ArgumentNullException.ThrowIfNull(strategies);
        this.strategies = strategies.ToDictionary(strategy => strategy.MethodCode, StringComparer.Ordinal);
        listPrice = this.strategies.TryGetValue(SellPriceMethodCodes.ListPrice, out ISellPriceCalculationStrategy? strategy)
            ? (strategy as ListPriceCalculationStrategy ?? throw new ArgumentException("Method 3 must use ListPriceCalculationStrategy.", nameof(strategies)))
            : throw new ArgumentException("A list-price strategy is required.", nameof(strategies));
    }

    public async ValueTask<SellPriceCalculationResult> CalculateAsync(
        string? methodCode,
        SellPriceCalculationInput input,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ISellPriceCalculationStrategy strategy = methodCode is not null && strategies.TryGetValue(methodCode, out ISellPriceCalculationStrategy? selected)
            ? selected
            : listPrice;
        SellPriceCalculationResult result = await strategy.CalculateAsync(input, cancellationToken).ConfigureAwait(false);
        return input.HealthcareOverride is null ? result : ApplyHealthcareOverride(input, input.HealthcareOverride);
    }

    private static SellPriceCalculationResult ApplyHealthcareOverride(
        SellPriceCalculationInput input,
        HealthcareSellOverrideTerms terms)
    {
        decimal percentage = terms.Percentage ?? 0m;
        decimal raw = terms.Type == HealthcareSellOverrideType.CostPlus
            ? input.Basis.TotalCost.Value * (1m + percentage)
            : terms.StatedPrice!.Value.Value;
        RuleProvenance provenance = new("Healthcare sell override", terms.Source, terms.Scope, terms.EffectiveDates);
        return SellCalculation.Create(input, raw, "NET DEL", terms.Percentage, provenance);
    }
}

public abstract class SellPriceCalculationStrategy : ISellPriceCalculationStrategy
{
    public abstract string MethodCode { get; }

    public abstract ValueTask<SellPriceCalculationResult> CalculateAsync(
        SellPriceCalculationInput input,
        CancellationToken cancellationToken);

    protected static ValueTask<SellPriceCalculationResult> Result(SellPriceCalculationResult result, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return ValueTask.FromResult(result);
    }
}

public sealed class GrossMarginCalculationStrategy : SellPriceCalculationStrategy
{
    public override string MethodCode => SellPriceMethodCodes.GrossMargin;

    public override ValueTask<SellPriceCalculationResult> CalculateAsync(SellPriceCalculationInput input, CancellationToken cancellationToken)
    {
        if (input.PercentageResolutionError is not null)
        {
            return Result(SellCalculation.Failure(input, MethodCode, input.PercentageResolutionError), cancellationToken);
        }

        decimal percentage = SellCalculation.RequiredPercentage(input).Value;
        percentage = percentage >= 1m ? 0.9999m : percentage;
        return Result(SellCalculation.Create(input, input.Basis.TotalCost.Value / (1m - percentage), "GM", percentage), cancellationToken);
    }
}

public sealed class CostPlusCalculationStrategy : SellPriceCalculationStrategy
{
    public override string MethodCode => SellPriceMethodCodes.CostPlus;

    public override ValueTask<SellPriceCalculationResult> CalculateAsync(SellPriceCalculationInput input, CancellationToken cancellationToken)
    {
        if (input.PercentageResolutionError is not null)
        {
            return Result(SellCalculation.Failure(input, MethodCode, input.PercentageResolutionError), cancellationToken);
        }

        decimal percentage = SellCalculation.RequiredPercentage(input).Value;
        return Result(SellCalculation.Create(input, input.Basis.TotalCost.Value * (1m + percentage), "C(+)", percentage), cancellationToken);
    }
}

public sealed class ListPriceCalculationStrategy : SellPriceCalculationStrategy
{
    public override string MethodCode => SellPriceMethodCodes.ListPrice;

    public override ValueTask<SellPriceCalculationResult> CalculateAsync(SellPriceCalculationInput input, CancellationToken cancellationToken) =>
        Result(SellCalculation.Create(input, SellCalculation.ListPrice(input).Value, input.Arrangement is null ? "LIST-DEF" : "LIST", 0m), cancellationToken);
}

public sealed class CostDiscountCalculationStrategy : SellPriceCalculationStrategy
{
    public override string MethodCode => SellPriceMethodCodes.CostDiscount;

    public override ValueTask<SellPriceCalculationResult> CalculateAsync(SellPriceCalculationInput input, CancellationToken cancellationToken)
    {
        decimal percentage = SellCalculation.RequiredPercentage(input).Value;
        decimal list = SellCalculation.ListPrice(input).Value;
        return Result(SellCalculation.Create(input, list * (1m - percentage), "LIST(-)", percentage), cancellationToken);
    }
}

public sealed class SuggestedSellCalculationStrategy : SellPriceCalculationStrategy
{
    public override string MethodCode => SellPriceMethodCodes.SuggestedSell;

    public override ValueTask<SellPriceCalculationResult> CalculateAsync(SellPriceCalculationInput input, CancellationToken cancellationToken) =>
        Result(SellCalculation.Suggested(input, "CSS", 0m, static (price, _) => price), cancellationToken);
}

public sealed class SuggestedSellMarkupCalculationStrategy : SellPriceCalculationStrategy
{
    public override string MethodCode => SellPriceMethodCodes.SuggestedSellMarkup;

    public override ValueTask<SellPriceCalculationResult> CalculateAsync(SellPriceCalculationInput input, CancellationToken cancellationToken) =>
        Result(SellCalculation.Suggested(input, "CSS(+)", SellCalculation.RequiredPercentage(input).Value, static (price, percentage) => price * (1m + percentage)), cancellationToken);
}

public sealed class SuggestedSellMarkdownCalculationStrategy : SellPriceCalculationStrategy
{
    public override string MethodCode => SellPriceMethodCodes.SuggestedSellMarkdown;

    public override ValueTask<SellPriceCalculationResult> CalculateAsync(SellPriceCalculationInput input, CancellationToken cancellationToken) =>
        Result(SellCalculation.Suggested(input, "CSS(-)", SellCalculation.RequiredPercentage(input).Value, static (price, percentage) => price * (1m - percentage)), cancellationToken);
}

public sealed class StatedPriceCalculationStrategy : SellPriceCalculationStrategy
{
    public override string MethodCode => SellPriceMethodCodes.StatedPrice;

    public override ValueTask<SellPriceCalculationResult> CalculateAsync(SellPriceCalculationInput input, CancellationToken cancellationToken)
    {
        if (input.StatedPrice is null || input.StatedConversionDownFactor <= 0m)
        {
            return Result(SellCalculation.Failure(input, MethodCode, new ValidationPricingError("SELL_STATED_INVALID", "A stated price and positive conversion denominator are required.", Field: "statedPrice")), cancellationToken);
        }

        decimal raw = input.StatedPrice.Value.Value * input.StatedConversionUpFactor / input.StatedConversionDownFactor;
        return Result(SellCalculation.Create(input, raw, "STATED PRC", 0m), cancellationToken);
    }
}

internal static class SellCalculation
{
    internal static Percentage RequiredPercentage(SellPriceCalculationInput input) =>
        input.Percentage ?? throw new ArgumentException("The selected sell method requires a percentage.", nameof(input));

    internal static Money ListPrice(SellPriceCalculationInput input) => input.BusinessType switch
    {
        "01" => input.Basis.BestQuantityPrice,
        "02" => input.Basis.HospitalListPrice,
        _ => input.Basis.DoctorListPrice,
    };

    internal static SellPriceCalculationResult Suggested(
        SellPriceCalculationInput input,
        string label,
        decimal percentage,
        Func<decimal, decimal, decimal> formula)
    {
        if (!input.CostContractFound || !input.CostSuggestedSellAvailable || input.Basis.SuggestedSellPrice.Value <= 0m || (label != "CSS" && percentage <= 0m))
        {
            return Create(input, ListPrice(input).Value, "LIST-DEF", 0m);
        }

        return Create(input, formula(input.Basis.SuggestedSellPrice.Value, percentage), label, percentage);
    }

    internal static SellPriceCalculationResult Create(
        SellPriceCalculationInput input,
        decimal raw,
        string label,
        decimal? percentage,
        RuleProvenance? provenance = null)
    {
        RuleProvenance source = provenance ?? input.Arrangement?.Provenance ?? new RuleProvenance("List-price default", "A6U01 7090", "DEFAULT", null);
        Money amount = new(decimal.Round(raw, Money.MaximumScale, MidpointRounding.AwayFromZero));
        return new(amount, label, percentage, ImmutableArray.Create(new PriceComponent(label, PriceComponentType.BaseSell, amount, source)), source);
    }

    internal static SellPriceCalculationResult Failure(SellPriceCalculationInput input, string methodCode, PricingError error)
    {
        RuleProvenance source = input.Arrangement?.Provenance ?? new RuleProvenance("Sell calculation", "A6U01 7090", null, null);
        return new(new Money(0m), methodCode, null, ImmutableArray<PriceComponent>.Empty, source, error);
    }
}
