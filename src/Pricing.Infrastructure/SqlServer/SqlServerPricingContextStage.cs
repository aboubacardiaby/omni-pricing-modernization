namespace Pricing.Infrastructure.SqlServer;

using System.Collections.Immutable;
using Pricing.Application.CustomerPricingContext;
using Pricing.Application.Orchestration;
using Pricing.Application.ProductClassification;
using Pricing.Application.ProductInformation;
using Pricing.Domain.Models;

/// <summary>Builds the immutable pre-rule context using the T020-T022 services.</summary>
public sealed class SqlServerPricingContextStage(
    ProductClassificationService classificationService,
    ProductInformationService productInformationService,
    CustomerPricingContextService customerContextService) : IPricingContextStage
{
    public async ValueTask<PricingContextStageResult> ExecuteAsync(PricingRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        ProductClassificationResult classification = await classificationService.ClassifyAsync(
            request.Vendor.Value, request.Product.Value, cancellationToken).ConfigureAwait(false);
        if (classification.Error is not null)
        {
            return new PricingContextStageResult(null, [], classification.Error);
        }

        ProductInformationResult product = await productInformationService.GetAsync(
            request.Vendor.Value, request.Product.Value, request.UnitOfMeasure.Value,
            request.Division.Value, cancellationToken).ConfigureAwait(false);
        if (product.Error is not null || product.Product is null)
        {
            return new PricingContextStageResult(null, Normalize(product.Warnings), product.Error);
        }

        CustomerPricingContextResult customer = await customerContextService.GetAsync(request, cancellationToken).ConfigureAwait(false);
        if (customer.Error is not null || customer.Customer is null)
        {
            return new PricingContextStageResult(null, Normalize(product.Warnings), customer.Error);
        }

        Pricing.Domain.Models.ProductInformation classifiedProduct = product.Product with
        {
            ProductType = classification.ProductType!.Value,
        };
        return new PricingContextStageResult(
            new PricingContext(request, classifiedProduct, customer.Customer, null, null),
            Normalize(product.Warnings));
    }

    private static ImmutableArray<T> Normalize<T>(ImmutableArray<T> values) => values.IsDefault ? [] : values;
}
