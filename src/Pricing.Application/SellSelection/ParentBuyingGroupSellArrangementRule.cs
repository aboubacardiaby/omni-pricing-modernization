namespace Pricing.Application.SellSelection;

using System.Collections.Immutable;
using Pricing.Domain.Models;

/// <summary>Walks parents nearest-first and levels in physical COBOL order within each parent.</summary>
public sealed class ParentBuyingGroupSellArrangementRule : ISellArrangementRule
{
    private readonly IBuyingGroupSellArrangementRepository repository;

    public ParentBuyingGroupSellArrangementRule(
        IBuyingGroupSellArrangementRepository repository,
        SellCostCascade cascade,
        int priority)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        Cascade = cascade;
        Priority = priority;
        Name = $"{cascade}-parent-chain".ToLowerInvariant();
    }

    public string Name { get; }
    public int Priority { get; }
    public SellCostCascade Cascade { get; }

    public async ValueTask<SellRuleDecision> EvaluateAsync(
        PricingContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (SellCostCascadeResolver.Resolve(context) != Cascade)
        {
            return SellRuleDecision.Skipped(
                SellRuleSkipReason.NotEligible($"The active cost source does not use the {Cascade} sell cascade."));
        }

        if (context.Customer.BuyingGroupMemberships.IsEmpty)
        {
            return SellRuleDecision.Skipped(
                SellRuleSkipReason.NotEligible("No buying-group membership is available for parent traversal."));
        }

        try
        {
            ImmutableArray<ParentSellArrangementCandidate> candidates = await repository
                .FindParentCandidatesAsync(context, Cascade, cancellationToken)
                .ConfigureAwait(false);
            ParentSellArrangementCandidate? selected = candidates
                .Where(candidate => IsSupportedLevel(Cascade, candidate.Level))
                .Where(candidate => candidate.Level != SellArrangementLevel.ProductCategory
                    || context.Product.ProductCategory is not null)
                .OrderBy(candidate => candidate.ParentDepth)
                .ThenBy(candidate => GetLevelOrder(Cascade, candidate.Level))
                .FirstOrDefault();
            return selected is null
                ? SellRuleDecision.Skipped(SellRuleSkipReason.NoCandidate(
                    "No parent-group sell arrangement matched before the parent chain was exhausted."))
                : SellRuleDecision.Applied(selected.Selection with { ParentDepth = selected.ParentDepth });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (BuyingGroupSellArrangementRepositoryException exception)
        {
            return SellRuleDecision.Failed(BuyingGroupSellArrangementRule.ToError(exception));
        }
    }

    private static bool IsSupportedLevel(SellCostCascade cascade, SellArrangementLevel level) =>
        level != SellArrangementLevel.VendorContract || cascade == SellCostCascade.GroupContract;

    private static int GetLevelOrder(SellCostCascade cascade, SellArrangementLevel level) =>
        (cascade, level) switch
        {
            (_, SellArrangementLevel.Product) => 1,
            (SellCostCascade.GroupContract, SellArrangementLevel.VendorContract) => 2,
            (_, SellArrangementLevel.ProductCategory) => 3,
            (_, SellArrangementLevel.SpecialServiceCode) => 4,
            (_, SellArrangementLevel.Vendor) => 5,
            (_, SellArrangementLevel.Default) => 6,
            _ => int.MaxValue,
        };
}

internal static class SellCostCascadeResolver
{
    internal static SellCostCascade? Resolve(PricingContext context) =>
        context.CostSelection switch
        {
            AcquisitionCostSelection => SellCostCascade.AcquisitionCost,
            HealthcareCostSelection => SellCostCascade.AcquisitionCost,
            ContractSelection { BuyingGroupId: not null } => SellCostCascade.GroupContract,
            ContractSelection => SellCostCascade.IndividualContract,
            _ => null,
        };
}
