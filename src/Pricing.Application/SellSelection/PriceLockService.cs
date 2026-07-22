namespace Pricing.Application.SellSelection;

using System.Collections.Immutable;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>Loads the effective CUG31 lock row keyed by account, vendor, and product.</summary>
public interface IPriceLockRepository
{
    ValueTask<PriceLockRecord?> FindEffectiveAsync(PricingContext context, CancellationToken cancellationToken);
}

/// <summary>A CUG31 snapshot. Monetary values remain in product base UOM as stored by COBOL.</summary>
public sealed record PriceLockRecord(
    Money TotalSell,
    Money TotalCost,
    Money TotalSellAdjustment,
    Money TotalCostAdjustment,
    Money UnadjustedUnitCost,
    string? SellMethodCode,
    Percentage? SellPercentage,
    PricingDateRange EffectiveDates,
    RuleProvenance Provenance);

public sealed record PriceLockInput(
    PricingContext Context,
    SellPriceCalculationResult CurrentCalculation,
    Money CurrentUnadjustedUnitCost,
    string CurrentSellMethodCode,
    Percentage CurrentPercentage,
    decimal ConversionUpFactor = 1m,
    decimal ConversionDownFactor = 1m);

public sealed record PriceLockResult(
    SellPriceCalculationResult Calculation,
    bool LockRecordFound,
    bool IsPriceLocked,
    bool BypassSurcharge,
    bool BypassVendorCostAdjustment,
    PricingDateRange? LockEffectiveDates,
    PricingError? Error = null)
{
    public bool IsFailure => Error is not null;
}

/// <summary>
/// Reconciles a newly calculated price with CUG31 after sell calculation.
/// COBOL: A6U01 7765 lookup, 0210 base-UOM conversion, and 7205 lock reconciliation.
/// </summary>
public sealed class PriceLockService(IPriceLockRepository repository)
{
    private static readonly HashSet<string> PercentageBasedMethods =
        new(["1", "2", "4", "6", "7"], StringComparer.Ordinal);

    public async ValueTask<PriceLockResult> ApplyAsync(PriceLockInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        PriceLockRecord? record = await repository.FindEffectiveAsync(input.Context, cancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            return Unlocked(input.CurrentCalculation);
        }

        if (input.ConversionDownFactor <= 0m)
        {
            var error = new ValidationPricingError(
                "PRICE_LOCK_UOM_INVALID",
                "Price-lock conversion requires a positive denominator.",
                Field: "conversionDownFactor");
            return new(input.CurrentCalculation, true, false, false, false, record.EffectiveDates, error);
        }

        decimal factor = input.ConversionUpFactor / input.ConversionDownFactor;
        Money lockedCost = Convert(record.UnadjustedUnitCost, factor);
        Money lockedSell = Convert(record.TotalSell, factor);
        Money lockedSellAdjustment = Convert(record.TotalSellAdjustment, factor);
        Money frozenBaseSell = new(lockedSell.Value - lockedSellAdjustment.Value);
        string lockedMethod = record.SellMethodCode ?? string.Empty;
        decimal lockedPercentage = record.SellPercentage?.Value ?? 0m;

        bool changed = lockedCost != input.CurrentUnadjustedUnitCost ||
            !StringComparer.Ordinal.Equals(lockedMethod, input.CurrentSellMethodCode) ||
            (PercentageBasedMethods.Contains(input.CurrentSellMethodCode)
                ? lockedPercentage != input.CurrentPercentage.Value
                : frozenBaseSell != input.CurrentCalculation.SellPrice);

        if (changed)
        {
            return new(input.CurrentCalculation, true, false, false, false, record.EffectiveDates);
        }

        SellPriceCalculationResult lockedCalculation = input.CurrentCalculation with
        {
            SellPrice = frozenBaseSell,
            Components = ImmutableArray.Create(new PriceComponent(
                "Locked base sell",
                PriceComponentType.BaseSell,
                frozenBaseSell,
                record.Provenance)),
            Provenance = record.Provenance,
        };
        return new(lockedCalculation, true, true, true, true, record.EffectiveDates);
    }

    private static PriceLockResult Unlocked(SellPriceCalculationResult calculation) =>
        new(calculation, false, false, false, false, null);

    private static Money Convert(Money value, decimal factor) =>
        new(decimal.Round(value.Value * factor, Money.MaximumScale, MidpointRounding.AwayFromZero));
}
