namespace Pricing.Application.SellSelection;

using System.Collections.Immutable;
using Pricing.Domain.Models;

public sealed record SellArrangementResult(
    SellArrangementSelection? Selection,
    ImmutableArray<SellRuleTraceEntry> Trace,
    PricingError? Error)
{
    public bool IsSelected => Selection is not null;
    public bool IsFailure => Error is not null;
    public bool HasNoArrangement => Selection is null && Error is null;
}

public sealed record SellRuleTraceEntry(
    string RuleName,
    int Priority,
    SellRuleOutcome Outcome,
    SellRuleSkipReason? SkipReason,
    RuleProvenance? Provenance,
    PricingError? Error);
