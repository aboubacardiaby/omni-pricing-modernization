namespace Pricing.Infrastructure.SqlServer;

using System.Collections.Immutable;
using Pricing.Application.ExpirationDates;
using Pricing.Application.Orchestration;
using Pricing.Application.SellSelection;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>Concrete A6U01 7180/7090/7205 sell stage.</summary>
public sealed class SqlServerSellPricingStage(
    SellArrangementRuleEvaluator arrangements,
    SellPriceCalculationDispatcher calculator,
    PriceLockService priceLocks,
    ISqlServerQueryExecutor executor) : ISellPricingStage
{
    private static readonly SqlServerQuery BasisQuery = new("sell.calculation-basis", """
        SELECT TOP (1) RTRIM(C_VND_PRC_UM) AS UnitOfMeasure,
          A_VND_PRC_DEALER AS DealerCost, A_VND_PRC_BEST_QTY AS BestQuantityPrice,
          A_VND_PRC_LST_HOSP AS HospitalListPrice, A_VND_PRC_LST_DOC AS DoctorListPrice
        FROM dbo.VNG03 WHERE I_VENDOR=@Vendor AND I_VND_PRODUCT=@Product
          AND D_VND_PRC_LIST_EFF<=@PricingDate
          AND (D_VND_PRC_EXPIRE>=@PricingDate OR D_VND_PRC_EXPIRE IS NULL)
        ORDER BY D_VND_PRC_LIST_EFF DESC
        """);

    public async ValueTask<SellPricingStageResult> ExecuteAsync(
        PricingContext context, Money totalCost, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        SellArrangementResult arrangement = await arrangements.EvaluateAsync(context, cancellationToken)
            .ConfigureAwait(false);
        if (arrangement.Error is not null)
        {
            return Failed(context, arrangement.Error);
        }

        SellArrangementSelection? selection = arrangement.Selection;
        PricingContext selectedContext = context with { SellArrangementSelection = selection };
        SellCalculationBasis basis;
        try
        {
            basis = await LoadBasisAsync(selectedContext, totalCost, cancellationToken).ConfigureAwait(false);
        }
        catch (SqlServerAccessException exception)
        {
            return Failed(selectedContext, new DependencyPricingError(
                "SELL_BASIS_LOOKUP_FAILED", "VNG03 sell-basis SQL lookup failed.",
                "130", exception.IsTransient, "90"));
        }

        catch (SellArrangementRepositoryException exception)
        {
            return Failed(selectedContext, new DependencyPricingError(
                "SELL_BASIS_LOOKUP_FAILED", exception.Message, exception.LegacyErrorCode,
                exception.IsTransient, exception.LegacySeverityCode));
        }

        decimal up = 1m;
        decimal down = 1m;
        if (selection?.StatedPriceUnitOfMeasure is { } statedUom &&
            statedUom != context.Request.UnitOfMeasure)
        {
            IReadOnlyDictionary<string, decimal> factors =
                await SqlServerCostUomConverter.LoadAsync(executor, context, cancellationToken).ConfigureAwait(false);
            if (!factors.TryGetValue(statedUom.Value, out up))
            {
                return Failed(selectedContext, new MissingDataPricingError(
                    "SELL_STATED_UOM_CONVERSION_MISSING",
                    $"No conversion exists from {statedUom.Value} to {context.Request.UnitOfMeasure.Value}.",
                    Entity: "VNG05"));
            }
        }

        var input = new SellPriceCalculationInput(selectedContext, selection, basis,
            Percentage: selection?.Percentage is { } percentage ? new Percentage(percentage) : null,
            CostContractFound: context.CostSelection is ContractSelection,
            CostSuggestedSellAvailable: context.ContractSelection?.SpecialContract?.SuggestedSellPrice.Value > 0m,
            StatedPrice: selection?.StatedPrice, StatedConversionUpFactor: up,
            StatedConversionDownFactor: down,
            HealthcareOverride: (context.CostSelection as HealthcareCostSelection)?.SellOverride);
        SellPriceCalculationResult calculated = await calculator
            .CalculateAsync(selection?.SellMethodCode, input, cancellationToken).ConfigureAwait(false);
        if (calculated.Error is not null)
        {
            return Failed(selectedContext, calculated.Error);
        }

        PriceLockResult locked;
        try
        {
            locked = await priceLocks.ApplyAsync(new PriceLockInput(selectedContext, calculated,
                context.CostSelection?.UnitCost ?? totalCost, selection?.SellMethodCode ?? SellPriceMethodCodes.ListPrice,
                selection?.Percentage is { } p ? new Percentage(p) : new Percentage(0m)),
                cancellationToken).ConfigureAwait(false);
        }
        catch (SellArrangementRepositoryException exception)
        {
            return Failed(selectedContext, new DependencyPricingError(
                "PRICE_LOCK_LOOKUP_FAILED", exception.Message, exception.LegacyErrorCode,
                exception.IsTransient, exception.LegacySeverityCode));
        }
        if (locked.Error is not null)
        {
            return Failed(selectedContext, locked.Error);
        }

        SellAdjustmentCompositionResult composed = SellAdjustmentComposer.Compose(
            new SellAdjustmentInput(locked.Calculation, new SellAdjustmentSet(), ""));
        Money totalSell = new(decimal.Round(
            composed.TotalSell.Value * context.Request.Quantity.Value,
            Money.MaximumScale, MidpointRounding.AwayFromZero));
        ImmutableArray<ExpirationDateSource> expirations = Expirations(selection, locked);
        return new SellPricingStageResult(selectedContext, composed.TotalSell, totalSell,
            composed.Components, expirations, []);
    }

    private async ValueTask<SellCalculationBasis> LoadBasisAsync(
        PricingContext context, Money totalCost, CancellationToken cancellationToken)
    {
        IReadOnlyList<BasisRow> rows = await executor.QueryAsync<BasisRow>(
            BasisQuery, SqlServerCostMapping.Parameters(context), cancellationToken).ConfigureAwait(false);
        if (rows.Count == 0)
        {
            throw new SellArrangementRepositoryException("No VNG03 sell basis was found.", "130", "90");
        }
        BasisRow row = rows[0];
        IReadOnlyDictionary<string, decimal> factors =
            await SqlServerCostUomConverter.LoadAsync(executor, context, cancellationToken).ConfigureAwait(false);
        Money Convert(decimal value)
        {
            if (!SqlServerCostUomConverter.TryNormalize(value, row.UnitOfMeasure,
                context.Request.UnitOfMeasure, factors, out Money normalized))
            {
                throw new SellArrangementRepositoryException("VNG03 sell-basis UOM conversion failed.", "122", "90");
            }
            return normalized;
        }
        Money suggested = context.ContractSelection?.SpecialContract?.SuggestedSellPrice ?? new Money(0m);
        return new SellCalculationBasis(totalCost, Convert(row.DealerCost), Convert(row.BestQuantityPrice),
            Convert(row.HospitalListPrice), Convert(row.DoctorListPrice), suggested,
            context.Request.UnitOfMeasure);
    }

    private static ImmutableArray<ExpirationDateSource> Expirations(
        SellArrangementSelection? selection, PriceLockResult locked)
    {
        var result = ImmutableArray.CreateBuilder<ExpirationDateSource>();
        if (selection?.Provenance.EffectiveDates?.ExpirationDate is { } arrangementExpiration)
        {
            result.Add(new ExpirationDateSource("Sell arrangement", arrangementExpiration, selection.Provenance));
        }
        if (locked.LockEffectiveDates?.ExpirationDate is { } lockExpiration)
        {
            RuleProvenance provenance = locked.Calculation.Provenance;
            result.Add(new ExpirationDateSource("Price lock", lockExpiration, provenance));
        }
        return result.ToImmutable();
    }

    private static SellPricingStageResult Failed(PricingContext context, PricingError error) =>
        new(context, new Money(0m), new Money(0m), [], [], [], error);

    private sealed record BasisRow(string UnitOfMeasure, decimal DealerCost,
        decimal BestQuantityPrice, decimal HospitalListPrice, decimal DoctorListPrice);
}
