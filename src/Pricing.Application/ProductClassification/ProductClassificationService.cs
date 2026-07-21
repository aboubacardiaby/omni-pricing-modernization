namespace Pricing.Application.ProductClassification;

using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

public sealed class ProductClassificationService
{
    private readonly IProductClassificationRepository repository;

    public ProductClassificationService(IProductClassificationRepository repository) =>
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));

    /// <summary>Validates and classifies a product using the A6O010U/A6O016U path.</summary>
    /// <remarks>
    /// A6O010U 1000-VALIDATE-INPUT validates vendor before product. A6O010U
    /// 0200-READ-PRODUCT-DATA delegates requirement P to A6O016U. A6O016U
    /// 2200-READ-PRODUCT-DATA preserves O (Owens kit) and S (supplier kit), and maps every
    /// other VNG02 product-type value, including spaces, to regular R.
    /// </remarks>
    public async ValueTask<ProductClassificationResult> ClassifyAsync(
        string? vendor,
        string? product,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(vendor))
        {
            return ProductClassificationResult.Failure(
                new ValidationPricingError(
                    "VENDOR_REQUIRED",
                    "61001- VENDOR# NOT PROVIDED",
                    "61001",
                    "vendor"));
        }

        if (string.IsNullOrWhiteSpace(product))
        {
            return ProductClassificationResult.Failure(
                new ValidationPricingError(
                    "PRODUCT_REQUIRED",
                    "61004- PROD# NOT PROVIDED",
                    "61004",
                    "product"));
        }

        VendorId vendorId;
        ProductId productId;
        try
        {
            vendorId = new VendorId(vendor);
            productId = new ProductId(product);
        }
        catch (ArgumentException exception)
        {
            return ProductClassificationResult.Failure(
                new ValidationPricingError(
                    "INVALID_IDENTIFIER",
                    exception.Message,
                    null,
                    exception.ParamName));
        }

        try
        {
            string? legacyType = await repository
                .FindLegacyProductTypeAsync(vendorId, productId, cancellationToken)
                .ConfigureAwait(false);

            if (legacyType is null)
            {
                return ProductClassificationResult.Failure(
                    new MissingDataPricingError(
                        "PRODUCT_NOT_FOUND",
                        "61603- PRODUCT NOT FOUND",
                        "61603",
                        "product"));
            }

            return legacyType switch
            {
                "O" => ProductClassificationResult.Success(ProductType.Kit, "O"),
                "S" => ProductClassificationResult.Success(ProductType.SupplierKit, "S"),
                _ => ProductClassificationResult.Success(ProductType.Regular, "R"),
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ProductClassificationRepositoryException exception)
        {
            return ProductClassificationResult.Failure(
                new DependencyPricingError(
                    "PRODUCT_LOOKUP_FAILED",
                    "61602- SQL ERROR IN SELCT VNG02",
                    "61602",
                    exception.IsTransient));
        }
    }
}
