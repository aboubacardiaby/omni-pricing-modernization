namespace Pricing.Application.CostSelection;

using System.Collections.Immutable;
using Pricing.Domain.Models;

public sealed class CostRuleEvaluator
{
    private readonly ImmutableArray<ICostRule> rules;

    public CostRuleEvaluator(IEnumerable<ICostRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        this.rules = rules
            .Select(rule => rule ?? throw new ArgumentException("Cost rules cannot contain null entries.", nameof(rules)))
            .OrderBy(rule => rule.Priority)
            .ToImmutableArray();
        ValidateConfiguration(this.rules);
    }

    /// <summary>Evaluates rules in ascending priority and stops after the first applied rule or error.</summary>
    /// <remarks>
    /// A6U01 0195-PRO-COST-CONT-010 exits immediately after a usable individual or group
    /// contract is found. Remaining rules are retained in the trace as not evaluated.
    /// </remarks>
    public async ValueTask<CostSelectionResult> EvaluateAsync(
        PricingContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var trace = ImmutableArray.CreateBuilder<CostRuleTraceEntry>(rules.Length);
        for (int index = 0; index < rules.Length; index++)
        {
            ICostRule rule = rules[index];
            cancellationToken.ThrowIfCancellationRequested();
            CostRuleDecision decision = await rule.EvaluateAsync(context, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Cost rule '{rule.Name}' returned no decision.");
            cancellationToken.ThrowIfCancellationRequested();

            trace.Add(new CostRuleTraceEntry(
                rule.Name,
                rule.Priority,
                decision.Outcome,
                decision.SkipReason,
                decision.Selection?.Provenance,
                decision.Error));

            if (decision.Outcome == CostRuleOutcome.Applied)
            {
                AddNotEvaluated(trace, index + 1, CostRuleSkipReason.HigherPriorityRuleApplied(rule.Name));
                return new CostSelectionResult(decision.Selection, trace.MoveToImmutable(), null);
            }

            if (decision.Outcome == CostRuleOutcome.Failed)
            {
                AddNotEvaluated(trace, index + 1, CostRuleSkipReason.PriorRuleFailed(rule.Name));
                return new CostSelectionResult(null, trace.MoveToImmutable(), decision.Error);
            }

            if (decision.Outcome != CostRuleOutcome.Skipped)
            {
                throw new InvalidOperationException($"Cost rule '{rule.Name}' returned unsupported outcome '{decision.Outcome}'.");
            }
        }

        return new CostSelectionResult(null, trace.MoveToImmutable(), null);
    }

    private void AddNotEvaluated(
        ImmutableArray<CostRuleTraceEntry>.Builder trace,
        int startIndex,
        CostRuleSkipReason reason)
    {
        for (int index = startIndex; index < rules.Length; index++)
        {
            ICostRule rule = rules[index];
            trace.Add(new CostRuleTraceEntry(
                rule.Name,
                rule.Priority,
                CostRuleOutcome.NotEvaluated,
                reason,
                null,
                null));
        }
    }

    private static void ValidateConfiguration(ImmutableArray<ICostRule> rules)
    {
        IGrouping<int, ICostRule>? duplicatePriority = rules
            .GroupBy(rule => rule.Priority)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicatePriority is not null)
        {
            string names = string.Join(", ", duplicatePriority.Select(rule => rule.Name));
            throw new InvalidOperationException(
                $"Cost rule priority {duplicatePriority.Key} is duplicated by: {names}.");
        }

        IGrouping<string, ICostRule>? duplicateName = rules
            .GroupBy(rule => rule.Name, StringComparer.Ordinal)
            .FirstOrDefault(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1);
        if (duplicateName is not null)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(duplicateName.Key)
                    ? "Cost rule names cannot be blank."
                    : $"Cost rule name '{duplicateName.Key}' is duplicated.");
        }
    }
}
