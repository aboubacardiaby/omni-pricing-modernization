namespace Pricing.Application.CostSelection;

using Pricing.Domain.Models;

/// <summary>An independently testable cost-selection strategy in the A6U01 cascade.</summary>
/// <remarks>
/// A6U01 7190-PRO-COST-AMTS-010 and 0195-PRO-COST-CONT-010 establish ordered,
/// first-usable-result behavior. Concrete rules and their priorities are introduced by T025-T029.
/// </remarks>
public interface ICostRule
{
    string Name { get; }
    int Priority { get; }

    ValueTask<CostRuleDecision> EvaluateAsync(
        PricingContext context,
        CancellationToken cancellationToken);
}
