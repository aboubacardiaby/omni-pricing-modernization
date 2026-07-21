namespace Pricing.Application.SellSelection;

using Pricing.Domain.Models;

public sealed record SellRuleDecision
{
    private SellRuleDecision(
        SellRuleOutcome outcome,
        SellArrangementSelection? selection,
        SellRuleSkipReason? skipReason,
        PricingError? error)
    {
        Outcome = outcome;
        Selection = selection;
        SkipReason = skipReason;
        Error = error;
    }

    public SellRuleOutcome Outcome { get; }
    public SellArrangementSelection? Selection { get; }
    public SellRuleSkipReason? SkipReason { get; }
    public PricingError? Error { get; }

    public static SellRuleDecision Applied(SellArrangementSelection selection) =>
        new(SellRuleOutcome.Applied, selection ?? throw new ArgumentNullException(nameof(selection)), null, null);

    public static SellRuleDecision Skipped(SellRuleSkipReason reason) =>
        new(SellRuleOutcome.Skipped, null, reason ?? throw new ArgumentNullException(nameof(reason)), null);

    public static SellRuleDecision Failed(PricingError error) =>
        new(SellRuleOutcome.Failed, null, null, error ?? throw new ArgumentNullException(nameof(error)));
}

public enum SellRuleOutcome
{
    Applied,
    Skipped,
    Failed,
    NotEvaluated,
}

public sealed record SellRuleSkipReason(string Code, string Description)
{
    public static SellRuleSkipReason NotEligible(string description) => new("NOT_ELIGIBLE", description);
    public static SellRuleSkipReason NoCandidate(string description) => new("NO_CANDIDATE", description);
    public static SellRuleSkipReason Expired(string description) => new("EXPIRED", description);
    public static SellRuleSkipReason HigherPriorityRuleApplied(string ruleName) =>
        new("HIGHER_PRIORITY_RULE_APPLIED", $"Rule '{ruleName}' already selected a sell arrangement.");
    public static SellRuleSkipReason PriorRuleFailed(string ruleName) =>
        new("PRIOR_RULE_FAILED", $"Rule '{ruleName}' stopped sell-arrangement selection with an error.");
}
