namespace Pricing.UnitTests.Kits;

using System.Collections.Immutable;
using Pricing.Application.Kits;
using Pricing.Application.Orchestration;
using Pricing.Application.Rounding;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class KitComponentPricingServiceTests
{
    [Fact]
    public async Task PricesComponentsInTraversalOrderAndMultipliesNestedQuantities()
    {
        KitProductNumber root = Product("ROOT0001");
        KitProductNumber sub = Product("SUB00001");
        var repository = new StubExplosionRepository(new Dictionary<KitProductNumber, KitExplosion>
        {
            [root] = Explosion(root,
                Item("COMP0001", 2m, KitExplosionItemType.Component),
                Item("SUB00001", 3m, KitExplosionItemType.SubPack)),
            [sub] = Explosion(sub, Item("COMP0002", 4m, KitExplosionItemType.Component)),
        });
        var orchestrator = new StubPricingOrchestrator();
        var service = new KitComponentPricingService(repository, orchestrator);

        KitComponentPricingResult result = await service.PriceAsync(Request(root), CancellationToken.None);

        Assert.True(result.IsComplete);
        Assert.Equal([root, sub], repository.Requests.Select(request => request.PackProduct));
        Assert.All(repository.Requests, request => Assert.Equal(KitExplosionView.FirstLevel, request.View));
        Assert.Equal([2m, 12m], result.Components.Select(component => component.EffectiveQuantity.Value));
        Assert.Equal(["COMP0001", "COMP0002"], orchestrator.Operations.Select(operation => operation.Request.Product.Value));
        Assert.Equal([2m, 12m], orchestrator.Operations.Select(operation => operation.Request.Quantity.Value));
        Assert.Equal([0, 1], result.Components.Select(component => component.Depth));
        Assert.Equal([1m, 3m], result.ExplodedKits.Select(node => node.EffectiveQuantity.Value));
    }

    [Fact]
    public async Task DetectsCycleBeforeReExplodingAncestor()
    {
        KitProductNumber root = Product("ROOT0001");
        KitProductNumber sub = Product("SUB00001");
        var repository = new StubExplosionRepository(new Dictionary<KitProductNumber, KitExplosion>
        {
            [root] = Explosion(root, Item("SUB00001", 1m, KitExplosionItemType.SubPack)),
            [sub] = Explosion(sub, Item("ROOT0001", 1m, KitExplosionItemType.SubPack)),
        });
        var service = new KitComponentPricingService(repository, new StubPricingOrchestrator());

        KitComponentPricingResult result = await service.PriceAsync(Request(root), CancellationToken.None);

        Assert.Equal("KIT_CYCLE_DETECTED", result.TraversalError?.Code);
        Assert.Equal(2, repository.Requests.Count);
        Assert.Empty(result.Components);
    }

    [Fact]
    public async Task EnforcesConfiguredDepthBeforeNestedExplosion()
    {
        KitProductNumber root = Product("ROOT0001");
        var repository = new StubExplosionRepository(new Dictionary<KitProductNumber, KitExplosion>
        {
            [root] = Explosion(root, Item("SUB00001", 1m, KitExplosionItemType.SubPack)),
        });
        KitComponentPricingRequest request = Request(root) with { MaximumDepth = 0 };
        var service = new KitComponentPricingService(repository, new StubPricingOrchestrator());

        KitComponentPricingResult result = await service.PriceAsync(request, CancellationToken.None);

        Assert.Equal("KIT_DEPTH_EXCEEDED", result.TraversalError?.Code);
        Assert.Single(repository.Requests);
    }

    [Fact]
    public async Task StopsAtFirstComponentPricingErrorWithoutPerformingRollup()
    {
        KitProductNumber root = Product("ROOT0001");
        var repository = new StubExplosionRepository(new Dictionary<KitProductNumber, KitExplosion>
        {
            [root] = Explosion(
                root,
                Item("COMP0001", 1m, KitExplosionItemType.Component),
                Item("COMP0002", 1m, KitExplosionItemType.Component)),
        });
        var orchestrator = new StubPricingOrchestrator(failFirst: true);
        var service = new KitComponentPricingService(repository, orchestrator);

        KitComponentPricingResult result = await service.PriceAsync(Request(root), CancellationToken.None);

        Assert.False(result.IsComplete);
        Assert.NotNull(result.FailedComponent);
        Assert.Null(result.TraversalError);
        Assert.Single(orchestrator.Operations);
        Assert.Single(result.Components);
    }

    [Fact]
    public async Task MapsExplosionFailureToTypedTraversalError()
    {
        var repository = new StubExplosionRepository(
            new KitExplosionRepositoryException("Legacy unavailable", "61210", "A", "-805", true));
        var service = new KitComponentPricingService(repository, new StubPricingOrchestrator());

        KitComponentPricingResult result = await service.PriceAsync(
            Request(Product("ROOT0001")),
            CancellationToken.None);

        DependencyPricingError error = Assert.IsType<DependencyPricingError>(result.TraversalError);
        Assert.Equal("61210", error.LegacyErrorCode);
        Assert.Equal("A", error.LegacySeverityCode);
        Assert.True(error.IsTransient);
    }

    [Fact]
    public async Task CancellationStopsBeforeExplosion()
    {
        var repository = new StubExplosionRepository(new Dictionary<KitProductNumber, KitExplosion>());
        var service = new KitComponentPricingService(repository, new StubPricingOrchestrator());
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await service.PriceAsync(Request(Product("ROOT0001")), cancellation.Token));

        Assert.Empty(repository.Requests);
    }

    private static KitComponentPricingRequest Request(KitProductNumber pack) => new(
        new PricingRequest(
            new DivisionId("01"),
            new AccountNumber("123456"),
            new VendorId("1234"),
            new ProductId("ROOT0001"),
            new Quantity(1m),
            new UnitOfMeasure("KT"),
            "001",
            "001",
            new DateOnly(2028, 1, 1),
            PricingRequestType.Full),
        pack,
        AccountRoundingConfiguration.FromLegacyCode("N"));

    private static KitExplosion Explosion(KitProductNumber pack, params KitExplosionItem[] items) => new(
        pack,
        new UnitOfMeasure("KT"),
        "O",
        string.Empty,
        string.Empty,
        string.Empty,
        null,
        Fee(),
        Fee(),
        Fee(),
        [.. items]);

    private static KitExplosionItem Item(string product, decimal quantity, KitExplosionItemType type) => new(
        1,
        null,
        Product(product),
        new UnitOfMeasure(type == KitExplosionItemType.SubPack ? "KT" : "EA"),
        new Quantity(quantity),
        type,
        1);

    private static KitFeeWindow Fee() => new(new Money(0m), null, null);

    private static KitProductNumber Product(string product) => new("1234" + product);

    private sealed class StubExplosionRepository : IKitExplosionRepository
    {
        private readonly IReadOnlyDictionary<KitProductNumber, KitExplosion>? explosions;
        private readonly KitExplosionRepositoryException? error;

        public StubExplosionRepository(IReadOnlyDictionary<KitProductNumber, KitExplosion> explosions) =>
            this.explosions = explosions;

        public StubExplosionRepository(KitExplosionRepositoryException error) => this.error = error;

        public List<KitExplosionRequest> Requests { get; } = [];

        public ValueTask<KitExplosion> ExplodeAsync(KitExplosionRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return error is not null
                ? ValueTask.FromException<KitExplosion>(error)
                : ValueTask.FromResult(explosions![request.PackProduct]);
        }
    }

    private sealed class StubPricingOrchestrator(bool failFirst = false) : IPricingOrchestrator
    {
        public List<PricingOperation> Operations { get; } = [];

        public ValueTask<PricingResult> PriceAsync(PricingOperation operation, CancellationToken cancellationToken)
        {
            Operations.Add(operation);
            ImmutableArray<PricingError> errors = failFirst && Operations.Count == 1
                ? [new MissingDataPricingError("COMPONENT_FAILED", "Component failed")]
                : [];
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
                errors));
        }
    }
}
