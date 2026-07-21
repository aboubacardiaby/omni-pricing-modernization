namespace Pricing.UnitTests.CostAdjustments;

using System.Collections.Immutable;
using Pricing.Application.CostAdjustments;
using Pricing.Application.Rebates;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class VendorCostAdjustmentServiceTests
{
    [Fact]
    public async Task LowestCobolEvaluationOrderWinsRegardlessOfRepositoryOrder()
    {
        var repository = new StubRepository(
        [
            Candidate(30, 0.30m, "CORP"),
            Candidate(10, 0.10m, "ACCOUNT"),
            Candidate(20, 0.20m, "GROUP"),
        ]);

        CostAdjustmentResult result = await Service(repository).ApplyAsync(Input(), CancellationToken.None);

        Assert.True(result.WasApplied);
        Assert.Equal("ACCOUNT", result.VendorAdjustmentSourceCode);
        Assert.Equal(1m, result.VendorAdjustment.Value);
        Assert.Equal(11m, result.TotalCost.Value);
    }

    [Fact]
    public async Task ExpiredHigherCandidateFallsThroughAndSameDayExpirationApplies()
    {
        var repository = new StubRepository(
        [
            Candidate(10, 0.50m, "EXPIRED") with
            {
                EffectiveDates = new PricingDateRange(
                    new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)),
            },
            Candidate(20, 0.20m, "SAME-DAY") with
            {
                EffectiveDates = new PricingDateRange(
                    new DateOnly(2026, 1, 1), new DateOnly(2026, 7, 21)),
            },
        ]);

        CostAdjustmentResult result = await Service(repository).ApplyAsync(Input(), CancellationToken.None);

        Assert.Equal("SAME-DAY", result.VendorAdjustmentSourceCode);
        Assert.Equal(2m, result.VendorAdjustment.Value);
    }

    [Theory]
    [InlineData(10, 0.125, 1.25, 11.25)]
    [InlineData(8.75, -0.04, -0.35, 8.40)]
    [InlineData(0, 0.50, 0, 0)]
    public async Task CharacterizedCompositionFixturesMatchCobolFormula(
        decimal baseCost,
        decimal percentage,
        decimal expectedAdjustment,
        decimal expectedTotal)
    {
        var repository = new StubRepository([Candidate(1, percentage, "FIXTURE")]);

        CostAdjustmentResult result = await Service(repository)
            .ApplyAsync(Input(baseCost: baseCost), CancellationToken.None);

        Assert.Equal(expectedAdjustment, result.VendorAdjustment.Value);
        Assert.Equal(expectedTotal, result.TotalCost.Value);
        PriceComponent component = Assert.Single(result.Components);
        Assert.Equal(PriceComponentType.CostAdjustment, component.Type);
        Assert.Equal("VNG23", component.Provenance.Source);
    }

    [Fact]
    public async Task ExemptionRetainsSelectedSourceButZerosAmount()
    {
        var repository = new StubRepository([Candidate(1, 0.25m, "ACCOUNT", isExempt: true)]);

        CostAdjustmentResult result = await Service(repository).ApplyAsync(Input(), CancellationToken.None);

        Assert.True(result.WasApplied);
        Assert.Equal("ACCOUNT", result.VendorAdjustmentSourceCode);
        Assert.Equal(0m, result.VendorAdjustmentPercentage);
        Assert.Equal(0m, result.VendorAdjustment.Value);
        Assert.Equal(10m, result.TotalCost.Value);
    }

    [Fact]
    public async Task NoMatchIsSuccessfulZeroAdjustment()
    {
        CostAdjustmentResult result = await Service(new StubRepository([]))
            .ApplyAsync(Input(), CancellationToken.None);

        Assert.False(result.WasApplied);
        Assert.False(result.IsFailure);
        Assert.Equal(10m, result.TotalCost.Value);
        Assert.Contains("No vendor", result.SkipReason, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task PriceLockAndHealthcareRecordBypassLookup(bool priceLocked, bool healthcare)
    {
        var repository = new StubRepository([Candidate(1, 0.25m, "ACCOUNT")]);

        CostAdjustmentResult result = await Service(repository).ApplyAsync(
            Input(isPriceLocked: priceLocked, hasHealthcare: healthcare),
            CancellationToken.None);

        Assert.Equal(0, repository.CallCount);
        Assert.False(result.WasApplied);
        Assert.Equal(10m, result.TotalCost.Value);
    }

    [Fact]
    public async Task RebateIsPreservedButNotNettedIntoTotalCost()
    {
        RebateCalculationResult rebate = RebateCalculator.Calculate(RebateInput());
        var repository = new StubRepository([Candidate(1, 0.10m, "ACCOUNT")]);

        CostAdjustmentResult result = await Service(repository).ApplyAsync(
            Input(rebate: rebate), CancellationToken.None);

        Assert.Same(rebate, result.Rebate);
        Assert.Equal(2m, result.Rebate.TotalRebate.Value);
        Assert.Equal(11m, result.TotalCost.Value);
    }

    [Fact]
    public async Task RepositoryFailureStopsCompositionWithTypedError()
    {
        var repository = new StubRepository(
            new VendorCostAdjustmentRepositoryException("CUG61 failed", "231", "90", true));

        CostAdjustmentResult result = await Service(repository).ApplyAsync(Input(), CancellationToken.None);

        DependencyPricingError error = Assert.IsType<DependencyPricingError>(result.Error);
        Assert.True(result.IsFailure);
        Assert.Equal("231", error.LegacyErrorCode);
        Assert.Equal("90", error.LegacySeverityCode);
        Assert.True(error.IsTransient);
        Assert.Empty(result.Components);
        Assert.Equal(10m, result.TotalCost.Value);
    }

    [Fact]
    public async Task CancellationIsPropagatedBeforeRepositoryCall()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var repository = new StubRepository([]);

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await Service(repository).ApplyAsync(Input(), cancellation.Token));
        Assert.Equal(0, repository.CallCount);
    }

    private static VendorCostAdjustmentService Service(IVendorCostAdjustmentRepository repository) => new(repository);

    private static VendorCostAdjustmentCandidate Candidate(
        int order,
        decimal percentage,
        string sourceCode,
        bool isExempt = false) => new(
        order,
        percentage,
        sourceCode,
        ActiveDates(),
        new RuleProvenance("vendor-cost-adjustment", "VNG23", sourceCode, ActiveDates()),
        isExempt);

    private static CostAdjustmentInput Input(
        decimal baseCost = 10m,
        bool isPriceLocked = false,
        bool hasHealthcare = false,
        RebateCalculationResult? rebate = null) => new(
        Context(),
        Selection(baseCost),
        true,
        isPriceLocked,
        hasHealthcare,
        rebate ?? RebateCalculationResult.NotCalculated("test"));

    private static ContractSelection Selection(decimal cost) => new(
        new ContractId("C-1"),
        "INDIVIDUAL",
        new Money(cost),
        new UnitOfMeasure("EA"),
        new RuleProvenance("individual", "CCG03", "individual", ActiveDates()));

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

    private static RebateCalculationInput RebateInput() => new(
        true,
        "01",
        new DateOnly(2026, 7, 21),
        new Money(10m),
        new Money(12m),
        new Money(12m),
        false,
        new Money(12m),
        new Money(0m),
        0m,
        0m,
        false,
        0m,
        false,
        new Money(0m),
        null,
        new Money(0m));

    private static PricingDateRange ActiveDates() =>
        new(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

    private sealed class StubRepository : IVendorCostAdjustmentRepository
    {
        private readonly ImmutableArray<VendorCostAdjustmentCandidate> candidates;
        private readonly Exception? exception;

        internal StubRepository(ImmutableArray<VendorCostAdjustmentCandidate> candidates) =>
            this.candidates = candidates;

        internal StubRepository(Exception exception) => this.exception = exception;

        internal int CallCount { get; private set; }

        public ValueTask<ImmutableArray<VendorCostAdjustmentCandidate>> FindAsync(
            PricingContext context,
            bool hasCostContract,
            CancellationToken cancellationToken)
        {
            CallCount++;
            cancellationToken.ThrowIfCancellationRequested();
            return exception is null
                ? ValueTask.FromResult(candidates)
                : ValueTask.FromException<ImmutableArray<VendorCostAdjustmentCandidate>>(exception);
        }
    }
}
