namespace Pricing.Infrastructure.SqlServer;

using System.Collections.Immutable;
using Pricing.Application.CostAdjustments;
using Pricing.Application.ExpirationDates;
using Pricing.Application.Orchestration;
using Pricing.Application.Rebates;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>Composes A6U01 7070 rebate calculation and 7215 vendor-cost adjustment.</summary>
public sealed class SqlServerRebatePricingStage(
    IRebateCalculationInputRepository rebateInputs,
    VendorCostAdjustmentService costAdjustments) : IRebatePricingStage
{
    public async ValueTask<RebatePricingStageResult> ExecuteAsync(
        PricingContext context,
        Money totalCost,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (context.CostSelection is not { } selection)
        {
            return Failed(totalCost, new MissingDataPricingError(
                "COST_SELECTION_REQUIRED",
                "Rebate processing requires a selected cost source.",
                Entity: "CostSelection"));
        }

        RebateCalculationResult rebate;
        if (selection is ContractSelection contract)
        {
            try
            {
                rebate = RebateCalculator.Calculate(await rebateInputs
                    .LoadAsync(context, contract, cancellationToken).ConfigureAwait(false));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (RebateCalculationInputRepositoryException exception)
            {
                return Failed(totalCost, new DependencyPricingError(
                    "REBATE_INPUT_LOOKUP_FAILED", exception.Message, exception.LegacyErrorCode,
                    exception.IsTransient, exception.LegacySeverityCode));
            }
        }
        else
        {
            rebate = RebateCalculationResult.NotCalculated("No cost contract was selected.");
        }

        CostAdjustmentResult adjustment = await costAdjustments.ApplyAsync(
            new CostAdjustmentInput(
                context,
                selection,
                selection is ContractSelection,
                selection is ContractSelection { SpecialContract.BypassAdjustments: true },
                selection is HealthcareCostSelection,
                rebate),
            cancellationToken).ConfigureAwait(false);
        if (adjustment.Error is not null)
        {
            return Failed(totalCost, adjustment.Error);
        }

        ImmutableArray<PriceComponent> components = rebate.Components.AddRange(adjustment.Components);
        var expirations = ImmutableArray.CreateBuilder<ExpirationDateSource>();
        if (rebate.PriceProtectionExpirationDate is { } protection && rebate.Provenance is { } rebateSource)
        {
            expirations.Add(new ExpirationDateSource("Price protection", protection, rebateSource));
        }
        if (adjustment.Provenance is { EffectiveDates.ExpirationDate: { } adjustmentExpiration } adjustmentSource)
        {
            expirations.Add(new ExpirationDateSource("Vendor cost adjustment", adjustmentExpiration, adjustmentSource));
        }

        return new RebatePricingStageResult(adjustment.TotalCost, components, expirations.ToImmutable(), []);
    }

    private static RebatePricingStageResult Failed(Money totalCost, PricingError error) =>
        new(totalCost, [], [], [], error);
}
