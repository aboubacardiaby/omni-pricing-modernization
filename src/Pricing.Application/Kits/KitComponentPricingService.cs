namespace Pricing.Application.Kits;

using System.Collections.Immutable;
using Pricing.Application.Orchestration;
using Pricing.Application.Rounding;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

public sealed record KitComponentPricingRequest(
    PricingRequest KitRequest,
    KitProductNumber PackProduct,
    AccountRoundingConfiguration RoundingConfiguration,
    int MaximumDepth = 10);

public sealed record ExplodedKitNode(
    KitProductNumber PackProduct,
    Quantity EffectiveQuantity,
    int Depth,
    KitExplosion Explosion);

public sealed record PricedKitComponent(
    KitExplosionItem ExplosionItem,
    Quantity EffectiveQuantity,
    int Depth,
    ImmutableArray<KitProductNumber> PackPath,
    PricingResult PricingResult);

public sealed record KitComponentPricingResult(
    ImmutableArray<ExplodedKitNode> ExplodedKits,
    ImmutableArray<PricedKitComponent> Components,
    PricingError? TraversalError,
    PricedKitComponent? FailedComponent)
{
    public bool IsComplete => TraversalError is null && FailedComponent is null;
}

/// <summary>Prices exploded components through the regular-item orchestrator without rolling up kit totals.</summary>
/// <remarks>
/// A6O011U 2000-PROCESS-PRICE prices components in returned order and stops on the first error.
/// First-level explosion is used here so nested sub-packs can be guarded explicitly for cycles and depth.
/// </remarks>
public sealed class KitComponentPricingService(
    IKitExplosionRepository explosionRepository,
    IPricingOrchestrator pricingOrchestrator)
{
    private readonly IKitExplosionRepository explosionRepository = explosionRepository
        ?? throw new ArgumentNullException(nameof(explosionRepository));
    private readonly IPricingOrchestrator pricingOrchestrator = pricingOrchestrator
        ?? throw new ArgumentNullException(nameof(pricingOrchestrator));

    public async ValueTask<KitComponentPricingResult> PriceAsync(
        KitComponentPricingRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MaximumDepth is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.MaximumDepth,
                "Maximum kit depth must be between zero and 100.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var state = new TraversalState();
        await TraverseAsync(
            request,
            request.PackProduct,
            new Quantity(1m),
            0,
            [],
            state,
            cancellationToken).ConfigureAwait(false);
        return new(
            state.ExplodedKits.ToImmutable(),
            state.Components.ToImmutable(),
            state.TraversalError,
            state.FailedComponent);
    }

    private async ValueTask TraverseAsync(
        KitComponentPricingRequest request,
        KitProductNumber pack,
        Quantity multiplier,
        int depth,
        ImmutableArray<KitProductNumber> ancestors,
        TraversalState state,
        CancellationToken cancellationToken)
    {
        if (state.ShouldStop)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (ancestors.Contains(pack))
        {
            state.TraversalError = new ValidationPricingError(
                "KIT_CYCLE_DETECTED",
                $"Kit cycle detected at '{pack.Value}'.",
                Field: "packProduct");
            return;
        }

        if (depth > request.MaximumDepth)
        {
            state.TraversalError = new ValidationPricingError(
                "KIT_DEPTH_EXCEEDED",
                $"Kit '{pack.Value}' exceeds the configured maximum depth of {request.MaximumDepth}.",
                Field: "maximumDepth");
            return;
        }

        KitExplosion explosion;
        try
        {
            explosion = await explosionRepository.ExplodeAsync(
                new KitExplosionRequest(
                    pack,
                    request.KitRequest.PricingDate,
                    KitExplosionView.FirstLevel,
                    KitRollupCostMode.LegacyDefault),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (KitExplosionRepositoryException exception)
        {
            state.TraversalError = new DependencyPricingError(
                "KIT_EXPLOSION_FAILED",
                exception.Message,
                exception.LegacyErrorCode,
                exception.IsTransient,
                exception.LegacyResponseCode);
            return;
        }

        state.ExplodedKits.Add(new ExplodedKitNode(pack, multiplier, depth, explosion));
        ImmutableArray<KitProductNumber> path = ancestors.Add(pack);
        foreach (KitExplosionItem item in explosion.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Quantity effectiveQuantity;
            try
            {
                effectiveQuantity = new Quantity(multiplier.Value * item.Quantity.Value);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                state.TraversalError = new ValidationPricingError(
                    "KIT_COMPONENT_QUANTITY_OUT_OF_RANGE",
                    exception.Message,
                    Field: "quantity");
                return;
            }

            if (item.Type == KitExplosionItemType.SubPack)
            {
                await TraverseAsync(
                    request,
                    item.Product,
                    effectiveQuantity,
                    depth + 1,
                    path,
                    state,
                    cancellationToken).ConfigureAwait(false);
                if (state.ShouldStop)
                {
                    return;
                }

                continue;
            }

            PricingRequest componentRequest;
            try
            {
                componentRequest = CreateComponentRequest(request.KitRequest, item, effectiveQuantity);
            }
            catch (ArgumentException exception)
            {
                state.TraversalError = new ValidationPricingError(
                    "KIT_COMPONENT_PRODUCT_INVALID",
                    exception.Message,
                    Field: "componentProduct");
                return;
            }

            PricingResult result = await pricingOrchestrator.PriceAsync(
                new PricingOperation(componentRequest, request.RoundingConfiguration),
                cancellationToken).ConfigureAwait(false);
            var priced = new PricedKitComponent(item, effectiveQuantity, depth, path, result);
            state.Components.Add(priced);
            if (!result.Errors.IsEmpty)
            {
                state.FailedComponent = priced;
                return;
            }
        }
    }

    private static PricingRequest CreateComponentRequest(
        PricingRequest kitRequest,
        KitExplosionItem item,
        Quantity quantity)
    {
        string fullProduct = item.Product.Value;
        if (fullProduct.Length <= VendorId.MaximumLength)
        {
            throw new ArgumentException(
                $"Kit component product '{fullProduct}' does not contain both vendor and product identifiers.",
                nameof(item));
        }

        string vendor = fullProduct[..VendorId.MaximumLength];
        string product = fullProduct[VendorId.MaximumLength..].TrimEnd();
        return kitRequest with
        {
            Vendor = new VendorId(vendor),
            Product = new ProductId(product),
            Quantity = quantity,
            UnitOfMeasure = item.UnitOfMeasure,
            RequestType = PricingRequestType.Full,
            IsSpecialContract = false,
        };
    }

    private sealed class TraversalState
    {
        public ImmutableArray<ExplodedKitNode>.Builder ExplodedKits { get; } = ImmutableArray.CreateBuilder<ExplodedKitNode>();
        public ImmutableArray<PricedKitComponent>.Builder Components { get; } = ImmutableArray.CreateBuilder<PricedKitComponent>();
        public PricingError? TraversalError { get; set; }
        public PricedKitComponent? FailedComponent { get; set; }
        public bool ShouldStop => TraversalError is not null || FailedComponent is not null;
    }
}
