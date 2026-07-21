namespace Pricing.Application.CostSelection;

using Pricing.Domain.Models;

/// <summary>Runs the account/customer buying-group contract cascade without flattening ancestry.</summary>
/// <remarks>
/// COBOL: A6U01 0240-PRO-GRP-CNT-010, 0275/0280 scope dispatch, 0310/0320/0330
/// overrides, and 0315/7315/7320/7321 priority and ancestor traversal.
/// </remarks>
public sealed class BuyingGroupCostContractRule : ICostRule
{
    private readonly IBuyingGroupCostContractRepository repository;

    public BuyingGroupCostContractRule(IBuyingGroupCostContractRepository repository) =>
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public string Name => "buying-group-cost-contract";
    public int Priority => CostRulePriorities.BuyingGroupContract;

    public async ValueTask<CostRuleDecision> EvaluateAsync(
        PricingContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (context.Request.IsSpecialContract)
        {
            return CostRuleDecision.Skipped(
                CostRuleSkipReason.NotEligible("Special requests cannot use buying-group cost contracts."));
        }

        try
        {
            BuyingGroupCostSearchData data = await repository
                .FindCandidatesAsync(context, cancellationToken).ConfigureAwait(false);
            ValidateScope(data.Account, BuyingGroupScope.Account);
            ValidateScope(data.Customer, BuyingGroupScope.Customer);

            BuyingGroupCostCandidate? selected = FindInScope(data.Account, context, cancellationToken)
                ?? FindInScope(data.Customer, context, cancellationToken);
            if (selected is null)
            {
                return CostRuleDecision.Skipped(
                    CostRuleSkipReason.NoCandidate("No usable account or customer buying-group cost contract was found."));
            }

            string contractType = selected.Priority == 1 ? "PRIMARY_GROUP" : "OTHER_GROUP";
            return CostRuleDecision.Applied(new ContractSelection(
                selected.Contract,
                contractType,
                selected.NormalizedUnitCost,
                selected.NormalizedUnitOfMeasure,
                selected.Provenance,
                selected.BuyingGroupId,
                selected.Priority,
                selected.HierarchyLevel.ToString().ToUpperInvariant(),
                selected.SelectionPath != BuyingGroupSelectionPath.Priority));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (BuyingGroupCostContractRepositoryException exception)
        {
            return CostRuleDecision.Failed(new DependencyPricingError(
                "BUYING_GROUP_COST_CONTRACT_LOOKUP_FAILED",
                exception.Message,
                exception.LegacyErrorCode,
                exception.IsTransient,
                exception.LegacySeverityCode));
        }
    }

    private static BuyingGroupCostCandidate? FindInScope(
        BuyingGroupScopeCandidates scope,
        PricingContext context,
        CancellationToken cancellationToken)
    {
        if (context.Product.ProductCategory is not null)
        {
            BuyingGroupCostCandidate? productOverride = FirstUsable(
                scope.ProductCategoryOverrides,
                scope.Scope,
                BuyingGroupSelectionPath.ProductCategoryOverride,
                context.Request.PricingDate,
                cancellationToken);
            if (productOverride is not null)
            {
                return productOverride;
            }
        }

        BuyingGroupCostCandidate? vendorOverride = FirstUsable(
            scope.VendorOverrides,
            scope.Scope,
            BuyingGroupSelectionPath.VendorOverride,
            context.Request.PricingDate,
            cancellationToken);
        if (vendorOverride is not null)
        {
            return vendorOverride;
        }

        foreach (BuyingGroupPriorityBranch branch in scope.PriorityBranches.OrderBy(item => item.Priority))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (branch.Child is { } child)
            {
                ValidateBranchCandidate(branch, child, BuyingGroupHierarchyLevel.Child);
                if (IsUsable(child, scope.Scope, BuyingGroupSelectionPath.Priority, context.Request.PricingDate))
                {
                    return child;
                }
            }

            foreach (BuyingGroupCostCandidate parent in branch.Parents)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateBranchCandidate(branch, parent, BuyingGroupHierarchyLevel.Parent);
                if (IsUsable(parent, scope.Scope, BuyingGroupSelectionPath.Priority, context.Request.PricingDate))
                {
                    return parent;
                }
            }
        }

        return null;
    }

    private static BuyingGroupCostCandidate? FirstUsable(
        IEnumerable<BuyingGroupCostCandidate> candidates,
        BuyingGroupScope scope,
        BuyingGroupSelectionPath path,
        DateOnly pricingDate,
        CancellationToken cancellationToken)
    {
        foreach (BuyingGroupCostCandidate candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsUsable(candidate, scope, path, pricingDate))
            {
                if (candidate.HierarchyLevel != BuyingGroupHierarchyLevel.Child)
                {
                    throw new InvalidOperationException("Buying-group override candidates must be child-level records.");
                }

                return candidate;
            }
        }

        return null;
    }

    private static bool IsUsable(
        BuyingGroupCostCandidate candidate,
        BuyingGroupScope expectedScope,
        BuyingGroupSelectionPath expectedPath,
        DateOnly pricingDate)
    {
        if (candidate.Scope != expectedScope || candidate.SelectionPath != expectedPath)
        {
            throw new InvalidOperationException("Buying-group candidate scope or selection path does not match its search branch.");
        }

        return candidate.IsMembershipEligible
            && !candidate.IsExcluded
            && !candidate.EligibilityDates.IsDefault
            && candidate.EligibilityDates.All(range => range.Contains(pricingDate));
    }

    private static void ValidateBranchCandidate(
        BuyingGroupPriorityBranch branch,
        BuyingGroupCostCandidate candidate,
        BuyingGroupHierarchyLevel hierarchyLevel)
    {
        if (candidate.Priority != branch.Priority || candidate.HierarchyLevel != hierarchyLevel)
        {
            throw new InvalidOperationException("Buying-group candidate priority or hierarchy does not match its branch.");
        }
    }

    private static void ValidateScope(BuyingGroupScopeCandidates scope, BuyingGroupScope expected)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if (scope.Scope != expected)
        {
            throw new InvalidOperationException($"Expected {expected} buying-group scope but received {scope.Scope}.");
        }
    }
}
