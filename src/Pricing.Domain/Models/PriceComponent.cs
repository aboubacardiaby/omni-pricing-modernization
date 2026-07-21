namespace Pricing.Domain.Models;

using Pricing.Domain.ValueObjects;

/// <summary>An itemized amount contributing to the explainable pricing result.</summary>
public sealed record PriceComponent(
    string Name,
    PriceComponentType Type,
    Money Amount,
    RuleProvenance Provenance);

public enum PriceComponentType
{
    BaseCost,
    Rebate,
    CostAdjustment,
    BaseSell,
    SellAdjustment,
    Fee,
    Surcharge,
    Markup
}
