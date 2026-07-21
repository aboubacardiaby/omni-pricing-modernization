namespace Pricing.UnitTests.CostSelection;

using System.Collections.Immutable;
using Pricing.Application.CostSelection;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class SpecialContractCostRuleTests
{
    [Fact]
    public async Task NonSpecialRequestSkipsWithoutRepositoryCall()
    {
        var repository = new StubRepository([Candidate("SPECIAL", 10m, 12m)]);

        CostRuleDecision decision = await new SpecialContractCostRule(repository)
            .EvaluateAsync(Context(isSpecial: false), CancellationToken.None);

        Assert.Equal(CostRuleOutcome.Skipped, decision.Outcome);
        Assert.Equal("NOT_ELIGIBLE", decision.SkipReason?.Code);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task SpecialContractAppliesRawCostAndSuggestedSellWithEarlyExitControls()
    {
        IndividualCostContractCandidate candidate = Candidate("SPECIAL", 10.12345678m, 12.87654321m);

        CostRuleDecision decision = await new SpecialContractCostRule(new StubRepository([candidate]))
            .EvaluateAsync(Context(), CancellationToken.None);

        ContractSelection selection = Assert.IsType<ContractSelection>(decision.Selection);
        Assert.Equal("SPECIAL", selection.ContractType);
        Assert.Equal(10.12345678m, selection.UnitCost.Value);
        Assert.Equal(12.87654321m, selection.SpecialContract!.SuggestedSellPrice.Value);
        Assert.True(selection.SpecialContract.BypassAdjustments);
        Assert.True(selection.SpecialContract.BypassFinalRounding);
        Assert.Equal(candidate.EligibilityDates, selection.SpecialContract.ExpirationSources);
    }

    [Fact]
    public async Task LowestRestrictedCandidateWinsAndFirstZeroIsStable()
    {
        IndividualCostContractCandidate[] candidates =
        [
            Candidate("HIGH", 4m, 8m),
            Candidate("FIRST-ZERO", 0m, 5m),
            Candidate("SECOND-ZERO", 0m, 6m),
        ];

        CostRuleDecision decision = await new SpecialContractCostRule(new StubRepository([.. candidates]))
            .EvaluateAsync(Context(), CancellationToken.None);

        ContractSelection selection = Assert.IsType<ContractSelection>(decision.Selection);
        Assert.Equal("FIRST-ZERO", selection.Contract.Value);
        Assert.Equal(5m, selection.SpecialContract!.SuggestedSellPrice.Value);
    }

    [Fact]
    public async Task MissingRestrictedContractReturnsFatal601AndStopsLowerRules()
    {
        var repository = new StubRepository([]);
        var lowerRule = new CountingRule();
        var evaluator = new CostRuleEvaluator([lowerRule, new SpecialContractCostRule(repository)]);

        CostSelectionResult result = await evaluator.EvaluateAsync(Context(), CancellationToken.None);

        MissingDataPricingError error = Assert.IsType<MissingDataPricingError>(result.Error);
        Assert.Equal("601", error.LegacyErrorCode);
        Assert.Equal(0, lowerRule.CallCount);
        Assert.Equal(CostRuleOutcome.NotEvaluated, result.Trace[1].Outcome);
    }

    [Fact]
    public async Task MissingSuggestedSellIsTypedFailure()
    {
        IndividualCostContractCandidate candidate = Candidate("SPECIAL", 10m, 12m) with
        {
            SuggestedSellPrice = null,
        };

        CostRuleDecision decision = await new SpecialContractCostRule(new StubRepository([candidate]))
            .EvaluateAsync(Context(), CancellationToken.None);

        MissingDataPricingError error = Assert.IsType<MissingDataPricingError>(decision.Error);
        Assert.Equal("SPECIAL_CONTRACT_SUGGESTED_SELL_MISSING", error.Code);
    }

    [Fact]
    public async Task RepositoryFailurePreservesLegacyErrorFields()
    {
        var failure = new IndividualCostContractRepositoryException("special lookup failed", "37", "90", true);

        CostRuleDecision decision = await new SpecialContractCostRule(new StubRepository(failure))
            .EvaluateAsync(Context(), CancellationToken.None);

        DependencyPricingError error = Assert.IsType<DependencyPricingError>(decision.Error);
        Assert.Equal("37", error.LegacyErrorCode);
        Assert.Equal("90", error.LegacySeverityCode);
        Assert.True(error.IsTransient);
    }

    [Fact]
    public async Task CancellationIsPropagatedBeforeRepositoryCall()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var repository = new StubRepository([]);

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await new SpecialContractCostRule(repository).EvaluateAsync(Context(), cancellation.Token));
        Assert.Equal(0, repository.CallCount);
    }

    private static IndividualCostContractCandidate Candidate(string id, decimal cost, decimal sell)
    {
        var dates = new PricingDateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        return new IndividualCostContractCandidate(
            new ContractId(id),
            "I",
            new Money(cost),
            new UnitOfMeasure("EA"),
            [dates],
            false,
            new RuleProvenance("special-contract", $"CCG01/{id}", "individual-customer", dates),
            new Money(sell));
    }

    private static PricingContext Context(bool isSpecial = true)
    {
        var request = new PricingRequest(
            new DivisionId("01"), new AccountNumber("123456"), new VendorId("V001"),
            new ProductId("P0000001"), new Quantity(1m), new UnitOfMeasure("EA"),
            null, null, new DateOnly(2026, 7, 21), PricingRequestType.Full, isSpecial);
        var product = new ProductInformation(
            request.Vendor, request.Product, ProductType.Regular, request.UnitOfMeasure,
            null, null, 1234, "A");
        var customer = new CustomerInformation(request.Account, new CustomerNumber(42), []);
        return new PricingContext(request, product, customer, null, null);
    }

    private sealed class StubRepository : IIndividualCostContractRepository
    {
        private readonly ImmutableArray<IndividualCostContractCandidate> candidates;
        private readonly Exception? exception;

        internal StubRepository(ImmutableArray<IndividualCostContractCandidate> candidates) => this.candidates = candidates;
        internal StubRepository(Exception exception) => this.exception = exception;
        internal int CallCount { get; private set; }

        public ValueTask<ImmutableArray<IndividualCostContractCandidate>> FindCandidatesAsync(
            PricingContext context,
            CancellationToken cancellationToken)
        {
            CallCount++;
            cancellationToken.ThrowIfCancellationRequested();
            return exception is null
                ? ValueTask.FromResult(candidates)
                : ValueTask.FromException<ImmutableArray<IndividualCostContractCandidate>>(exception);
        }
    }

    private sealed class CountingRule : ICostRule
    {
        public string Name => "lower-rule";
        public int Priority => CostRulePriorities.IndividualCustomerContract;
        internal int CallCount { get; private set; }

        public ValueTask<CostRuleDecision> EvaluateAsync(PricingContext context, CancellationToken cancellationToken)
        {
            CallCount++;
            return ValueTask.FromResult(CostRuleDecision.Skipped(CostRuleSkipReason.NoCandidate("none")));
        }
    }
}
