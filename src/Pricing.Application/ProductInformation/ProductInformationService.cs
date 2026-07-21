namespace Pricing.Application.ProductInformation;

using System.Collections.Immutable;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

public sealed class ProductInformationService
{
    private readonly IProductInformationRepository repository;

    public ProductInformationService(IProductInformationRepository repository) =>
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));

    /// <summary>Loads product facts using the confirmed A6O013U/A6O016U orchestration.</summary>
    /// <remarks>
    /// A6O013U 1000-VALIDATE-INPUT validates vendor then product; 2000-READ-PRODUCT-DATA
    /// delegates requirement C. A6O016U 0020-PROCESS sequences VNG02, VNG06, optional ING01,
    /// and optional VNG05, with base-UOM factor 1 handled without VNG05 access.
    /// </remarks>
    public async ValueTask<ProductInformationResult> GetAsync(
        string? vendor,
        string? product,
        string? requestedUnitOfMeasure,
        string? division,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(vendor))
        {
            return ProductInformationResult.Failure(
                new ValidationPricingError("VENDOR_REQUIRED", "61301- VENDOR# NOT PROVIDED", "61301", "vendor"));
        }

        if (string.IsNullOrWhiteSpace(product))
        {
            return ProductInformationResult.Failure(
                new ValidationPricingError("PRODUCT_REQUIRED", "61304- PROD# NOT PROVIDED", "61304", "product"));
        }

        try
        {
            var vendorId = new VendorId(vendor);
            var productId = new ProductId(product);
            UnitOfMeasure? requestedUom = string.IsNullOrWhiteSpace(requestedUnitOfMeasure)
                ? null
                : new UnitOfMeasure(requestedUnitOfMeasure);
            DivisionId? divisionId = string.IsNullOrWhiteSpace(division) ? null : new DivisionId(division);

            ProductInformationData? data = await repository
                .FindAsync(vendorId, productId, requestedUom, divisionId, cancellationToken)
                .ConfigureAwait(false);
            if (data is null)
            {
                return ProductInformationResult.Failure(
                    new MissingDataPricingError("PRODUCT_NOT_FOUND", "61603- PRODUCT NOT FOUND", "61603", "product"));
            }

            decimal? conversionFactor = null;
            if (requestedUom is { } unit)
            {
                if (unit == data.BaseUnitOfMeasure)
                {
                    conversionFactor = 1m;
                }
                else if (!data.AlternateUnitOfMeasureFound || data.AlternateConversionFactor is null)
                {
                    return ProductInformationResult.Failure(
                        new MissingDataPricingError("UOM_NOT_FOUND", "61606- UOM NOT FOUND", "61606", "unitOfMeasure"));
                }
                else
                {
                    conversionFactor = data.AlternateConversionFactor;
                }
            }

            ImmutableArray<PricingWarning> warnings = data.ProductCategoryFound
                ? []
                : [new PricingWarning("PRODUCT_CATEGORY_NOT_FOUND", "61605- PRODUCT CATEGORY NOT FOUND")];
            var information = new Pricing.Domain.Models.ProductInformation(
                vendorId,
                productId,
                MapProductType(data.LegacyProductTypeCode),
                data.BaseUnitOfMeasure,
                requestedUom is { } requested && requested != data.BaseUnitOfMeasure ? requested : null,
                conversionFactor,
                data.ProductCategory,
                divisionId is null ? null : data.InventoryClass,
                data.ProductCategoryEffectiveDate,
                data.ProductCategoryExpirationDate);
            return ProductInformationResult.Success(information, warnings);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ProductInformationRepositoryException exception)
        {
            return ProductInformationResult.Failure(
                new DependencyPricingError(
                    "PRODUCT_INFORMATION_LOOKUP_FAILED",
                    exception.Message,
                    exception.LegacyErrorCode,
                    exception.IsTransient));
        }
        catch (ArgumentException exception)
        {
            return ProductInformationResult.Failure(
                new ValidationPricingError("INVALID_PRODUCT_INFORMATION_INPUT", exception.Message, null, exception.ParamName));
        }
    }

    private static ProductType MapProductType(string legacyProductTypeCode) => legacyProductTypeCode switch
    {
        "O" => ProductType.Kit,
        "S" => ProductType.SupplierKit,
        _ => ProductType.Regular,
    };
}
