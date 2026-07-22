namespace Pricing.Api.Pricing;

using System.Collections.Immutable;
using global::Pricing.Application.Orchestration;
using global::Pricing.Domain.Models;

public sealed class UnavailablePricingCalculationService : IPricingCalculationService
{
    public ValueTask<PricingResult> CalculateAsync(
        PricingRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new PricingResult(
            ProductType.Regular,
            null,
            null,
            null,
            null,
            null,
            ImmutableArray<PriceComponent>.Empty,
            ImmutableArray<RuleProvenance>.Empty,
            ImmutableArray<PricingWarning>.Empty,
            ImmutableArray.Create<PricingError>(new DependencyPricingError(
                "PRICING_DATA_ACCESS_NOT_CONFIGURED",
                "Pricing data access is not configured. Register the MSSQL repositories and pricing pipeline.",
                IsTransient: false))));
    }
}
