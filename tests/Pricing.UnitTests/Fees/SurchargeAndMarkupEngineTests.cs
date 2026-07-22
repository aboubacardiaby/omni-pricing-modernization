namespace Pricing.UnitTests.Fees;

using Pricing.Application.Fees;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class SurchargeAndMarkupEngineTests
{
    [Fact]
    public async Task SurchargeUsesFirstMatchInExactSixteenLevelOrder()
    {
        var repository = new StubSurchargeRepository
        {
            Candidates =
            {
                [SurchargeLevel.AccountDefault] = Candidate(SurchargeLevel.AccountDefault, 0.1m),
                [SurchargeLevel.CustomerCategoryVendor] = Candidate(SurchargeLevel.CustomerCategoryVendor, 0.2m),
            },
        };
        SurchargeResult result = await new SurchargeEngine(repository).CalculateAsync(SurchargeInput(), default);
        Assert.Equal(SurchargeLevel.AccountDefault.ToString(), result.SourceCode);
        Assert.Equal(10m, result.Amount.Value);
        Assert.Equal(SurchargeLevel.AccountDefault, repository.Calls.Last());
        Assert.DoesNotContain(SurchargeLevel.CustomerCategoryVendor, repository.Calls);
    }

    [Fact]
    public async Task MissingCategorySkipsEveryCategorySpecificLookup()
    {
        var repository = new StubSurchargeRepository { Candidates = { [SurchargeLevel.AccountVendor] = Candidate(SurchargeLevel.AccountVendor, 0.1m) } };
        await new SurchargeEngine(repository).CalculateAsync(SurchargeInput() with { HasProductCategory = false }, default);
        Assert.DoesNotContain(repository.Calls, level => level.ToString().Contains("Category", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BuyingGroupIsOnlyTriedAfterAllNamedScopesMiss()
    {
        var repository = new StubSurchargeRepository { Candidates = { [SurchargeLevel.BuyingGroup] = Candidate(SurchargeLevel.BuyingGroup, 0.1m) } };
        SurchargeResult result = await new SurchargeEngine(repository).CalculateAsync(SurchargeInput(), default);
        Assert.Equal(17, repository.Calls.Count);
        Assert.Equal(SurchargeLevel.BuyingGroup, repository.Calls[^1]);
        Assert.Equal(10m, result.Amount.Value);
    }

    [Fact]
    public async Task PriceLockBypassesLookupAndMonthlyBillingSuppressesFoldIn()
    {
        var repository = new StubSurchargeRepository { Candidates = { [SurchargeLevel.AccountCategoryVendor] = Candidate(SurchargeLevel.AccountCategoryVendor, 0.1m) } };
        Assert.Empty((await new SurchargeEngine(repository).CalculateAsync(SurchargeInput() with { IsPriceLocked = true }, default)).SourceCode);
        Assert.Empty(repository.Calls);
        SurchargeResult monthly = await new SurchargeEngine(repository).CalculateAsync(SurchargeInput() with { BillingFrequency = "MA" }, default);
        Assert.False(monthly.FoldIntoLine);
        Assert.Equal(10m, monthly.Amount.Value);
    }

    [Fact]
    public async Task SurchargeTruncatesInsteadOfRoundingAndSoftFailureDoesNotThrow()
    {
        var repository = new StubSurchargeRepository { Candidates = { [SurchargeLevel.AccountCategoryVendor] = Candidate(SurchargeLevel.AccountCategoryVendor, 0.333333333m) } };
        SurchargeResult truncated = await new SurchargeEngine(repository).CalculateAsync(SurchargeInput() with { TotalCost = new Money(1m) }, default);
        Assert.Equal(0.33333333m, truncated.Amount.Value);
        repository.Candidates.Clear();
        repository.Error = new DependencyPricingError("SURCHARGE_DB", "Lookup failed.");
        SurchargeResult failed = await new SurchargeEngine(repository).CalculateAsync(SurchargeInput(), default);
        Assert.NotNull(failed.SoftError);
        Assert.Equal(0m, failed.Amount.Value);
    }

    [Fact]
    public void CostContractClassificationUsesConfirmedPriority()
    {
        MarkupCompositionResult custom = MarkupCompositionEngine.Calculate(Markup() with { IsCustomProduct = true, CustomPercentage = 0.1m, IsGroupSanctioned = true });
        Assert.Equal(DistributionCategory.Customer, custom.Category);
        MarkupCompositionResult sanctioned = MarkupCompositionEngine.Calculate(Markup() with { IsGroupSanctioned = true, IsDivision01NonSanctioned = true });
        Assert.Equal(DistributionCategory.GroupSanctioned, sanctioned.Category);
        MarkupCompositionResult nonSanctioned = MarkupCompositionEngine.Calculate(Markup() with { IsDivision01NonSanctioned = true, IsIndividualContract = true });
        Assert.Equal(DistributionCategory.GroupNonSanctioned, nonSanctioned.Category);
        Assert.Equal(DistributionCategory.Individual, MarkupCompositionEngine.Calculate(Markup() with { IsIndividualContract = true }).Category);
        Assert.Equal(DistributionCategory.NonContract, MarkupCompositionEngine.Calculate(Markup() with { CostContractFound = false }).Category);
    }

    [Fact]
    public void InvalidCostContractScenarioReturnsLegacy602TypedError()
    {
        MarkupCompositionResult result = MarkupCompositionEngine.Calculate(Markup());
        Assert.True(result.IsFailure);
        Assert.Equal("602", result.Error?.LegacyErrorCode);
        Assert.Equal("70", result.Error?.LegacySeverityCode);
    }

    [Theory]
    [InlineData(AccountPriceMethod.Stock, true, DistributionCategory.GroupSanctioned)]
    [InlineData(AccountPriceMethod.Stock, false, DistributionCategory.NonContract)]
    [InlineData(AccountPriceMethod.Usage, true, DistributionCategory.GroupSanctioned)]
    [InlineData(AccountPriceMethod.Usage, false, DistributionCategory.NonContract)]
    public void StockAndUsageUseTheirIndependentTwoWayClassification(AccountPriceMethod method, bool matched, DistributionCategory expected)
    {
        MarkupClassificationInput input = Markup() with { PriceMethod = method, IsStockItem = matched, IsSpecifiedUsage = matched };
        Assert.Equal(expected, MarkupCompositionEngine.Calculate(input).Category);
    }

    [Fact]
    public void MarkupClampsPercentageAndComposesMonthlyDistributionReversal()
    {
        var billing = new Dictionary<DistributionCategory, string?> { [DistributionCategory.GroupSanctioned] = "MM" };
        MarkupCompositionResult result = MarkupCompositionEngine.Calculate(Markup() with { IsGroupSanctioned = true, SanctionedPercentage = 1m, BillingFrequency = billing });
        Assert.Equal(0.9999m, result.Percentage);
        Assert.Equal(100m, result.SellPrice.Value);
        Assert.True(result.Distribution?.BilledSeparately);
    }

    private static SurchargeInput SurchargeInput() => new(Context(), new Money(100m), false, true, null);
    private static SurchargeCandidate Candidate(SurchargeLevel level, decimal percentage) => new(level, percentage, level.ToString(), Provenance("surcharge"));
    private static MarkupClassificationInput Markup() => new(
        true, AccountPriceMethod.CostContract, false, null, true, false, false, false, false, false, false,
        0.2m, 0.3m, 0.4m, 0.1m, new Money(100m), new Dictionary<DistributionCategory, string?>(), Provenance("markup"));
    private static RuleProvenance Provenance(string name) => new(name, $"A6U01 {name}", "ACCOUNT", null);

    private static PricingContext Context()
    {
        var request = new PricingRequest(new DivisionId("01"), new AccountNumber("123456"), new VendorId("V001"), new ProductId("P0000001"),
            new Quantity(1m), new UnitOfMeasure("EA"), null, null, new DateOnly(2026, 7, 21), PricingRequestType.Full);
        var product = new ProductInformation(request.Vendor, request.Product, ProductType.Regular, request.UnitOfMeasure, null, null, 1234, "A");
        return new(request, product, new CustomerInformation(request.Account, new CustomerNumber(42), []), null, null);
    }

    private sealed class StubSurchargeRepository : ISurchargeRepository
    {
        public Dictionary<SurchargeLevel, SurchargeCandidate> Candidates { get; } = [];
        public List<SurchargeLevel> Calls { get; } = [];
        public PricingError? Error { get; set; }
        public ValueTask<SurchargeLookupResult> FindAsync(PricingContext context, SurchargeLevel level, CancellationToken cancellationToken)
        { cancellationToken.ThrowIfCancellationRequested(); Calls.Add(level); return ValueTask.FromResult(new SurchargeLookupResult(Candidates.GetValueOrDefault(level), Error)); }
    }
}
