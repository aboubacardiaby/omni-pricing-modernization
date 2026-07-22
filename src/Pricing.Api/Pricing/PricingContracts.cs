namespace Pricing.Api.Pricing;

using global::Pricing.Domain.Models;

public sealed record CalculatePriceRequest(
    string Division,
    string Account,
    string Vendor,
    string Product,
    decimal Quantity,
    string UnitOfMeasure,
    string? ShipTo,
    string? BillTo,
    DateOnly PricingDate,
    PricingRequestType RequestType);

public sealed record CalculatePriceResponse(
    string ProductType,
    decimal? Cost,
    decimal? SellPrice,
    DateOnly? ExpirationDate,
    string? ContractIdentifier,
    string? BuyingGroupIdentifier,
    string? RuleType,
    IReadOnlyList<PriceComponentResponse> Components,
    IReadOnlyList<RuleProvenanceResponse> Provenance,
    IReadOnlyList<PricingWarningResponse> Warnings);

public sealed record PriceComponentResponse(
    string Name,
    decimal Amount,
    RuleProvenanceResponse Provenance);

public sealed record RuleProvenanceResponse(
    string RuleName,
    string Source,
    string? HierarchyLevel,
    DateOnly? EffectiveDate,
    DateOnly? ExpirationDate);

public sealed record PricingWarningResponse(string Code, string Message);
