namespace Pricing.Infrastructure.SqlServer;

using Pricing.Application.SellSelection;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>Reads the effective CUG31 locked-price row.</summary>
/// <remarks>COBOL: A6U01 7765-SEL-PRC-LOCK-010 and 9470-SQL-SELECT-010.</remarks>
public sealed class SqlServerPriceLockRepository(ISqlServerQueryExecutor queries) : IPriceLockRepository
{
    private static readonly SqlServerQuery Query = new(
        "price-lock.cug31",
        """
        SELECT TOP (1)
               A_LP_TOTAL_SELL AS TotalSell,
               A_LP_TOTAL_COST AS TotalCost,
               A_LP_TOT_SELL_ADJ AS TotalSellAdjustment,
               A_LP_TOT_COST_ADJ AS TotalCostAdjustment,
               A_LP_UNADJ_UNT_CST AS UnadjustedUnitCost,
               C_LP_SELL_PRC_METH AS SellMethodCode,
               P_LP_SELL_PRC_PCT AS SellPercentage,
               D_LOCKED_PRICE_EFF AS EffectiveDate,
               D_LOCKED_PRICE_EXP AS ExpirationDate
        FROM dbo.CUG31
        WHERE I_ACCOUNT = @Account
          AND I_VENDOR = @Vendor
          AND I_VND_PRODUCT = @Product
          AND D_LOCKED_PRICE_EFF <= @PricingDate
          AND D_LOCKED_PRICE_EXP >= @PricingDate
        ORDER BY D_LOCKED_PRICE_EFF DESC;
        """);

    private readonly ISqlServerQueryExecutor queries = queries
        ?? throw new ArgumentNullException(nameof(queries));

    public async ValueTask<PriceLockRecord?> FindEffectiveAsync(
        PricingContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            Row? row = await queries.QuerySingleOrDefaultAsync<Row>(
                Query,
                new
                {
                    Account = context.Request.Account.Value,
                    Vendor = context.Request.Vendor.Value,
                    Product = context.Request.Product.Value,
                    PricingDate = context.Request.PricingDate.ToDateTime(TimeOnly.MinValue),
                },
                cancellationToken).ConfigureAwait(false);
            if (row is null)
            {
                return null;
            }

            var dates = new PricingDateRange(
                DateOnly.FromDateTime(row.EffectiveDate),
                DateOnly.FromDateTime(row.ExpirationDate));
            return new PriceLockRecord(
                new Money(row.TotalSell),
                new Money(row.TotalCost),
                new Money(row.TotalSellAdjustment),
                new Money(row.TotalCostAdjustment),
                new Money(row.UnadjustedUnitCost),
                row.SellMethodCode?.Trim(),
                row.SellPercentage is null ? null : new Percentage(row.SellPercentage.Value),
                dates,
                new RuleProvenance("price-lock", "CUG31", "Account", dates));
        }
        catch (SqlServerAccessException exception)
        {
            throw new SellArrangementRepositoryException(
                $"SQL Server price-lock lookup '{exception.Operation}' failed.",
                isTransient: exception.IsTransient,
                innerException: exception);
        }
    }

    private sealed class Row
    {
        public decimal TotalSell { get; init; }
        public decimal TotalCost { get; init; }
        public decimal TotalSellAdjustment { get; init; }
        public decimal TotalCostAdjustment { get; init; }
        public decimal UnadjustedUnitCost { get; init; }
        public string? SellMethodCode { get; init; }
        public decimal? SellPercentage { get; init; }
        public DateTime EffectiveDate { get; init; }
        public DateTime ExpirationDate { get; init; }
    }
}
