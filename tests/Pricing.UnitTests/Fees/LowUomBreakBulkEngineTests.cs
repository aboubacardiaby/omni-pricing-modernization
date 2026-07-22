namespace Pricing.UnitTests.Fees;

using System.Collections.Immutable;
using Pricing.Application.Fees;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class LowUomBreakBulkEngineTests
{
    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public async Task EligibilityStockAndContractExclusionGateAllWork(bool eligible, bool stock, bool excluded)
    {
        var repository = new StubRepository();
        LowUomResult result = await new LowUomBreakBulkEngine(repository).CalculateAsync(Input() with
        { IsEligible = eligible, IsStockOrder = stock, IsContractExcluded = excluded }, default);
        Assert.Equal(0m, result.Amount.Value);
        Assert.Equal(0, repository.LoadCalls);
    }

    [Fact]
    public async Task AccountSourceWinsOverGroupHierarchy()
    {
        LowUomChargeSource account = Source(LowUomSourceScope.Account, low: 0.1m);
        LowUomChargeSource group = Source(LowUomSourceScope.BuyingGroup, low: 0.5m, priority: 1, groupId: 10);
        LowUomResult result = await Engine().CalculateAsync(Input([group, account]), default);
        Assert.Same(account, result.Source);
        Assert.Equal(10m, result.Amount.Value);
    }

    [Fact]
    public async Task GroupUsesLowestPriorityThenNearestParent()
    {
        LowUomChargeSource far = Source(LowUomSourceScope.BuyingGroup, low: 0.3m, priority: 1, depth: 2, groupId: 30);
        LowUomChargeSource near = Source(LowUomSourceScope.BuyingGroup, low: 0.2m, priority: 1, depth: 1, groupId: 20);
        LowUomResult result = await Engine().CalculateAsync(Input([far, near]), default);
        Assert.Same(near, result.Source);
        Assert.Equal(20m, result.Amount.Value);
    }

    [Fact]
    public async Task VendorExclusionSuppressesGroupButNotAccount()
    {
        var repository = new StubRepository { VendorExcluded = true };
        LowUomChargeSource group = Source(LowUomSourceScope.BuyingGroup, low: 0.2m, groupId: 20, checkVendor: true);
        LowUomResult groupResult = await new LowUomBreakBulkEngine(repository).CalculateAsync(Input([group]), default);
        Assert.True(groupResult.IsVendorExcluded);

        repository.VendorChecks = 0;
        LowUomResult accountResult = await new LowUomBreakBulkEngine(repository).CalculateAsync(Input([Source(LowUomSourceScope.Account, low: 0.2m)]), default);
        Assert.False(accountResult.IsVendorExcluded);
        Assert.Equal(0, repository.VendorChecks);
    }

    [Fact]
    public async Task QuantityGreaterThanOneUsesFirstExactAlternateFactor()
    {
        var repository = new StubRepository
        {
            Candidates = [Alt("CS", 4m, LowUomDesignator.BreakBulk), Alt("BX", 3m, LowUomDesignator.LowUnitOfMeasure)],
        };
        LowUomInput input = Input() with { OrderedQuantity = new Quantity(6m) };
        LowUomResult result = await new LowUomBreakBulkEngine(repository).CalculateAsync(input, default);
        Assert.Equal(LowUomDesignator.LowUnitOfMeasure, result.Designator);
    }

    [Fact]
    public async Task QuantityOneUsesDirectUomAndFallsBackToBaseDesignator()
    {
        var repository = new StubRepository { Candidates = [Alt("CS", 4m, LowUomDesignator.BreakBulk)] };
        LowUomResult direct = await new LowUomBreakBulkEngine(repository).CalculateAsync(Input() with { OrderedUnitOfMeasure = new UnitOfMeasure("CS") }, default);
        Assert.Equal(LowUomDesignator.BreakBulk, direct.Designator);
        LowUomResult fallback = await new LowUomBreakBulkEngine(repository).CalculateAsync(Input() with { OrderedUnitOfMeasure = new UnitOfMeasure("EA") }, default);
        Assert.Equal(LowUomDesignator.LowUnitOfMeasure, fallback.Designator);
    }

    [Fact]
    public async Task LegacyPrivateLabelUsesSellAndZeroPercentageIsNoCharge()
    {
        LowUomResult sell = await Engine().CalculateAsync(Input() with { IsPrivateLabelO = true }, default);
        Assert.Equal(20m, sell.Amount.Value);
        LowUomResult zero = await Engine().CalculateAsync(Input([Source(LowUomSourceScope.Account, low: 0m)]), default);
        Assert.Equal(0m, zero.Amount.Value);
        Assert.Equal(0m, zero.AppliedPercentage);
    }

    [Fact]
    public async Task FeeComponentPandacSuppressesAndFlatBypassesPercentage()
    {
        LowUomFeeComponent suppressed = Component(FeeComponentBasis.Flat) with { PandacVendor = true, PandacItem = true, PandacCustomer = true };
        Assert.Equal(0m, (await Engine().CalculateAsync(Input() with { FeeComponent = suppressed }, default)).Amount.Value);
        LowUomResult flat = await Engine().CalculateAsync(Input() with { FeeComponent = Component(FeeComponentBasis.Flat) }, default);
        Assert.Equal(7m, flat.Amount.Value);
    }

    [Fact]
    public async Task FeeComponentOwnBasisOverridesJitCode()
    {
        LowUomResult cost = await Engine().CalculateAsync(Input() with { JitServiceFeeCode = "P", FeeComponent = Component(FeeComponentBasis.CostPercentage) }, default);
        LowUomResult sell = await Engine().CalculateAsync(Input() with { JitServiceFeeCode = "C", FeeComponent = Component(FeeComponentBasis.SellPercentage) }, default);
        Assert.Equal(30m, cost.Amount.Value);
        Assert.Equal(60m, sell.Amount.Value);
    }

    [Fact]
    public async Task RepositoryErrorIsTypedFailureAndCancellationIsObserved()
    {
        var repository = new StubRepository { Error = new DependencyPricingError("ALT_UOM_DB", "VNG05 failed.", "701") };
        Assert.True((await new LowUomBreakBulkEngine(repository).CalculateAsync(Input(), default)).IsFailure);
        using var source = new CancellationTokenSource(); source.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await Engine().CalculateAsync(Input(), source.Token));
    }

    private static LowUomBreakBulkEngine Engine() => new(new StubRepository());
    private static LowUomInput Input(ImmutableArray<LowUomChargeSource> sources = default) => new(
        Context(), true, true, false, new Quantity(1m), new UnitOfMeasure("EA"), null, 1m,
        LowUomDesignator.LowUnitOfMeasure, sources.IsDefault ? [Source(LowUomSourceScope.Account, low: 0.1m, bulk: 0.2m)] : sources,
        new Money(100m), new Money(200m), "C", false);

    private static LowUomChargeSource Source(LowUomSourceScope scope, decimal low = 0m, decimal bulk = 0m, int priority = 1, int depth = 0, long? groupId = null, bool checkVendor = false) =>
        new(scope, low, bulk, Provenance(), groupId, priority, depth, checkVendor);
    private static AlternateUomCandidate Alt(string uom, decimal factor, LowUomDesignator designator) => new(new UnitOfMeasure(uom), factor, designator);
    private static LowUomFeeComponent Component(FeeComponentBasis basis) => new(basis, 0.3m, 0.4m, new Money(7m), new Money(8m), Provenance());
    private static RuleProvenance Provenance() => new("LUOM", "A6U01 0245/7872-7898", "ACCOUNT", null);

    private static PricingContext Context()
    {
        var request = new PricingRequest(new DivisionId("01"), new AccountNumber("123456"), new VendorId("V001"), new ProductId("P0000001"),
            new Quantity(1m), new UnitOfMeasure("EA"), null, null, new DateOnly(2026, 7, 21), PricingRequestType.Full);
        var product = new ProductInformation(request.Vendor, request.Product, ProductType.Regular, request.UnitOfMeasure, null, null, 1234, "A");
        return new(request, product, new CustomerInformation(request.Account, new CustomerNumber(42), []), null, null);
    }

    private sealed class StubRepository : ILowUomRepository
    {
        public ImmutableArray<AlternateUomCandidate> Candidates { get; init; } = [];
        public PricingError? Error { get; init; }
        public bool VendorExcluded { get; init; }
        public int LoadCalls { get; private set; }
        public int VendorChecks { get; set; }
        public ValueTask<AlternateUomLookupResult> LoadAlternateUomsAsync(PricingContext context, CancellationToken cancellationToken)
        { cancellationToken.ThrowIfCancellationRequested(); LoadCalls++; return ValueTask.FromResult(new AlternateUomLookupResult(Candidates, Error)); }
        public ValueTask<bool> IsGroupVendorExcludedAsync(PricingContext context, long buyingGroupId, DateOnly effectiveDate, CancellationToken cancellationToken)
        { cancellationToken.ThrowIfCancellationRequested(); VendorChecks++; return ValueTask.FromResult(VendorExcluded); }
    }
}
