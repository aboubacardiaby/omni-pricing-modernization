namespace Pricing.Application.CostSelection;

using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>Selects the healthcare price-list dealer cost after all contract rules miss.</summary>
/// <remarks>
/// COBOL: A6U01 9940/9945/9950 account-then-CID eligibility, product-over-group flag
/// precedence, and 9970/9975 highest-acquisition-cost VNG03 selection and conversion.
/// </remarks>
public sealed class HealthcareCostOverrideRule : ICostRule
{
    private readonly IHealthcareCostOverrideRepository repository;

    public HealthcareCostOverrideRule(IHealthcareCostOverrideRepository repository) =>
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public string Name => "healthcare-cost-override";
    public int Priority => CostRulePriorities.HealthcareOverride;

    public async ValueTask<CostRuleDecision> EvaluateAsync(
        PricingContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (context.Request.IsSpecialContract)
        {
            return CostRuleDecision.Skipped(
                CostRuleSkipReason.NotEligible("Special-contract requests exit before healthcare processing."));
        }

        try
        {
            HealthcareOverrideData data = await repository.FindAsync(context, cancellationToken).ConfigureAwait(false);
            HealthcareCostEligibility? eligibility = ChooseEligibility(data, context.Request.PricingDate);
            if (eligibility is null)
            {
                return CostRuleDecision.Skipped(
                    CostRuleSkipReason.NoCandidate("No active healthcare group/account or group/customer product record was found."));
            }

            bool isCostOverridden = eligibility.ProductCostOverridden ?? eligibility.GroupCostOverridden;
            if (isCostOverridden)
            {
                return CostRuleDecision.Skipped(
                    CostRuleSkipReason.Excluded("Healthcare product/group override flag preserves the normal dealer-cost fallback."));
            }

            HealthcarePriceListCandidate? selected = data.PriceListCandidates
                .Where(candidate => IsPriceListEligible(candidate, eligibility.ProductCostDates, context.Request.PricingDate))
                .OrderByDescending(candidate => candidate.AcquisitionCost.Value)
                .ThenBy(candidate => candidate.ActiveDate)
                .FirstOrDefault();
            if (selected is null)
            {
                return CostRuleDecision.Skipped(
                    CostRuleSkipReason.NoCandidate("No VNG03 price-list row overlaps the healthcare product-cost window."));
            }

            HealthcareSellOverrideTerms? sellOverride = ChooseSellOverride(data, context.Request.PricingDate);
            return CostRuleDecision.Applied(new HealthcareCostSelection(
                eligibility.HealthcareGroupId,
                eligibility.Scope.ToString().ToUpperInvariant(),
                selected.DealerCost,
                selected.UnitOfMeasure,
                selected.AcquisitionCost,
                selected.ListEffectiveDate,
                selected.Provenance,
                sellOverride));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HealthcareCostOverrideRepositoryException exception)
        {
            return CostRuleDecision.Failed(new DependencyPricingError(
                "HEALTHCARE_COST_OVERRIDE_LOOKUP_FAILED",
                exception.Message,
                exception.LegacyErrorCode,
                exception.IsTransient,
                exception.LegacySeverityCode));
        }
    }

    private static HealthcareCostEligibility? ChooseEligibility(HealthcareOverrideData data, DateOnly pricingDate)
    {
        if (data.AccountCostEligibility is { } account && IsEligibilityActive(account, pricingDate))
        {
            return account;
        }

        return data.CustomerCostEligibility is { } customer && IsEligibilityActive(customer, pricingDate)
            ? customer
            : null;
    }

    private static bool IsEligibilityActive(HealthcareCostEligibility eligibility, DateOnly pricingDate) =>
        eligibility.HeaderDates.Contains(pricingDate)
        && eligibility.AssignmentDates.Contains(pricingDate)
        && eligibility.GroupDetailDates.Contains(pricingDate)
        && eligibility.ProductCostDates.Contains(pricingDate);

    private static bool IsPriceListEligible(
        HealthcarePriceListCandidate candidate,
        PricingDateRange productCostDates,
        DateOnly pricingDate) =>
        candidate.ActiveDate <= pricingDate
        && ((candidate.ActiveDate <= productCostDates.EffectiveDate
                && (candidate.ExpirationDate is null || candidate.ExpirationDate >= productCostDates.EffectiveDate))
            || (candidate.ActiveDate >= productCostDates.EffectiveDate
                && (productCostDates.ExpirationDate is null
                    || candidate.ActiveDate <= productCostDates.ExpirationDate)));

    private static HealthcareSellOverrideTerms? ChooseSellOverride(HealthcareOverrideData data, DateOnly pricingDate)
    {
        if (data.AccountSellOverride is { } account && account.EffectiveDates.Contains(pricingDate))
        {
            return account;
        }

        return data.CustomerSellOverride is { } customer && customer.EffectiveDates.Contains(pricingDate)
            ? customer
            : null;
    }
}
