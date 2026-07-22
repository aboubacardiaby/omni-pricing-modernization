namespace Pricing.UnitTests.SellSelection;

using System.Collections.Immutable;
using Pricing.Application.SellSelection;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class CorporateSellArrangementRuleTests
{
    [Fact]
    public async Task CorporateMatchAppliesWhenGroupHasNoSameLevelOverride()
    {
        SellArrangementSelection corporate = Selection("CORPORATE", "corporate");
        var repository = new StubCorporateRepository(new CorporateSellArrangementMatch(corporate, null));

        SellRuleDecision decision = await Rule(repository, SellArrangementLevel.Product, 1020)
            .EvaluateAsync(Context(SellCostCascade.IndividualContract), CancellationToken.None);

        Assert.Same(corporate, decision.Selection);
        Assert.Equal("corporate", decision.Selection?.Provenance.HierarchyLevel);
    }

    [Theory]
    [InlineData(SellArrangementLevel.Product)]
    [InlineData(SellArrangementLevel.ProductCategory)]
    [InlineData(SellArrangementLevel.Vendor)]
    public async Task SameLevelGroupMatchOverridesCorporate(SellArrangementLevel level)
    {
        SellArrangementSelection corporate = Selection("CORPORATE", "corporate");
        SellArrangementSelection group = Selection("GROUP", "group", 100);
        var repository = new StubCorporateRepository(new CorporateSellArrangementMatch(corporate, group));

        SellRuleDecision decision = await Rule(repository, level, 1020)
            .EvaluateAsync(Context(SellCostCascade.IndividualContract), CancellationToken.None);

        Assert.Same(group, decision.Selection);
        Assert.Equal(100, decision.Selection?.BuyingGroupId);
    }

    [Fact]
    public async Task GroupOverrideIsIgnoredWhenCorporateDidNotMatch()
    {
        SellArrangementSelection group = Selection("GROUP", "group", 100);
        var repository = new StubCorporateRepository(new CorporateSellArrangementMatch(null, group));

        SellRuleDecision decision = await Rule(repository, SellArrangementLevel.Product, 1020)
            .EvaluateAsync(Context(SellCostCascade.IndividualContract), CancellationToken.None);

        Assert.Equal(SellRuleOutcome.Skipped, decision.Outcome);
        Assert.Equal("NO_CANDIDATE", decision.SkipReason?.Code);
    }

    [Theory]
    [InlineData(SellCostCascade.GroupContract)]
    [InlineData(SellCostCascade.AcquisitionCost)]
    public async Task CorporateTierIsAbsentOutsideIndividualContractCascade(SellCostCascade cascade)
    {
        var repository = new StubCorporateRepository(new CorporateSellArrangementMatch(
            Selection("CORPORATE", "corporate"), null));

        SellRuleDecision decision = await Rule(repository, SellArrangementLevel.Product, 1020)
            .EvaluateAsync(Context(cascade), CancellationToken.None);

        Assert.Equal("NOT_ELIGIBLE", decision.SkipReason?.Code);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public void CorporateRuleSetOccupiesConfirmedInterleavingSlots()
    {
        ISellArrangementRule[] rules = CorporateSellRuleSet.Create(new StubCorporateRepository(
            new CorporateSellArrangementMatch(null, null))).ToArray();

        Assert.Equal([1020, 1070, 1115], rules.Select(rule => rule.Priority));
        Assert.Equal(
            [SellArrangementLevel.Product, SellArrangementLevel.ProductCategory, SellArrangementLevel.Vendor],
            rules.Cast<CorporateSellArrangementRule>().Select(rule => rule.Level));
    }

    [Fact]
    public async Task ComposedHierarchyPlacesCorporateProductAfterCustomerProductBeforeContractOverride()
    {
        var account = new EmptyAccountRepository();
        var group = new EmptyGroupRepository();
        var corporate = new StubCorporateRepository(new CorporateSellArrangementMatch(
            Selection("CORPORATE-PRODUCT", "corporate"), null));
        ImmutableArray<ISellArrangementRule> rules = SellArrangementRuleSet.Create(account, group, corporate);

        SellArrangementResult result = await new SellArrangementRuleEvaluator(rules)
            .EvaluateAsync(Context(SellCostCascade.IndividualContract), CancellationToken.None);

        Assert.Equal("CORPORATE-PRODUCT", result.Selection?.ArrangementIdentifier);
        SellRuleTraceEntry applied = Assert.Single(result.Trace, entry => entry.Outcome == SellRuleOutcome.Applied);
        Assert.Equal(1020, applied.Priority);
        Assert.Equal(
            [
                (SellArrangementScope.Account, SellArrangementLevel.Product),
                (SellArrangementScope.CustomerNumber, SellArrangementLevel.Product),
            ],
            account.Calls);
    }

    [Fact]
    public void ComposedHierarchyContainsProductCategoryVendorAndDefaultLevelsWithoutDuplicatePriorities()
    {
        ImmutableArray<ISellArrangementRule> rules = SellArrangementRuleSet.Create(
            new EmptyAccountRepository(),
            new EmptyGroupRepository(),
            new StubCorporateRepository(new CorporateSellArrangementMatch(null, null)));

        _ = new SellArrangementRuleEvaluator(rules);
        AccountCustomerSellArrangementRule[] accountRules = rules.OfType<AccountCustomerSellArrangementRule>().ToArray();
        BuyingGroupSellArrangementRule[] groupRules = rules.OfType<BuyingGroupSellArrangementRule>().ToArray();
        Assert.Contains(accountRules, rule => rule.Level == SellArrangementLevel.Default);
        Assert.Contains(groupRules, rule => rule.Level == SellArrangementLevel.Default);
        Assert.Contains(rules, rule => rule is CorporateSellArrangementRule { Level: SellArrangementLevel.Product });
        Assert.Contains(rules, rule => rule is CorporateSellArrangementRule { Level: SellArrangementLevel.ProductCategory });
        Assert.Contains(rules, rule => rule is CorporateSellArrangementRule { Level: SellArrangementLevel.Vendor });
    }

    [Fact]
    public async Task MissingCategoryRepositoryFailureAndCancellationAreHandled()
    {
        var unused = new StubCorporateRepository(new CorporateSellArrangementMatch(
            Selection("CORPORATE", "corporate"), null));
        SellRuleDecision missingCategory = await Rule(unused, SellArrangementLevel.ProductCategory, 1070)
            .EvaluateAsync(Context(SellCostCascade.IndividualContract, hasCategory: false), CancellationToken.None);
        Assert.Equal("NOT_ELIGIBLE", missingCategory.SkipReason?.Code);
        Assert.Equal(0, unused.CallCount);

        var failure = new CorporateSellArrangementRepositoryException("SAG07 failed", "78", "90", true);
        SellRuleDecision failed = await Rule(new StubCorporateRepository(failure), SellArrangementLevel.Product, 1020)
            .EvaluateAsync(Context(SellCostCascade.IndividualContract), CancellationToken.None);
        DependencyPricingError error = Assert.IsType<DependencyPricingError>(failed.Error);
        Assert.Equal("78", error.LegacyErrorCode);
        Assert.True(error.IsTransient);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var canceled = new StubCorporateRepository(new CorporateSellArrangementMatch(null, null));
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await Rule(canceled, SellArrangementLevel.Product, 1020)
                .EvaluateAsync(Context(SellCostCascade.IndividualContract), cancellation.Token));
        Assert.Equal(0, canceled.CallCount);
    }

    private static CorporateSellArrangementRule Rule(
        ICorporateSellArrangementRepository repository,
        SellArrangementLevel level,
        int priority) => new(repository, level, priority);

    private static SellArrangementSelection Selection(string id, string hierarchy, long? groupId = null) => new(
        id,
        "TEST",
        new RuleProvenance("corporate-sell", "SAG07", hierarchy, ActiveDates()),
        groupId);

    private static PricingContext Context(SellCostCascade cascade, bool hasCategory = true)
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
            new CustomerNumber(42),
            [new BuyingGroupMembership(100, 200, 1, ActiveDates(), "CUG11")]);
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

    private sealed class StubCorporateRepository : ICorporateSellArrangementRepository
    {
        private readonly CorporateSellArrangementMatch? match;
        private readonly Exception? exception;

        internal StubCorporateRepository(CorporateSellArrangementMatch match) => this.match = match;
        internal StubCorporateRepository(Exception exception) => this.exception = exception;
        internal int CallCount { get; private set; }

        public ValueTask<CorporateSellArrangementMatch> FindAsync(
            PricingContext context,
            SellArrangementLevel level,
            CancellationToken cancellationToken)
        {
            CallCount++;
            cancellationToken.ThrowIfCancellationRequested();
            return exception is null
                ? ValueTask.FromResult(match!)
                : ValueTask.FromException<CorporateSellArrangementMatch>(exception);
        }
    }

    private sealed class EmptyAccountRepository : IAccountCustomerSellArrangementRepository
    {
        internal List<(SellArrangementScope Scope, SellArrangementLevel Level)> Calls { get; } = [];

        public ValueTask<SellArrangementSelection?> FindAsync(
            PricingContext context,
            SellArrangementScope scope,
            SellArrangementLevel level,
            CancellationToken cancellationToken)
        {
            Calls.Add((scope, level));
            return ValueTask.FromResult<SellArrangementSelection?>(null);
        }
    }

    private sealed class EmptyGroupRepository : IBuyingGroupSellArrangementRepository
    {
        public ValueTask<SellArrangementSelection?> FindSubgroupAsync(
            PricingContext context,
            SellCostCascade cascade,
            SellArrangementLevel level,
            CancellationToken cancellationToken) => ValueTask.FromResult<SellArrangementSelection?>(null);

        public ValueTask<ImmutableArray<ParentSellArrangementCandidate>> FindParentCandidatesAsync(
            PricingContext context,
            SellCostCascade cascade,
            CancellationToken cancellationToken) => ValueTask.FromResult(ImmutableArray<ParentSellArrangementCandidate>.Empty);
    }
}
