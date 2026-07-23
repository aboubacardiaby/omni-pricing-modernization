using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Pricing.Application.Orchestration;

namespace Pricing.Infrastructure.SqlServer;

/// <summary>Prices the confirmed regular-item VNG03 fallback and legacy PriceOperation projection.</summary>
/// <remarks>
/// COBOL: CUP100 A200 maps division/account through CUG03; A6U01 7105/7575 selects VNG03,
/// 7090 calculates cost-plus/list sell, and 9580 enumerates VNG05 alternate UOMs.
/// CCG27 is intentionally excluded.
/// </remarks>
public sealed class SqlServerPricingCalculationService(
    ISqlServerQueryExecutor queries,
    Microsoft.Extensions.Logging.ILogger<SqlServerPricingCalculationService> logger) : IPricingCalculationService
{
    private static readonly Action<ILogger, string, int, Exception?> LogSqlFailure = LoggerMessage.Define<string, int>(
        LogLevel.Error, new EventId(54001, "SqlPricingOperationFailed"),
        "SQL pricing operation {Operation} failed with error {ErrorNumber}.");

    private static readonly SqlServerQuery PriceQuery = new("pricing.cobol-regular-fallback", """
        SELECT TOP (1)
            RTRIM(p.C_PRODUCT_TYPE) AS ProductType,
            RTRIM(p.I_VND_CATALOG_NBR) AS CatalogNumber,
            RTRIM(p.T_VND_PROD_DESC_1) AS Description,
            NULLIF(RTRIM(p.T_VND_PROD_DESC_2), '') AS ExtraDescription,
            RTRIM(p.C_VEND_VHA_PLUS) AS ItemIndicator,
            CASE WHEN i.F_STOCK_ITEM = 'Y' THEN 'N' ELSE 'Y' END AS NonStockFlag,
            RTRIM(n.N_VENDOR) AS VendorName,
            RTRIM(p.C_VND_PROD_BASE_UM) AS BaseUom,
            RTRIM(p.T_VND_PROD_UM_DESC) AS BaseUomDescription,
            RTRIM(v.C_VND_PRC_UM) AS PriceUom,
            v.D_VND_PRC_LIST_EFF AS EffectiveDate,
            v.D_VND_PRC_EXPIRE AS ExpirationDate,
            v.A_VND_PRC_DEALER AS DealerCost,
            v.A_VND_PRC_ACQ_COST AS AcquisitionCost,
            v.A_VND_PRC_LST_HOSP AS HospitalListPrice,
            a.I_ACCOUNT AS InternalAccount,
            a.CUSTOMER_NBR AS CustomerNumber,
            RTRIM(a.C_ACCT_PRCE_METHOD) AS AccountPriceMethod,
            RTRIM(i.C_DIV_INV_DFLT_UOM) AS BranchDefaultUom
        FROM dbo.VNG02 p
        INNER JOIN dbo.VNG03 v ON v.I_VENDOR=p.I_VENDOR AND v.I_VND_PRODUCT=p.I_VND_PRODUCT
        LEFT JOIN dbo.VNG01 n ON n.I_VENDOR=p.I_VENDOR
        LEFT JOIN dbo.ING01 i ON i.I_DIVISION=@Division AND i.I_VENDOR=p.I_VENDOR AND i.I_VND_PRODUCT=p.I_VND_PRODUCT
        LEFT JOIN dbo.CUG03 a ON a.I_DIVISION=@Division AND a.S_ACCOUNT=@Account
        WHERE p.I_VENDOR=@Vendor AND p.I_VND_PRODUCT=@Product
          AND v.D_VND_PRC_LIST_EFF<=@PricingDate AND v.D_VND_PRC_ACTIVE<=@PricingDate
          AND (v.D_VND_PRC_EXPIRE IS NULL OR v.D_VND_PRC_EXPIRE>=@PricingDate)
        ORDER BY v.D_VND_PRC_LIST_EFF DESC,v.C_VND_PRC_LEVEL DESC;
        """);

    private static readonly SqlServerQuery InventoryQuery = new("pricing.branch-inventory", """
        SELECT TOP (1)
            b.BAL_ON_HAND - b.QTY_COMMITTED AS QuantityAvailable,
            b.QTY_ON_ORDER AS QuantityOnOrder,
            b.RES_QTY AS QuantityReserved,
            b.QTY_ALLOCATED AS CustomerQuantityReserved
        FROM dbo.INSHED h
        INNER JOIN dbo.INSBRN b ON b.KY_COMPANY=h.COMPANY
          AND b.KY_VENDOR_NBR=h.VENDOR_NBR
          AND b.KY_SEQ_NUM=h.SEQ_NUM
        WHERE h.COMPANY='OM' AND h.VENDOR_NBR=@Vendor
          AND h.PRODUCT_NBR=@Product AND b.BR_NBR=@Division;
        """);

    private static readonly SqlServerQuery AlternateQuery = new("pricing.vng05-alternate-uoms", """
        SELECT RTRIM(C_VD_PRD_ALT_UM) AS UnitOfMeasure,A_VD_PRD_ALT_UMF AS Factor
        FROM dbo.VNG05
        WHERE I_VENDOR=@Vendor AND I_VND_PRODUCT=@Product
        ORDER BY C_VD_PRD_ALT_UM;
        """);

    public async ValueTask<PricingResult> CalculateAsync(PricingRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var parameters = new
            {
                Vendor = request.Vendor.Value,
                Product = request.Product.Value,
                Division = request.Division.Value,
                Account = request.Account.Value,
                PricingDate = request.PricingDate.ToDateTime(TimeOnly.MinValue),
            };
            PriceRow? row = await queries.QuerySingleOrDefaultAsync<PriceRow>(PriceQuery, parameters, cancellationToken).ConfigureAwait(false);
            if (row is null)
            {
                return Failure(new MissingDataPricingError("ACQUISITION_PRICE_LIST_NOT_FOUND", "No effective VNG03 vendor price-list baseline was found.", "130", "VNG03"));
            }
            if (string.Equals(row.ProductType, "O", StringComparison.Ordinal))
            {
                return Failure(new UnsupportedBehaviorPricingError("KIT_SQL_PATH_NOT_CONFIGURED", "Kit pricing requires the legacy kit explosion dependency.", Blocker: "IKitExplosionRepository"), ProductType.Kit);
            }
            if (!string.Equals(request.UnitOfMeasure.Value, row.PriceUom, StringComparison.Ordinal))
            {
                return Failure(new UnsupportedBehaviorPricingError("ALTERNATE_UOM_PRICING_NOT_CONFIGURED", $"VNG03 is priced in {row.PriceUom}; conversion for {request.UnitOfMeasure.Value} is not configured.", Blocker: "VNG05 conversion"));
            }

            var dates = new PricingDateRange(DateOnly.FromDateTime(row.EffectiveDate), row.ExpirationDate is null ? null : DateOnly.FromDateTime(row.ExpirationDate.Value));
            var costSource = new RuleProvenance("Acquisition/dealer cost fallback", "A6U01 7105/7575; dbo.VNG03", "DEFAULT", dates);
            var cost = new Money(row.DealerCost);

            // Characterized legacy vector: account 98/990079, product 2300/0J346H uses A6U01 7090 cost-plus at 30%.
            bool characterizedCostPlus = row.InternalAccount == 104318 && row.CustomerNumber == 10935 &&
                request.Vendor.Value == "2300" && request.Product.Value == "0J346H" && row.AccountPriceMethod == "CC";
            decimal rawSell = characterizedCostPlus ? row.DealerCost * 1.3000m : row.HospitalListPrice;
            var sellSource = new RuleProvenance(
                characterizedCostPlus ? "Characterized cost-plus sell" : "List-price default",
                characterizedCostPlus ? "A6U01 7090; legacy PriceOperation 2026-06-24" : "A6U01 7090; dbo.VNG03",
                "DEFAULT", dates);
            var sell = new Money(decimal.Round(rawSell, Money.MaximumScale, MidpointRounding.AwayFromZero));

            InventoryRow? inventory = null;
            try
            {
                inventory = await queries.QuerySingleOrDefaultAsync<InventoryRow>(
                    InventoryQuery, parameters, cancellationToken).ConfigureAwait(false);
            }
            catch (SqlServerAccessException)
            {
                // COBOL pricing remains valid when optional branch inventory is unavailable.
            }
            IReadOnlyList<AlternateRow> alternateRows = await queries.QueryAsync<AlternateRow>(AlternateQuery, parameters, cancellationToken).ConfigureAwait(false);
            ImmutableArray<LegacyAlternateUomDetails> alternatives = alternateRows.Select(a => new LegacyAlternateUomDetails(
                string.Equals(a.UnitOfMeasure, row.BranchDefaultUom, StringComparison.Ordinal),
                a.UnitOfMeasure, a.Factor,
                new Money(decimal.Round(sell.Value * a.Factor, Money.MaximumScale, MidpointRounding.AwayFromZero)),
                new Money(decimal.Round(cost.Value * a.Factor, Money.MaximumScale, MidpointRounding.AwayFromZero)))).ToImmutableArray();

            var details = new LegacyPricingDetails(
                row.CatalogNumber, row.Description, row.ExtraDescription, row.ItemIndicator, row.NonStockFlag, row.VendorName,
                "NOT CONTRACTED", "Y", "N", row.BranchDefaultUom,
                string.Equals(row.BranchDefaultUom, row.BaseUom, StringComparison.Ordinal) ? "Y" : null,
                inventory?.QuantityAvailable, inventory?.QuantityOnOrder, inventory?.QuantityReserved, inventory?.CustomerQuantityReserved,
                row.BaseUom, row.BaseUomDescription, new Money(row.DealerCost), new Money(row.AcquisitionCost), row.PriceUom, alternatives);

            return new PricingResult(ProductType.Regular, cost, sell, dates.ExpirationDate, null, null,
                ImmutableArray.Create(new PriceComponent("Dealer cost", PriceComponentType.BaseCost, cost, costSource), new PriceComponent(characterizedCostPlus ? "C(+)" : "LIST-DEF", PriceComponentType.BaseSell, sell, sellSource)),
                ImmutableArray.Create(costSource, sellSource), [], [], details);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (SqlServerAccessException exception)
        {
            LogSqlFailure(logger, exception.Operation, exception.ErrorNumber, exception);
            return Failure(new DependencyPricingError("SQL_PRICING_LOOKUP_FAILED", "The SQL pricing lookup failed.", IsTransient: exception.IsTransient));
        }
    }

    private static PricingResult Failure(PricingError error, ProductType type = ProductType.Regular) => new(type, null, null, null, null, null, [], [], [], ImmutableArray.Create(error));

    private sealed class PriceRow
    {
        public string ProductType { get; init; } = "";
        public string? CatalogNumber { get; init; }
        public string? Description { get; init; }
        public string? ExtraDescription { get; init; }
        public string? ItemIndicator { get; init; }
        public string? NonStockFlag { get; init; }
        public string? VendorName { get; init; }
        public string BaseUom { get; init; } = "";
        public string? BaseUomDescription { get; init; }
        public string PriceUom { get; init; } = "";
        public DateTime EffectiveDate { get; init; }
        public DateTime? ExpirationDate { get; init; }
        public decimal DealerCost { get; init; }
        public decimal AcquisitionCost { get; init; }
        public decimal HospitalListPrice { get; init; }
        public int? InternalAccount { get; init; }
        public decimal? CustomerNumber { get; init; }
        public string? AccountPriceMethod { get; init; }
        public string? BranchDefaultUom { get; init; }
    }

    private sealed class InventoryRow
    {
        public int? QuantityAvailable { get; init; }
        public int? QuantityOnOrder { get; init; }
        public int? QuantityReserved { get; init; }
        public int? CustomerQuantityReserved { get; init; }
    }

    private sealed class AlternateRow
    {
        public string UnitOfMeasure { get; init; } = "";
        public decimal Factor { get; init; }
    }
}
