namespace Pricing.Infrastructure.SqlServer;

using System.Collections.Immutable;
using Pricing.Application.Legacy;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>VNG02/VNG01/ING01/VNG03/INSHED/INSBRN/VNG05 adapter for the legacy PriceOperation catalog, inventory, and cost display fields.</summary>
/// <remarks>Supplementary display data only; a lookup failure here must not fail an otherwise-successful price.</remarks>
public sealed class SqlServerLegacyPricingDetailsRepository(ISqlServerQueryExecutor executor)
    : ILegacyPricingDetailsRepository
{
    private static readonly SqlServerQuery DetailsQuery = new("legacy.product-details", """
        SELECT TOP (1)
            RTRIM(p.I_VND_CATALOG_NBR) AS CatalogNumber,
            RTRIM(p.T_VND_PROD_DESC_1) AS Description,
            NULLIF(RTRIM(p.T_VND_PROD_DESC_2), '') AS ExtraDescription,
            RTRIM(p.C_VEND_VHA_PLUS) AS ItemIndicator,
            CASE WHEN i.F_STOCK_ITEM = 'Y' THEN 'N' ELSE 'Y' END AS NonStockFlag,
            RTRIM(n.N_VENDOR) AS VendorName,
            RTRIM(p.C_VND_PROD_BASE_UM) AS BaseUom,
            RTRIM(p.T_VND_PROD_UM_DESC) AS BaseUomDescription,
            RTRIM(i.C_DIV_INV_DFLT_UOM) AS BranchDefaultUom,
            RTRIM(v.C_VND_PRC_UM) AS PriceUom,
            v.A_VND_PRC_DEALER AS FileCost,
            v.A_VND_PRC_ACQ_COST AS AcquisitionCost
        FROM dbo.VNG02 p
        LEFT JOIN dbo.VNG01 n ON n.I_VENDOR=p.I_VENDOR
        LEFT JOIN dbo.ING01 i ON i.I_DIVISION=@Division AND i.I_VENDOR=p.I_VENDOR AND i.I_VND_PRODUCT=p.I_VND_PRODUCT
        LEFT JOIN dbo.VNG03 v ON v.I_VENDOR=p.I_VENDOR AND v.I_VND_PRODUCT=p.I_VND_PRODUCT
          AND v.D_VND_PRC_LIST_EFF<=@PricingDate AND v.D_VND_PRC_ACTIVE<=@PricingDate
          AND (v.D_VND_PRC_EXPIRE IS NULL OR v.D_VND_PRC_EXPIRE>=@PricingDate)
        WHERE p.I_VENDOR=@Vendor AND p.I_VND_PRODUCT=@Product
        ORDER BY v.D_VND_PRC_LIST_EFF DESC, v.C_VND_PRC_LEVEL DESC
        """);

    private static readonly SqlServerQuery InventoryQuery = new("legacy.branch-inventory", """
        SELECT TOP (1)
            b.BAL_ON_HAND - b.QTY_COMMITTED AS QuantityAvailable,
            b.QTY_ON_ORDER AS QuantityOnOrder,
            b.RES_QTY AS QuantityReserved,
            b.QTY_ALLOCATED AS CustomerQuantityReserved
        FROM dbo.INSHED h
        INNER JOIN dbo.INSBRN b ON b.KY_COMPANY=h.COMPANY AND b.KY_VENDOR_NBR=h.VENDOR_NBR AND b.KY_SEQ_NUM=h.SEQ_NUM
        WHERE h.COMPANY='OM' AND h.VENDOR_NBR=@Vendor AND h.PRODUCT_NBR=@Product AND b.BR_NBR=@Division
        """);

    private static readonly SqlServerQuery AlternateQuery = new("legacy.vng05-alternate-uoms", """
        SELECT RTRIM(C_VD_PRD_ALT_UM) AS UnitOfMeasure, A_VD_PRD_ALT_UMF AS Factor
        FROM dbo.VNG05
        WHERE I_VENDOR=@Vendor AND I_VND_PRODUCT=@Product
        ORDER BY C_VD_PRD_ALT_UM
        """);

    public async ValueTask<LegacyPricingDetails?> FindAsync(
        DivisionId division, VendorId vendor, ProductId product, DateOnly pricingDate,
        Money? cost, Money? sellPrice, CancellationToken cancellationToken)
    {
        var parameters = new
        {
            Division = division.Value,
            Vendor = vendor.Value,
            Product = product.Value,
            PricingDate = pricingDate.ToDateTime(TimeOnly.MinValue),
        };
        try
        {
            IReadOnlyList<DetailsRow> detailRows = await executor
                .QueryAsync<DetailsRow>(DetailsQuery, parameters, cancellationToken).ConfigureAwait(false);
            if (detailRows.Count == 0)
            {
                return null;
            }
            DetailsRow row = detailRows[0];

            InventoryRow? inventory = null;
            try
            {
                IReadOnlyList<InventoryRow> inventoryRows = await executor
                    .QueryAsync<InventoryRow>(InventoryQuery, parameters, cancellationToken).ConfigureAwait(false);
                inventory = inventoryRows.Count > 0 ? inventoryRows[0] : null;
            }
            catch (SqlServerAccessException)
            {
                // Branch inventory is optional; COBOL pricing remains valid when it is unavailable.
            }

            IReadOnlyList<AlternateRow> alternateRows = await executor
                .QueryAsync<AlternateRow>(AlternateQuery, parameters, cancellationToken).ConfigureAwait(false);
            ImmutableArray<LegacyAlternateUomDetails> alternates = cost is null || sellPrice is null
                ? []
                : alternateRows.Select(a => new LegacyAlternateUomDetails(
                    string.Equals(a.UnitOfMeasure, row.BranchDefaultUom, StringComparison.Ordinal),
                    a.UnitOfMeasure, a.Factor,
                    new Money(decimal.Round(sellPrice.Value.Value * a.Factor, Money.MaximumScale, MidpointRounding.AwayFromZero)),
                    new Money(decimal.Round(cost.Value.Value * a.Factor, Money.MaximumScale, MidpointRounding.AwayFromZero))))
                    .ToImmutableArray();

            return new LegacyPricingDetails(
                row.CatalogNumber, row.Description, row.ExtraDescription, row.ItemIndicator, row.NonStockFlag,
                row.VendorName, null, null, null, row.BranchDefaultUom,
                string.Equals(row.BranchDefaultUom, row.BaseUom, StringComparison.Ordinal) ? "Y" : null,
                inventory?.QuantityAvailable, inventory?.QuantityOnOrder, inventory?.QuantityReserved, inventory?.CustomerQuantityReserved,
                row.BaseUom, row.BaseUomDescription,
                row.FileCost is { } fileCost ? new Money(fileCost) : null,
                row.AcquisitionCost is { } acquisitionCost ? new Money(acquisitionCost) : null,
                row.PriceUom, alternates);
        }
        catch (SqlServerAccessException)
        {
            return null;
        }
    }

    private sealed record DetailsRow(string? CatalogNumber, string? Description, string? ExtraDescription,
        string? ItemIndicator, string? NonStockFlag, string? VendorName, string? BaseUom, string? BaseUomDescription,
        string? BranchDefaultUom, string? PriceUom, decimal? FileCost, decimal? AcquisitionCost);
    private sealed record InventoryRow(int QuantityAvailable, int QuantityOnOrder, int QuantityReserved, int CustomerQuantityReserved);
    private sealed record AlternateRow(string UnitOfMeasure, decimal Factor);
}
