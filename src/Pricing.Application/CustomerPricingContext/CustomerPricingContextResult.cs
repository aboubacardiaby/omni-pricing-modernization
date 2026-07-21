namespace Pricing.Application.CustomerPricingContext;

using Pricing.Domain.Models;

public sealed record CustomerPricingContextResult(CustomerInformation? Customer, PricingError? Error)
{
    public bool IsSuccess => Customer is not null && Error is null;

    public static CustomerPricingContextResult Success(CustomerInformation customer) => new(customer, null);

    public static CustomerPricingContextResult Failure(PricingError error) =>
        new(null, error ?? throw new ArgumentNullException(nameof(error)));
}
