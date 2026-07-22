namespace Pricing.UnitTests.SellSelection;

using Pricing.Application.SellSelection;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class AccountCustomerSellArrangementRuleTests
{
    [Theory]
    [InlineData(SellCostCascade.IndividualContract)]
    [InlineData(SellCostCascade.GroupContract)]
    [InlineData(SellCostCascade.AcquisitionCost)]
    public async Task AccountProductPrecedesCustomerProductInEveryCascade(SellCostCascade cascade)
    {
        var repository = new StubRepository();
        repository.Add(SellArrangementScope.Account, SellArrangementLevel.Product, Selection("ACCOUNT-PRODUCT"));
        repository.Add(SellArrangementScope.CustomerNumber, SellArrangementLevel.Product, Selection("CUSTOMER-PRODUCT"));

        SellArrangementResult result = await new SellArrangementRuleEvaluator(
                AccountCustomerSellRuleSet.Create(repository))
            .EvaluateAsync(Context(cascade), CancellationToken.None);

        Assert.Equal("ACCOUNT-PRODUCT", result.Selection?.ArrangementIdentifier);
        Assert.Equal([(SellArrangementScope.Account, SellArrangementLevel.Product)], repository.Calls);
    }

    [Fact]
    public async Task CustomerProductIsFallbackAfterAccountProductMisses()
    {
        var repository = new StubRepository();
        repository.Add(SellArrangementScope.CustomerNumber, SellArrangementLevel.Product, Selection("CUSTOMER-PRODUCT"));

        SellArrangementResult result = await new SellArrangementRuleEvaluator(
                AccountCustomerSellRuleSet.Create(repository))
            .EvaluateAsync(Context(SellCostCascade.IndividualContract), CancellationToken.None);

        Assert.Equal("CUSTOMER-PRODUCT", result.Selection?.ArrangementIdentifier);
        Assert.Equal(
            [
                (SellArrangementScope.Account, SellArrangementLevel.Product),
                (SellArrangementScope.CustomerNumber, SellArrangementLevel.Product),
            ],
            repository.Calls);
    }

    [Fact]
    public async Task IndividualCascadeIncludesVendorContractAndSpecialServiceLevels()
    {
        var repository = new StubRepository();
        repository.Add(SellArrangementScope.Account, SellArrangementLevel.SpecialServiceCode, Selection("ACCOUNT-SSC"));

        SellArrangementResult result = await new SellArrangementRuleEvaluator(
                AccountCustomerSellRuleSet.Create(repository))
            .EvaluateAsync(Context(SellCostCascade.IndividualContract), CancellationToken.None);

        Assert.Equal("ACCOUNT-SSC", result.Selection?.ArrangementIdentifier);
        Assert.Contains((SellArrangementScope.Account, SellArrangementLevel.VendorContract), repository.Calls);
        Assert.Contains((SellArrangementScope.CustomerNumber, SellArrangementLevel.VendorContract), repository.Calls);
        Assert.Equal(
            (SellArrangementScope.Account, SellArrangementLevel.SpecialServiceCode),
            repository.Calls[^1]);
    }

    [Fact]
    public void AcquisitionCascadeOmitsContractOverrideAndAccountCustomerSpecialService()
    {
        AccountCustomerSellArrangementRule[] rules = AccountCustomerSellRuleSet.Create(new StubRepository())
            .OfType<AccountCustomerSellArrangementRule>()
            .Where(rule => rule.Cascade == SellCostCascade.AcquisitionCost)
            .ToArray();

        Assert.DoesNotContain(rules, rule => rule.Level == SellArrangementLevel.VendorContract);
        Assert.DoesNotContain(rules, rule => rule.Level == SellArrangementLevel.SpecialServiceCode);
        Assert.Equal(8, rules.Length);
    }

    [Fact]
    public async Task GroupCostUsesOnlyGroupCascadeRules()
    {
        var repository = new StubRepository();
        repository.Add(SellArrangementScope.Account, SellArrangementLevel.Product, Selection("GROUP-CASCADE"));

        SellArrangementResult result = await new SellArrangementRuleEvaluator(
                AccountCustomerSellRuleSet.Create(repository))
            .EvaluateAsync(Context(SellCostCascade.GroupContract), CancellationToken.None);

        Assert.Equal("GROUP-CASCADE", result.Selection?.ArrangementIdentifier);
        Assert.Single(repository.Calls);
        Assert.Equal(2000, result.Trace.Single(entry => entry.Outcome == SellRuleOutcome.Applied).Priority);
    }

    [Fact]
    public async Task MissingCustomerNumberSkipsCustomerRulesWithoutRepositoryCalls()
    {
        var repository = new StubRepository();
        var rule = new AccountCustomerSellArrangementRule(
            repository,
            SellCostCascade.IndividualContract,
            SellArrangementScope.CustomerNumber,
            SellArrangementLevel.Product,
            1010);

        SellRuleDecision decision = await rule.EvaluateAsync(
            Context(SellCostCascade.IndividualContract, hasCustomer: false), CancellationToken.None);

        Assert.Equal(SellRuleOutcome.Skipped, decision.Outcome);
        Assert.Equal("NOT_ELIGIBLE", decision.SkipReason?.Code);
        Assert.Empty(repository.Calls);
    }

    [Fact]
    public async Task MissingCategorySkipsCategoryLookup()
    {
        var repository = new StubRepository();
        var rule = new AccountCustomerSellArrangementRule(
            repository,
            SellCostCascade.IndividualContract,
            SellArrangementScope.Account,
            SellArrangementLevel.ProductCategory,
            1050);

        SellRuleDecision decision = await rule.EvaluateAsync(
            Context(SellCostCascade.IndividualContract, hasCategory: false), CancellationToken.None);

        Assert.Equal("NOT_ELIGIBLE", decision.SkipReason?.Code);
        Assert.Empty(repository.Calls);
    }

    [Fact]
    public async Task NoAccountOrCustomerMatchLeavesNoArrangementForLaterRules()
    {
        SellArrangementResult result = await new SellArrangementRuleEvaluator(
                AccountCustomerSellRuleSet.Create(new StubRepository()))
            .EvaluateAsync(Context(SellCostCascade.AcquisitionCost), CancellationToken.None);

        Assert.True(result.HasNoArrangement);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task RepositoryFailureAndCancellationPropagateCorrectly()
    {
        var failedRepository = new StubRepository(
            new SellArrangementRepositoryException("SAG08 query failed", "77", "90", true));
        var rule = new AccountCustomerSellArrangementRule(
            failedRepository,
            SellCostCascade.IndividualContract,
            SellArrangementScope.Account,
            SellArrangementLevel.Product,
            1000);

        SellRuleDecision failed = await rule.EvaluateAsync(
            Context(SellCostCascade.IndividualContract), CancellationToken.None);
        DependencyPricingError error = Assert.IsType<DependencyPricingError>(failed.Error);
        Assert.Equal("77", error.LegacyErrorCode);
        Assert.Equal("90", error.LegacySeverityCode);
        Assert.True(error.IsTransient);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var canceledRepository = new StubRepository();
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await new AccountCustomerSellArrangementRule(
                    canceledRepository,
                    SellCostCascade.IndividualContract,
                    SellArrangementScope.Account,
                    SellArrangementLevel.Product,
                    1000)
                .EvaluateAsync(Context(SellCostCascade.IndividualContract), cancellation.Token));
        Assert.Empty(canceledRepository.Calls);
    }

    private static SellArrangementSelection Selection(string id) => new(
        id,
        "TEST",
        new RuleProvenance("account-customer-sell", "SAG08", "account/customer", ActiveDates()));

    private static PricingContext Context(
        SellCostCascade cascade,
        bool hasCustomer = true,
        bool hasCategory = true)
    {
        var request = new PricingRequest(
            new DivisionId("01"), new AccountNumber("123456"), new VendorId("V001"),
            new ProductId("P0000001"), new Quantity(1m), new UnitOfMeasure("EA"),
            null, null, new DateOnly(2026, 7, 21), PricingRequestType.Full);
        var product = new ProductInformation(
            request.Vendor, request.Product, ProductType.Regular, request.UnitOfMeasure,
            null, null, hasCategory ? 1234 : null, "A");
        var customer = new CustomerInformation(
            request.Account,
            hasCustomer ? new CustomerNumber(42) : null,
            []);
        ICostSourceSelection cost = cascade switch
        {
            SellCostCascade.IndividualContract => Contract(null),
            SellCostCascade.GroupContract => Contract(100),
            SellCostCascade.AcquisitionCost => new AcquisitionCostSelection(
                new Money(10m), new UnitOfMeasure("EA"), new Money(8m), "01",
                new DateOnly(2026, 1, 1), false, false,
                new RuleProvenance("acquisition", "VNG03", "vendor", ActiveDates())),
            _ => throw new ArgumentOutOfRangeException(nameof(cascade)),
        };
        return new PricingContext(request, product, customer, cost as ContractSelection, null, cost);
    }

    private static ContractSelection Contract(long? buyingGroupId) => new(
        new ContractId("C-1"),
        buyingGroupId is null ? "INDIVIDUAL" : "GROUP",
        new Money(10m),
        new UnitOfMeasure("EA"),
        new RuleProvenance("cost", "CCG03", "contract", ActiveDates()),
        buyingGroupId);

    private static PricingDateRange ActiveDates() =>
        new(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

    private sealed class StubRepository : IAccountCustomerSellArrangementRepository
    {
        private readonly Dictionary<(SellArrangementScope Scope, SellArrangementLevel Level), SellArrangementSelection> matches = [];
        private readonly Exception? exception;

        internal StubRepository()
        {
        }

        internal StubRepository(Exception exception) => this.exception = exception;

        internal List<(SellArrangementScope Scope, SellArrangementLevel Level)> Calls { get; } = [];

        internal void Add(
            SellArrangementScope scope,
            SellArrangementLevel level,
            SellArrangementSelection selection) => matches.Add((scope, level), selection);

        public ValueTask<SellArrangementSelection?> FindAsync(
            PricingContext context,
            SellArrangementScope scope,
            SellArrangementLevel level,
            CancellationToken cancellationToken)
        {
            Calls.Add((scope, level));
            cancellationToken.ThrowIfCancellationRequested();
            if (exception is not null)
            {
                return ValueTask.FromException<SellArrangementSelection?>(exception);
            }

            matches.TryGetValue((scope, level), out SellArrangementSelection? selection);
            return ValueTask.FromResult(selection);
        }
    }
}
