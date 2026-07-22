namespace Pricing.Application.Fees;

using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

public enum FreightApplicationMode { Cost, Sell, Variable }
public enum FreightSource { Account, Customer, BuyingGroup, Product, DivisionVendor, CorporateVendor }
public enum FreightContractClass { SanctionedGroup, NonSanctionedGroup, Individual, OtherContract, NonContract }

public sealed record FreightCandidate(
    FreightSource Source,
    decimal CostPercentage,
    decimal SellPercentage,
    Money? DirectAmount,
    FreightApplicationMode? SourceMode,
    RuleProvenance Provenance,
    PricingDateRange? EffectiveDates = null,
    long? BuyingGroupId = null);

public sealed record FreightLookupResult(FreightCandidate? Candidate, PricingError? Error = null);

/// <summary>Business-oriented access to the ordered A6U01 freight sources.</summary>
public interface IFreightRepository
{
    ValueTask<FreightLookupResult> FindAccountAsync(PricingContext context, CancellationToken cancellationToken);
    ValueTask<FreightLookupResult> FindCustomerAsync(PricingContext context, CancellationToken cancellationToken);
    ValueTask<FreightLookupResult> FindBuyingGroupAsync(PricingContext context, CancellationToken cancellationToken);
    ValueTask<FreightLookupResult> FindProductAsync(PricingContext context, CancellationToken cancellationToken);
    ValueTask<FreightLookupResult> FindDivisionVendorAsync(PricingContext context, CancellationToken cancellationToken);
    ValueTask<FreightLookupResult> FindCorporateVendorAsync(PricingContext context, CancellationToken cancellationToken);
}

public sealed record FreightExemptionFacts(
    bool IsCustomProduct,
    bool CustomChargeEnabled,
    FreightContractClass ContractClass,
    bool SanctionedChargeEnabled,
    bool NonSanctionedChargeEnabled,
    bool IndividualChargeEnabled,
    bool NonContractChargeEnabled);

public sealed record FreightInput(
    PricingContext Context,
    Money DealerCost,
    Money TotalCost,
    Money? VariableUnitCost,
    FreightApplicationMode RequestedMode,
    FreightExemptionFacts Exemption,
    bool ContractLineExempt = false,
    bool VhaPlusVendor = false,
    bool AccountVha = false,
    bool FreightDisabled = false,
    bool UseBuyingGroupPath = true,
    decimal ProductUomConversionFactor = 1m,
    bool BilledMonthly = false);

public sealed record FreightResult(
    Money Amount,
    decimal Percentage,
    string TypeCode,
    FreightSource? Source,
    FreightApplicationMode Mode,
    bool IsExempt,
    bool FoldIntoCost,
    bool FoldIntoSell,
    PriceComponent? Component,
    PricingDateRange? EffectiveDates,
    PricingError? Error = null)
{
    public bool IsFailure => Error is not null;
}

/// <summary>
/// Implements A6U01 7200/0205/7226 freight selection and application.
/// Account and CID lookups always precede the single-winner downstream waterfall.
/// </summary>
public sealed class FreightEngine(IFreightRepository repository)
{
    public async ValueTask<FreightResult> CalculateAsync(FreightInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        if (input.ContractLineExempt || (input.VhaPlusVendor && input.AccountVha) || input.FreightDisabled)
        {
            return Empty(input.RequestedMode);
        }

        FreightLookupResult lookup = await repository.FindAccountAsync(input.Context, cancellationToken).ConfigureAwait(false);
        if (lookup.Error is not null) return Failure(input.RequestedMode, lookup.Error);
        bool accountOrCustomer = lookup.Candidate is not null;

        if (lookup.Candidate is null)
        {
            lookup = await repository.FindCustomerAsync(input.Context, cancellationToken).ConfigureAwait(false);
            if (lookup.Error is not null) return Failure(input.RequestedMode, lookup.Error);
            accountOrCustomer = lookup.Candidate is not null;
        }

        if (lookup.Candidate is null && input.UseBuyingGroupPath)
        {
            lookup = await repository.FindBuyingGroupAsync(input.Context, cancellationToken).ConfigureAwait(false);
            if (lookup.Error is not null) return Failure(input.RequestedMode, lookup.Error);
        }

        if (lookup.Candidate is null)
        {
            lookup = await repository.FindProductAsync(input.Context, cancellationToken).ConfigureAwait(false);
            if (lookup.Error is not null) return Failure(input.RequestedMode, lookup.Error);
        }

        if (lookup.Candidate is null && input.RequestedMode != FreightApplicationMode.Variable)
        {
            lookup = await repository.FindDivisionVendorAsync(input.Context, cancellationToken).ConfigureAwait(false);
            if (lookup.Error is not null) return Failure(input.RequestedMode, lookup.Error);
        }

        if (lookup.Candidate is null)
        {
            lookup = await repository.FindCorporateVendorAsync(input.Context, cancellationToken).ConfigureAwait(false);
            if (lookup.Error is not null) return Failure(input.RequestedMode, lookup.Error);
        }

        if (lookup.Candidate is null) return Empty(input.RequestedMode);
        FreightCandidate candidate = lookup.Candidate;
        FreightApplicationMode mode = accountOrCustomer && input.RequestedMode == FreightApplicationMode.Variable
            ? FreightApplicationMode.Sell
            : input.RequestedMode == FreightApplicationMode.Variable ? FreightApplicationMode.Variable : candidate.SourceMode ?? input.RequestedMode;

        decimal percentage = mode == FreightApplicationMode.Sell ? candidate.SellPercentage : candidate.CostPercentage;
        decimal raw = candidate.Source == FreightSource.Product
            ? (candidate.DirectAmount?.Value ?? 0m) * input.ProductUomConversionFactor
            : mode switch
            {
                FreightApplicationMode.Cost => input.DealerCost.Value * percentage,
                FreightApplicationMode.Sell => input.TotalCost.Value * percentage,
                FreightApplicationMode.Variable => (input.VariableUnitCost ?? input.DealerCost).Value * percentage,
                _ => throw new InvalidOperationException("Unsupported freight mode."),
            };
        Money calculated = new(decimal.Round(raw, Money.MaximumScale, MidpointRounding.AwayFromZero));
        bool exempt = IsExempt(input.Exemption);
        Money amount = exempt ? new Money(0m) : calculated;
        string typeCode = Recode(SourceCode(candidate.Source), mode, exempt);
        var component = new PriceComponent("Inbound freight", PriceComponentType.Fee, amount, candidate.Provenance);
        return new(amount, percentage, typeCode, candidate.Source, mode, exempt,
            !exempt && mode == FreightApplicationMode.Cost,
            !exempt && mode != FreightApplicationMode.Cost && !input.BilledMonthly,
            component, candidate.EffectiveDates);
    }

    private static bool IsExempt(FreightExemptionFacts facts)
    {
        if (facts.IsCustomProduct) return !facts.CustomChargeEnabled;
        return facts.ContractClass switch
        {
            FreightContractClass.SanctionedGroup => !facts.SanctionedChargeEnabled,
            FreightContractClass.NonSanctionedGroup => !facts.NonSanctionedChargeEnabled,
            FreightContractClass.Individual => !facts.IndividualChargeEnabled,
            FreightContractClass.NonContract => !facts.NonContractChargeEnabled,
            _ => false,
        };
    }

    private static string SourceCode(FreightSource source) => source switch
    {
        FreightSource.Account => "A",
        FreightSource.Customer => "W",
        FreightSource.BuyingGroup => "B",
        FreightSource.Product => "P",
        FreightSource.DivisionVendor => "D",
        FreightSource.CorporateVendor => "V",
        _ => string.Empty,
    };

    private static string Recode(string code, FreightApplicationMode mode, bool exempt)
    {
        Dictionary<string, string> map = exempt
            ? new Dictionary<string, string> { ["B"] = "J", ["D"] = "K", ["P"] = "L", ["V"] = "M", ["A"] = "N", ["W"] = "Y" }
            : mode == FreightApplicationMode.Sell
                ? new Dictionary<string, string> { ["B"] = "E", ["D"] = "F", ["P"] = "G", ["V"] = "H", ["A"] = "I", ["W"] = "X" }
                : mode == FreightApplicationMode.Variable
                    ? new Dictionary<string, string> { ["B"] = "Q", ["D"] = "R", ["P"] = "S", ["V"] = "T", ["A"] = "U", ["W"] = "Z" }
                    : new Dictionary<string, string>();
        return map.TryGetValue(code, out string? recoded) ? recoded : code;
    }

    private static FreightResult Empty(FreightApplicationMode mode) =>
        new(new Money(0m), 0m, string.Empty, null, mode, false, false, false, null, null);

    private static FreightResult Failure(FreightApplicationMode mode, PricingError error) =>
        new(new Money(0m), 0m, string.Empty, null, mode, false, false, false, null, null, error);
}
