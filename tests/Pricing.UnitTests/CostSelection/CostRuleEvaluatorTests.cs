namespace Pricing.UnitTests.CostSelection;

using Pricing.Application.CostSelection;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class CostRuleEvaluatorTests
{
    [Fact]
    public async Task EvaluatesInPriorityOrderAndStopsAtFirstAppliedRule()
    {
        var calls = new List<string>();
        var lowerPriority = Rule("group", 20, calls, CostRuleDecision.Applied(Selection("group")));
        var higherPriority = Rule("individual", 10, calls, CostRuleDecision.Applied(Selection("individual")));
        var fallback = Rule("fallback", 30, calls, CostRuleDecision.Applied(Selection("fallback")));

        CostSelectionResult result = await new CostRuleEvaluator([fallback, lowerPriority, higherPriority])
            .EvaluateAsync(Context(), CancellationToken.None);

        Assert.Equal(["individual"], calls);
        Assert.Equal("individual", result.Selection!.Provenance.RuleName);
        Assert.Equal(3, result.Trace.Length);
        Assert.Equal(CostRuleOutcome.Applied, result.Trace[0].Outcome);
        Assert.All(result.Trace.Skip(1), entry =>
        {
            Assert.Equal(CostRuleOutcome.NotEvaluated, entry.Outcome);
            Assert.Equal("HIGHER_PRIORITY_RULE_APPLIED", entry.SkipReason?.Code);
        });
        Assert.Equal([10, 20, 30], result.Trace.Select(entry => entry.Priority));
    }

    [Fact]
    public async Task RecordsEveryExplicitSkipBeforeSelection()
    {
        var first = new StubRule("individual", 10, CostRuleDecision.Skipped(
            CostRuleSkipReason.Excluded("Account contract exclusion matched.")));
        var second = new StubRule("group", 20, CostRuleDecision.Skipped(
            CostRuleSkipReason.Expired("No effective group contract.")));
        var third = new StubRule("fallback", 30, CostRuleDecision.Applied(Selection("fallback")));

        CostSelectionResult result = await new CostRuleEvaluator([first, second, third])
            .EvaluateAsync(Context(), CancellationToken.None);

        Assert.Equal([CostRuleOutcome.Skipped, CostRuleOutcome.Skipped, CostRuleOutcome.Applied],
            result.Trace.Select(entry => entry.Outcome));
        Assert.Equal("EXCLUDED", result.Trace[0].SkipReason?.Code);
        Assert.Equal("EXPIRED", result.Trace[1].SkipReason?.Code);
        Assert.Equal("fallback", result.Trace[2].Provenance?.RuleName);
    }

    [Fact]
    public async Task AllSkippedReturnsNoSelectionWithFullTrace()
    {
        var evaluator = new CostRuleEvaluator(
        [
            new StubRule("individual", 10, CostRuleDecision.Skipped(CostRuleSkipReason.NoCandidate("None."))),
            new StubRule("group", 20, CostRuleDecision.Skipped(CostRuleSkipReason.NotEligible("None."))),
        ]);

        CostSelectionResult result = await evaluator.EvaluateAsync(Context(), CancellationToken.None);

        Assert.False(result.IsSelected);
        Assert.False(result.IsFailure);
        Assert.Null(result.Selection);
        Assert.Equal(2, result.Trace.Length);
    }

    [Fact]
    public async Task FailureStopsEvaluationAndMarksRemainingRules()
    {
        var error = new DependencyPricingError("COST_LOOKUP_FAILED", "Database failure", "37", true, "90");
        var first = new StubRule("individual", 10, CostRuleDecision.Failed(error));
        var second = new StubRule("group", 20, CostRuleDecision.Applied(Selection("group")));

        CostSelectionResult result = await new CostRuleEvaluator([second, first])
            .EvaluateAsync(Context(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Same(error, result.Error);
        Assert.Equal(CostRuleOutcome.Failed, result.Trace[0].Outcome);
        Assert.Equal(CostRuleOutcome.NotEvaluated, result.Trace[1].Outcome);
        Assert.Equal("PRIOR_RULE_FAILED", result.Trace[1].SkipReason?.Code);
        Assert.Equal(0, second.CallCount);
    }

    [Fact]
    public void DuplicatePriorityIsRejectedAtConfigurationTime()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            new CostRuleEvaluator(
            [
                new StubRule("individual", 10, CostRuleDecision.Skipped(CostRuleSkipReason.NoCandidate("None."))),
                new StubRule("group", 10, CostRuleDecision.Skipped(CostRuleSkipReason.NoCandidate("None."))),
            ]));

        Assert.Contains("priority 10", exception.Message, StringComparison.Ordinal);
        Assert.Contains("individual", exception.Message, StringComparison.Ordinal);
        Assert.Contains("group", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DuplicateAndBlankNamesAreRejectedAtConfigurationTime()
    {
        Assert.Throws<InvalidOperationException>(() => new CostRuleEvaluator(
        [
            new StubRule("same", 10, CostRuleDecision.Skipped(CostRuleSkipReason.NoCandidate("None."))),
            new StubRule("same", 20, CostRuleDecision.Skipped(CostRuleSkipReason.NoCandidate("None."))),
        ]));
        Assert.Throws<InvalidOperationException>(() => new CostRuleEvaluator(
        [
            new StubRule(" ", 10, CostRuleDecision.Skipped(CostRuleSkipReason.NoCandidate("None."))),
        ]));
    }

    [Fact]
    public async Task CancellationIsPropagatedBeforeAnyRuleRuns()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var rule = new StubRule("individual", 10, CostRuleDecision.Applied(Selection("individual")));

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await new CostRuleEvaluator([rule]).EvaluateAsync(Context(), cancellation.Token));
        Assert.Equal(0, rule.CallCount);
    }

    private static StubRule Rule(
        string name,
        int priority,
        List<string> calls,
        CostRuleDecision decision) => new(name, priority, decision, calls);

    private static ContractSelection Selection(string ruleName) => new(
        new ContractId("C-1"),
        "TEST",
        new Money(10m),
        new UnitOfMeasure("EA"),
        new RuleProvenance(ruleName, "A6U01 test double", "test", null));

    private static PricingContext Context()
    {
        var request = new PricingRequest(
            new DivisionId("01"),
            new AccountNumber("123456"),
            new VendorId("V001"),
            new ProductId("P0000001"),
            new Quantity(1m),
            new UnitOfMeasure("EA"),
            null,
            null,
            new DateOnly(2026, 7, 21),
            PricingRequestType.Full);
        var product = new ProductInformation(
            request.Vendor,
            request.Product,
            ProductType.Regular,
            request.UnitOfMeasure,
            null,
            null,
            1234,
            "A");
        var customer = new CustomerInformation(request.Account, new CustomerNumber(42), []);
        return new PricingContext(request, product, customer, null, null);
    }

    private sealed class StubRule(
        string name,
        int priority,
        CostRuleDecision decision,
        List<string>? calls = null) : ICostRule
    {
        public string Name { get; } = name;
        public int Priority { get; } = priority;
        public int CallCount { get; private set; }

        public ValueTask<CostRuleDecision> EvaluateAsync(
            PricingContext context,
            CancellationToken cancellationToken)
        {
            CallCount++;
            calls?.Add(Name);
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(decision);
        }
    }
}
