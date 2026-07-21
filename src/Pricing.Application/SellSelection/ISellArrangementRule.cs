namespace Pricing.Application.SellSelection;

using Pricing.Domain.Models;

/// <summary>One ordered sell-arrangement lookup in the A6U01 cascade.</summary>
public interface ISellArrangementRule
{
    string Name { get; }
    int Priority { get; }

    ValueTask<SellRuleDecision> EvaluateAsync(
        PricingContext context,
        CancellationToken cancellationToken);
}
