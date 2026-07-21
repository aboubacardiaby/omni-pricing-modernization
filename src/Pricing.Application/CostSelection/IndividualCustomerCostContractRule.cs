namespace Pricing.Application.CostSelection;

using Pricing.Domain.Models;

/// <summary>Selects a usable individual customer cost contract, including duplicate resolution.</summary>
/// <remarks>
/// COBOL: A6U01 0235-SEL-INDV-CNT-010, 0270-SEL-MIN-INDV-CNT-010,
/// 0272-COMPARE-COST, and 7360-VERIFY-FOR-EXCL. This normal-request rule does not
/// implement the special-contract #601 behavior owned by T027.
/// </remarks>
public sealed class IndividualCustomerCostContractRule : ICostRule
{
    private readonly IIndividualCostContractRepository repository;

    public IndividualCustomerCostContractRule(IIndividualCostContractRepository repository) =>
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public string Name => "individual-customer-cost-contract";
    public int Priority => CostRulePriorities.IndividualCustomerContract;

    public async ValueTask<CostRuleDecision> EvaluateAsync(
        PricingContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (context.Request.IsSpecialContract)
        {
            return CostRuleDecision.Skipped(
                CostRuleSkipReason.NotEligible("Special requests are handled only by SpecialContractCostRule."));
        }

        if (context.Customer.Customer is null)
        {
            return CostRuleDecision.Skipped(
                CostRuleSkipReason.NotEligible("No active customer number is available for CCG06 assignment lookup."));
        }

        try
        {
            var candidates = await repository.FindCandidatesAsync(context, cancellationToken).ConfigureAwait(false);
            if (candidates.IsDefaultOrEmpty)
            {
                return CostRuleDecision.Skipped(
                    CostRuleSkipReason.NoCandidate("No active individual customer cost contract was found."));
            }

            IndividualCostContractCandidate? selected = null;
            int excludedCount = 0;
            int outOfDateCount = 0;
            foreach (IndividualCostContractCandidate candidate in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (candidate.IsExcluded)
                {
                    excludedCount++;
                    continue;
                }

                if (candidate.EligibilityDates.IsDefault
                    || candidate.EligibilityDates.Any(range => !range.Contains(context.Request.PricingDate)))
                {
                    outOfDateCount++;
                    continue;
                }

                // A6U01 0272 uses strict-less-than. The first zero is valid; equal later rows never replace it.
                if (selected is null || candidate.NormalizedUnitCost.Value < selected.NormalizedUnitCost.Value)
                {
                    selected = candidate;
                }
            }

            if (selected is null)
            {
                return CostRuleDecision.Skipped(CreateRejectedReason(candidates.Length, excludedCount, outOfDateCount));
            }

            return CostRuleDecision.Applied(new ContractSelection(
                selected.Contract,
                selected.ContractType,
                selected.NormalizedUnitCost,
                selected.NormalizedUnitOfMeasure,
                selected.Provenance));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (IndividualCostContractRepositoryException exception)
        {
            return CostRuleDecision.Failed(new DependencyPricingError(
                "INDIVIDUAL_COST_CONTRACT_LOOKUP_FAILED",
                exception.Message,
                exception.LegacyErrorCode,
                exception.IsTransient,
                exception.LegacySeverityCode));
        }
    }

    private static CostRuleSkipReason CreateRejectedReason(int total, int excluded, int outOfDate)
    {
        if (excluded == total)
        {
            return CostRuleSkipReason.Excluded("Every individual cost-contract candidate was excluded.");
        }

        if (outOfDate == total)
        {
            return CostRuleSkipReason.Expired("Every individual cost-contract candidate was outside its effective dates.");
        }

        return CostRuleSkipReason.NotEligible(
            $"No usable individual contract remained: {excluded} excluded and {outOfDate} outside effective dates.");
    }
}
