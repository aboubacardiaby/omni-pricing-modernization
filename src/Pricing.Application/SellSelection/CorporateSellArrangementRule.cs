namespace Pricing.Application.SellSelection;

using Pricing.Domain.Models;

/// <summary>Selects corporate product/category/vendor, allowing the same-level group override.</summary>
/// <remarks>
/// COBOL: A6U01 0180 levels 1.5/3.5/4.5 call 7240/7250/7255 after a corporate
/// match. A subgroup or nearest-parent match wins; otherwise 7245 restores corporate data.
/// </remarks>
public sealed class CorporateSellArrangementRule : ISellArrangementRule
{
    private readonly ICorporateSellArrangementRepository repository;

    public CorporateSellArrangementRule(
        ICorporateSellArrangementRepository repository,
        SellArrangementLevel level,
        int priority)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        if (level is not (SellArrangementLevel.Product
            or SellArrangementLevel.ProductCategory
            or SellArrangementLevel.Vendor))
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "Corporate rules support product, category, and vendor only.");
        }

        Level = level;
        Priority = priority;
        Name = $"individual-contract-corporate-{level}".ToLowerInvariant();
    }

    public string Name { get; }
    public int Priority { get; }
    public SellArrangementLevel Level { get; }

    public async ValueTask<SellRuleDecision> EvaluateAsync(
        PricingContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (SellCostCascadeResolver.Resolve(context) != SellCostCascade.IndividualContract)
        {
            return SellRuleDecision.Skipped(
                SellRuleSkipReason.NotEligible("Corporate sell levels exist only in the individual-contract cascade."));
        }

        if (Level == SellArrangementLevel.ProductCategory && context.Product.ProductCategory is null)
        {
            return SellRuleDecision.Skipped(
                SellRuleSkipReason.NotEligible("No product category is available for corporate category lookup."));
        }

        try
        {
            CorporateSellArrangementMatch match = await repository
                .FindAsync(context, Level, cancellationToken)
                .ConfigureAwait(false);
            if (match.CorporateSelection is null)
            {
                return SellRuleDecision.Skipped(
                    SellRuleSkipReason.NoCandidate($"No corporate {Level} sell arrangement matched."));
            }

            return SellRuleDecision.Applied(match.GroupOverrideSelection ?? match.CorporateSelection);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (CorporateSellArrangementRepositoryException exception)
        {
            return SellRuleDecision.Failed(new DependencyPricingError(
                "CORPORATE_SELL_ARRANGEMENT_LOOKUP_FAILED",
                exception.Message,
                exception.LegacyErrorCode,
                exception.IsTransient,
                exception.LegacySeverityCode));
        }
    }
}
