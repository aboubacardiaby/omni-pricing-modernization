namespace Pricing.Infrastructure.SqlServer;

using Pricing.Application.ProductClassification;
using Pricing.Application.ProductInformation;
using Pricing.Domain.ValueObjects;

/// <summary>
/// Reads the A6O016U product aggregate from the SQL Server compatibility views.
/// </summary>
/// <remarks>
/// COBOL: A6O016U 9015-SELECT-VNG02, 9025-SELECT-VNG06,
/// 9030-SELECT-ING01, and 9050-SELECT-VNG05.
/// </remarks>
public sealed class SqlServerProductRepository(ISqlServerQueryExecutor queries) :
    IProductClassificationRepository,
    IProductInformationRepository
{
    private static readonly SqlServerQuery ProductQuery = new(
        "product.vng02",
        """
        SELECT C_PRODUCT_TYPE AS ProductType,
               C_VND_PROD_BASE_UM AS BaseUnitOfMeasure
        FROM dbo.VNG02
        WHERE I_VENDOR = @Vendor
          AND I_VND_PRODUCT = @Product;
        """);

    private static readonly SqlServerQuery CategoryQuery = new(
        "product.vng06",
        """
        SELECT S_PROD_CATEGORY AS ProductCategory,
               D_CAT_PROD_EFFECT AS EffectiveDate,
               D_CAT_PROD_EXPIRE AS ExpirationDate
        FROM dbo.VNG06
        WHERE I_VENDOR = @Vendor
          AND I_VND_PRODUCT = @Product;
        """);

    private static readonly SqlServerQuery InventoryQuery = new(
        "product.ing01",
        """
        SELECT C_INV_CLASS AS InventoryClass,
               C_DIV_INV_PRC_LVL AS InventoryPriceLevel
        FROM dbo.ING01
        WHERE I_DIVISION = @Division
          AND I_VENDOR = @Vendor
          AND I_VND_PRODUCT = @Product;
        """);

    private static readonly SqlServerQuery AlternateUomQuery = new(
        "product.vng05",
        """
        SELECT A_VD_PRD_ALT_UMF AS ConversionFactor
        FROM dbo.VNG05
        WHERE I_VENDOR = @Vendor
          AND I_VND_PRODUCT = @Product
          AND C_VD_PRD_ALT_UM = @UnitOfMeasure;
        """);

    private readonly ISqlServerQueryExecutor queries = queries
        ?? throw new ArgumentNullException(nameof(queries));

    public async ValueTask<string?> FindLegacyProductTypeAsync(
        VendorId vendor,
        ProductId product,
        CancellationToken cancellationToken)
    {
        try
        {
            ProductRow? row = await queries.QuerySingleOrDefaultAsync<ProductRow>(
                ProductQuery,
                Key(vendor, product),
                cancellationToken).ConfigureAwait(false);
            return row?.ProductType?.Trim();
        }
        catch (SqlServerAccessException exception)
        {
            throw new ProductClassificationRepositoryException(
                "61602- SQL ERROR IN SELCT VNG02",
                exception.IsTransient,
                exception);
        }
    }

    public async ValueTask<ProductInformationData?> FindAsync(
        VendorId vendor,
        ProductId product,
        UnitOfMeasure? requestedUnitOfMeasure,
        DivisionId? division,
        CancellationToken cancellationToken)
    {
        try
        {
            object key = Key(vendor, product);
            ProductRow? productRow = await queries.QuerySingleOrDefaultAsync<ProductRow>(
                ProductQuery,
                key,
                cancellationToken).ConfigureAwait(false);
            if (productRow is null)
            {
                return null;
            }

            CategoryRow? category = await queries.QuerySingleOrDefaultAsync<CategoryRow>(
                CategoryQuery,
                key,
                cancellationToken).ConfigureAwait(false);
            InventoryRow? inventory = division is null
                ? null
                : await queries.QuerySingleOrDefaultAsync<InventoryRow>(
                    InventoryQuery,
                    new { Division = division.Value.Value, Vendor = vendor.Value, Product = product.Value },
                    cancellationToken).ConfigureAwait(false);

            string baseUom = RequiredCode(productRow.BaseUnitOfMeasure, "VNG02.C_VND_PROD_BASE_UM");
            decimal? conversionFactor = null;
            bool alternateFound = true;
            if (requestedUnitOfMeasure is { } requested &&
                !StringComparer.Ordinal.Equals(requested.Value, baseUom))
            {
                AlternateUomRow? alternate = await queries.QuerySingleOrDefaultAsync<AlternateUomRow>(
                    AlternateUomQuery,
                    new
                    {
                        Vendor = vendor.Value,
                        Product = product.Value,
                        UnitOfMeasure = requested.Value,
                    },
                    cancellationToken).ConfigureAwait(false);
                alternateFound = alternate is not null;
                conversionFactor = alternate?.ConversionFactor;
            }

            return new ProductInformationData(
                productRow.ProductType?.Trim() ?? string.Empty,
                new UnitOfMeasure(baseUom),
                category?.ProductCategory,
                ToDateOnly(category?.EffectiveDate),
                ToDateOnly(category?.ExpirationDate),
                inventory?.InventoryClass?.Trim(),
                conversionFactor,
                ProductCategoryFound: category is not null,
                AlternateUnitOfMeasureFound: alternateFound,
                InventoryPriceLevel: inventory?.InventoryPriceLevel?.Trim());
        }
        catch (SqlServerAccessException exception)
        {
            throw new ProductInformationRepositoryException(
                "61602",
                $"SQL Server product lookup '{exception.Operation}' failed.",
                exception.IsTransient,
                exception);
        }
    }

    private static object Key(VendorId vendor, ProductId product) =>
        new { Vendor = vendor.Value, Product = product.Value };

    private static string RequiredCode(string? value, string field) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ProductInformationRepositoryException("61603", $"Required field {field} was empty.")
            : value.Trim();

    private static DateOnly? ToDateOnly(DateTime? value) =>
        value is null ? null : DateOnly.FromDateTime(value.Value);

    private sealed class ProductRow
    {
        public string? ProductType { get; init; }
        public string? BaseUnitOfMeasure { get; init; }
    }

    private sealed class CategoryRow
    {
        public int? ProductCategory { get; init; }
        public DateTime? EffectiveDate { get; init; }
        public DateTime? ExpirationDate { get; init; }
    }

    private sealed class InventoryRow
    {
        public string? InventoryClass { get; init; }
        public string? InventoryPriceLevel { get; init; }
    }

    private sealed class AlternateUomRow
    {
        public decimal ConversionFactor { get; init; }
    }
}
