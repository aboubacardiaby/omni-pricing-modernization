namespace Pricing.Application.CostSelection;

using Pricing.Domain.Models;

/// <summary>Handles the restricted individual lookup and early-exit controls for special contracts.</summary>
/// <remarks>
/// COBOL: A6U01 0235 restricts CCG01.F_BYPASS to B and raises #601 when absent;
/// 0365-PROCESS-SPECIAL-C-010 and 7960-PRO-SPECIAL-MOVES-010 copy raw cost/suggested
/// sell, skip adjustments and 7715 rounding, but still run 7720 expiration selection.
/// </remarks>
public sealed class SpecialContractCostRule : ICostRule
{
    private readonly IIndividualCostContractRepository repository;

    public SpecialContractCostRule(IIndividualCostContractRepository repository) =>
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public string Name => "special-contract";
    public int Priority => CostRulePriorities.SpecialContract;

    public async ValueTask<CostRuleDecision> EvaluateAsync(
        PricingContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (!context.Request.IsSpecialContract)
        {
            return CostRuleDecision.Skipped(
                CostRuleSkipReason.NotEligible("The request did not set OMGPR-F-SPECIAL-CONTRACT."));
        }

        try
        {
            var candidates = await repository.FindCandidatesAsync(context, cancellationToken).ConfigureAwait(false);
            IndividualCostContractCandidate? selected = null;
            foreach (IndividualCostContractCandidate candidate in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (candidate.IsExcluded
                    || candidate.EligibilityDates.IsDefault
                    || candidate.EligibilityDates.Any(range => !range.Contains(context.Request.PricingDate)))
                {
                    continue;
                }

                if (selected is null || candidate.NormalizedUnitCost.Value < selected.NormalizedUnitCost.Value)
                {
                    selected = candidate;
                }
            }

            if (selected is null)
            {
                return CostRuleDecision.Failed(new MissingDataPricingError(
                    "SPECIAL_CONTRACT_NOT_FOUND",
                    "#601-SPECIAL CONTRACT NOTFOUND FOR PRODUCT",
                    "601",
                    "contract"));
            }

            if (selected.SuggestedSellPrice is null)
            {
                return CostRuleDecision.Failed(new MissingDataPricingError(
                    "SPECIAL_CONTRACT_SUGGESTED_SELL_MISSING",
                    "Special contract did not provide the required suggested-sell amount.",
                    null,
                    "suggestedSellPrice"));
            }

            var special = new SpecialContractSelection(
                selected.SuggestedSellPrice.Value,
                BypassAdjustments: true,
                BypassFinalRounding: true,
                selected.EligibilityDates);
            return CostRuleDecision.Applied(new ContractSelection(
                selected.Contract,
                "SPECIAL",
                selected.NormalizedUnitCost,
                selected.NormalizedUnitOfMeasure,
                selected.Provenance,
                SpecialContract: special));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (IndividualCostContractRepositoryException exception)
        {
            return CostRuleDecision.Failed(new DependencyPricingError(
                "SPECIAL_CONTRACT_LOOKUP_FAILED",
                exception.Message,
                exception.LegacyErrorCode,
                exception.IsTransient,
                exception.LegacySeverityCode));
        }
    }
}
