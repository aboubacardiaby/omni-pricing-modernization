namespace Pricing.Infrastructure.SqlServer;

using Pricing.Application.Rounding;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>A6U01 7080/7110 conversion factors sourced from VNG02/VNG05.</summary>
internal static class SqlServerCostUomConverter
{
    private static readonly SqlServerQuery Query = new("cost.uom-factors", """
        SELECT RTRIM(C_VND_PROD_BASE_UM) AS UnitOfMeasure, CAST(1 AS decimal(15,8)) AS Factor
        FROM dbo.VNG02 WHERE I_VENDOR=@Vendor AND I_VND_PRODUCT=@Product
        UNION ALL
        SELECT RTRIM(C_VD_PRD_ALT_UM), A_VD_PRD_ALT_UMF
        FROM dbo.VNG05 WHERE I_VENDOR=@Vendor AND I_VND_PRODUCT=@Product
        """);

    internal static async ValueTask<IReadOnlyDictionary<string, decimal>> LoadAsync(
        ISqlServerQueryExecutor executor, PricingContext context, CancellationToken cancellationToken)
    {
        IReadOnlyList<Row> rows = await executor.QueryAsync<Row>(
            Query, SqlServerCostMapping.Parameters(context), cancellationToken).ConfigureAwait(false);
        return rows.GroupBy(x => x.UnitOfMeasure.Trim(), StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.First().Factor, StringComparer.Ordinal);
    }

    internal static bool TryNormalize(
        decimal amount, string sourceUom, UnitOfMeasure targetUom,
        IReadOnlyDictionary<string, decimal> factors, out Money normalized)
    {
        string source = sourceUom.Trim();
        if (source == targetUom.Value)
        {
            normalized = new Money(amount);
            return true;
        }
        if (!factors.TryGetValue(source, out decimal sourceFactor) || sourceFactor == 0m
            || !factors.TryGetValue(targetUom.Value, out decimal targetFactor))
        {
            normalized = default;
            return false;
        }
        normalized = new Money(CobolRoundingPolicy.RoundIntermediate(amount * targetFactor / sourceFactor));
        return true;
    }

    private sealed record Row(string UnitOfMeasure, decimal Factor);
}
