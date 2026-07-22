namespace Pricing.IntegrationTests;

using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Pricing.Application.Kits;
using Pricing.Domain.ValueObjects;
using Pricing.Infrastructure.Kits;
using Xunit;

public sealed class LegacyKitExplosionRepositoryTests
{
    [Fact]
    public async Task MapsRequestAndPreservesLegacyOrderAndOpaqueHeaderCodes()
    {
        var client = new StubClient(Response(
            [
                new(null, "1234COMP0001", "EA", 2m, "C", 1),
                new("1234SUB00001", "1234COMP0002", "BX", 6m, "C", 2),
            ]));
        var repository = new LegacyKitExplosionRepository(client);
        var request = new KitExplosionRequest(
            new KitProductNumber("1234KIT00001"),
            new DateOnly(2028, 7, 21),
            KitExplosionView.Structure,
            KitRollupCostMode.IncludeSubPackFees);

        KitExplosion result = await repository.ExplodeAsync(request, CancellationToken.None);

        Assert.Equal("S", client.Request?.RequestType);
        Assert.Equal("Y", client.Request?.RollupCostSwitch);
        Assert.Equal(request.PackProduct.Value, client.Request?.PackProductNumber);
        Assert.Equal("opaque-product", result.LegacyProductTypeCode);
        Assert.Equal("opaque-status", result.LegacyPackStatusCode);
        Assert.Equal("opaque-complete", result.LegacyCompleteExplosionCode);
        Assert.Equal([1, 2], result.Items.Select(item => item.LegacyOrdinal));
        Assert.Equal(["1234COMP0001", "1234COMP0002"], result.Items.Select(item => item.Product.Value));
        Assert.Null(result.Items[0].ParentPack);
        Assert.Equal("1234SUB00001", result.Items[1].ParentPack?.Value);
        Assert.Equal(6m, result.Items[1].Quantity.Value);
        Assert.Equal(new DateOnly(2028, 12, 31), result.Overhead.ExpirationDate);
    }

    [Theory]
    [InlineData(KitExplosionView.Components, KitRollupCostMode.LegacyDefault, "C", " ")]
    [InlineData(KitExplosionView.FirstLevel, KitRollupCostMode.ExcludeSubPackFees, "L", "N")]
    public async Task MapsConfirmedLegacySwitchValues(
        KitExplosionView view,
        KitRollupCostMode rollup,
        string expectedView,
        string expectedRollup)
    {
        var client = new StubClient(Response([]));
        var repository = new LegacyKitExplosionRepository(client);

        await repository.ExplodeAsync(
            new KitExplosionRequest(new KitProductNumber("1234KIT00001"), new DateOnly(2028, 1, 1), view, rollup),
            CancellationToken.None);

        Assert.Equal(expectedView, client.Request?.RequestType);
        Assert.Equal(expectedRollup, client.Request?.RollupCostSwitch);
    }

    [Fact]
    public async Task MapsLegacyErrorWithoutInventingSeverity()
    {
        LegacyKitExplosionResponse response = Response([]) with
        {
            Error = new LegacyKitExplosionError("E", "61201", "PRODUCT# NOT PROVIDED", "-805"),
        };
        var repository = new LegacyKitExplosionRepository(new StubClient(response));

        KitExplosionRepositoryException error = await Assert.ThrowsAsync<KitExplosionRepositoryException>(async () =>
            await repository.ExplodeAsync(Request(), CancellationToken.None));

        Assert.Equal("61201", error.LegacyErrorCode);
        Assert.Equal("E", error.LegacyResponseCode);
        Assert.Equal("-805", error.LegacySqlCode);
        Assert.False(error.IsTransient);
    }

    [Fact]
    public async Task MapsTransientTransportFailureAndPreservesCause()
    {
        var repository = new LegacyKitExplosionRepository(new ThrowingClient());

        KitExplosionRepositoryException error = await Assert.ThrowsAsync<KitExplosionRepositoryException>(async () =>
            await repository.ExplodeAsync(Request(), CancellationToken.None));

        Assert.True(error.IsTransient);
        Assert.IsType<LegacyKitExplosionTransportException>(error.InnerException);
    }

    [Fact]
    public async Task RejectsResponseBeyondConfirmedA6O012UMaximum()
    {
        ImmutableArray<LegacyKitExplosionItem> items = Enumerable.Range(0, 700)
            .Select(index => new LegacyKitExplosionItem(null, $"P{index:00000000000}", "EA", 1m, "C", 1))
            .ToImmutableArray();
        var repository = new LegacyKitExplosionRepository(new StubClient(Response(items)));

        KitExplosionRepositoryException error = await Assert.ThrowsAsync<KitExplosionRepositoryException>(async () =>
            await repository.ExplodeAsync(Request(), CancellationToken.None));

        Assert.Contains("699", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CancellationStopsBeforeCallingLegacyClient()
    {
        var client = new StubClient(Response([]));
        var repository = new LegacyKitExplosionRepository(client);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await repository.ExplodeAsync(Request(), cancellation.Token));

        Assert.Null(client.Request);
    }

    [Fact]
    public void RegistersRepositoryWithoutOwningTransportImplementation()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILegacyKitExplosionClient>(new StubClient(Response([])));
        services.AddLegacyKitExplosionRepository();

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsType<LegacyKitExplosionRepository>(provider.GetRequiredService<IKitExplosionRepository>());
    }

    private static KitExplosionRequest Request() =>
        new(new KitProductNumber("1234KIT00001"), new DateOnly(2028, 1, 1));

    private static LegacyKitExplosionResponse Response(ImmutableArray<LegacyKitExplosionItem> items) => new(
        "EA",
        "opaque-product",
        "opaque-status",
        "opaque-source",
        "opaque-complete",
        "1234NEXT0001",
        new LegacyKitFee(1.25m, new DateOnly(2028, 1, 1), new DateOnly(2028, 12, 31)),
        new LegacyKitFee(2.50m, null, null),
        new LegacyKitFee(3.75m, null, null),
        items);

    private sealed class StubClient(LegacyKitExplosionResponse response) : ILegacyKitExplosionClient
    {
        public LegacyKitExplosionRequest? Request { get; private set; }

        public ValueTask<LegacyKitExplosionResponse> ExplodeAsync(
            LegacyKitExplosionRequest request,
            CancellationToken cancellationToken)
        {
            Request = request;
            return ValueTask.FromResult(response);
        }
    }

    private sealed class ThrowingClient : ILegacyKitExplosionClient
    {
        public ValueTask<LegacyKitExplosionResponse> ExplodeAsync(
            LegacyKitExplosionRequest request,
            CancellationToken cancellationToken) =>
            ValueTask.FromException<LegacyKitExplosionResponse>(
                new LegacyKitExplosionTransportException("CICS link failed.", true));
    }
}
