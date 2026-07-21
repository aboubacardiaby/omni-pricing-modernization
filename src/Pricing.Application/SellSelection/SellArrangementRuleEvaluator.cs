namespace Pricing.Application.SellSelection;

using System.Collections.Immutable;
using Pricing.Domain.Models;

/// <summary>Evaluates the active A6U01 sell cascade in deterministic first-match order.</summary>
/// <remarks>
/// COBOL: A6U01 7180 dispatches by cost-source state into 0180, 7075, or 0185;
/// each cascade exits on its first match. An exhausted cascade is not an error and is
/// retained as no-arrangement so T036 can apply the confirmed list-price default.
/// </remarks>
public sealed class SellArrangementRuleEvaluator
{
    private readonly ImmutableArray<ISellArrangementRule> rules;

    public SellArrangementRuleEvaluator(IEnumerable<ISellArrangementRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        this.rules = rules
            .Select(rule => rule ?? throw new ArgumentException("Sell rules cannot contain null entries.", nameof(rules)))
            .OrderBy(rule => rule.Priority)
            .ToImmutableArray();
        ValidateConfiguration(this.rules);
    }

    public async ValueTask<SellArrangementResult> EvaluateAsync(
        PricingContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var trace = ImmutableArray.CreateBuilder<SellRuleTraceEntry>(rules.Length);
        for (int index = 0; index < rules.Length; index++)
        {
            ISellArrangementRule rule = rules[index];
            cancellationToken.ThrowIfCancellationRequested();
            SellRuleDecision decision = await rule.EvaluateAsync(context, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Sell rule '{rule.Name}' returned no decision.");
            cancellationToken.ThrowIfCancellationRequested();

            trace.Add(new SellRuleTraceEntry(
                rule.Name,
                rule.Priority,
                decision.Outcome,
                decision.SkipReason,
                decision.Selection?.Provenance,
                decision.Error));

            if (decision.Outcome == SellRuleOutcome.Applied)
            {
                AddNotEvaluated(trace, index + 1, SellRuleSkipReason.HigherPriorityRuleApplied(rule.Name));
                return new SellArrangementResult(decision.Selection, trace.MoveToImmutable(), null);
            }

            if (decision.Outcome == SellRuleOutcome.Failed)
            {
                AddNotEvaluated(trace, index + 1, SellRuleSkipReason.PriorRuleFailed(rule.Name));
                return new SellArrangementResult(null, trace.MoveToImmutable(), decision.Error);
            }

            if (decision.Outcome != SellRuleOutcome.Skipped)
            {
                throw new InvalidOperationException(
                    $"Sell rule '{rule.Name}' returned unsupported outcome '{decision.Outcome}'.");
            }
        }

        return new SellArrangementResult(null, trace.MoveToImmutable(), null);
    }

    private void AddNotEvaluated(
        ImmutableArray<SellRuleTraceEntry>.Builder trace,
        int startIndex,
        SellRuleSkipReason reason)
    {
        for (int index = startIndex; index < rules.Length; index++)
        {
            ISellArrangementRule rule = rules[index];
            trace.Add(new SellRuleTraceEntry(
                rule.Name,
                rule.Priority,
                SellRuleOutcome.NotEvaluated,
                reason,
                null,
                null));
        }
    }

    private static void ValidateConfiguration(ImmutableArray<ISellArrangementRule> rules)
    {
        IGrouping<int, ISellArrangementRule>? duplicatePriority = rules
            .GroupBy(rule => rule.Priority)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicatePriority is not null)
        {
            string names = string.Join(", ", duplicatePriority.Select(rule => rule.Name));
            throw new InvalidOperationException(
                $"Sell rule priority {duplicatePriority.Key} is duplicated by: {names}.");
        }

        IGrouping<string, ISellArrangementRule>? duplicateName = rules
            .GroupBy(rule => rule.Name, StringComparer.Ordinal)
            .FirstOrDefault(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1);
        if (duplicateName is not null)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(duplicateName.Key)
                    ? "Sell rule names cannot be blank."
                    : $"Sell rule name '{duplicateName.Key}' is duplicated.");
        }
    }
}
