namespace Pricing.UnitTests.Orchestration;

using Pricing.Application.Kits;
using Pricing.Application.Orchestration;
using Pricing.Application.ProductClassification;
using Pricing.Application.ProductInformation;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class PricingCalculationServiceTests
{
    [Theory]
    [InlineData("R")]
    [InlineData("S")]
    public async Task RoutesEveryNonOwensKitTypeToRegularPricing(string legacyType)
    {
        var orchestrator = new StubOrchestrator();
        PricingCalculationService service = Create(legacyType, orchestrator);
        PricingRequest request = Request(PricingRequestType.SellOnly);

        PricingResult result = await service.CalculateAsync(request, CancellationToken.None);

        Assert.Equal(ProductType.Regular, result.ProductType);
        PricingOperation operation = Assert.Single(orchestrator.Operations);
        Assert.Equal(PricingRequestType.SellOnly, operation.Request.RequestType);
    }

    [Fact]
    public async Task RoutesOwensKitThroughExplosionAndRollup()
    {
        var orchestrator = new StubOrchestrator();
        var explosions = new StubExplosionRepository();
        PricingCalculationService service = Create("O", orchestrator, explosions);

        PricingResult result = await service.CalculateAsync(Request(PricingRequestType.Full), CancellationToken.None);

        Assert.Equal(ProductType.Kit, result.ProductType);
        Assert.Single(explosions.Requests);
        Assert.Empty(orchestrator.Operations);
    }

    [Fact]
    public async Task KitReturnsTypedBlockerWhenLegacyExplosionTransportIsNotRegistered()
    {
        var orchestrator = new StubOrchestrator();
        var service = new PricingCalculationService(
            new ProductClassificationService(new StubClassificationRepository("O")),
            new ProductInformationService(new StubProductInformationRepository("O")),
            orchestrator);

        PricingResult result = await service.CalculateAsync(Request(PricingRequestType.Full), CancellationToken.None);

        UnsupportedBehaviorPricingError error = Assert.IsType<UnsupportedBehaviorPricingError>(Assert.Single(result.Errors));
        Assert.Equal("KIT_EXPLOSION_NOT_CONFIGURED", error.Code);
        Assert.Equal("ILegacyKitExplosionClient", error.Blocker);
        Assert.Empty(orchestrator.Operations);
    }

    private static PricingCalculationService Create(
        string legacyType,
        StubOrchestrator orchestrator,
        StubExplosionRepository? explosions = null) => new(
        new ProductClassificationService(new StubClassificationRepository(legacyType)),
        new ProductInformationService(new StubProductInformationRepository(legacyType)),
        orchestrator,
        new KitComponentPricingService(explosions ?? new StubExplosionRepository(), orchestrator));

    private static PricingRequest Request(PricingRequestType requestType) => new(
        new DivisionId("01"),
        new AccountNumber("123456"),
        new VendorId("1234"),
        new ProductId("ABC123"),
        new Quantity(1m),
        new UnitOfMeasure("KT"),
        "01",
        "02",
        new DateOnly(2026, 7, 21),
        requestType);

    private sealed class StubClassificationRepository(string legacyType) : IProductClassificationRepository
    {
        public ValueTask<string?> FindLegacyProductTypeAsync(
            VendorId vendor,
            ProductId product,
            CancellationToken cancellationToken) => ValueTask.FromResult<string?>(legacyType);
    }

    private sealed class StubProductInformationRepository(string legacyType) : IProductInformationRepository
    {
        public ValueTask<ProductInformationData?> FindAsync(
            VendorId vendor,
            ProductId product,
            UnitOfMeasure? requestedUnitOfMeasure,
            DivisionId? division,
            CancellationToken cancellationToken) => ValueTask.FromResult<ProductInformationData?>(new(
                legacyType,
                new UnitOfMeasure("KT"),
                null,
                null,
                null,
                null,
                1m));
    }

    private sealed class StubExplosionRepository : IKitExplosionRepository
    {
        public List<KitExplosionRequest> Requests { get; } = [];

        public ValueTask<KitExplosion> ExplodeAsync(
            KitExplosionRequest request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            var noFee = new KitFeeWindow(new Money(0m), null, null);
            return ValueTask.FromResult(new KitExplosion(
                request.PackProduct,
                new UnitOfMeasure("KT"),
                "O",
                string.Empty,
                string.Empty,
                string.Empty,
                null,
                noFee,
                noFee,
                noFee,
                []));
        }
    }

    private sealed class StubOrchestrator : IPricingOrchestrator
    {
        public List<PricingOperation> Operations { get; } = [];

        public ValueTask<PricingResult> PriceAsync(
            PricingOperation operation,
            CancellationToken cancellationToken)
        {
            Operations.Add(operation);
            return ValueTask.FromResult(new PricingResult(
                ProductType.Regular,
                new Money(1m),
                new Money(2m),
                null,
                null,
                null,
                [],
                [],
                [],
                []));
        }
    }
}
