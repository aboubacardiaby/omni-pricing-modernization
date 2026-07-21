namespace Pricing.Application.CostSelection;

using System.Collections.Immutable;
using Pricing.Domain.Models;

public sealed record CostRuleTraceEntry(
    string RuleName,
    int Priority,
    CostRuleOutcome Outcome,
    CostRuleSkipReason? SkipReason,
    RuleProvenance? Provenance,
    PricingError? Error);

public sealed record CostSelectionResult(
    ICostSourceSelection? Selection,
    ImmutableArray<CostRuleTraceEntry> Trace,
    PricingError? Error)
{
    public bool IsSelected => Selection is not null && Error is null;
    public bool IsFailure => Error is not null;
}
