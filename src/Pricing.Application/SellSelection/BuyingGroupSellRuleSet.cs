namespace Pricing.Application.SellSelection;

using System.Collections.Immutable;

/// <summary>Builds subgroup and parent stages in the T033-reserved cascade slots.</summary>
public static class BuyingGroupSellRuleSet
{
    public static ImmutableArray<ISellArrangementRule> Create(
        IBuyingGroupSellArrangementRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        return
        [
            Subgroup(repository, SellCostCascade.IndividualContract, SellArrangementLevel.Product, 1120),
            Subgroup(repository, SellCostCascade.IndividualContract, SellArrangementLevel.ProductCategory, 1130),
            Subgroup(repository, SellCostCascade.IndividualContract, SellArrangementLevel.SpecialServiceCode, 1140),
            Subgroup(repository, SellCostCascade.IndividualContract, SellArrangementLevel.Vendor, 1150),
            Subgroup(repository, SellCostCascade.IndividualContract, SellArrangementLevel.Default, 1180),
            Parent(repository, SellCostCascade.IndividualContract, 1190),

            Subgroup(repository, SellCostCascade.GroupContract, SellArrangementLevel.Product, 2020),
            Subgroup(repository, SellCostCascade.GroupContract, SellArrangementLevel.VendorContract, 2050),
            Subgroup(repository, SellCostCascade.GroupContract, SellArrangementLevel.ProductCategory, 2080),
            Subgroup(repository, SellCostCascade.GroupContract, SellArrangementLevel.SpecialServiceCode, 2090),
            Subgroup(repository, SellCostCascade.GroupContract, SellArrangementLevel.Vendor, 2120),
            Subgroup(repository, SellCostCascade.GroupContract, SellArrangementLevel.Default, 2150),
            Parent(repository, SellCostCascade.GroupContract, 2160),

            Subgroup(repository, SellCostCascade.AcquisitionCost, SellArrangementLevel.Product, 3060),
            Subgroup(repository, SellCostCascade.AcquisitionCost, SellArrangementLevel.ProductCategory, 3070),
            Subgroup(repository, SellCostCascade.AcquisitionCost, SellArrangementLevel.SpecialServiceCode, 3080),
            Subgroup(repository, SellCostCascade.AcquisitionCost, SellArrangementLevel.Vendor, 3090),
            Subgroup(repository, SellCostCascade.AcquisitionCost, SellArrangementLevel.Default, 3120),
            Parent(repository, SellCostCascade.AcquisitionCost, 3130),
        ];
    }

    private static BuyingGroupSellArrangementRule Subgroup(
        IBuyingGroupSellArrangementRepository repository,
        SellCostCascade cascade,
        SellArrangementLevel level,
        int priority) => new(repository, cascade, level, priority);

    private static ParentBuyingGroupSellArrangementRule Parent(
        IBuyingGroupSellArrangementRepository repository,
        SellCostCascade cascade,
        int priority) => new(repository, cascade, priority);
}
