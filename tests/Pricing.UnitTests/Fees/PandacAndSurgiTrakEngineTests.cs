namespace Pricing.UnitTests.Fees;

using Pricing.Application.Fees;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class PandacAndSurgiTrakEngineTests
{
    [Fact]
    public async Task PandacRequiresVendorAndItemEligibility()
    {
        var repository = new StubPandacRepository { Default = Candidate(PandacSource.DefaultShipTo, PandacFeeBasis.ResolvedDefault, amount: 5m) };
        Assert.Equal(0m, (await new PandacEngine(repository).CalculateAsync(Input() with { VendorParticipates = false }, default)).Amount.Value);
        Assert.Equal(0m, (await new PandacEngine(repository).CalculateAsync(Input() with { ItemEligible = false }, default)).Amount.Value);
        Assert.Empty(repository.Calls);
    }

    [Fact]
    public async Task NewSpecificWinsAndUsesWholeNumberPercentageScaling()
    {
        var repository = new StubPandacRepository
        {
            New = Candidate(PandacSource.NewSpecificShipTo, PandacFeeBasis.SellPercentage, rate: 5m),
            Legacy = Candidate(PandacSource.LegacySpecificShipTo, PandacFeeBasis.CostPercentage, rate: 0.2m),
        };
        PandacResult result = await new PandacEngine(repository).CalculateAsync(Input(), default);
        Assert.Equal(10m, result.Amount.Value);
        Assert.Equal(0.05m, result.AppliedRate);
        Assert.Equal(PandacSource.NewSpecificShipTo, result.Source);
        Assert.Equal(["new"], repository.Calls);
    }

    [Fact]
    public async Task IneligibleNewRowFallsThroughToLegacyThenDefault()
    {
        var repository = new StubPandacRepository
        {
            New = Candidate(PandacSource.NewSpecificShipTo, PandacFeeBasis.Flat, amount: 9m) with { AccountShipToEligible = false },
            Legacy = null,
            Default = Candidate(PandacSource.DefaultShipTo, PandacFeeBasis.ResolvedDefault, amount: 4m),
        };
        PandacResult result = await new PandacEngine(repository).CalculateAsync(Input(), default);
        Assert.Equal(4m, result.Amount.Value);
        Assert.Equal(["new", "legacy", "default"], repository.Calls);
    }

    [Fact]
    public async Task LegacyPercentageIsAlreadyScaledAndAlwaysCostBased()
    {
        var repository = new StubPandacRepository { Legacy = Candidate(PandacSource.LegacySpecificShipTo, PandacFeeBasis.CostPercentage, rate: 0.1m) };
        PandacResult result = await new PandacEngine(repository).CalculateAsync(Input(), default);
        Assert.Equal(10m, result.Amount.Value);
        Assert.Equal(0.1m, result.AppliedRate);
    }

    [Fact]
    public async Task FlatNewFeePopulatesParallelAuditAmountAndMonthlyDoesNotFold()
    {
        var repository = new StubPandacRepository { New = Candidate(PandacSource.NewSpecificShipTo, PandacFeeBasis.Flat, amount: 7m, billing: "MA") };
        PandacResult result = await new PandacEngine(repository).CalculateAsync(Input(), default);
        Assert.Equal(7m, result.Amount.Value);
        Assert.Equal(7m, result.AuditAmount.Value);
        Assert.False(result.FoldIntoLine);
    }

    [Fact]
    public async Task LivePandacNeverShortCircuitsAsImpliedSellArrangement()
    {
        var repository = new StubPandacRepository { New = Candidate(PandacSource.NewSpecificShipTo, PandacFeeBasis.Flat, amount: 7m) };
        PandacResult result = await new PandacEngine(repository).CalculateAsync(Input(), default);
        Assert.False(result.IsImpliedSellArrangement);
        Assert.True(result.FoldIntoLine);
        Assert.Equal("A6U01 PANDAC", result.Component?.Provenance.Source);
    }

    [Fact]
    public async Task PandacTypedFailureStopsCascade()
    {
        var repository = new StubPandacRepository { NewError = new DependencyPricingError("PANDAC_DB", "CUTFEE failed.", "93") };
        PandacResult result = await new PandacEngine(repository).CalculateAsync(Input(), default);
        Assert.True(result.IsFailure);
        Assert.Equal(["new"], repository.Calls);
    }

    [Theory]
    [InlineData(SurgiTrakFeeBasis.PerLine, 7)]
    [InlineData(SurgiTrakFeeBasis.PerQuantity, 7)]
    [InlineData(SurgiTrakFeeBasis.PerOrder, 7)]
    [InlineData(SurgiTrakFeeBasis.CostPercentage, 10)]
    [InlineData(SurgiTrakFeeBasis.SellPercentage, 20)]
    [InlineData(SurgiTrakFeeBasis.Unknown, 0)]
    public void SurgiTrakImplementsAllConfirmedFeeTypes(SurgiTrakFeeBasis basis, decimal expected)
    {
        SurgiTrakResult result = SurgiTrakEngine.Calculate(SurgiInput(basis));
        Assert.Equal(expected, result.Amount.Value);
    }

    [Fact]
    public void SurgiTrakEligibilityAndMonthlyBillingAreIndependent()
    {
        Assert.Equal(0m, SurgiTrakEngine.Calculate(SurgiInput(SurgiTrakFeeBasis.PerLine) with { IsOwensProduct = false }).Amount.Value);
        Assert.Equal(0m, SurgiTrakEngine.Calculate(SurgiInput(SurgiTrakFeeBasis.PerLine) with { FeeComponentFound = false }).Amount.Value);
        SurgiTrakResult monthly = SurgiTrakEngine.Calculate(SurgiInput(SurgiTrakFeeBasis.CostPercentage) with { BillingFrequency = "MM" });
        Assert.Equal(10m, monthly.Amount.Value);
        Assert.False(monthly.FoldIntoLine);
    }

    private static PandacInput Input() => new(Context(), true, true, true, new Money(100m), new Money(200m));
    private static PandacCandidate Candidate(PandacSource source, PandacFeeBasis basis, decimal rate = 0m, decimal amount = 0m, string? billing = null) =>
        new(source, basis, rate, new Money(amount), true, Provenance("PANDAC"), null, billing);
    private static SurgiTrakInput SurgiInput(SurgiTrakFeeBasis basis) =>
        new(true, true, basis, new Money(7m), 0.1m, new Money(100m), new Money(200m), null, Provenance("SurgiTrak"));
    private static RuleProvenance Provenance(string name) => new(name, $"A6U01 {name}", "ACCOUNT", null);

    private static PricingContext Context()
    {
        var request = new PricingRequest(new DivisionId("01"), new AccountNumber("123456"), new VendorId("V001"), new ProductId("P0000001"),
            new Quantity(1m), new UnitOfMeasure("EA"), null, null, new DateOnly(2026, 7, 21), PricingRequestType.Full);
        var product = new ProductInformation(request.Vendor, request.Product, ProductType.Regular, request.UnitOfMeasure, null, null, 1234, "A");
        return new(request, product, new CustomerInformation(request.Account, new CustomerNumber(42), []), null, null);
    }

    private sealed class StubPandacRepository : IPandacRepository
    {
        public List<string> Calls { get; } = [];
        public PandacCandidate? New { get; init; }
        public PandacCandidate? Legacy { get; init; }
        public PandacCandidate? Default { get; init; }
        public PricingError? NewError { get; init; }
        public ValueTask<PandacLookupResult> FindNewSpecificAsync(PricingContext context, CancellationToken cancellationToken) => Find("new", New, NewError, cancellationToken);
        public ValueTask<PandacLookupResult> FindLegacySpecificAsync(PricingContext context, CancellationToken cancellationToken) => Find("legacy", Legacy, null, cancellationToken);
        public ValueTask<PandacLookupResult> FindDefaultAsync(PricingContext context, CancellationToken cancellationToken) => Find("default", Default, null, cancellationToken);
        private ValueTask<PandacLookupResult> Find(string name, PandacCandidate? value, PricingError? error, CancellationToken token)
        { token.ThrowIfCancellationRequested(); Calls.Add(name); return ValueTask.FromResult(new PandacLookupResult(value, error)); }
    }
}
