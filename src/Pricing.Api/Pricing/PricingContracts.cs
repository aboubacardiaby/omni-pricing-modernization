namespace Pricing.Api.Pricing;

using global::Pricing.Domain.Models;

public sealed record CalculatePricesRequest(
    string Division,
    string Account,
    IReadOnlyList<CalculateProductRequest> Products,
    decimal Quantity,
    string UnitOfMeasure,
    string? ShipTo,
    string? BillTo,
    DateOnly PricingDate,
    PricingRequestType RequestType);

public sealed record CalculateProductRequest(string Vendor, string Product);

public sealed record CalculatePricesResponse(IReadOnlyList<CalculateProductResponse> Results);

public sealed record CalculateProductResponse(
    string Vendor,
    string Product,
    PricingErrorResponse? Error,
    string? ProductType,
    decimal? Cost,
    decimal? SellPrice,
    DateOnly? ExpirationDate,
    string? ContractIdentifier,
    string? BuyingGroupIdentifier,
    string? RuleType,
    IReadOnlyList<PriceComponentResponse> Components,
    IReadOnlyList<RuleProvenanceResponse> Provenance,
    IReadOnlyList<PricingWarningResponse> Warnings);

public sealed record PricingErrorResponse(string Code, string Message, string? LegacyErrorCode);

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