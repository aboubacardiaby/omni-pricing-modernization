namespace Pricing.UnitTests.CostSelection;

using System.Collections.Immutable;
using Pricing.Application.CostSelection;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class HealthcareCostOverrideRuleTests
{
    [Fact]
    public async Task ActiveAccountEligibilityWinsBeforeCustomer()
    {
        var data = Data(
            account: Eligibility(10, HealthcareOverrideScope.Account),
            customer: Eligibility(20, HealthcareOverrideScope.Customer),
            prices: [Price(20m, 12m)]);

        HealthcareCostSelection selection = await SelectAsync(data);

        Assert.Equal(10, selection.HealthcareGroupId);
        Assert.Equal("ACCOUNT", selection.Scope);
        Assert.Equal(12m, selection.UnitCost.Value);
    }

    [Fact]
    public async Task CustomerEligibilityIsFallbackWhenAccountDatesAreInactive()
    {
        HealthcareCostEligibility expiredAccount = Eligibility(10, HealthcareOverrideScope.Account) with
        {
            HeaderDates = new PricingDateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)),
        };
        var data = Data(expiredAccount, Eligibility(20, HealthcareOverrideScope.Customer), prices: [Price(20m, 12m)]);

        HealthcareCostSelection selection = await SelectAsync(data);

        Assert.Equal(20, selection.HealthcareGroupId);
        Assert.Equal("CUSTOMER", selection.Scope);
    }

    [Fact]
    public async Task AllFourIndependentDateWindowsMustContainPricingDate()
    {
        HealthcareCostEligibility eligibility = Eligibility(10, HealthcareOverrideScope.Account) with
        {
            GroupDetailDates = new PricingDateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)),
        };

        CostRuleDecision decision = await Rule(new StubRepository(Data(account: eligibility)))
            .EvaluateAsync(Context(), CancellationToken.None);

        Assert.Equal(CostRuleOutcome.Skipped, decision.Outcome);
        Assert.Equal("NO_CANDIDATE", decision.SkipReason?.Code);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, true)]
    public async Task ProductOverrideFlagTakesPrecedenceOverGroupFlag(
        bool groupOverridden,
        bool? productOverridden,
        bool expectedExcluded)
    {
        HealthcareCostEligibility eligibility = Eligibility(10, HealthcareOverrideScope.Account) with
        {
            GroupCostOverridden = groupOverridden,
            ProductCostOverridden = productOverridden,
        };

        CostRuleDecision decision = await Rule(new StubRepository(Data(account: eligibility, prices: [Price(20m, 12m)])))
            .EvaluateAsync(Context(), CancellationToken.None);

        Assert.Equal(expectedExcluded ? CostRuleOutcome.Skipped : CostRuleOutcome.Applied, decision.Outcome);
        if (expectedExcluded)
        {
            Assert.Equal("EXCLUDED", decision.SkipReason?.Code);
        }
    }

    [Fact]
    public async Task GroupFlagIsUsedWhenProductFlagIsAbsent()
    {
        HealthcareCostEligibility eligibility = Eligibility(10, HealthcareOverrideScope.Account) with
        {
            GroupCostOverridden = true,
            ProductCostOverridden = null,
        };

        CostRuleDecision decision = await Rule(new StubRepository(Data(account: eligibility, prices: [Price(20m, 12m)])))
            .EvaluateAsync(Context(), CancellationToken.None);

        Assert.Equal("EXCLUDED", decision.SkipReason?.Code);
    }

    [Fact]
    public async Task HighestAcquisitionCostWinsWithEarliestActiveDateTieBreak()
    {
        HealthcarePriceListCandidate[] prices =
        [
            Price(20m, 8m, new DateOnly(2026, 2, 1)),
            Price(30m, 11m, new DateOnly(2026, 3, 1)),
            Price(30m, 12m, new DateOnly(2026, 1, 15)),
        ];

        HealthcareCostSelection selection = await SelectAsync(Data(
            account: Eligibility(10, HealthcareOverrideScope.Account),
            prices: [.. prices]));

        Assert.Equal(30m, selection.AcquisitionCost.Value);
        Assert.Equal(12m, selection.UnitCost.Value);
        Assert.Equal(new DateOnly(2026, 1, 15), selection.PriceListEffectiveDate);
    }

    [Fact]
    public async Task NonOverlappingPriceListRowsFallThroughToAcquisitionRule()
    {
        HealthcarePriceListCandidate price = Price(30m, 12m, new DateOnly(2025, 1, 1)) with
        {
            ExpirationDate = new DateOnly(2025, 12, 31),
        };

        CostRuleDecision decision = await Rule(new StubRepository(Data(
                account: Eligibility(10, HealthcareOverrideScope.Account),
                prices: [price])))
            .EvaluateAsync(Context(), CancellationToken.None);

        Assert.Equal(CostRuleOutcome.Skipped, decision.Outcome);
        Assert.Equal("NO_CANDIDATE", decision.SkipReason?.Code);
    }

    [Fact]
    public async Task AccountCostPlusSellTermsAreRetainedWithoutCalculatingSell()
    {
        HealthcareSellOverrideTerms terms = CostPlusTerms(10, HealthcareOverrideScope.Account, 0.1250m);
        var data = Data(
            account: Eligibility(10, HealthcareOverrideScope.Account),
            accountSell: terms,
            prices: [Price(20m, 12m)]);

        HealthcareCostSelection selection = await SelectAsync(data);

        Assert.Same(terms, selection.SellOverride);
        Assert.Equal(HealthcareSellOverrideType.CostPlus, selection.SellOverride!.Type);
        Assert.Equal(0.1250m, selection.SellOverride.Percentage);
        Assert.Null(selection.SellOverride.StatedPrice);
    }

    [Fact]
    public async Task CustomerStatedPriceTermsAreFallbackAndRetained()
    {
        var expiredAccount = new HealthcareSellOverrideTerms(
            HealthcareSellOverrideType.CostPlus,
            0.1m,
            null,
            new UnitOfMeasure("EA"),
            new PricingDateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)),
            "HC_OVRD_PRODUCT",
            10,
            "ACCOUNT");
        var stated = new HealthcareSellOverrideTerms(
            HealthcareSellOverrideType.StatedPrice,
            null,
            new Money(15.75m),
            new UnitOfMeasure("EA"),
            ActiveDates(),
            "HC_OVRD_PRODUCT",
            20,
            "CUSTOMER");
        var data = Data(
            account: Eligibility(10, HealthcareOverrideScope.Account),
            accountSell: expiredAccount,
            customerSell: stated,
            prices: [Price(20m, 12m)]);

        HealthcareCostSelection selection = await SelectAsync(data);

        Assert.Same(stated, selection.SellOverride);
        Assert.Equal(15.75m, selection.SellOverride!.StatedPrice?.Value);
        Assert.Null(selection.SellOverride.Percentage);
    }

    [Fact]
    public void HealthcareSellTermsRequireValueMatchingType()
    {
        Assert.Throws<ArgumentException>(() => new HealthcareSellOverrideTerms(
            HealthcareSellOverrideType.CostPlus, null, null, new UnitOfMeasure("EA"), ActiveDates(), "HC"));
        Assert.Throws<ArgumentException>(() => new HealthcareSellOverrideTerms(
            HealthcareSellOverrideType.StatedPrice, null, null, new UnitOfMeasure("EA"), ActiveDates(), "HC"));
    }

    [Fact]
    public async Task RepositoryFailureAndCancellationArePropagatedCorrectly()
    {
        var failure = new HealthcareCostOverrideRepositoryException("HC query failed", "945", "90", true);
        CostRuleDecision failed = await Rule(new StubRepository(failure))
            .EvaluateAsync(Context(), CancellationToken.None);
        DependencyPricingError error = Assert.IsType<DependencyPricingError>(failed.Error);
        Assert.Equal("945", error.LegacyErrorCode);
        Assert.True(error.IsTransient);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var repository = new StubRepository(Data());
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await Rule(repository).EvaluateAsync(Context(), cancellation.Token));
        Assert.Equal(0, repository.CallCount);
    }

    private static async Task<HealthcareCostSelection> SelectAsync(HealthcareOverrideData data)
    {
        CostRuleDecision decision = await Rule(new StubRepository(data))
            .EvaluateAsync(Context(), CancellationToken.None);
        return Assert.IsType<HealthcareCostSelection>(decision.Selection);
    }

    private static HealthcareCostOverrideRule Rule(IHealthcareCostOverrideRepository repository) => new(repository);

    private static HealthcareOverrideData Data(
        HealthcareCostEligibility? account = null,
        HealthcareCostEligibility? customer = null,
        HealthcareSellOverrideTerms? accountSell = null,
        HealthcareSellOverrideTerms? customerSell = null,
        ImmutableArray<HealthcarePriceListCandidate> prices = default) =>
        new(account, customer, accountSell, customerSell, prices.IsDefault ? [] : prices);

    private static HealthcareCostEligibility Eligibility(long groupId, HealthcareOverrideScope scope) => new(
        groupId,
        scope,
        ActiveDates(),
        ActiveDates(),
        ActiveDates(),
        ActiveDates(),
        false,
        false,
        scope == HealthcareOverrideScope.Account ? "HC_OVRD_GROUP_ACCOUNT" : "HC_OVRD_GROUP_CUSTOMER");

    private static HealthcarePriceListCandidate Price(
        decimal acquisition,
        decimal dealer,
        DateOnly? active = null) => new(
        new Money(acquisition),
        new Money(dealer),
        new UnitOfMeasure("EA"),
        active ?? new DateOnly(2026, 1, 1),
        active ?? new DateOnly(2026, 1, 1),
        new DateOnly(2026, 12, 31),
        new RuleProvenance("healthcare-cost-override", "VNG03", "healthcare", ActiveDates()));

    private static HealthcareSellOverrideTerms CostPlusTerms(
        long groupId,
        HealthcareOverrideScope scope,
        decimal percentage) => new(
        HealthcareSellOverrideType.CostPlus,
        percentage,
        null,
        new UnitOfMeasure("EA"),
        ActiveDates(),
        "HC_OVRD_PRODUCT",
        groupId,
        scope.ToString().ToUpperInvariant());

    private static PricingDateRange ActiveDates() =>
        new(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

    private static PricingContext Context()
    {
        var request = new PricingRequest(
            new DivisionId("01"), new AccountNumber("123456"), new VendorId("V001"),
            new ProductId("P0000001"), new Quantity(1m), new UnitOfMeasure("EA"),
            null, null, new DateOnly(2026, 7, 21), PricingRequestType.Full);
        var product = new ProductInformation(
            request.Vendor, request.Product, ProductType.Regular, request.UnitOfMeasure,
            null, null, 1234, "A");
        var customer = new CustomerInformation(request.Account, new CustomerNumber(42), []);
        return new PricingContext(request, product, customer, null, null);
    }

    private sealed class StubRepository : IHealthcareCostOverrideRepository
    {
        private readonly HealthcareOverrideData? data;
        private readonly Exception? exception;

        internal StubRepository(HealthcareOverrideData data) => this.data = data;
        internal StubRepository(Exception exception) => this.exception = exception;
        internal int CallCount { get; private set; }

        public ValueTask<HealthcareOverrideData> FindAsync(PricingContext context, CancellationToken cancellationToken)
        {
            CallCount++;
            cancellationToken.ThrowIfCancellationRequested();
            return exception is null
                ? ValueTask.FromResult(data!)
                : ValueTask.FromException<HealthcareOverrideData>(exception);
        }
    }
}
