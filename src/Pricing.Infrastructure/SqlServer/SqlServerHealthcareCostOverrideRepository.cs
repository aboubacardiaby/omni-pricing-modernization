namespace Pricing.Infrastructure.SqlServer;

using System.Collections.Immutable;
using Pricing.Application.CostSelection;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>SQL Server adapter for A6U01 9945-9970 healthcare cost and sell override reads.</summary>
public sealed class SqlServerHealthcareCostOverrideRepository(ISqlServerQueryExecutor executor)
    : IHealthcareCostOverrideRepository
{
    private static readonly SqlServerQuery EligibilityQuery = new("cost.healthcare-eligibility", """
        SELECT TOP (1) * FROM (
          SELECT a.HC_GROUP_ID AS HealthcareGroupId, 'ACCOUNT' AS Scope,
            h.HC_GROUP_HDR_START_DATE AS HeaderEffective, h.HC_GROUP_HDR_EXPIRE_DATE AS HeaderExpiration,
            a.HC_START_DATE AS AssignmentEffective, a.HC_EXPIRE_DATE AS AssignmentExpiration,
            d.HC_GROUP_OVRD_START_DATE AS DetailEffective, d.HC_GROUP_OVRD_EXPIRE_DATE AS DetailExpiration,
            p.HC_PROD_CST_START_DATE AS ProductEffective, p.HC_PROD_CST_EXPIRE_DATE AS ProductExpiration,
            d.HC_GROUP_OVRD_FLAG AS GroupOverride, p.HC_PROD_OVRD_FLAG AS ProductOverride,
            p.HC_PROD_SELL_START_DATE AS SellEffective, p.HC_PROD_SELL_EXPIRE_DATE AS SellExpiration,
            p.HC_PROD_OVRD_TYPE AS SellType, p.HC_PROD_OVRD_PERCENT AS SellPercentage,
            p.HC_PROD_OVRD_SELL_PRC AS StatedSellPrice, RTRIM(p.HC_PROD_SELL_UM) AS SellUom,
            h.HC_GROUP_HDR_COMMENT AS Comment, 1 AS ScopeOrder
          FROM dbo.HC_OVRD_GROUP_HEADER h
          JOIN dbo.HC_OVRD_GROUP_ACCOUNT a ON a.HC_GROUP_ID=h.HC_GROUP_ID
          JOIN dbo.HC_OVRD_GROUP_DETAIL d ON d.HC_GROUP_ID=a.HC_GROUP_ID
          JOIN dbo.HC_OVRD_PRODUCT p ON p.HC_GROUP_ID=a.HC_GROUP_ID
          WHERE a.HC_DIVISION=@Division AND a.HC_ACCOUNT=@Account AND p.HC_VEND_NBR=@Vendor AND p.HC_PROD_NBR=@Product
          UNION ALL
          SELECT a.HC_GROUP_ID, 'CUSTOMER', h.HC_GROUP_HDR_START_DATE, h.HC_GROUP_HDR_EXPIRE_DATE,
            a.HC_START_DATE, a.HC_EXPIRE_DATE, d.HC_GROUP_OVRD_START_DATE, d.HC_GROUP_OVRD_EXPIRE_DATE,
            p.HC_PROD_CST_START_DATE, p.HC_PROD_CST_EXPIRE_DATE, d.HC_GROUP_OVRD_FLAG, p.HC_PROD_OVRD_FLAG,
            p.HC_PROD_SELL_START_DATE, p.HC_PROD_SELL_EXPIRE_DATE, p.HC_PROD_OVRD_TYPE,
            p.HC_PROD_OVRD_PERCENT, p.HC_PROD_OVRD_SELL_PRC, RTRIM(p.HC_PROD_SELL_UM), h.HC_GROUP_HDR_COMMENT, 2
          FROM dbo.HC_OVRD_GROUP_HEADER h
          JOIN dbo.HC_OVRD_GROUP_CUSTOMER a ON a.HC_GROUP_ID=h.HC_GROUP_ID
          JOIN dbo.HC_OVRD_GROUP_DETAIL d ON d.HC_GROUP_ID=a.HC_GROUP_ID
          JOIN dbo.HC_OVRD_PRODUCT p ON p.HC_GROUP_ID=a.HC_GROUP_ID
          WHERE a.HC_CUSTOMER_NBR=@Customer AND p.HC_VEND_NBR=@Vendor AND p.HC_PROD_NBR=@Product
        ) q
        WHERE HeaderEffective<=@PricingDate AND (HeaderExpiration>=@PricingDate OR HeaderExpiration IS NULL)
          AND AssignmentEffective<=@PricingDate AND (AssignmentExpiration>=@PricingDate OR AssignmentExpiration IS NULL)
          AND DetailEffective<=@PricingDate AND (DetailExpiration>=@PricingDate OR DetailExpiration IS NULL)
          AND ProductEffective<=@PricingDate AND (ProductExpiration>=@PricingDate OR ProductExpiration IS NULL)
        ORDER BY ScopeOrder
        """);
    private static readonly SqlServerQuery PriceQuery = new("cost.healthcare-price-list", """
        SELECT RTRIM(C_VND_PRC_UM) AS UnitOfMeasure, D_VND_PRC_LIST_EFF AS ListEffectiveDate,
          D_VND_PRC_ACTIVE AS ActiveDate, D_VND_PRC_EXPIRE AS ExpirationDate,
          A_VND_PRC_DEALER AS DealerCost, A_VND_PRC_ACQ_COST AS AcquisitionCost
        FROM dbo.VNG03 WHERE I_VENDOR=@Vendor AND I_VND_PRODUCT=@Product AND D_VND_PRC_ACTIVE<=@PricingDate
        ORDER BY A_VND_PRC_ACQ_COST DESC, D_VND_PRC_ACTIVE ASC
        """);

    public async ValueTask<HealthcareOverrideData> FindAsync(PricingContext context, CancellationToken cancellationToken)
    {
        try
        {
            EligibilityRow? row = await executor.QuerySingleOrDefaultAsync<EligibilityRow>(
                EligibilityQuery, SqlServerCostMapping.Parameters(context), cancellationToken).ConfigureAwait(false);
            IReadOnlyList<PriceRow> prices = row is null
                ? []
                : await executor.QueryAsync<PriceRow>(PriceQuery, SqlServerCostMapping.Parameters(context), cancellationToken).ConfigureAwait(false);
            HealthcareCostEligibility? eligibility = row is null ? null : MapEligibility(row);
            HealthcareSellOverrideTerms? sell = row is null ? null : MapSell(row);
            return new HealthcareOverrideData(
                row?.Scope == "ACCOUNT" ? eligibility : null,
                row?.Scope == "CUSTOMER" ? eligibility : null,
                row?.Scope == "ACCOUNT" ? sell : null,
                row?.Scope == "CUSTOMER" ? sell : null,
                prices.Select(MapPrice).ToImmutableArray());
        }
        catch (SqlServerAccessException exception)
        {
            throw new HealthcareCostOverrideRepositoryException("Healthcare override SQL lookup failed.", "949", "90", exception.IsTransient, exception);
        }
    }

    private static HealthcareCostEligibility MapEligibility(EligibilityRow row)
    {
        HealthcareOverrideScope scope = row.Scope == "ACCOUNT" ? HealthcareOverrideScope.Account : HealthcareOverrideScope.Customer;
        return new HealthcareCostEligibility(row.HealthcareGroupId, scope,
            SqlServerCostMapping.Range(row.HeaderEffective, row.HeaderExpiration),
            SqlServerCostMapping.Range(row.AssignmentEffective, row.AssignmentExpiration),
            SqlServerCostMapping.Range(row.DetailEffective, row.DetailExpiration),
            SqlServerCostMapping.Range(row.ProductEffective, row.ProductExpiration),
            SqlServerCostMapping.Yes(row.GroupOverride), row.ProductOverride is null ? null : SqlServerCostMapping.Yes(row.ProductOverride),
            scope == HealthcareOverrideScope.Account ? "HC_OVRD_GROUP_ACCOUNT" : "HC_OVRD_GROUP_CUSTOMER");
    }

    private static HealthcareSellOverrideTerms? MapSell(EligibilityRow row)
    {
        if (row.SellEffective is null || string.IsNullOrWhiteSpace(row.SellUom)) return null;
        PricingDateRange dates = SqlServerCostMapping.Range(row.SellEffective.Value, row.SellExpiration);
        return row.SellType?.Trim() switch
        {
            "C" or "P" when row.SellPercentage is not null => new HealthcareSellOverrideTerms(
                HealthcareSellOverrideType.CostPlus, row.SellPercentage, null, new UnitOfMeasure(row.SellUom), dates,
                "HC_OVRD_PRODUCT", row.HealthcareGroupId, row.Scope, row.Comment),
            "S" when row.StatedSellPrice is not null => new HealthcareSellOverrideTerms(
                HealthcareSellOverrideType.StatedPrice, null, new Money(row.StatedSellPrice.Value), new UnitOfMeasure(row.SellUom), dates,
                "HC_OVRD_PRODUCT", row.HealthcareGroupId, row.Scope, row.Comment),
            _ => null,
        };
    }

    private static HealthcarePriceListCandidate MapPrice(PriceRow row)
    {
        PricingDateRange dates = new(SqlServerCostMapping.Date(row.ActiveDate), SqlServerCostMapping.Date(row.ExpirationDate));
        return new HealthcarePriceListCandidate(new Money(row.AcquisitionCost), new Money(row.DealerCost),
            new UnitOfMeasure(row.UnitOfMeasure), SqlServerCostMapping.Date(row.ListEffectiveDate),
            SqlServerCostMapping.Date(row.ActiveDate), SqlServerCostMapping.Date(row.ExpirationDate),
            new RuleProvenance("healthcare-cost-override", "VNG03", "HEALTHCARE", dates));
    }

    private sealed record EligibilityRow(int HealthcareGroupId, string Scope,
        DateTime HeaderEffective, DateTime? HeaderExpiration, DateTime AssignmentEffective, DateTime? AssignmentExpiration,
        DateTime DetailEffective, DateTime? DetailExpiration, DateTime ProductEffective, DateTime? ProductExpiration,
        string? GroupOverride, string? ProductOverride, DateTime? SellEffective, DateTime? SellExpiration,
        string? SellType, decimal? SellPercentage, decimal? StatedSellPrice, string? SellUom, string? Comment);
    private sealed record PriceRow(string UnitOfMeasure, DateTime ListEffectiveDate, DateTime ActiveDate,
        DateTime? ExpirationDate, decimal DealerCost, decimal AcquisitionCost);
}
