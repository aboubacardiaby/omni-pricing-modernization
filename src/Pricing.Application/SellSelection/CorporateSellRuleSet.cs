namespace Pricing.Application.SellSelection;

using System.Collections.Immutable;

public static class CorporateSellRuleSet
{
    public static ImmutableArray<ISellArrangementRule> Create(
        ICorporateSellArrangementRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        return
        [
            new CorporateSellArrangementRule(repository, SellArrangementLevel.Product, 1020),
            new CorporateSellArrangementRule(repository, SellArrangementLevel.ProductCategory, 1070),
            new CorporateSellArrangementRule(repository, SellArrangementLevel.Vendor, 1115),
        ];
    }
}
