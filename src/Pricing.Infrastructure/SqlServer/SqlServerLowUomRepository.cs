namespace Pricing.Infrastructure.SqlServer;

using System.Collections.Immutable;
using Pricing.Application.Fees;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>Reads alternate UOMs and buying-group vendor exclusions.</summary>
/// <remarks>COBOL: A6O016U 9050-SELECT-VNG05 and A6U01 7885/9595.</remarks>
public sealed class SqlServerLowUomRepository(ISqlServerQueryExecutor queries) : ILowUomRepository
{
    private static readonly SqlServerQuery AlternateQuery = new(
        "low-uom.vng05",
        """
        SELECT C_VD_PRD_ALT_UM AS UnitOfMeasure,
               A_VD_PRD_ALT_UMF AS ConversionFactor,
               F_ALT_UOM_DESIGNATOR AS Designator
        FROM dbo.VNG05
        WHERE I_VENDOR = @Vendor
          AND I_VND_PRODUCT = @Product;
        """);

    private static readonly SqlServerQuery ExclusionQuery = new(
        "low-uom.bgg25",
        """
        SELECT TOP (1) 1
        FROM dbo.BGG25
        WHERE I_VENDOR = @Vendor
          AND I_BUY_GROUP = @BuyingGroup
          AND D_BG_LOW_UOM_EFF = @LowUomEffectiveDate
          AND D_BG_LUOM_VEND_EFF <= @PricingDate
          AND (D_BG_LUOM_VEND_EXP >= @PricingDate OR D_BG_LUOM_VEND_EXP IS NULL);
        """);

    private readonly ISqlServerQueryExecutor queries = queries
        ?? throw new ArgumentNullException(nameof(queries));

    public async ValueTask<AlternateUomLookupResult> LoadAlternateUomsAsync(
        PricingContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            IReadOnlyList<Row> rows = await queries.QueryAsync<Row>(
                AlternateQuery,
                new { Vendor = context.Request.Vendor.Value, Product = context.Request.Product.Value },
                cancellationToken).ConfigureAwait(false);
            ImmutableArray<AlternateUomCandidate> candidates = rows
                .Where(row => !string.IsNullOrWhiteSpace(row.UnitOfMeasure) && row.ConversionFactor > 0m)
                .Select(row => new AlternateUomCandidate(
                    new UnitOfMeasure(row.UnitOfMeasure!.Trim()),
                    row.ConversionFactor,
                    MapDesignator(row.Designator)))
                .ToImmutableArray();
            return new AlternateUomLookupResult(candidates);
        }
        catch (SqlServerAccessException exception)
        {
            return new AlternateUomLookupResult(
                [],
                new DependencyPricingError(
                    "LOW_UOM_LOOKUP_FAILED",
                    $"SQL Server alternate-UOM lookup '{exception.Operation}' failed.",
                    "301",
                    exception.IsTransient,
                    "70"));
        }
    }

    public async ValueTask<bool> IsGroupVendorExcludedAsync(
        PricingContext context,
        long buyingGroupId,
        DateOnly effectiveDate,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            int? found = await queries.QuerySingleOrDefaultAsync<int?>(
                ExclusionQuery,
                new
                {
                    Vendor = context.Request.Vendor.Value,
                    BuyingGroup = buyingGroupId,
                    LowUomEffectiveDate = effectiveDate.ToDateTime(TimeOnly.MinValue),
                    PricingDate = context.Request.PricingDate.ToDateTime(TimeOnly.MinValue),
                },
                cancellationToken).ConfigureAwait(false);
            return found is not null;
        }
        catch (SqlServerAccessException exception)
        {
            throw new InvalidOperationException(
                $"SQL Server BGG25 lookup '{exception.Operation}' failed with legacy error 301.",
                exception);
        }
    }

    private static LowUomDesignator MapDesignator(string? value) => value?.Trim() switch
    {
        "L" => LowUomDesignator.LowUnitOfMeasure,
        "B" => LowUomDesignator.BreakBulk,
        _ => LowUomDesignator.None,
    };

    private sealed class Row
    {
        public string? UnitOfMeasure { get; init; }
        public decimal ConversionFactor { get; init; }
        public string? Designator { get; init; }
    }
}
