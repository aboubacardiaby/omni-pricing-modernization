namespace Pricing.Application.CostAdjustments;

using System.Collections.Immutable;
using Pricing.Application.Rebates;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>Selects and composes the vendor cost adjustment after rebate calculation.</summary>
/// <remarks>
/// COBOL: A6U01 0030 preserves cost before adjustment; 7215 skips price-lock/healthcare,
/// selects the first waterfall result, and computes base cost * percentage; 7200 includes
/// the amount in total adjusted cost. Rebate remains a separate output and is not netted here.
/// </remarks>
public sealed class VendorCostAdjustmentService
{
    private const int OutputScale = 4;
    private readonly IVendorCostAdjustmentRepository repository;

    public VendorCostAdjustmentService(IVendorCostAdjustmentRepository repository) =>
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async ValueTask<CostAdjustmentResult> ApplyAsync(
        CostAdjustmentInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        Money baseCost = input.CostSelection.UnitCost;
        if (input.IsPriceLocked || input.HasHealthcareCostRecord)
        {
            string reason = input.IsPriceLocked
                ? "Price-locked lines bypass vendor cost adjustment."
                : "Healthcare cost records bypass vendor cost adjustment.";
            return CostAdjustmentResult.Unadjusted(baseCost, input.Rebate, reason);
        }

        try
        {
            ImmutableArray<VendorCostAdjustmentCandidate> candidates = await repository
                .FindAsync(input.Context, input.HasCostContract, cancellationToken)
                .ConfigureAwait(false);
            VendorCostAdjustmentCandidate? selected = candidates
                .Where(candidate => candidate.EffectiveDates.Contains(input.Context.Request.PricingDate))
                .OrderBy(candidate => candidate.EvaluationOrder)
                .FirstOrDefault();

            if (selected is null)
            {
                return CostAdjustmentResult.Unadjusted(
                    baseCost,
                    input.Rebate,
                    "No vendor cost adjustment was defined.");
            }

            decimal appliedPercentage = selected.IsExempt ? 0m : selected.Percentage;
            decimal adjustment = Round(baseCost.Value * appliedPercentage);
            decimal totalCost = Round(baseCost.Value + adjustment);
            var component = new PriceComponent(
                "Vendor cost adjustment",
                PriceComponentType.CostAdjustment,
                new Money(adjustment),
                selected.Provenance);

            return new CostAdjustmentResult(
                true,
                null,
                baseCost,
                appliedPercentage,
                selected.SourceCode,
                new Money(adjustment),
                new Money(totalCost),
                input.Rebate,
                [component],
                selected.Provenance,
                null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (VendorCostAdjustmentRepositoryException exception)
        {
            var error = new DependencyPricingError(
                "VENDOR_COST_ADJUSTMENT_LOOKUP_FAILED",
                exception.Message,
                exception.LegacyErrorCode,
                exception.IsTransient,
                exception.LegacySeverityCode);
            return CostAdjustmentResult.Failed(baseCost, input.Rebate, error);
        }
    }

    private static decimal Round(decimal value) =>
        decimal.Round(value, OutputScale, MidpointRounding.AwayFromZero);
}

public sealed record CostAdjustmentInput(
    PricingContext Context,
    ICostSourceSelection CostSelection,
    bool HasCostContract,
    bool IsPriceLocked,
    bool HasHealthcareCostRecord,
    RebateCalculationResult Rebate);

public sealed record CostAdjustmentResult(
    bool WasApplied,
    string? SkipReason,
    Money BaseCost,
    decimal VendorAdjustmentPercentage,
    string? VendorAdjustmentSourceCode,
    Money VendorAdjustment,
    Money TotalCost,
    RebateCalculationResult Rebate,
    ImmutableArray<PriceComponent> Components,
    RuleProvenance? Provenance,
    PricingError? Error)
{
    public bool IsFailure => Error is not null;

    public static CostAdjustmentResult Unadjusted(
        Money baseCost,
        RebateCalculationResult rebate,
        string reason) => new(
        false,
        reason,
        baseCost,
        0m,
        null,
        new Money(0m),
        baseCost,
        rebate,
        [],
        null,
        null);

    public static CostAdjustmentResult Failed(
        Money baseCost,
        RebateCalculationResult rebate,
        PricingError error) => new(
        false,
        null,
        baseCost,
        0m,
        null,
        new Money(0m),
        baseCost,
        rebate,
        [],
        null,
        error);
}
