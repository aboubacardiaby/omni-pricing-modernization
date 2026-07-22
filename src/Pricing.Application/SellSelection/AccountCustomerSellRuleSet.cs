namespace Pricing.Application.SellSelection;

using System.Collections.Immutable;

/// <summary>Builds T033 rules in reserved slots matching A6U01 0180, 7075, and 0185.</summary>
public static class AccountCustomerSellRuleSet
{
    public static ImmutableArray<ISellArrangementRule> Create(
        IAccountCustomerSellArrangementRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        return
        [
            // 0180 individual-cost-contract cascade.
            Rule(repository, SellCostCascade.IndividualContract, SellArrangementScope.Account, SellArrangementLevel.Product, 1000),
            Rule(repository, SellCostCascade.IndividualContract, SellArrangementScope.CustomerNumber, SellArrangementLevel.Product, 1010),
            Rule(repository, SellCostCascade.IndividualContract, SellArrangementScope.Account, SellArrangementLevel.VendorContract, 1030),
            Rule(repository, SellCostCascade.IndividualContract, SellArrangementScope.CustomerNumber, SellArrangementLevel.VendorContract, 1040),
            Rule(repository, SellCostCascade.IndividualContract, SellArrangementScope.Account, SellArrangementLevel.ProductCategory, 1050),
            Rule(repository, SellCostCascade.IndividualContract, SellArrangementScope.CustomerNumber, SellArrangementLevel.ProductCategory, 1060),
            Rule(repository, SellCostCascade.IndividualContract, SellArrangementScope.Account, SellArrangementLevel.SpecialServiceCode, 1080),
            Rule(repository, SellCostCascade.IndividualContract, SellArrangementScope.CustomerNumber, SellArrangementLevel.SpecialServiceCode, 1090),
            Rule(repository, SellCostCascade.IndividualContract, SellArrangementScope.Account, SellArrangementLevel.Vendor, 1100),
            Rule(repository, SellCostCascade.IndividualContract, SellArrangementScope.CustomerNumber, SellArrangementLevel.Vendor, 1110),
            Rule(repository, SellCostCascade.IndividualContract, SellArrangementScope.Account, SellArrangementLevel.Default, 1160),
            Rule(repository, SellCostCascade.IndividualContract, SellArrangementScope.CustomerNumber, SellArrangementLevel.Default, 1170),

            // 7075 group-cost-contract cascade.
            Rule(repository, SellCostCascade.GroupContract, SellArrangementScope.Account, SellArrangementLevel.Product, 2000),
            Rule(repository, SellCostCascade.GroupContract, SellArrangementScope.CustomerNumber, SellArrangementLevel.Product, 2010),
            Rule(repository, SellCostCascade.GroupContract, SellArrangementScope.Account, SellArrangementLevel.VendorContract, 2030),
            Rule(repository, SellCostCascade.GroupContract, SellArrangementScope.CustomerNumber, SellArrangementLevel.VendorContract, 2040),
            Rule(repository, SellCostCascade.GroupContract, SellArrangementScope.Account, SellArrangementLevel.ProductCategory, 2060),
            Rule(repository, SellCostCascade.GroupContract, SellArrangementScope.CustomerNumber, SellArrangementLevel.ProductCategory, 2070),
            Rule(repository, SellCostCascade.GroupContract, SellArrangementScope.Account, SellArrangementLevel.Vendor, 2100),
            Rule(repository, SellCostCascade.GroupContract, SellArrangementScope.CustomerNumber, SellArrangementLevel.Vendor, 2110),
            Rule(repository, SellCostCascade.GroupContract, SellArrangementScope.Account, SellArrangementLevel.Default, 2130),
            Rule(repository, SellCostCascade.GroupContract, SellArrangementScope.CustomerNumber, SellArrangementLevel.Default, 2140),

            // 0185 acquisition-cost cascade; no vendor-contract or account/customer SSC levels.
            Rule(repository, SellCostCascade.AcquisitionCost, SellArrangementScope.Account, SellArrangementLevel.Product, 3000),
            Rule(repository, SellCostCascade.AcquisitionCost, SellArrangementScope.CustomerNumber, SellArrangementLevel.Product, 3010),
            Rule(repository, SellCostCascade.AcquisitionCost, SellArrangementScope.Account, SellArrangementLevel.ProductCategory, 3020),
            Rule(repository, SellCostCascade.AcquisitionCost, SellArrangementScope.CustomerNumber, SellArrangementLevel.ProductCategory, 3030),
            Rule(repository, SellCostCascade.AcquisitionCost, SellArrangementScope.Account, SellArrangementLevel.Vendor, 3040),
            Rule(repository, SellCostCascade.AcquisitionCost, SellArrangementScope.CustomerNumber, SellArrangementLevel.Vendor, 3050),
            Rule(repository, SellCostCascade.AcquisitionCost, SellArrangementScope.Account, SellArrangementLevel.Default, 3100),
            Rule(repository, SellCostCascade.AcquisitionCost, SellArrangementScope.CustomerNumber, SellArrangementLevel.Default, 3110),
        ];
    }

    private static AccountCustomerSellArrangementRule Rule(
        IAccountCustomerSellArrangementRepository repository,
        SellCostCascade cascade,
        SellArrangementScope scope,
        SellArrangementLevel level,
        int priority) => new AccountCustomerSellArrangementRule(repository, cascade, scope, level, priority);
}
