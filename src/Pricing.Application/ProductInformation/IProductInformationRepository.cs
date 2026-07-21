namespace Pricing.Application.ProductInformation;

using Pricing.Domain.ValueObjects;

public interface IProductInformationRepository
{
    /// <summary>Loads the A6O016U product/category/inventory/UOM aggregate.</summary>
    /// <remarks>
    /// COBOL: A6O016U 9015-SELECT-VNG02, 9025-SELECT-VNG06, 9030-SELECT-ING01,
    /// and 9050-SELECT-VNG05. Implementations must retain the distinct not-found semantics.
    /// </remarks>
    ValueTask<ProductInformationData?> FindAsync(
        VendorId vendor,
        ProductId product,
        UnitOfMeasure? requestedUnitOfMeasure,
        DivisionId? division,
        CancellationToken cancellationToken);
}

public sealed record ProductInformationData(
    string LegacyProductTypeCode,
    UnitOfMeasure BaseUnitOfMeasure,
    int? ProductCategory,
    DateOnly? ProductCategoryEffectiveDate,
    DateOnly? ProductCategoryExpirationDate,
    string? InventoryClass,
    decimal? AlternateConversionFactor,
    bool ProductCategoryFound = true,
    bool AlternateUnitOfMeasureFound = true);

public sealed class ProductInformationRepositoryException : Exception
{
    public ProductInformationRepositoryException(
        string legacyErrorCode,
        string message,
        bool isTransient = false,
        Exception? innerException = null)
        : base(message, innerException)
    {
        LegacyErrorCode = legacyErrorCode;
        IsTransient = isTransient;
    }

    public string LegacyErrorCode { get; }
    public bool IsTransient { get; }
}
