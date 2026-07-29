namespace Pricing.Application.Legacy;

using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>Supplies legacy PriceOperation catalog, inventory, and cost display fields.</summary>
public interface ILegacyPricingDetailsRepository
{
    ValueTask<LegacyPricingDetails?> FindAsync(
        DivisionId division,
        VendorId vendor,
        ProductId product,
        DateOnly pricingDate,
        Money? cost,
        Money? sellPrice,
        CancellationToken cancellationToken);
}
