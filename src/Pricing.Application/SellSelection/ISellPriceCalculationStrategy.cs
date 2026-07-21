namespace Pricing.Application.SellSelection;

using System.Collections.Immutable;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>Contract for one A6U01 7090 sell-method calculation.</summary>
public interface ISellPriceCalculationStrategy
{
    string MethodCode { get; }

    ValueTask<SellPriceCalculationResult> CalculateAsync(
        SellPriceCalculationInput input,
        CancellationToken cancellationToken);
}

/// <summary>Confirmed cost and reference-price inputs used by A6U01 7090 strategies.</summary>
public sealed record SellCalculationBasis(
    Money TotalCost,
    Money DealerCost,
    Money BestQuantityPrice,
    Money HospitalListPrice,
    Money DoctorListPrice,
    Money SuggestedSellPrice,
    UnitOfMeasure UnitOfMeasure);

public sealed record SellPriceCalculationInput(
    PricingContext Context,
    SellArrangementSelection? Arrangement,
    SellCalculationBasis Basis);

public sealed record SellPriceCalculationResult(
    Money SellPrice,
    string MethodCode,
    decimal? AppliedPercentage,
    ImmutableArray<PriceComponent> Components,
    RuleProvenance Provenance,
    PricingError? Error = null)
{
    public bool IsFailure => Error is not null;
}
