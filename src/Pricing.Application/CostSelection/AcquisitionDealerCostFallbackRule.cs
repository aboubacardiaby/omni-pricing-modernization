namespace Pricing.Application.CostSelection;

using Pricing.Domain.Models;

/// <summary>Selects normalized VNG03 dealer cost as the terminal cost source.</summary>
/// <remarks>
/// COBOL: A6U01 7105 direct VNG03 lookup; 7575 latest-effective/highest-level fallback;
/// 7190 moves dealer cost/UOM and resets JIT and freight exemptions to N.
/// </remarks>
public sealed class AcquisitionDealerCostFallbackRule : ICostRule
{
    private readonly IAcquisitionCostRepository repository;

    public AcquisitionDealerCostFallbackRule(IAcquisitionCostRepository repository) =>
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public string Name => "acquisition-dealer-cost-fallback";
    public int Priority => CostRulePriorities.AcquisitionDealerFallback;

    public async ValueTask<CostRuleDecision> EvaluateAsync(
        PricingContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (context.Request.IsSpecialContract)
        {
            return CostRuleDecision.Skipped(
                CostRuleSkipReason.NotEligible("Special-contract requests cannot fall back to acquisition cost."));
        }

        try
        {
            AcquisitionCostData data = await repository.FindAsync(context, cancellationToken).ConfigureAwait(false);
            AcquisitionPriceListCandidate? selected = data.DirectMatch
                ?? data.FallbackCandidates
                    .Where(candidate => candidate.PriceListEffectiveDate <= context.Request.PricingDate)
                    .OrderByDescending(candidate => candidate.PriceListEffectiveDate)
                    .ThenByDescending(candidate => candidate.PriceLevel, StringComparer.Ordinal)
                    .FirstOrDefault();

            if (selected is null)
            {
                return CostRuleDecision.Failed(new MissingDataPricingError(
                    "ACQUISITION_PRICE_LIST_NOT_FOUND",
                    "No effective vendor price-list baseline was found.",
                    "130",
                    "VNG03"));
            }

            return CostRuleDecision.Applied(new AcquisitionCostSelection(
                selected.DealerCost,
                selected.UnitOfMeasure,
                selected.AcquisitionCost,
                selected.PriceLevel,
                selected.PriceListEffectiveDate,
                false,
                false,
                selected.Provenance));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (AcquisitionCostRepositoryException exception)
        {
            return CostRuleDecision.Failed(new DependencyPricingError(
                "ACQUISITION_COST_LOOKUP_FAILED",
                exception.Message,
                exception.LegacyErrorCode,
                exception.IsTransient,
                exception.LegacySeverityCode));
        }
    }
}
