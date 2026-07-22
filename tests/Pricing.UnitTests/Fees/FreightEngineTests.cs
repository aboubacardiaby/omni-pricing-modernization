namespace Pricing.UnitTests.Fees;

using Pricing.Application.Fees;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class FreightEngineTests
{
    [Fact]
    public async Task AccountWinnerStopsAllLowerPriorityLookups()
    {
        var repository = new StubRepository { Account = Candidate(FreightSource.Account, 0.1m) };
        FreightResult result = await new FreightEngine(repository).CalculateAsync(Input(), default);
        Assert.Equal(FreightSource.Account, result.Source);
        Assert.Equal(["account"], repository.Calls);
        Assert.Equal(10m, result.Amount.Value);
    }

    [Fact]
    public async Task CustomerPrecedesBuyingGroupAndStopsWaterfall()
    {
        var repository = new StubRepository { Customer = Candidate(FreightSource.Customer, 0.2m), BuyingGroup = Candidate(FreightSource.BuyingGroup, 0.3m) };
        FreightResult result = await new FreightEngine(repository).CalculateAsync(Input(), default);
        Assert.Equal(FreightSource.Customer, result.Source);
        Assert.Equal(["account", "customer"], repository.Calls);
    }

    [Fact]
    public async Task DownstreamWaterfallUsesFirstAvailableSource()
    {
        var repository = new StubRepository { Product = Candidate(FreightSource.Product, direct: 4m), Division = Candidate(FreightSource.DivisionVendor, 0.3m) };
        FreightResult result = await new FreightEngine(repository).CalculateAsync(Input(productFactor: 2m), default);
        Assert.Equal(FreightSource.Product, result.Source);
        Assert.Equal(8m, result.Amount.Value);
        Assert.Equal(["account", "customer", "group", "product"], repository.Calls);
    }

    [Theory]
    [InlineData(FreightApplicationMode.Cost, 10, true, false)]
    [InlineData(FreightApplicationMode.Sell, 30, false, true)]
    [InlineData(FreightApplicationMode.Variable, 5, false, true)]
    public async Task AppliesModeSpecificBasis(FreightApplicationMode mode, decimal expected, bool cost, bool sell)
    {
        var repository = new StubRepository { BuyingGroup = Candidate(FreightSource.BuyingGroup, 0.1m, 0.2m, mode: mode) };
        FreightResult result = await new FreightEngine(repository).CalculateAsync(Input(mode), default);
        Assert.Equal(expected, result.Amount.Value);
        Assert.Equal(cost, result.FoldIntoCost);
        Assert.Equal(sell, result.FoldIntoSell);
    }

    [Fact]
    public async Task AccountVariableModeDowngradesToSell()
    {
        var repository = new StubRepository { Account = Candidate(FreightSource.Account, 0.1m, 0.2m) };
        FreightResult result = await new FreightEngine(repository).CalculateAsync(Input(FreightApplicationMode.Variable), default);
        Assert.Equal(FreightApplicationMode.Sell, result.Mode);
        Assert.Equal(30m, result.Amount.Value);
        Assert.Equal("I", result.TypeCode);
    }

    [Fact]
    public async Task ExemptionZerosAmountButRetainsRecodedAuditSource()
    {
        var repository = new StubRepository { BuyingGroup = Candidate(FreightSource.BuyingGroup, 0.1m) };
        FreightInput input = Input() with { Exemption = Facts(FreightContractClass.SanctionedGroup, sanctionedEnabled: false) };
        FreightResult result = await new FreightEngine(repository).CalculateAsync(input, default);
        Assert.True(result.IsExempt);
        Assert.Equal(0m, result.Amount.Value);
        Assert.Equal("J", result.TypeCode);
        Assert.NotNull(result.Component);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, true)]
    [InlineData(false, false, false, true)]
    public async Task ApplicabilityGatesAvoidRepository(bool contractExempt, bool vhaPlus, bool accountVha, bool disabled = false)
    {
        var repository = new StubRepository { Account = Candidate(FreightSource.Account, 0.1m) };
        FreightResult result = await new FreightEngine(repository).CalculateAsync(
            Input() with { ContractLineExempt = contractExempt, VhaPlusVendor = vhaPlus, AccountVha = accountVha, FreightDisabled = disabled }, default);
        Assert.Null(result.Source);
        Assert.Empty(repository.Calls);
    }

    [Fact]
    public async Task MonthlySellFreightIsCalculatedButNotFoldedIn()
    {
        var repository = new StubRepository { Account = Candidate(FreightSource.Account, 0.1m, 0.2m) };
        FreightResult result = await new FreightEngine(repository).CalculateAsync(Input(FreightApplicationMode.Sell) with { BilledMonthly = true }, default);
        Assert.Equal(30m, result.Amount.Value);
        Assert.False(result.FoldIntoSell);
    }

    [Fact]
    public async Task RepositoryFailureStopsImmediatelyAsTypedError()
    {
        var repository = new StubRepository { AccountError = new DependencyPricingError("FREIGHT_DB", "CUG31 failed.", "36") };
        FreightResult result = await new FreightEngine(repository).CalculateAsync(Input(), default);
        Assert.True(result.IsFailure);
        Assert.Equal(["account"], repository.Calls);
    }

    private static FreightInput Input(FreightApplicationMode mode = FreightApplicationMode.Cost, decimal productFactor = 1m) =>
        new(Context(), new Money(100m), new Money(150m), new Money(50m), mode, Facts(FreightContractClass.OtherContract), ProductUomConversionFactor: productFactor);

    private static FreightExemptionFacts Facts(FreightContractClass contractClass, bool sanctionedEnabled = true) =>
        new(false, true, contractClass, sanctionedEnabled, true, true, true);

    private static FreightCandidate Candidate(FreightSource source, decimal cost = 0m, decimal sell = 0m, decimal? direct = null, FreightApplicationMode? mode = null) =>
        new(source, cost, sell, direct is null ? null : new Money(direct.Value), mode,
            new RuleProvenance(source.ToString(), $"A6U01 freight {source}", source.ToString(), null));

    private static PricingContext Context()
    {
        var request = new PricingRequest(new DivisionId("01"), new AccountNumber("123456"), new VendorId("V001"),
            new ProductId("P0000001"), new Quantity(1m), new UnitOfMeasure("EA"), null, null, new DateOnly(2026, 7, 21), PricingRequestType.Full);
        var product = new ProductInformation(request.Vendor, request.Product, ProductType.Regular, request.UnitOfMeasure, null, null, 1234, "A");
        return new(request, product, new CustomerInformation(request.Account, new CustomerNumber(42), []), null, null);
    }

    private sealed class StubRepository : IFreightRepository
    {
        public List<string> Calls { get; } = [];
        public FreightCandidate? Account { get; init; }
        public FreightCandidate? Customer { get; init; }
        public FreightCandidate? BuyingGroup { get; init; }
        public FreightCandidate? Product { get; init; }
        public FreightCandidate? Division { get; init; }
        public FreightCandidate? Corporate { get; init; }
        public PricingError? AccountError { get; init; }

        public ValueTask<FreightLookupResult> FindAccountAsync(PricingContext context, CancellationToken cancellationToken) => Find("account", Account, AccountError, cancellationToken);
        public ValueTask<FreightLookupResult> FindCustomerAsync(PricingContext context, CancellationToken cancellationToken) => Find("customer", Customer, null, cancellationToken);
        public ValueTask<FreightLookupResult> FindBuyingGroupAsync(PricingContext context, CancellationToken cancellationToken) => Find("group", BuyingGroup, null, cancellationToken);
        public ValueTask<FreightLookupResult> FindProductAsync(PricingContext context, CancellationToken cancellationToken) => Find("product", Product, null, cancellationToken);
        public ValueTask<FreightLookupResult> FindDivisionVendorAsync(PricingContext context, CancellationToken cancellationToken) => Find("division", Division, null, cancellationToken);
        public ValueTask<FreightLookupResult> FindCorporateVendorAsync(PricingContext context, CancellationToken cancellationToken) => Find("corporate", Corporate, null, cancellationToken);

        private ValueTask<FreightLookupResult> Find(string name, FreightCandidate? candidate, PricingError? error, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Calls.Add(name);
            return ValueTask.FromResult(new FreightLookupResult(candidate, error));
        }
    }
}
