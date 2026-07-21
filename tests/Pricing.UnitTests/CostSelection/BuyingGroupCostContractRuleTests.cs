namespace Pricing.UnitTests.CostSelection;

using System.Collections.Immutable;
using Pricing.Application.CostSelection;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class BuyingGroupCostContractRuleTests
{
    [Fact]
    public async Task AccountProductCategoryOverrideWinsBeforeEveryOtherPath()
    {
        BuyingGroupScopeCandidates account = Scope(
            BuyingGroupScope.Account,
            productOverrides: [Candidate("ACCOUNT-PRODUCT", 1, BuyingGroupScope.Account,
                BuyingGroupSelectionPath.ProductCategoryOverride)],
            vendorOverrides: [Candidate("ACCOUNT-VENDOR", 1, BuyingGroupScope.Account,
                BuyingGroupSelectionPath.VendorOverride)],
            branches: [Branch(1, Candidate("ACCOUNT-PRIMARY", 1, BuyingGroupScope.Account))]);
        BuyingGroupScopeCandidates customer = Scope(
            BuyingGroupScope.Customer,
            vendorOverrides: [Candidate("CUSTOMER-VENDOR", 1, BuyingGroupScope.Customer,
                BuyingGroupSelectionPath.VendorOverride)]);

        CostRuleDecision decision = await Rule(new StubRepository(new BuyingGroupCostSearchData(account, customer)))
            .EvaluateAsync(Context(), CancellationToken.None);

        ContractSelection selection = Assert.IsType<ContractSelection>(decision.Selection);
        Assert.Equal("ACCOUNT-PRODUCT", selection.Contract.Value);
        Assert.True(selection.IsBuyingGroupOverride);
        Assert.Equal("PRIMARY_GROUP", selection.ContractType);
    }

    [Fact]
    public async Task ProductCategoryOverrideIsSkippedWhenProductHasNoCategory()
    {
        BuyingGroupScopeCandidates account = Scope(
            BuyingGroupScope.Account,
            productOverrides: [Candidate("PRODUCT", 1, BuyingGroupScope.Account,
                BuyingGroupSelectionPath.ProductCategoryOverride)],
            vendorOverrides: [Candidate("VENDOR", 2, BuyingGroupScope.Account,
                BuyingGroupSelectionPath.VendorOverride)]);

        CostRuleDecision decision = await Rule(new StubRepository(
                new BuyingGroupCostSearchData(account, EmptyScope(BuyingGroupScope.Customer))))
            .EvaluateAsync(Context(hasProductCategory: false), CancellationToken.None);

        ContractSelection selection = Assert.IsType<ContractSelection>(decision.Selection);
        Assert.Equal("VENDOR", selection.Contract.Value);
        Assert.Equal("OTHER_GROUP", selection.ContractType);
    }

    [Fact]
    public async Task ParentAtCurrentPriorityWinsBeforeChildAtNextPriority()
    {
        BuyingGroupCostCandidate excludedChild = Candidate("P1-CHILD", 1, BuyingGroupScope.Account) with
        {
            IsExcluded = true,
        };
        BuyingGroupCostCandidate parent = Candidate(
            "P1-PARENT", 1, BuyingGroupScope.Account,
            hierarchy: BuyingGroupHierarchyLevel.Parent,
            buyingGroupId: 200);
        BuyingGroupCostCandidate nextChild = Candidate("P2-CHILD", 2, BuyingGroupScope.Account);
        BuyingGroupScopeCandidates account = Scope(
            BuyingGroupScope.Account,
            branches:
            [
                Branch(2, nextChild),
                Branch(1, excludedChild, [parent]),
            ]);

        CostRuleDecision decision = await Rule(new StubRepository(
                new BuyingGroupCostSearchData(account, EmptyScope(BuyingGroupScope.Customer))))
            .EvaluateAsync(Context(), CancellationToken.None);

        ContractSelection selection = Assert.IsType<ContractSelection>(decision.Selection);
        Assert.Equal("P1-PARENT", selection.Contract.Value);
        Assert.Equal(200, selection.BuyingGroupId);
        Assert.Equal(1, selection.BuyingGroupPriority);
        Assert.Equal("PARENT", selection.BuyingGroupHierarchyLevel);
        Assert.Equal("PRIMARY_GROUP", selection.ContractType);
    }

    [Fact]
    public async Task OtherPriorityChildIsClassifiedAndPreservesGroupMetadata()
    {
        BuyingGroupCostCandidate other = Candidate("OTHER", 3, BuyingGroupScope.Account, buyingGroupId: 303);
        BuyingGroupScopeCandidates account = Scope(BuyingGroupScope.Account, branches: [Branch(3, other)]);

        CostRuleDecision decision = await Rule(new StubRepository(
                new BuyingGroupCostSearchData(account, EmptyScope(BuyingGroupScope.Customer))))
            .EvaluateAsync(Context(), CancellationToken.None);

        ContractSelection selection = Assert.IsType<ContractSelection>(decision.Selection);
        Assert.Equal("OTHER_GROUP", selection.ContractType);
        Assert.Equal(303, selection.BuyingGroupId);
        Assert.Equal(3, selection.BuyingGroupPriority);
        Assert.Equal("CHILD", selection.BuyingGroupHierarchyLevel);
        Assert.False(selection.IsBuyingGroupOverride);
    }

    [Fact]
    public async Task ParentTraversalContinuesThroughFullAncestorChain()
    {
        BuyingGroupCostCandidate child = Candidate("CHILD", 1, BuyingGroupScope.Account) with
        {
            IsMembershipEligible = false,
        };
        BuyingGroupCostCandidate immediateParent = Candidate(
            "PARENT", 1, BuyingGroupScope.Account,
            hierarchy: BuyingGroupHierarchyLevel.Parent,
            buyingGroupId: 200) with { IsExcluded = true };
        BuyingGroupCostCandidate grandparent = Candidate(
            "GRANDPARENT", 1, BuyingGroupScope.Account,
            hierarchy: BuyingGroupHierarchyLevel.Parent,
            buyingGroupId: 300);
        BuyingGroupScopeCandidates account = Scope(
            BuyingGroupScope.Account,
            branches: [Branch(1, child, [immediateParent, grandparent])]);

        CostRuleDecision decision = await Rule(new StubRepository(
                new BuyingGroupCostSearchData(account, EmptyScope(BuyingGroupScope.Customer))))
            .EvaluateAsync(Context(), CancellationToken.None);

        ContractSelection selection = Assert.IsType<ContractSelection>(decision.Selection);
        Assert.Equal("GRANDPARENT", selection.Contract.Value);
        Assert.Equal(300, selection.BuyingGroupId);
        Assert.Equal("PARENT", selection.BuyingGroupHierarchyLevel);
    }

    [Fact]
    public async Task CustomerScopeRunsOnlyAfterAccountScopeHasNoUsableContract()
    {
        BuyingGroupCostCandidate expiredAccount = Candidate("ACCOUNT", 1, BuyingGroupScope.Account) with
        {
            EligibilityDates = [new PricingDateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31))],
        };
        BuyingGroupScopeCandidates account = Scope(
            BuyingGroupScope.Account,
            branches: [Branch(1, expiredAccount)]);
        BuyingGroupScopeCandidates customer = Scope(
            BuyingGroupScope.Customer,
            branches: [Branch(1, Candidate("CUSTOMER", 1, BuyingGroupScope.Customer))]);

        CostRuleDecision decision = await Rule(new StubRepository(new BuyingGroupCostSearchData(account, customer)))
            .EvaluateAsync(Context(), CancellationToken.None);

        ContractSelection selection = Assert.IsType<ContractSelection>(decision.Selection);
        Assert.Equal("CUSTOMER", selection.Contract.Value);
        Assert.Equal("customer", selection.Provenance.HierarchyLevel);
    }

    [Fact]
    public async Task IneligibleMembershipAndExclusionFallThroughToNoCandidate()
    {
        BuyingGroupCostCandidate ineligible = Candidate("INELIGIBLE", 1, BuyingGroupScope.Account) with
        {
            IsMembershipEligible = false,
        };
        BuyingGroupCostCandidate excludedParent = Candidate(
            "EXCLUDED", 1, BuyingGroupScope.Account, hierarchy: BuyingGroupHierarchyLevel.Parent) with
        {
            IsExcluded = true,
        };
        BuyingGroupScopeCandidates account = Scope(
            BuyingGroupScope.Account,
            branches: [Branch(1, ineligible, [excludedParent])]);

        CostRuleDecision decision = await Rule(new StubRepository(
                new BuyingGroupCostSearchData(account, EmptyScope(BuyingGroupScope.Customer))))
            .EvaluateAsync(Context(), CancellationToken.None);

        Assert.Equal(CostRuleOutcome.Skipped, decision.Outcome);
        Assert.Equal("NO_CANDIDATE", decision.SkipReason?.Code);
    }

    [Fact]
    public async Task RepositoryFailureBecomesTypedRuleFailure()
    {
        var failure = new BuyingGroupCostContractRepositoryException("group cursor failed", "13", "90", true);

        CostRuleDecision decision = await Rule(new StubRepository(failure))
            .EvaluateAsync(Context(), CancellationToken.None);

        DependencyPricingError error = Assert.IsType<DependencyPricingError>(decision.Error);
        Assert.Equal("13", error.LegacyErrorCode);
        Assert.Equal("90", error.LegacySeverityCode);
        Assert.True(error.IsTransient);
    }

    [Fact]
    public async Task CancellationIsPropagatedBeforeRepositoryCall()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var repository = new StubRepository(new BuyingGroupCostSearchData(
            EmptyScope(BuyingGroupScope.Account),
            EmptyScope(BuyingGroupScope.Customer)));

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await Rule(repository).EvaluateAsync(Context(), cancellation.Token));
        Assert.Equal(0, repository.CallCount);
    }

    private static BuyingGroupCostContractRule Rule(IBuyingGroupCostContractRepository repository) => new(repository);

    private static BuyingGroupScopeCandidates Scope(
        BuyingGroupScope scope,
        ImmutableArray<BuyingGroupCostCandidate> productOverrides = default,
        ImmutableArray<BuyingGroupCostCandidate> vendorOverrides = default,
        ImmutableArray<BuyingGroupPriorityBranch> branches = default) =>
        new(
            scope,
            productOverrides.IsDefault ? [] : productOverrides,
            vendorOverrides.IsDefault ? [] : vendorOverrides,
            branches.IsDefault ? [] : branches);

    private static BuyingGroupScopeCandidates EmptyScope(BuyingGroupScope scope) => Scope(scope);

    private static BuyingGroupPriorityBranch Branch(
        int priority,
        BuyingGroupCostCandidate? child,
        ImmutableArray<BuyingGroupCostCandidate> parents = default) =>
        new(priority, child, parents.IsDefault ? [] : parents);

    private static BuyingGroupCostCandidate Candidate(
        string id,
        int priority,
        BuyingGroupScope scope,
        BuyingGroupSelectionPath path = BuyingGroupSelectionPath.Priority,
        BuyingGroupHierarchyLevel hierarchy = BuyingGroupHierarchyLevel.Child,
        long buyingGroupId = 100) =>
        new(
            new ContractId(id),
            new Money(priority),
            new UnitOfMeasure("EA"),
            buyingGroupId,
            priority,
            scope,
            path,
            hierarchy,
            true,
            false,
            [new PricingDateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31))],
            new RuleProvenance(
                "buying-group-cost-contract",
                $"CCG05/{id}",
                scope.ToString().ToLowerInvariant(),
                new PricingDateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31))));

    private static PricingContext Context(bool hasProductCategory = true)
    {
        var request = new PricingRequest(
            new DivisionId("01"), new AccountNumber("123456"), new VendorId("V001"),
            new ProductId("P0000001"), new Quantity(1m), new UnitOfMeasure("EA"),
            null, null, new DateOnly(2026, 7, 21), PricingRequestType.Full);
        var product = new ProductInformation(
            request.Vendor, request.Product, ProductType.Regular, request.UnitOfMeasure,
            null, null, hasProductCategory ? 1234 : null, "A");
        var customer = new CustomerInformation(request.Account, new CustomerNumber(42), []);
        return new PricingContext(request, product, customer, null, null);
    }

    private sealed class StubRepository : IBuyingGroupCostContractRepository
    {
        private readonly BuyingGroupCostSearchData? data;
        private readonly Exception? exception;

        internal StubRepository(BuyingGroupCostSearchData data) => this.data = data;
        internal StubRepository(Exception exception) => this.exception = exception;
        internal int CallCount { get; private set; }

        public ValueTask<BuyingGroupCostSearchData> FindCandidatesAsync(
            PricingContext context,
            CancellationToken cancellationToken)
        {
            CallCount++;
            cancellationToken.ThrowIfCancellationRequested();
            return exception is null
                ? ValueTask.FromResult(data!)
                : ValueTask.FromException<BuyingGroupCostSearchData>(exception);
        }
    }
}
