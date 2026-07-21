namespace Pricing.Application.ProductClassification;

using Pricing.Domain.Models;

public sealed record ProductClassificationResult(
    ProductType? ProductType,
    string? LegacyProductTypeCode,
    PricingError? Error)
{
    public bool IsSuccess => ProductType.HasValue && Error is null;

    public static ProductClassificationResult Success(ProductType productType, string legacyProductTypeCode) =>
        new(productType, legacyProductTypeCode, null);

    public static ProductClassificationResult Failure(PricingError error) =>
        new(null, null, error ?? throw new ArgumentNullException(nameof(error)));
}
