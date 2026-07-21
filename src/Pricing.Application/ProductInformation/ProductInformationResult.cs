namespace Pricing.Application.ProductInformation;

using System.Collections.Immutable;
using Pricing.Domain.Models;

public sealed record ProductInformationResult(
    Pricing.Domain.Models.ProductInformation? Product,
    ImmutableArray<PricingWarning> Warnings,
    PricingError? Error)
{
    public bool IsSuccess => Product is not null && Error is null;

    public static ProductInformationResult Success(
        Pricing.Domain.Models.ProductInformation product,
        ImmutableArray<PricingWarning> warnings) =>
        new(product, warnings, null);

    public static ProductInformationResult Failure(PricingError error) =>
        new(null, [], error ?? throw new ArgumentNullException(nameof(error)));
}
