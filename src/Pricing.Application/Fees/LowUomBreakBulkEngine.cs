namespace Pricing.Application.Fees;

using System.Collections.Immutable;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

public enum LowUomDesignator { None, LowUnitOfMeasure, BreakBulk }
public enum LowUomSourceScope { Account, BuyingGroup }
public enum FeeComponentBasis { None, CostPercentage, SellPercentage, Flat }

public sealed record AlternateUomCandidate(UnitOfMeasure UnitOfMeasure, decimal ConversionFactor, LowUomDesignator Designator);
public sealed record AlternateUomLookupResult(ImmutableArray<AlternateUomCandidate> Candidates, PricingError? Error = null);

public interface ILowUomRepository
{
    ValueTask<AlternateUomLookupResult> LoadAlternateUomsAsync(PricingContext context, CancellationToken cancellationToken);
    ValueTask<bool> IsGroupVendorExcludedAsync(PricingContext context, long buyingGroupId, DateOnly effectiveDate, CancellationToken cancellationToken);
}

public sealed record LowUomChargeSource(
    LowUomSourceScope Scope,
    decimal LowUomPercentage,
    decimal BreakBulkPercentage,
    RuleProvenance Provenance,
    long? BuyingGroupId = null,
    int Priority = int.MaxValue,
    int ParentDepth = 0,
    bool CheckVendorExclusion = false);

public sealed record LowUomFeeComponent(
    FeeComponentBasis Basis,
    decimal LowUomPercentage,
    decimal BreakBulkPercentage,
    Money LowUomFlatAmount,
    Money BreakBulkFlatAmount,
    RuleProvenance Provenance,
    bool PandacVendor = false,
    bool PandacItem = false,
    bool PandacCustomer = false);

public sealed record LowUomInput(
    PricingContext Context,
    bool IsEligible,
    bool IsStockOrder,
    bool IsContractExcluded,
    Quantity OrderedQuantity,
    UnitOfMeasure OrderedUnitOfMeasure,
    UnitOfMeasure? AlternateOrderUnitOfMeasure,
    decimal BaseOrderConversionFactor,
    LowUomDesignator BaseDesignator,
    ImmutableArray<LowUomChargeSource> Sources,
    Money TotalCost,
    Money SellBeforeAdjustment,
    string JitServiceFeeCode,
    bool IsPrivateLabelO,
    LowUomFeeComponent? FeeComponent = null);

public sealed record LowUomResult(
    LowUomDesignator Designator,
    Money Amount,
    decimal AppliedPercentage,
    LowUomChargeSource? Source,
    bool IsVendorExcluded,
    PriceComponent? Component,
    PricingError? Error = null)
{
    public bool IsFailure => Error is not null;
}

/// <summary>Implements A6U01 0245 and 7872–7898 low-UOM/break-bulk processing.</summary>
public sealed class LowUomBreakBulkEngine(ILowUomRepository repository)
{
    public async ValueTask<LowUomResult> CalculateAsync(LowUomInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        if (!input.IsEligible || !input.IsStockOrder || input.IsContractExcluded) return Empty();
        if (input.BaseOrderConversionFactor <= 0m)
            return Failure(new ValidationPricingError("LUOM_BASE_FACTOR_INVALID", "Base order conversion factor must be positive.", Field: "baseOrderConversionFactor"));

        LowUomChargeSource? source = SelectSource(input.Sources);
        if (source is null) return Empty();
        if (source.Scope == LowUomSourceScope.BuyingGroup && source.CheckVendorExclusion && source.BuyingGroupId is long groupId)
        {
            bool excluded = await repository.IsGroupVendorExcludedAsync(input.Context, groupId, input.Context.Request.PricingDate, cancellationToken).ConfigureAwait(false);
            if (excluded) return new(LowUomDesignator.None, new Money(0m), 0m, source, true, null);
        }

        AlternateUomLookupResult lookup = await repository.LoadAlternateUomsAsync(input.Context, cancellationToken).ConfigureAwait(false);
        if (lookup.Error is not null) return Failure(lookup.Error);
        LowUomDesignator designator = ResolveDesignator(input, lookup.Candidates);
        if (designator == LowUomDesignator.None) return Empty(source);

        if (input.FeeComponent is { } component)
        {
            if (component.PandacVendor && component.PandacItem && component.PandacCustomer) return Empty(source);
            return CalculateFeeComponent(input, source, designator, component);
        }

        decimal percentage = designator == LowUomDesignator.LowUnitOfMeasure ? source.LowUomPercentage : source.BreakBulkPercentage;
        if (percentage <= 0m) return Result(designator, new Money(0m), 0m, source, source.Provenance);
        bool costBased = input.JitServiceFeeCode is "A" or "C" && !input.IsPrivateLabelO;
        Money basis = costBased ? input.TotalCost : input.SellBeforeAdjustment;
        return Result(designator, Percent(basis, percentage), percentage, source, source.Provenance);
    }

    private static LowUomChargeSource? SelectSource(ImmutableArray<LowUomChargeSource> sources) =>
        sources.Where(candidate => candidate.Scope == LowUomSourceScope.Account)
            .Concat(sources.Where(candidate => candidate.Scope == LowUomSourceScope.BuyingGroup)
                .OrderBy(candidate => candidate.Priority).ThenBy(candidate => candidate.ParentDepth))
            .FirstOrDefault();

    private static LowUomDesignator ResolveDesignator(LowUomInput input, ImmutableArray<AlternateUomCandidate> candidates)
    {
        UnitOfMeasure actualUom = input.AlternateOrderUnitOfMeasure ?? input.OrderedUnitOfMeasure;
        decimal actualQuantity = input.AlternateOrderUnitOfMeasure is null
            ? input.OrderedQuantity.Value
            : input.OrderedQuantity.Value * input.BaseOrderConversionFactor;

        if (input.OrderedQuantity.Value > 1m)
        {
            IEnumerable<AlternateUomCandidate> entries = candidates.IsDefaultOrEmpty
                ? [new(actualUom, input.BaseOrderConversionFactor, input.BaseDesignator)]
                : candidates;
            foreach (AlternateUomCandidate candidate in entries)
            {
                decimal factor = candidate.ConversionFactor * input.BaseOrderConversionFactor;
                if (factor > 0m && actualQuantity % factor == 0m) return candidate.Designator;
            }

            return LowUomDesignator.None;
        }

        AlternateUomCandidate? direct = candidates.FirstOrDefault(candidate => candidate.UnitOfMeasure == actualUom);
        return direct?.Designator ?? input.BaseDesignator;
    }

    private static LowUomResult CalculateFeeComponent(LowUomInput input, LowUomChargeSource source, LowUomDesignator designator, LowUomFeeComponent component)
    {
        if (component.Basis == FeeComponentBasis.Flat)
        {
            Money amount = designator == LowUomDesignator.LowUnitOfMeasure ? component.LowUomFlatAmount : component.BreakBulkFlatAmount;
            return Result(designator, amount, 0m, source, component.Provenance);
        }

        decimal percentage = designator == LowUomDesignator.LowUnitOfMeasure ? component.LowUomPercentage : component.BreakBulkPercentage;
        if (component.Basis == FeeComponentBasis.None || percentage <= 0m)
            return Result(designator, new Money(0m), 0m, source, component.Provenance);
        bool costBased = component.Basis == FeeComponentBasis.CostPercentage && !input.IsPrivateLabelO;
        return Result(designator, Percent(costBased ? input.TotalCost : input.SellBeforeAdjustment, percentage), percentage, source, component.Provenance);
    }

    private static Money Percent(Money basis, decimal percentage) =>
        new(decimal.Round(basis.Value * percentage, Money.MaximumScale, MidpointRounding.AwayFromZero));

    private static LowUomResult Result(LowUomDesignator designator, Money amount, decimal percentage, LowUomChargeSource source, RuleProvenance provenance) =>
        new(designator, amount, percentage, source, false, new PriceComponent("Low-UOM/break-bulk", PriceComponentType.Fee, amount, provenance));

    private static LowUomResult Empty(LowUomChargeSource? source = null) => new(LowUomDesignator.None, new Money(0m), 0m, source, false, null);
    private static LowUomResult Failure(PricingError error) => new(LowUomDesignator.None, new Money(0m), 0m, null, false, null, error);
}
