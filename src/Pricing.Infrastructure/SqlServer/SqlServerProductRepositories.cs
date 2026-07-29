namespace Pricing.Infrastructure.SqlServer;

using Pricing.Application.ProductClassification;
using Pricing.Application.ProductInformation;
using Pricing.Domain.ValueObjects;

/// <summary>SQL Server adapter for A6O016U 9015-SELECT-VNG02.</summary>
public sealed class SqlServerProductClassificationRepository(ISqlServerQueryExecutor executor)
    : IProductClassificationRepository
{
    private static readonly SqlServerQuery Query = new(
        "product.classify",
        "SELECT C_PRODUCT_TYPE FROM dbo.VNG02 WHERE I_VENDOR=@Vendor AND I_VND_PRODUCT=@Product");

    public async ValueTask<string?> FindLegacyProductTypeAsync(
        VendorId vendor, ProductId product, CancellationToken cancellationToken)
    {
        try
        {
            ProductTypeRow? row = await executor.QuerySingleOrDefaultAsync<ProductTypeRow>(
                Query, new { Vendor = vendor.Value, Product = product.Value }, cancellationToken).ConfigureAwait(false);
            return row?.C_PRODUCT_TYPE?.Trim();
        }
        catch (SqlServerAccessException exception)
        {
            throw new ProductClassificationRepositoryException(
                "VNG02 product classification query failed.", exception.IsTransient, exception);
        }
    }

    private sealed record ProductTypeRow(string? C_PRODUCT_TYPE);
}

/// <summary>SQL Server adapter for A6O016U 9015/9025/9030/9050.</summary>
public sealed class SqlServerProductInformationRepository(ISqlServerQueryExecutor executor)
    : IProductInformationRepository
{
    private static readonly SqlServerQuery ProductQuery = new("product.information", """
        SELECT TOP (1) p.C_PRODUCT_TYPE AS LegacyProductTypeCode,
               p.C_VND_PROD_BASE_UM AS BaseUnitOfMeasure,
               c.S_PROD_CATEGORY AS ProductCategory,
               c.D_CAT_PROD_EFFECT AS ProductCategoryEffectiveDate,
               c.D_CAT_PROD_EXPIRE AS ProductCategoryExpirationDate,
               i.C_INV_CLASS AS InventoryClass
        FROM dbo.VNG02 p
        LEFT JOIN dbo.VNG06 c ON c.I_VENDOR=p.I_VENDOR AND c.I_VND_PRODUCT=p.I_VND_PRODUCT
          AND c.D_CAT_PROD_START<=CAST(GETDATE() AS date) AND (c.D_CAT_PROD_EXPIRE>=CAST(GETDATE() AS date) OR c.D_CAT_PROD_EXPIRE IS NULL)
        LEFT JOIN dbo.ING01 i ON i.I_DIVISION=@Division AND i.I_VENDOR=p.I_VENDOR AND i.I_VND_PRODUCT=p.I_VND_PRODUCT
        WHERE p.I_VENDOR=@Vendor AND p.I_VND_PRODUCT=@Product
        ORDER BY c.D_CAT_PROD_START DESC
        """);
    private static readonly SqlServerQuery AlternateUomQuery = new("product.alternate-uom", """
        SELECT A_VD_PRD_ALT_UMF
        FROM dbo.VNG05
        WHERE I_VENDOR=@Vendor AND I_VND_PRODUCT=@Product AND C_VD_PRD_ALT_UM=@UnitOfMeasure
        """);

    public async ValueTask<ProductInformationData?> FindAsync(
        VendorId vendor, ProductId product, UnitOfMeasure? requestedUnitOfMeasure,
        DivisionId? division, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = new
            {
                Vendor = vendor.Value,
                Product = product.Value,
                Division = division?.Value,
            };
            ProductRow? row = await executor.QuerySingleOrDefaultAsync<ProductRow>(
                ProductQuery, parameters, cancellationToken).ConfigureAwait(false);
            if (row is null)
            {
                return null;
            }

            var baseUom = new UnitOfMeasure(row.BaseUnitOfMeasure.Trim());
            decimal? factor = null;
            bool alternateFound = true;
            if (requestedUnitOfMeasure is { } requested && requested != baseUom)
            {
                AlternateUomRow? alternate = await executor.QuerySingleOrDefaultAsync<AlternateUomRow>(
                    AlternateUomQuery,
                    new { Vendor = vendor.Value, Product = product.Value, UnitOfMeasure = requested.Value },
                    cancellationToken).ConfigureAwait(false);
                factor = alternate?.A_VD_PRD_ALT_UMF;
                alternateFound = alternate is not null;
            }

            return new ProductInformationData(
                row.LegacyProductTypeCode?.Trim() ?? string.Empty,
                baseUom,
                row.ProductCategory,
                ToDateOnly(row.ProductCategoryEffectiveDate),
                ToDateOnly(row.ProductCategoryExpirationDate),
                row.InventoryClass?.Trim(),
                factor,
                row.ProductCategory is not null,
                alternateFound);
        }
        catch (SqlServerAccessException exception)
        {
            string legacyCode = exception.Operation == AlternateUomQuery.Operation ? "61607" : "61604";
            throw new ProductInformationRepositoryException(
                legacyCode, $"{legacyCode}- SQL ERROR IN PRODUCT LOOKUP", exception.IsTransient, exception);
        }
    }

    private static DateOnly? ToDateOnly(DateTime? value) => value is null ? null : DateOnly.FromDateTime(value.Value);
    private sealed record ProductRow(
        string? LegacyProductTypeCode, string BaseUnitOfMeasure, int? ProductCategory,
        DateTime? ProductCategoryEffectiveDate, DateTime? ProductCategoryExpirationDate, string? InventoryClass);
    private sealed record AlternateUomRow(decimal A_VD_PRD_ALT_UMF);
}
