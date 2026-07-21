namespace Pricing.UnitTests.SellSelection;

using Pricing.Application.SellSelection;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class SellArrangementRuleEvaluatorTests
{
    [Fact]
    public async Task EvaluatesByPriorityAndStopsAtFirstArrangement()
    {
        var calls = new List<string>();
        var lower = Rule("category", 30, calls, SellRuleDecision.Applied(Selection("CATEGORY")));
        var winner = Rule("account-product", 10, calls, SellRuleDecision.Applied(Selection("ACCOUNT")));
        var middle = Rule("customer-product", 20, calls, SellRuleDecision.Applied(Selection("CUSTOMER")));

        SellArrangementResult result = await new SellArrangementRuleEvaluator([lower, middle, winner])
            .EvaluateAsync(Context(), CancellationToken.None);

        Assert.Equal(["account-product"], calls);
        Assert.Equal("ACCOUNT", result.Selection?.ArrangementIdentifier);
        Assert.Equal([10, 20, 30], result.Trace.Select(entry => entry.Priority));
        Assert.Equal(SellRuleOutcome.Applied, result.Trace[0].Outcome);
        Assert.All(result.Trace.Skip(1), entry =>
        {
            Assert.Equal(SellRuleOutcome.NotEvaluated, entry.Outcome);
            Assert.Equal("HIGHER_PRIORITY_RULE_APPLIED", entry.SkipReason?.Code);
        });
    }

    [Fact]
    public async Task TraceRetainsSkipsAndWinningProvenance()
    {
        var first = new StubRule("account", 10, SellRuleDecision.Skipped(
            SellRuleSkipReason.NoCandidate("No account arrangement.")));
        var second = new StubRule("customer", 20, SellRuleDecision.Skipped(
            SellRuleSkipReason.Expired("Customer arrangement expired.")));
        SellArrangementSelection selection = Selection("GROUP");
        var third = new StubRule("group", 30, SellRuleDecision.Applied(selection));

        SellArrangementResult result = await new SellArrangementRuleEvaluator([third, first, second])
            .EvaluateAsync(Context(), CancellationToken.None);

        Assert.Equal(
            [SellRuleOutcome.Skipped, SellRuleOutcome.Skipped, SellRuleOutcome.Applied],
            result.Trace.Select(entry => entry.Outcome));
        Assert.Equal("NO_CANDIDATE", result.Trace[0].SkipReason?.Code);
        Assert.Equal("EXPIRED", result.Trace[1].SkipReason?.Code);
        Assert.Same(selection.Provenance, result.Trace[2].Provenance);
    }

    [Fact]
    public async Task ExhaustedCascadeReturnsExplicitNoArrangementWithoutError()
    {
        var evaluator = new SellArrangementRuleEvaluator(
        [
            new StubRule("account", 10, SellRuleDecision.Skipped(SellRuleSkipReason.NoCandidate("None."))),
            new StubRule("default", 20, SellRuleDecision.Skipped(SellRuleSkipReason.NoCandidate("None."))),
        ]);

        SellArrangementResult result = await evaluator.EvaluateAsync(Context(), CancellationToken.None);

        Assert.True(result.HasNoArrangement);
        Assert.False(result.IsSelected);
        Assert.False(result.IsFailure);
        Assert.Null(result.Selection);
        Assert.Null(result.Error);
        Assert.Equal(2, result.Trace.Length);
    }

    [Fact]
    public async Task FailureStopsCascadeAndMarksRemainingRules()
    {
        var error = new DependencyPricingError("SELL_LOOKUP_FAILED", "SAG lookup failed", "77", true, "90");
        var failed = new StubRule("account", 10, SellRuleDecision.Failed(error));
        var lower = new StubRule("group", 20, SellRuleDecision.Applied(Selection("GROUP")));

        SellArrangementResult result = await new SellArrangementRuleEvaluator([lower, failed])
            .EvaluateAsync(Context(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Same(error, result.Error);
        Assert.Equal(SellRuleOutcome.Failed, result.Trace[0].Outcome);
        Assert.Equal(SellRuleOutcome.NotEvaluated, result.Trace[1].Outcome);
        Assert.Equal("PRIOR_RULE_FAILED", result.Trace[1].SkipReason?.Code);
        Assert.Equal(0, lower.CallCount);
    }

    [Fact]
    public void DuplicatePrioritiesNamesAndBlankNamesAreRejected()
    {
        Assert.Throws<InvalidOperationException>(() => new SellArrangementRuleEvaluator(
        [
            SkipRule("account", 10),
            SkipRule("group", 10),
        ]));
        Assert.Throws<InvalidOperationException>(() => new SellArrangementRuleEvaluator(
        [
            SkipRule("same", 10),
            SkipRule("same", 20),
        ]));
        Assert.Throws<InvalidOperationException>(() => new SellArrangementRuleEvaluator(
        [
            SkipRule(" ", 10),
        ]));
    }

    [Fact]
    public async Task CancellationIsObservedBeforeAnyRuleRuns()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var rule = new StubRule("account", 10, SellRuleDecision.Applied(Selection("ACCOUNT")));

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await new SellArrangementRuleEvaluator([rule]).EvaluateAsync(Context(), cancellation.Token));
        Assert.Equal(0, rule.CallCount);
    }

    [Fact]
    public async Task CalculationStrategyContractCarriesBasisAndProvenance()
    {
        ISellPriceCalculationStrategy strategy = new StubCalculationStrategy();
        var basis = new SellCalculationBasis(
            new Money(10m), new Money(9m), new Money(12m), new Money(15m),
            new Money(16m), new Money(14m), new UnitOfMeasure("EA"));
        var input = new SellPriceCalculationInput(Context(), Selection("ACCOUNT"), basis);

        SellPriceCalculationResult result = await strategy.CalculateAsync(input, CancellationToken.None);

        Assert.Equal("TEST", strategy.MethodCode);
        Assert.Equal(11m, result.SellPrice.Value);
        Assert.Equal("TEST", result.MethodCode);
        Assert.Equal("A6U01 test strategy", result.Provenance.Source);
        Assert.False(result.IsFailure);
    }

    private static StubRule Rule(
        string name,
        int priority,
        List<string> calls,
        SellRuleDecision decision) => new(name, priority, decision, calls);

    private static StubRule SkipRule(string name, int priority) => new(
        name,
        priority,
        SellRuleDecision.Skipped(SellRuleSkipReason.NoCandidate("None.")));

    private static SellArrangementSelection Selection(string id) => new(
        id,
        "TEST",
        new RuleProvenance("sell-test", "A6U01 test", "test", null));

    private static PricingContext Context()
    {
        var request = new PricingRequest(
            new DivisionId("01"), new AccountNumber("123456"), new VendorId("V001"),
            new ProductId("P0000001"), new Quantity(1m), new UnitOfMeasure("EA"),
            null, null, new DateOnly(2026, 7, 21), PricingRequestType.Full);
        var product = new ProductInformation(
            request.Vendor, request.Product, ProductType.Regular, request.UnitOfMeasure,
            null, null, 1234, "A");
        return new PricingContext(
            request,
            product,
            new CustomerInformation(request.Account, new CustomerNumber(42), []),
            null,
            null);
    }

    private sealed class StubRule(
        string name,
        int priority,
        SellRuleDecision decision,
        List<string>? calls = null) : ISellArrangementRule
    {
        public string Name => name;
        public int Priority => priority;
        public int CallCount { get; private set; }

        public ValueTask<SellRuleDecision> EvaluateAsync(
            PricingContext context,
            CancellationToken cancellationToken)
        {
            CallCount++;
            calls?.Add(name);
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(decision);
        }
    }

    private sealed class StubCalculationStrategy : ISellPriceCalculationStrategy
    {
        public string MethodCode => "TEST";

        public ValueTask<SellPriceCalculationResult> CalculateAsync(
            SellPriceCalculationInput input,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var provenance = new RuleProvenance("test-calculator", "A6U01 test strategy", "test", null);
            return ValueTask.FromResult(new SellPriceCalculationResult(
                new Money(input.Basis.TotalCost.Value + 1m),
                MethodCode,
                0.1m,
                [new PriceComponent("Base sell", PriceComponentType.BaseSell, new Money(11m), provenance)],
                provenance));
        }
    }
}
