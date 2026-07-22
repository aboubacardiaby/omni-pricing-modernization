namespace Pricing.Application.Fees;

using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

public enum SurchargeLevel
{
    AccountCategoryVendor, AccountCategoryDefault, AccountVendor, AccountDefault,
    CustomerCategoryVendor, CustomerCategoryDefault, CustomerVendor, CustomerDefault,
    DivisionCategoryVendor, DivisionCategoryDefault, DivisionVendor, DivisionDefault,
    CorporateCategoryVendor, CorporateCategoryDefault, CorporateVendor, CorporateDefault,
    BuyingGroup,
}

public sealed record SurchargeCandidate(SurchargeLevel Level, decimal Percentage, string SourceCode, RuleProvenance Provenance);
public sealed record SurchargeLookupResult(SurchargeCandidate? Candidate, PricingError? SoftError = null);

public interface ISurchargeRepository
{
    ValueTask<SurchargeLookupResult> FindAsync(PricingContext context, SurchargeLevel level, CancellationToken cancellationToken);
}

public sealed record SurchargeInput(PricingContext Context, Money TotalCost, bool IsPriceLocked, bool HasProductCategory, string? BillingFrequency);
public sealed record SurchargeResult(Money Amount, decimal Percentage, string SourceCode, bool FoldIntoLine, PriceComponent? Component, PricingError? SoftError = null);

/// <summary>Implements A6U01 0215/0250/7210 category surcharge selection and calculation.</summary>
public sealed class SurchargeEngine(ISurchargeRepository repository)
{
    private static readonly SurchargeLevel[] Priority =
    [
        SurchargeLevel.AccountCategoryVendor, SurchargeLevel.AccountCategoryDefault, SurchargeLevel.AccountVendor, SurchargeLevel.AccountDefault,
        SurchargeLevel.CustomerCategoryVendor, SurchargeLevel.CustomerCategoryDefault, SurchargeLevel.CustomerVendor, SurchargeLevel.CustomerDefault,
        SurchargeLevel.DivisionCategoryVendor, SurchargeLevel.DivisionCategoryDefault, SurchargeLevel.DivisionVendor, SurchargeLevel.DivisionDefault,
        SurchargeLevel.CorporateCategoryVendor, SurchargeLevel.CorporateCategoryDefault, SurchargeLevel.CorporateVendor, SurchargeLevel.CorporateDefault,
        SurchargeLevel.BuyingGroup,
    ];

    public async ValueTask<SurchargeResult> CalculateAsync(SurchargeInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        if (input.IsPriceLocked) return Empty();

        foreach (SurchargeLevel level in Priority)
        {
            if (!input.HasProductCategory && IsCategory(level)) continue;
            SurchargeLookupResult lookup = await repository.FindAsync(input.Context, level, cancellationToken).ConfigureAwait(false);
            if (lookup.SoftError is not null) return new(new Money(0m), 0m, string.Empty, false, null, lookup.SoftError);
            if (lookup.Candidate is null) continue;

            SurchargeCandidate candidate = lookup.Candidate;
            Money amount = Truncate(input.TotalCost.Value * candidate.Percentage);
            bool monthly = input.BillingFrequency is "MA" or "MM";
            var component = new PriceComponent("Category surcharge", PriceComponentType.Surcharge, amount, candidate.Provenance);
            return new(amount, candidate.Percentage, candidate.SourceCode, !monthly, component);
        }

        return Empty();
    }

    private static bool IsCategory(SurchargeLevel level) => level.ToString().Contains("Category", StringComparison.Ordinal);
    private static Money Truncate(decimal value)
    {
        decimal factor = 100_000_000m;
        return new Money(decimal.Truncate(value * factor) / factor);
    }
    private static SurchargeResult Empty() => new(new Money(0m), 0m, string.Empty, false, null);
}

public enum AccountPriceMethod { CostContract, Stock, Usage, Other }

public sealed record MarkupClassificationInput(
    bool IsGrossMargin,
    AccountPriceMethod PriceMethod,
    bool IsCustomProduct,
    decimal? CustomPercentage,
    bool CostContractFound,
    bool IsGroupSanctioned,
    bool IsDivision01NonSanctioned,
    bool IsIndividualContract,
    bool IsNonDivision01,
    bool IsStockItem,
    bool IsSpecifiedUsage,
    decimal SanctionedPercentage,
    decimal NonSanctionedPercentage,
    decimal IndividualPercentage,
    decimal NonContractPercentage,
    Money TotalCost,
    IReadOnlyDictionary<DistributionCategory, string?> BillingFrequency,
    RuleProvenance Provenance);

public sealed record MarkupCompositionResult(
    DistributionCategory? Category,
    decimal Percentage,
    Money SellPrice,
    DistributionFeeResult? Distribution,
    PricingError? Error = null)
{
    public bool IsFailure => Error is not null;
}

/// <summary>Implements A6U01 7090 gross-margin category selection and delegates 7095 composition.</summary>
public static class MarkupCompositionEngine
{
    public static MarkupCompositionResult Calculate(MarkupClassificationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!input.IsGrossMargin) return new(null, 0m, input.TotalCost, null);

        (DistributionCategory Category, decimal Percentage)? selected = input.PriceMethod switch
        {
            AccountPriceMethod.CostContract => SelectCostContract(input),
            AccountPriceMethod.Stock => input.IsStockItem
                ? (DistributionCategory.GroupSanctioned, input.SanctionedPercentage)
                : (DistributionCategory.NonContract, input.NonContractPercentage),
            AccountPriceMethod.Usage => input.IsSpecifiedUsage
                ? (DistributionCategory.GroupSanctioned, input.SanctionedPercentage)
                : (DistributionCategory.NonContract, input.NonContractPercentage),
            _ => null,
        };
        if (selected is null)
        {
            PricingError error = input.PriceMethod == AccountPriceMethod.CostContract
                ? new UnsupportedBehaviorPricingError("INVALID_GROSS_MARGIN_SCENARIO", "Invalid cost-contract scenario for gross margin.", "602", "Cost-contract classification", "70")
                : new UnsupportedBehaviorPricingError("GROSS_MARGIN_METHOD_UNSUPPORTED", "Gross-margin account price method is unsupported.", Blocker: "accountPriceMethod");
            return new(null, 0m, input.TotalCost, null, error);
        }

        decimal percentage = selected.Value.Percentage >= 1m ? 0.9999m : selected.Value.Percentage;
        Money sell = new(decimal.Round(input.TotalCost.Value / (1m - percentage), Money.MaximumScale, MidpointRounding.AwayFromZero));
        input.BillingFrequency.TryGetValue(selected.Value.Category, out string? billing);
        DistributionFeeResult distribution = DistributionFeeEngine.Calculate(new(selected.Value.Category, sell, input.TotalCost, billing, input.Provenance));
        return new(selected.Value.Category, percentage, distribution.LineSellPrice, distribution);
    }

    private static (DistributionCategory, decimal)? SelectCostContract(MarkupClassificationInput input)
    {
        if (input.IsCustomProduct && input.CustomPercentage is decimal custom) return (DistributionCategory.Customer, custom);
        if (!input.CostContractFound) return (DistributionCategory.NonContract, input.NonContractPercentage);
        if (input.IsGroupSanctioned) return (DistributionCategory.GroupSanctioned, input.SanctionedPercentage);
        if (input.IsDivision01NonSanctioned) return (DistributionCategory.GroupNonSanctioned, input.NonSanctionedPercentage);
        if (input.IsIndividualContract || input.IsNonDivision01) return (DistributionCategory.Individual, input.IndividualPercentage);
        return null;
    }
}
