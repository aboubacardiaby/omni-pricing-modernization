namespace Pricing.Domain.Models;

/// <summary>Enriched facts and selections used during pricing, separate from input and output.</summary>
public sealed record PricingContext(
    PricingRequest Request,
    ProductInformation Product,
    CustomerInformation Customer,
    ContractSelection? ContractSelection,
    SellArrangementSelection? SellArrangementSelection,
    ICostSourceSelection? CostSelection = null);
