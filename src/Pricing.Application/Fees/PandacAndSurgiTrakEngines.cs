namespace Pricing.Application.Fees;

using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

public enum PandacSource { NewSpecificShipTo, LegacySpecificShipTo, DefaultShipTo }
public enum PandacFeeBasis { CostPercentage, SellPercentage, Flat, ResolvedDefault }

public sealed record PandacCandidate(
    PandacSource Source,
    PandacFeeBasis Basis,
    decimal Rate,
    Money Amount,
    bool AccountShipToEligible,
    RuleProvenance Provenance,
    PricingDateRange? EffectiveDates = null,
    string? BillingFrequency = null);

public sealed record PandacLookupResult(PandacCandidate? Candidate, PricingError? Error = null);

public interface IPandacRepository
{
    ValueTask<PandacLookupResult> FindNewSpecificAsync(PricingContext context, CancellationToken cancellationToken);
    ValueTask<PandacLookupResult> FindLegacySpecificAsync(PricingContext context, CancellationToken cancellationToken);
    ValueTask<PandacLookupResult> FindDefaultAsync(PricingContext context, CancellationToken cancellationToken);
}

public sealed record PandacInput(
    PricingContext Context,
    bool VendorParticipates,
    bool ItemEligible,
    bool HasSpecificShipTo,
    Money TotalCost,
    Money SellBeforeAdjustment);

public sealed record PandacResult(
    Money Amount,
    decimal AppliedRate,
    Money AuditAmount,
    decimal AuditRate,
    PandacSource? Source,
    bool FoldIntoLine,
    bool IsImpliedSellArrangement,
    PriceComponent? Component,
    PricingDateRange? EffectiveDates,
    PricingError? Error = null)
{
    public bool IsFailure => Error is not null;
}

/// <summary>Implements the live A6U01 0165/0170/7705/7706 PANDAC flow.</summary>
public sealed class PandacEngine(IPandacRepository repository)
{
    public async ValueTask<PandacResult> CalculateAsync(PandacInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        if (!input.VendorParticipates || !input.ItemEligible) return Empty();

        PandacLookupResult lookup = new(null);
        if (input.HasSpecificShipTo)
        {
            lookup = await repository.FindNewSpecificAsync(input.Context, cancellationToken).ConfigureAwait(false);
            if (lookup.Error is not null) return Failure(lookup.Error);
            if (lookup.Candidate is { AccountShipToEligible: false }) lookup = new(null);
            if (lookup.Candidate is null)
            {
                lookup = await repository.FindLegacySpecificAsync(input.Context, cancellationToken).ConfigureAwait(false);
                if (lookup.Error is not null) return Failure(lookup.Error);
            }
        }

        if (lookup.Candidate is null)
        {
            lookup = await repository.FindDefaultAsync(input.Context, cancellationToken).ConfigureAwait(false);
            if (lookup.Error is not null) return Failure(lookup.Error);
        }

        if (lookup.Candidate is null) return Empty();
        PandacCandidate candidate = lookup.Candidate;
        decimal appliedRate = candidate.Basis switch
        {
            PandacFeeBasis.CostPercentage or PandacFeeBasis.SellPercentage when candidate.Source == PandacSource.NewSpecificShipTo => candidate.Rate / 100m,
            PandacFeeBasis.CostPercentage or PandacFeeBasis.SellPercentage => candidate.Rate,
            _ => 0m,
        };
        Money amount = candidate.Basis switch
        {
            PandacFeeBasis.CostPercentage => Percent(input.TotalCost, appliedRate),
            PandacFeeBasis.SellPercentage => Percent(input.SellBeforeAdjustment, appliedRate),
            _ => candidate.Amount,
        };
        bool monthly = candidate.BillingFrequency is "MA" or "MM";
        var component = new PriceComponent("PANDAC", PriceComponentType.Fee, amount, candidate.Provenance);

        // The old implied-arrangement GO TO is commented out in live 7195; normal adjustments continue.
        return new(amount, appliedRate, candidate.Source == PandacSource.NewSpecificShipTo ? candidate.Amount : new Money(0m),
            candidate.Source == PandacSource.NewSpecificShipTo ? appliedRate : 0m,
            candidate.Source, !monthly, false, component, candidate.EffectiveDates);
    }

    private static Money Percent(Money basis, decimal percentage) => new(decimal.Round(basis.Value * percentage, Money.MaximumScale, MidpointRounding.AwayFromZero));
    private static PandacResult Empty() => new(new(0m), 0m, new(0m), 0m, null, false, false, null, null);
    private static PandacResult Failure(PricingError error) => new(new(0m), 0m, new(0m), 0m, null, false, false, null, null, error);
}

public enum SurgiTrakFeeBasis { PerLine, PerQuantity, PerOrder, CostPercentage, SellPercentage, Unknown }

public sealed record SurgiTrakInput(
    bool IsOwensProduct,
    bool FeeComponentFound,
    SurgiTrakFeeBasis Basis,
    Money ResolvedAmount,
    decimal Percentage,
    Money TotalCost,
    Money SellBeforeAdjustment,
    string? BillingFrequency,
    RuleProvenance Provenance);

public sealed record SurgiTrakResult(Money Amount, bool FoldIntoLine, PriceComponent? Component);

/// <summary>Implements A6U01 7758 and its 7195 monthly-billing fold-in gate.</summary>
public static class SurgiTrakEngine
{
    public static SurgiTrakResult Calculate(SurgiTrakInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!input.IsOwensProduct || !input.FeeComponentFound) return new(new Money(0m), false, null);
        Money amount = input.Basis switch
        {
            SurgiTrakFeeBasis.PerLine or SurgiTrakFeeBasis.PerQuantity or SurgiTrakFeeBasis.PerOrder => input.ResolvedAmount,
            SurgiTrakFeeBasis.CostPercentage => Percent(input.TotalCost, input.Percentage),
            SurgiTrakFeeBasis.SellPercentage => Percent(input.SellBeforeAdjustment, input.Percentage),
            _ => new Money(0m),
        };
        bool monthly = input.BillingFrequency is "MA" or "MM";
        PriceComponent? component = amount.Value == 0m ? null : new("SurgiTrak", PriceComponentType.Fee, amount, input.Provenance);
        return new(amount, !monthly, component);
    }

    private static Money Percent(Money basis, decimal percentage) => new(decimal.Round(basis.Value * percentage, Money.MaximumScale, MidpointRounding.AwayFromZero));
}
