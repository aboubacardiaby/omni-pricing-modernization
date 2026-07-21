namespace Pricing.Application.CostSelection;

using Pricing.Domain.Models;

public sealed record CostRuleDecision
{
    private CostRuleDecision(
        CostRuleOutcome outcome,
        ICostSourceSelection? selection,
        CostRuleSkipReason? skipReason,
        PricingError? error)
    {
        Outcome = outcome;
        Selection = selection;
        SkipReason = skipReason;
        Error = error;
    }

    public CostRuleOutcome Outcome { get; }
    public ICostSourceSelection? Selection { get; }
    public CostRuleSkipReason? SkipReason { get; }
    public PricingError? Error { get; }

    public static CostRuleDecision Applied(ICostSourceSelection selection) =>
        new(CostRuleOutcome.Applied, selection ?? throw new ArgumentNullException(nameof(selection)), null, null);

    public static CostRuleDecision Skipped(CostRuleSkipReason reason) =>
        new(CostRuleOutcome.Skipped, null, reason ?? throw new ArgumentNullException(nameof(reason)), null);

    public static CostRuleDecision Failed(PricingError error) =>
        new(CostRuleOutcome.Failed, null, null, error ?? throw new ArgumentNullException(nameof(error)));
}

public enum CostRuleOutcome
{
    Applied,
    Skipped,
    Failed,
    NotEvaluated,
}

public sealed record CostRuleSkipReason(string Code, string Description)
{
    public static CostRuleSkipReason NotEligible(string description) => new("NOT_ELIGIBLE", description);
    public static CostRuleSkipReason NoCandidate(string description) => new("NO_CANDIDATE", description);
    public static CostRuleSkipReason Excluded(string description) => new("EXCLUDED", description);
    public static CostRuleSkipReason Expired(string description) => new("EXPIRED", description);
    public static CostRuleSkipReason UnsupportedEvidence(string description) => new("UNSUPPORTED_EVIDENCE", description);
    public static CostRuleSkipReason HigherPriorityRuleApplied(string ruleName) =>
        new("HIGHER_PRIORITY_RULE_APPLIED", $"Rule '{ruleName}' already selected cost.");
    public static CostRuleSkipReason PriorRuleFailed(string ruleName) =>
        new("PRIOR_RULE_FAILED", $"Rule '{ruleName}' stopped cost selection with an error.");
}
