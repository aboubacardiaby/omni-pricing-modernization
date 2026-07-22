namespace Pricing.Application.SellSelection;

using System.Collections.Immutable;

/// <summary>Composes the T033–T035 hierarchy into one deterministic rule set.</summary>
public static class SellArrangementRuleSet
{
    public static ImmutableArray<ISellArrangementRule> Create(
        IAccountCustomerSellArrangementRepository accountCustomerRepository,
        IBuyingGroupSellArrangementRepository buyingGroupRepository,
        ICorporateSellArrangementRepository corporateRepository) =>
        [
            .. AccountCustomerSellRuleSet.Create(accountCustomerRepository),
            .. BuyingGroupSellRuleSet.Create(buyingGroupRepository),
            .. CorporateSellRuleSet.Create(corporateRepository),
        ];
}
