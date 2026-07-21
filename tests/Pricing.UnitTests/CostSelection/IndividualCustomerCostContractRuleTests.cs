namespace Pricing.UnitTests.CostSelection;

using System.Collections.Immutable;
using Pricing.Application.CostSelection;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class IndividualCustomerCostContractRuleTests
{
    [Fact]
    public async Task SingleUsableCandidateAppliesWithRepositoryProvenance()
    {
        IndividualCostContractCandidate candidate = Candidate("C1", 12.34m);

        CostRuleDecision decision = await Rule(new StubRepository([candidate]))
            .EvaluateAsync(Context(), CancellationToken.None);

        Assert.Equal(CostRuleOutcome.Applied, decision.Outcome);
        Assert.Equal("C1", Assert.IsType<ContractSelection>(decision.Selection).Contract.Value);
        Assert.Equal(12.34m, decision.Selection.UnitCost.Value);
        Assert.Same(candidate.Provenance, decision.Selection.Provenance);
        Assert.Equal(CostRulePriorities.IndividualCustomerContract, Rule(new StubRepository([])).Priority);
    }

    [Fact]
    public async Task NoCandidateSkipsQuietlyForLaterGroupRules()
    {
        CostRuleDecision decision = await Rule(new StubRepository([]))
            .EvaluateAsync(Context(), CancellationToken.None);

        Assert.Equal(CostRuleOutcome.Skipped, decision.Outcome);
        Assert.Equal("NO_CANDIDATE", decision.SkipReason?.Code);
    }

    [Fact]
    public async Task MissingCustomerSkipsWithoutRepositoryCall()
    {
        var repository = new StubRepository([Candidate("C1", 1m)]);

        CostRuleDecision decision = await Rule(repository)
            .EvaluateAsync(Context(hasCustomer: false), CancellationToken.None);

        Assert.Equal("NOT_ELIGIBLE", decision.SkipReason?.Code);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task ExcludedCandidateIsRejectedAfterLookup()
    {
        IndividualCostContractCandidate candidate = Candidate("C1", 1m) with { IsExcluded = true };

        CostRuleDecision decision = await Rule(new StubRepository([candidate]))
            .EvaluateAsync(Context(), CancellationToken.None);

        Assert.Equal("EXCLUDED", decision.SkipReason?.Code);
    }

    [Fact]
    public async Task ExpiredCandidateIsRejectedOnInclusivePricingDateCheck()
    {
        IndividualCostContractCandidate candidate = Candidate("C1", 1m) with
        {
            EligibilityDates = [new PricingDateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31))],
        };

        CostRuleDecision decision = await Rule(new StubRepository([candidate]))
            .EvaluateAsync(Context(), CancellationToken.None);

        Assert.Equal("EXPIRED", decision.SkipReason?.Code);
    }

    [Fact]
    public async Task SameDayExpirationIsStillEligible()
    {
        IndividualCostContractCandidate candidate = Candidate("C1", 1m) with
        {
            EligibilityDates = [new PricingDateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 7, 21))],
        };

        CostRuleDecision decision = await Rule(new StubRepository([candidate]))
            .EvaluateAsync(Context(), CancellationToken.None);

        Assert.Equal(CostRuleOutcome.Applied, decision.Outcome);
    }

    [Fact]
    public async Task LowestUsableDuplicateWinsRegardlessOfHigherCostCompetitors()
    {
        IndividualCostContractCandidate[] candidates =
        [
            Candidate("HIGH", 20m),
            Candidate("LOW", 5m),
            Candidate("MID", 10m),
        ];

        CostRuleDecision decision = await Rule(new StubRepository([.. candidates]))
            .EvaluateAsync(Context(), CancellationToken.None);

        Assert.Equal("LOW", Assert.IsType<ContractSelection>(decision.Selection).Contract.Value);
        Assert.Equal(5m, decision.Selection.UnitCost.Value);
    }

    [Fact]
    public async Task FirstZeroCostContractRemainsSelectedOverLaterEqualZero()
    {
        IndividualCostContractCandidate[] candidates =
        [
            Candidate("FIRST-ZERO", 0m),
            Candidate("SECOND-ZERO", 0m),
            Candidate("POSITIVE", 1m),
        ];

        CostRuleDecision decision = await Rule(new StubRepository([.. candidates]))
            .EvaluateAsync(Context(), CancellationToken.None);

        Assert.Equal("FIRST-ZERO", Assert.IsType<ContractSelection>(decision.Selection).Contract.Value);
        Assert.Equal(0m, decision.Selection.UnitCost.Value);
    }

    [Fact]
    public async Task ExcludedLowerCostCannotDefeatUsableContract()
    {
        IndividualCostContractCandidate excluded = Candidate("EXCLUDED", 1m) with { IsExcluded = true };

        CostRuleDecision decision = await Rule(new StubRepository([excluded, Candidate("USABLE", 8m)]))
            .EvaluateAsync(Context(), CancellationToken.None);

        Assert.Equal("USABLE", Assert.IsType<ContractSelection>(decision.Selection).Contract.Value);
    }

    [Fact]
    public async Task RepositoryFailureBecomesTypedRuleFailure()
    {
        var exception = new IndividualCostContractRepositoryException("cursor fetch failed", "42", "90", true);

        CostRuleDecision decision = await Rule(new StubRepository(exception))
            .EvaluateAsync(Context(), CancellationToken.None);

        DependencyPricingError error = Assert.IsType<DependencyPricingError>(decision.Error);
        Assert.Equal(CostRuleOutcome.Failed, decision.Outcome);
        Assert.Equal("42", error.LegacyErrorCode);
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
            await Rule(repository).EvaluateAsync(Context(), cancellation.Token));
        Assert.Equal(0, repository.CallCount);
    }

    private static IndividualCustomerCostContractRule Rule(IIndividualCostContractRepository repository) => new(repository);

    private static IndividualCostContractCandidate Candidate(string id, decimal cost) => new(
        new ContractId(id),
        "I",
        new Money(cost),
        new UnitOfMeasure("EA"),
        [new PricingDateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31))],
        false,
        new RuleProvenance(
            "individual-customer-cost-contract",
            $"CCG01/{id}",
            "individual-customer",
            new PricingDateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31))));

    private static PricingContext Context(bool hasCustomer = true)
    {
        CustomerNumber? effectiveCustomer = hasCustomer ? new CustomerNumber(42) : null;
        var request = new PricingRequest(
            new DivisionId("01"),
            new AccountNumber("123456"),
            new VendorId("V001"),
            new ProductId("P0000001"),
            new Quantity(1m),
            new UnitOfMeasure("EA"),
            "001",
            null,
            new DateOnly(2026, 7, 21),
            PricingRequestType.Full);
        var product = new ProductInformation(
            request.Vendor, request.Product, ProductType.Regular, request.UnitOfMeasure,
            null, null, 1234, "A");
        var customerInformation = new CustomerInformation(request.Account, effectiveCustomer, []);
        return new PricingContext(request, product, customerInformation, null, null);
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
}
