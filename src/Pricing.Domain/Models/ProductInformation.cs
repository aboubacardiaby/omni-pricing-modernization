namespace Pricing.Domain.Models;

using Pricing.Domain.ValueObjects;

/// <summary>Product facts used to build pricing context; it contains no selection behavior.</summary>
public sealed record ProductInformation(
    VendorId Vendor,
    ProductId Product,
    ProductType ProductType,
    UnitOfMeasure BaseUnitOfMeasure,
    UnitOfMeasure? AlternateUnitOfMeasure,
    decimal? AlternateConversionFactor,
    int? ProductCategory,
    string? InventoryClass,
    DateOnly? ProductCategoryEffectiveDate = null,
    DateOnly? ProductCategoryExpirationDate = null,
    string? InventoryPriceLevel = null);

public enum ProductType
{
    Regular,
    Kit,
    SupplierKit,
}
