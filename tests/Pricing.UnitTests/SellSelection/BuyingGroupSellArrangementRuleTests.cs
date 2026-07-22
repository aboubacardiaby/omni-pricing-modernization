namespace Pricing.UnitTests.SellSelection;

using System.Collections.Immutable;
using Pricing.Application.SellSelection;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class BuyingGroupSellArrangementRuleTests
{
    [Theory]
    [InlineData(SellCostCascade.IndividualContract)]
    [InlineData(SellCostCascade.GroupContract)]
    [InlineData(SellCostCascade.AcquisitionCost)]
    public async Task SubgroupProductIsFirstBuyingGroupLevel(SellCostCascade cascade)
    {
        var repository = new StubRepository();
        repository.AddSubgroup(SellArrangementLevel.Product, Selection("SUBGROUP-PRODUCT", 100));
        repository.AddSubgroup(SellArrangementLevel.Vendor, Selection("SUBGROUP-VENDOR", 100));

        SellArrangementResult result = await new SellArrangementRuleEvaluator(
                BuyingGroupSellRuleSet.Create(repository))
            .EvaluateAsync(Context(cascade), CancellationToken.None);

        Assert.Equal("SUBGROUP-PRODUCT", result.Selection?.ArrangementIdentifier);
        Assert.Equal([SellArrangementLevel.Product], repository.SubgroupCalls);
    }

    [Fact]
    public async Task CombinedT033AndT034RulesPreserveInterleaving()
    {
        var accountRepository = new EmptyAccountRepository();
        var groupRepository = new StubRepository();
        groupRepository.AddSubgroup(SellArrangementLevel.Product, Selection("GROUP-PRODUCT", 100));
        ISellArrangementRule[] rules =
        [
            .. AccountCustomerSellRuleSet.Create(accountRepository),
            .. BuyingGroupSellRuleSet.Create(groupRepository),
        ];

        SellArrangementResult result = await new SellArrangementRuleEvaluator(rules)
            .EvaluateAsync(Context(SellCostCascade.GroupContract), CancellationToken.None);

        Assert.Equal("GROUP-PRODUCT", result.Selection?.ArrangementIdentifier);
        SellRuleTraceEntry applied = Assert.Single(result.Trace, entry => entry.Outcome == SellRuleOutcome.Applied);
        Assert.Equal(2020, applied.Priority);
        Assert.Equal(
            [
                (SellArrangementScope.Account, SellArrangementLevel.Product),
                (SellArrangementScope.CustomerNumber, SellArrangementLevel.Product),
            ],
            accountRepository.Calls);
    }

    [Fact]
    public void ContractSpecificSubgroupOverrideExistsOnlyForGroupCostCascade()
    {
        BuyingGroupSellArrangementRule[] rules = BuyingGroupSellRuleSet.Create(new StubRepository())
            .OfType<BuyingGroupSellArrangementRule>()
            .Where(rule => rule.Level == SellArrangementLevel.VendorContract)
            .ToArray();

        BuyingGroupSellArrangementRule rule = Assert.Single(rules);
        Assert.Equal(SellCostCascade.GroupContract, rule.Cascade);
        Assert.Equal(2050, rule.Priority);
    }

    [Fact]
    public async Task NearestParentWinsBeforeMoreDistantHigherLevelMatch()
    {
        var repository = new StubRepository(parentCandidates:
        [
            Parent(2, SellArrangementLevel.Product, "GRANDPARENT-PRODUCT", 300),
            Parent(1, SellArrangementLevel.Vendor, "PARENT-VENDOR", 200),
        ]);
        var rule = new ParentBuyingGroupSellArrangementRule(
            repository, SellCostCascade.IndividualContract, 1190);

        SellRuleDecision decision = await rule.EvaluateAsync(
            Context(SellCostCascade.IndividualContract), CancellationToken.None);

        SellArrangementSelection selection = Assert.IsType<SellArrangementSelection>(decision.Selection);
        Assert.Equal("PARENT-VENDOR", selection.ArrangementIdentifier);
        Assert.Equal(1, selection.ParentDepth);
        Assert.Equal(200, selection.BuyingGroupId);
    }

    [Fact]
    public async Task GroupContractParentProductPrecedesContractOverrideByPhysicalOrder()
    {
        var repository = new StubRepository(parentCandidates:
        [
            Parent(1, SellArrangementLevel.VendorContract, "PARENT-CONTRACT", 200),
            Parent(1, SellArrangementLevel.Product, "PARENT-PRODUCT", 200),
        ]);
        var rule = new ParentBuyingGroupSellArrangementRule(
            repository, SellCostCascade.GroupContract, 2160);

        SellRuleDecision decision = await rule.EvaluateAsync(
            Context(SellCostCascade.GroupContract), CancellationToken.None);

        Assert.Equal("PARENT-PRODUCT", decision.Selection?.ArrangementIdentifier);
    }

    [Theory]
    [InlineData(SellCostCascade.IndividualContract)]
    [InlineData(SellCostCascade.AcquisitionCost)]
    public async Task NonGroupCascadesRejectParentContractOverride(SellCostCascade cascade)
    {
        var repository = new StubRepository(parentCandidates:
        [
            Parent(1, SellArrangementLevel.VendorContract, "INVALID-CONTRACT", 200),
            Parent(1, SellArrangementLevel.Vendor, "PARENT-VENDOR", 200),
        ]);

        SellRuleDecision decision = await new ParentBuyingGroupSellArrangementRule(repository, cascade, 9999)
            .EvaluateAsync(Context(cascade), CancellationToken.None);

        Assert.Equal("PARENT-VENDOR", decision.Selection?.ArrangementIdentifier);
    }

    [Fact]
    public async Task MissingMembershipSkipsSubgroupAndParentRepositories()
    {
        var repository = new StubRepository();
        PricingContext context = Context(SellCostCascade.IndividualContract, hasMembership: false);

        SellRuleDecision subgroup = await new BuyingGroupSellArrangementRule(
                repository, SellCostCascade.IndividualContract, SellArrangementLevel.Product, 1120)
            .EvaluateAsync(context, CancellationToken.None);
        SellRuleDecision parent = await new ParentBuyingGroupSellArrangementRule(
                repository, SellCostCascade.IndividualContract, 1190)
            .EvaluateAsync(context, CancellationToken.None);

        Assert.Equal("NOT_ELIGIBLE", subgroup.SkipReason?.Code);
        Assert.Equal("NOT_ELIGIBLE", parent.SkipReason?.Code);
        Assert.Empty(repository.SubgroupCalls);
        Assert.Equal(0, repository.ParentCallCount);
    }

    [Fact]
    public async Task RepositoryFailureAndCancellationPropagateCorrectly()
    {
        var failure = new BuyingGroupSellArrangementRepositoryException(
            "Parent lookup failed", "88", "90", true);
        var failedRepository = new StubRepository(failure);
        SellRuleDecision failed = await new ParentBuyingGroupSellArrangementRule(
                failedRepository, SellCostCascade.IndividualContract, 1190)
            .EvaluateAsync(Context(SellCostCascade.IndividualContract), CancellationToken.None);

        DependencyPricingError error = Assert.IsType<DependencyPricingError>(failed.Error);
        Assert.Equal("88", error.LegacyErrorCode);
        Assert.True(error.IsTransient);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var canceledRepository = new StubRepository();
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await new BuyingGroupSellArrangementRule(
                    canceledRepository,
                    SellCostCascade.IndividualContract,
                    SellArrangementLevel.Product,
                    1120)
                .EvaluateAsync(Context(SellCostCascade.IndividualContract), cancellation.Token));
        Assert.Empty(canceledRepository.SubgroupCalls);
    }

    private static ParentSellArrangementCandidate Parent(
        int depth,
        SellArrangementLevel level,
        string id,
        long groupId) => new(depth, level, Selection(id, groupId));

    private static SellArrangementSelection Selection(string id, long groupId) => new(
        id,
        "GROUP",
        new RuleProvenance("buying-group-sell", "SAG07", "group", ActiveDates()),
        groupId,
        "001",
        1,
        new DateOnly(2026, 1, 1));

    private static PricingContext Context(SellCostCascade cascade, bool hasMembership = true)
    {
        var request = new PricingRequest(
            new DivisionId("01"), new AccountNumber("123456"), new VendorId("V001"),
            new ProductId("P0000001"), new Quantity(1m), new UnitOfMeasure("EA"),
            null, null, new DateOnly(2026, 7, 21), PricingRequestType.Full);
        var product = new ProductInformation(
            request.Vendor, request.Product, ProductType.Regular, request.UnitOfMeasure,
            null, null, 1234, "A");
        var memberships = hasMembership
            ? ImmutableArray.Create(new BuyingGroupMembership(100, 200, 1, ActiveDates(), "CUG11"))
            : ImmutableArray<BuyingGroupMembership>.Empty;
        var customer = new CustomerInformation(request.Account, new CustomerNumber(42), memberships);
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

    private sealed class StubRepository : IBuyingGroupSellArrangementRepository
    {
        private readonly Dictionary<SellArrangementLevel, SellArrangementSelection> subgroup = [];
        private readonly ImmutableArray<ParentSellArrangementCandidate> parentCandidates;
        private readonly Exception? exception;

        internal StubRepository(ImmutableArray<ParentSellArrangementCandidate> parentCandidates = default) =>
            this.parentCandidates = parentCandidates.IsDefault ? [] : parentCandidates;

        internal StubRepository(Exception exception) => this.exception = exception;

        internal List<SellArrangementLevel> SubgroupCalls { get; } = [];
        internal int ParentCallCount { get; private set; }

        internal void AddSubgroup(SellArrangementLevel level, SellArrangementSelection selection) =>
            subgroup.Add(level, selection);

        public ValueTask<SellArrangementSelection?> FindSubgroupAsync(
            PricingContext context,
            SellCostCascade cascade,
            SellArrangementLevel level,
            CancellationToken cancellationToken)
        {
            SubgroupCalls.Add(level);
            cancellationToken.ThrowIfCancellationRequested();
            if (exception is not null)
            {
                return ValueTask.FromException<SellArrangementSelection?>(exception);
            }

            subgroup.TryGetValue(level, out SellArrangementSelection? selection);
            return ValueTask.FromResult(selection);
        }

        public ValueTask<ImmutableArray<ParentSellArrangementCandidate>> FindParentCandidatesAsync(
            PricingContext context,
            SellCostCascade cascade,
            CancellationToken cancellationToken)
        {
            ParentCallCount++;
            cancellationToken.ThrowIfCancellationRequested();
            return exception is null
                ? ValueTask.FromResult(parentCandidates)
                : ValueTask.FromException<ImmutableArray<ParentSellArrangementCandidate>>(exception);
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
}
