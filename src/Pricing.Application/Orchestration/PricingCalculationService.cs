namespace Pricing.Application.Orchestration;

using System.Collections.Immutable;
using Pricing.Application.Kits;
using Pricing.Application.ProductClassification;
using Pricing.Application.ProductInformation;
using Pricing.Application.Rounding;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

public interface IPricingCalculationService
{
    ValueTask<PricingResult> CalculateAsync(PricingRequest request, CancellationToken cancellationToken);
}

/// <summary>Routes the public pricing operation using the legacy A6X01 product-type dispatch.</summary>
/// <remarks>
/// A6X01 050-PROCESS-PRICE sends product type O to A6O011U and every other classified type to
/// A6U01. The request switch is retained on <see cref="PricingRequest"/> for A6U01's full,
/// cost-only, sell-only, and JIT processing paths.
/// </remarks>
public sealed class PricingCalculationService(
    ProductClassificationService classificationService,
    ProductInformationService productInformationService,
    IPricingOrchestrator regularPricing,
    KitComponentPricingService kitComponentPricing)
    : IPricingCalculationService
{
    private static readonly AccountRoundingConfiguration DefaultRounding =
        AccountRoundingConfiguration.FromLegacyCode(null);

    public async ValueTask<PricingResult> CalculateAsync(
        PricingRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        ProductClassificationResult classification = await classificationService
            .ClassifyAsync(request.Vendor.Value, request.Product.Value, cancellationToken)
            .ConfigureAwait(false);
        if (classification.Error is { } classificationError)
        {
            return Failure(ProductType.Regular, classificationError);
        }

        if (classification.ProductType != ProductType.Kit)
        {
            return await regularPricing
                .PriceAsync(new PricingOperation(request, DefaultRounding), cancellationToken)
                .ConfigureAwait(false);
        }

        ProductInformationResult information = await productInformationService.GetAsync(
            request.Vendor.Value,
            request.Product.Value,
            request.UnitOfMeasure.Value,
            request.Division.Value,
            cancellationToken).ConfigureAwait(false);
        if (information.Error is { } informationError)
        {
            return Failure(ProductType.Kit, informationError);
        }

        if (information.Product is null)
        {
            return Failure(ProductType.Kit, new MissingDataPricingError(
                "PRODUCT_INFORMATION_MISSING",
                "Product information completed without a product.",
                Entity: "product"));
        }

        var packProduct = new KitProductNumber(request.Vendor.Value + request.Product.Value);
        KitComponentPricingResult components = await kitComponentPricing.PriceAsync(
            new KitComponentPricingRequest(request, packProduct, DefaultRounding),
            cancellationToken).ConfigureAwait(false);
        KitRollupResult rollup = KitRollupCalculator.Calculate(components);
        return KitResultFinalizer.Create(new KitFinalizationRequest(
            request,
            information.Product.BaseUnitOfMeasure,
            information.Product.AlternateConversionFactor,
            components,
            rollup)).PricingResult;
    }

    private static PricingResult Failure(ProductType productType, PricingError error) => new(
        productType,
        null,
        null,
        null,
        null,
        null,
        [],
        [],
        [],
        ImmutableArray.Create(error));
}
