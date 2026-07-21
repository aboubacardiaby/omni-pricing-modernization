namespace Pricing.UnitTests.CostSelection;

using System.Collections.Immutable;
using Pricing.Application.CostSelection;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class AcquisitionDealerCostFallbackRuleTests
{
    [Fact]
    public async Task DirectMatchWinsOverFallbackCandidates()
    {
        var repository = new StubRepository(Data(
            direct: Price(7m, 5m, "01", new DateOnly(2025, 1, 1)),
            fallback:
            [
                Price(99m, 90m, "99", new DateOnly(2026, 7, 1)),
            ]));

        AcquisitionCostSelection selection = await SelectAsync(repository);

        Assert.Equal(7m, selection.UnitCost.Value);
        Assert.Equal("01", selection.PriceLevel);
    }

    [Fact]
    public async Task FallbackUsesLatestEffectiveDateThenHighestPriceLevel()
    {
        var repository = new StubRepository(Data(fallback:
        [
            Price(100m, 90m, "99", new DateOnly(2026, 6, 30)),
            Price(5m, 4m, "01", new DateOnly(2026, 7, 1)),
            Price(8m, 6m, "03", new DateOnly(2026, 7, 1)),
            Price(1m, 1m, "99", new DateOnly(2026, 7, 22)),
        ]));

        AcquisitionCostSelection selection = await SelectAsync(repository);

        Assert.Equal(new DateOnly(2026, 7, 1), selection.PriceListEffectiveDate);
        Assert.Equal("03", selection.PriceLevel);
        Assert.Equal(8m, selection.UnitCost.Value);
    }

    [Fact]
    public async Task SameDayAndZeroDealerCostAreValidAndExemptionsResetToFalse()
    {
        var repository = new StubRepository(Data(fallback:
        [
            Price(0m, 4m, "01", new DateOnly(2026, 7, 21)),
        ]));

        AcquisitionCostSelection selection = await SelectAsync(repository);

        Assert.Equal(0m, selection.UnitCost.Value);
        Assert.False(selection.IsJitExempt);
        Assert.False(selection.IsFreightExempt);
        Assert.Equal("VNG03", selection.Provenance.Source);
    }

    [Fact]
    public async Task MissingEffectiveBaselineIsFatalLegacy130()
    {
        var repository = new StubRepository(Data(fallback:
        [
            Price(10m, 8m, "01", new DateOnly(2026, 7, 22)),
        ]));

        CostRuleDecision decision = await Rule(repository).EvaluateAsync(Context(), CancellationToken.None);

        MissingDataPricingError error = Assert.IsType<MissingDataPricingError>(decision.Error);
        Assert.Equal(CostRuleOutcome.Failed, decision.Outcome);
        Assert.Equal("130", error.LegacyErrorCode);
        Assert.Equal("VNG03", error.Entity);
    }

    [Fact]
    public async Task EvaluatorRunsFallbackOnlyAfterEveryHigherRuleSkips()
    {
        var calls = new List<string>();
        var repository = new StubRepository(Data(direct: Price(7m, 5m, "01", new DateOnly(2026, 1, 1))));
        ICostRule[] rules =
        [
            new StubRule("healthcare", 300, calls, CostRuleDecision.Skipped(CostRuleSkipReason.NoCandidate("None."))),
            new AcquisitionDealerCostFallbackRule(repository),
            new StubRule("individual", 100, calls, CostRuleDecision.Skipped(CostRuleSkipReason.NoCandidate("None."))),
            new StubRule("group", 200, calls, CostRuleDecision.Skipped(CostRuleSkipReason.NoCandidate("None."))),
        ];

        CostSelectionResult result = await new CostRuleEvaluator(rules).EvaluateAsync(Context(), CancellationToken.None);

        Assert.IsType<AcquisitionCostSelection>(result.Selection);
        Assert.Equal(["individual", "group", "healthcare"], calls);
        Assert.Equal(1, repository.CallCount);
        Assert.Equal([100, 200, 300, 400], result.Trace.Select(entry => entry.Priority));
    }

    [Fact]
    public async Task HigherRuleSelectionPreventsFallbackRepositoryCall()
    {
        var repository = new StubRepository(Data(direct: Price(7m, 5m, "01", new DateOnly(2026, 1, 1))));
        var higher = new StubRule("contract", 100, [], CostRuleDecision.Applied(Contract()));

        CostSelectionResult result = await new CostRuleEvaluator(
            [new AcquisitionDealerCostFallbackRule(repository), higher])
            .EvaluateAsync(Context(), CancellationToken.None);

        Assert.IsType<ContractSelection>(result.Selection);
        Assert.Equal(0, repository.CallCount);
        Assert.Equal(CostRuleOutcome.NotEvaluated, result.Trace[1].Outcome);
    }

    [Fact]
    public async Task SpecialContractSkipsRepositoryAndFailureAndCancellationRemainTyped()
    {
        var specialRepository = new StubRepository(Data());
        CostRuleDecision skipped = await Rule(specialRepository)
            .EvaluateAsync(Context(isSpecial: true), CancellationToken.None);
        Assert.Equal(CostRuleOutcome.Skipped, skipped.Outcome);
        Assert.Equal(0, specialRepository.CallCount);

        var failedRepository = new StubRepository(
            new AcquisitionCostRepositoryException("VNG03 unavailable", "45", "90", true));
        CostRuleDecision failed = await Rule(failedRepository).EvaluateAsync(Context(), CancellationToken.None);
        DependencyPricingError error = Assert.IsType<DependencyPricingError>(failed.Error);
        Assert.Equal("45", error.LegacyErrorCode);
        Assert.Equal("90", error.LegacySeverityCode);
        Assert.True(error.IsTransient);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await Rule(new StubRepository(Data())).EvaluateAsync(Context(), cancellation.Token));
    }

    private static async Task<AcquisitionCostSelection> SelectAsync(StubRepository repository)
    {
        CostRuleDecision decision = await Rule(repository).EvaluateAsync(Context(), CancellationToken.None);
        return Assert.IsType<AcquisitionCostSelection>(decision.Selection);
    }

    private static AcquisitionDealerCostFallbackRule Rule(IAcquisitionCostRepository repository) => new(repository);

    private static AcquisitionCostData Data(
        AcquisitionPriceListCandidate? direct = null,
        ImmutableArray<AcquisitionPriceListCandidate> fallback = default) =>
        new(direct, fallback.IsDefault ? [] : fallback);

    private static AcquisitionPriceListCandidate Price(
        decimal dealer,
        decimal acquisition,
        string level,
        DateOnly effectiveDate) => new(
        new Money(dealer),
        new Money(acquisition),
        new UnitOfMeasure("EA"),
        level,
        effectiveDate,
        new RuleProvenance(
            "acquisition-dealer-cost-fallback",
            "VNG03",
            "vendor-product-price-list",
            new PricingDateRange(effectiveDate, null)));

    private static ContractSelection Contract() => new(
        new ContractId("C-1"),
        "INDIVIDUAL",
        new Money(9m),
        new UnitOfMeasure("EA"),
        new RuleProvenance("contract", "CCG03", "individual", null));

    private static PricingContext Context(bool isSpecial = false)
    {
        var request = new PricingRequest(
            new DivisionId("01"), new AccountNumber("123456"), new VendorId("V001"),
            new ProductId("P0000001"), new Quantity(1m), new UnitOfMeasure("EA"),
            null, null, new DateOnly(2026, 7, 21), PricingRequestType.Full, isSpecial);
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

    private sealed class StubRepository : IAcquisitionCostRepository
    {
        private readonly AcquisitionCostData? data;
        private readonly Exception? exception;

        internal StubRepository(AcquisitionCostData data) => this.data = data;
        internal StubRepository(Exception exception) => this.exception = exception;
        internal int CallCount { get; private set; }

        public ValueTask<AcquisitionCostData> FindAsync(PricingContext context, CancellationToken cancellationToken)
        {
            CallCount++;
            cancellationToken.ThrowIfCancellationRequested();
            return exception is null
                ? ValueTask.FromResult(data!)
                : ValueTask.FromException<AcquisitionCostData>(exception);
        }
    }

    private sealed class StubRule(
        string name,
        int priority,
        List<string> calls,
        CostRuleDecision decision) : ICostRule
    {
        public string Name => name;
        public int Priority => priority;

        public ValueTask<CostRuleDecision> EvaluateAsync(PricingContext context, CancellationToken cancellationToken)
        {
            calls.Add(name);
            return ValueTask.FromResult(decision);
        }
    }
}
