namespace Pricing.Application.SellSelection;

using Pricing.Domain.Models;

/// <summary>One account/customer-number level in an A6U01 cost-linked sell cascade.</summary>
public sealed class AccountCustomerSellArrangementRule : ISellArrangementRule
{
    private readonly IAccountCustomerSellArrangementRepository repository;

    public AccountCustomerSellArrangementRule(
        IAccountCustomerSellArrangementRepository repository,
        SellCostCascade cascade,
        SellArrangementScope scope,
        SellArrangementLevel level,
        int priority)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        Cascade = cascade;
        Scope = scope;
        Level = level;
        Priority = priority;
        Name = $"{cascade}-{scope}-{level}".ToLowerInvariant();
    }

    public string Name { get; }
    public int Priority { get; }
    public SellCostCascade Cascade { get; }
    public SellArrangementScope Scope { get; }
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

        if (Scope == SellArrangementScope.CustomerNumber && context.Customer.Customer is null)
        {
            return SellRuleDecision.Skipped(
                SellRuleSkipReason.NotEligible("No customer number is available for customer-number sell lookup."));
        }

        if (Level == SellArrangementLevel.ProductCategory && context.Product.ProductCategory is null)
        {
            return SellRuleDecision.Skipped(
                SellRuleSkipReason.NotEligible("No product category is available for category sell lookup."));
        }

        try
        {
            SellArrangementSelection? selection = await repository
                .FindAsync(context, Scope, Level, cancellationToken)
                .ConfigureAwait(false);
            return selection is null
                ? SellRuleDecision.Skipped(SellRuleSkipReason.NoCandidate(
                    $"No {Scope} {Level} sell arrangement matched."))
                : SellRuleDecision.Applied(selection);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (SellArrangementRepositoryException exception)
        {
            return SellRuleDecision.Failed(new DependencyPricingError(
                "SELL_ARRANGEMENT_LOOKUP_FAILED",
                exception.Message,
                exception.LegacyErrorCode,
                exception.IsTransient,
                exception.LegacySeverityCode));
        }
    }

}
