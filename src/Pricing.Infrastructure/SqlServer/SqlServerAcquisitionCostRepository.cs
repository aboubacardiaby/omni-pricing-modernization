namespace Pricing.Infrastructure.SqlServer;

using System.Collections.Immutable;
using Pricing.Application.CostSelection;
using Pricing.Application.Rounding;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>Loads and normalizes the effective VNG03 price-list waterfall.</summary>
/// <remarks>COBOL: A6U01 7105/7110, 7575, 7595, and SQL paragraphs 9200/9300/9305/9310.</remarks>
public sealed class SqlServerAcquisitionCostRepository(ISqlServerQueryExecutor queries) : IAcquisitionCostRepository
{
    private static readonly SqlServerQuery Prices = new(
        "acquisition.vng03",
        """
        SELECT C_VND_PRC_UM AS UnitOfMeasure,
               C_VND_PRC_LEVEL AS PriceLevel,
               D_VND_PRC_LIST_EFF AS PriceListEffectiveDate,
               D_VND_PRC_ACTIVE AS ActiveDate,
               D_VND_PRC_EXPIRE AS ExpirationDate,
               A_VND_PRC_DEALER AS DealerCost,
               A_VND_PRC_ACQ_COST AS AcquisitionCost
        FROM dbo.VNG03
        WHERE I_VENDOR = @Vendor
          AND I_VND_PRODUCT = @Product
          AND D_VND_PRC_ACTIVE <= @PricingDate
          AND (D_VND_PRC_EXPIRE >= @PricingDate OR D_VND_PRC_EXPIRE IS NULL);
        """);

    private static readonly SqlServerQuery Factors = new(
        "acquisition.vng05-factors",
        """
        SELECT C_VD_PRD_ALT_UM AS UnitOfMeasure,
               A_VD_PRD_ALT_UMF AS ConversionFactor
        FROM dbo.VNG05
        WHERE I_VENDOR = @Vendor
          AND I_VND_PRODUCT = @Product;
        """);

    private readonly ISqlServerQueryExecutor queries = queries ?? throw new ArgumentNullException(nameof(queries));

    public async ValueTask<AcquisitionCostData> FindAsync(
        PricingContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            var key = new
            {
                Vendor = context.Request.Vendor.Value,
                Product = context.Request.Product.Value,
                PricingDate = context.Request.PricingDate.ToDateTime(TimeOnly.MinValue),
            };
            IReadOnlyList<PriceRow> prices = await queries.QueryAsync<PriceRow>(Prices, key, cancellationToken)
                .ConfigureAwait(false);
            IReadOnlyList<FactorRow> factors = await queries.QueryAsync<FactorRow>(Factors, key, cancellationToken)
                .ConfigureAwait(false);

            Dictionary<string, decimal> factorByUom = factors
                .Where(row => !string.IsNullOrWhiteSpace(row.UnitOfMeasure) && row.ConversionFactor > 0m)
                .GroupBy(row => row.UnitOfMeasure!.Trim(), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().ConversionFactor, StringComparer.Ordinal);
            factorByUom[context.Product.BaseUnitOfMeasure.Value] = 1m;

            ImmutableArray<AcquisitionPriceListCandidate> candidates = prices
                .Select(row => Map(row, context, factorByUom))
                .ToImmutableArray();
            AcquisitionPriceListCandidate? direct = string.IsNullOrWhiteSpace(context.Product.InventoryPriceLevel)
                ? null
                : candidates
                    .Where(candidate => StringComparer.Ordinal.Equals(
                        candidate.PriceLevel,
                        context.Product.InventoryPriceLevel.Trim()))
                    .OrderByDescending(candidate => candidate.PriceListEffectiveDate)
                    .FirstOrDefault();
            return new AcquisitionCostData(direct, candidates);
        }
        catch (SqlServerAccessException exception)
        {
            throw new AcquisitionCostRepositoryException(
                $"SQL Server acquisition lookup '{exception.Operation}' failed.",
                "45",
                "70",
                exception.IsTransient,
                exception);
        }
    }

    private static AcquisitionPriceListCandidate Map(
        PriceRow row,
        PricingContext context,
        IReadOnlyDictionary<string, decimal> factors)
    {
        string fileUom = Required(row.UnitOfMeasure, "VNG03.C_VND_PRC_UM");
        string screenUom = context.Request.UnitOfMeasure.Value;
        decimal up = Factor(screenUom, context.Product.BaseUnitOfMeasure.Value, factors, "95");
        decimal down = Factor(fileUom, context.Product.BaseUnitOfMeasure.Value, factors, "96");
        decimal multiplier = up / down;
        DateOnly effective = DateOnly.FromDateTime(row.PriceListEffectiveDate);
        DateOnly? expiration = row.ExpirationDate is null ? null : DateOnly.FromDateTime(row.ExpirationDate.Value);
        var dates = new PricingDateRange(effective, expiration);
        return new AcquisitionPriceListCandidate(
            new Money(CobolRoundingPolicy.RoundIntermediate(row.DealerCost * multiplier)),
            new Money(CobolRoundingPolicy.RoundIntermediate(row.AcquisitionCost * multiplier)),
            context.Request.UnitOfMeasure,
            Required(row.PriceLevel, "VNG03.C_VND_PRC_LEVEL"),
            effective,
            new RuleProvenance("acquisition-dealer-cost-fallback", "VNG03", "Vendor product", dates));
    }

    private static decimal Factor(
        string uom,
        string baseUom,
        IReadOnlyDictionary<string, decimal> factors,
        string legacyError)
    {
        if (StringComparer.Ordinal.Equals(uom, baseUom)) return 1m;
        return factors.TryGetValue(uom, out decimal value)
            ? value
            : throw new AcquisitionCostRepositoryException(
                $"Alternate unit of measure '{uom}' was not found in VNG05.", legacyError, "60");
    }

    private static string Required(string? value, string field) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new AcquisitionCostRepositoryException($"Required field {field} was empty.")
            : value.Trim();

    private sealed class PriceRow
    {
        public string? UnitOfMeasure { get; init; }
        public string? PriceLevel { get; init; }
        public DateTime PriceListEffectiveDate { get; init; }
        public DateTime ActiveDate { get; init; }
        public DateTime? ExpirationDate { get; init; }
        public decimal DealerCost { get; init; }
        public decimal AcquisitionCost { get; init; }
    }

    private sealed class FactorRow
    {
        public string? UnitOfMeasure { get; init; }
        public decimal ConversionFactor { get; init; }
    }
}
