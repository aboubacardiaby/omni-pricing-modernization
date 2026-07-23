namespace Pricing.Domain.Models;

using System.Collections.Immutable;
using Pricing.Domain.ValueObjects;

/// <summary>Explainable pricing output, separate from request and processing context.</summary>
public sealed record PricingResult(
    ProductType ProductType,
    Money? Cost,
    Money? SellPrice,
    DateOnly? ExpirationDate,
    ContractSelection? ContractSelection,
    SellArrangementSelection? SellArrangementSelection,
    ImmutableArray<PriceComponent> Components,
    ImmutableArray<RuleProvenance> Provenance,
    ImmutableArray<PricingWarning> Warnings,
    ImmutableArray<PricingError> Errors,
    LegacyPricingDetails? LegacyDetails = null);

public sealed record PricingWarning(string Code, string Message);
