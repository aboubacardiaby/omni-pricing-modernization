namespace Pricing.Application.SellSelection;

using Pricing.Domain.Models;

/// <summary>One subgroup level in an A6U01 sell-arrangement cascade.</summary>
public sealed class BuyingGroupSellArrangementRule : ISellArrangementRule
{
    private readonly IBuyingGroupSellArrangementRepository repository;

    public BuyingGroupSellArrangementRule(
        IBuyingGroupSellArrangementRepository repository,
        SellCostCascade cascade,
        SellArrangementLevel level,
        int priority)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        Cascade = cascade;
        Level = level;
        Priority = priority;
        Name = $"{cascade}-subgroup-{level}".ToLowerInvariant();
    }

    public string Name { get; }
    public int Priority { get; }
    public SellCostCascade Cascade { get; }
    public SellArrangementLevel Level { get; }

    public async ValueTask<SellRuleDecision> EvaluateAsync(
        PricingContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (SellCostCascadeResolver.Resolve(context) != Cascade)
        {
            return SellRuleDecision.Skipped(
                SellRuleSkipReason.NotEligible($"The active cost source does not use the {Cascade} sell cascade."));
        }

        if (context.Customer.BuyingGroupMemberships.IsEmpty)
        {
            return SellRuleDecision.Skipped(
                SellRuleSkipReason.NotEligible("No buying-group membership is available."));
        }

        if (Level == SellArrangementLevel.ProductCategory && context.Product.ProductCategory is null)
        {
            return SellRuleDecision.Skipped(
                SellRuleSkipReason.NotEligible("No product category is available for group category lookup."));
        }

        try
        {
            SellArrangementSelection? selection = await repository
                .FindSubgroupAsync(context, Cascade, Level, cancellationToken)
                .ConfigureAwait(false);
            return selection is null
                ? SellRuleDecision.Skipped(SellRuleSkipReason.NoCandidate(
                    $"No subgroup {Level} sell arrangement matched."))
                : SellRuleDecision.Applied(selection);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (BuyingGroupSellArrangementRepositoryException exception)
        {
            return SellRuleDecision.Failed(ToError(exception));
        }
    }

    internal static DependencyPricingError ToError(BuyingGroupSellArrangementRepositoryException exception) => new(
        "BUYING_GROUP_SELL_ARRANGEMENT_LOOKUP_FAILED",
        exception.Message,
        exception.LegacyErrorCode,
        exception.IsTransient,
        exception.LegacySeverityCode);
}
