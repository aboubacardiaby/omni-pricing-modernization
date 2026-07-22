namespace Pricing.Infrastructure.Kits;

using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Pricing.Application.Kits;
using Pricing.Domain.ValueObjects;

public sealed record LegacyKitExplosionRequest(
    string PackProductNumber,
    DateOnly EffectiveDate,
    string RequestType,
    string RollupCostSwitch);

public sealed record LegacyKitFee(
    decimal Amount,
    DateOnly? EffectiveDate,
    DateOnly? ExpirationDate);

public sealed record LegacyKitExplosionItem(
    string? ParentPackProductNumber,
    string ProductNumber,
    string UnitOfMeasure,
    decimal Quantity,
    string ProductType,
    int? Level);

public sealed record LegacyKitExplosionError(
    string ResponseCode,
    string? ErrorNumber,
    string Message,
    string? SqlCode = null);

public sealed record LegacyKitExplosionResponse(
    string BaseUnitOfMeasure,
    string ProductTypeCode,
    string PackStatusCode,
    string CustomSourceCode,
    string CompleteExplosionCode,
    string? NextSubPackProductNumber,
    LegacyKitFee Overhead,
    LegacyKitFee ThirdPartyCost,
    LegacyKitFee ThirdPartySell,
    ImmutableArray<LegacyKitExplosionItem> Items,
    LegacyKitExplosionError? Error = null);

/// <summary>Transport boundary implemented by the environment-specific A6O012U COMMAREA client.</summary>
public interface ILegacyKitExplosionClient
{
    ValueTask<LegacyKitExplosionResponse> ExplodeAsync(
        LegacyKitExplosionRequest request,
        CancellationToken cancellationToken);
}

public sealed class LegacyKitExplosionTransportException : Exception
{
    public LegacyKitExplosionTransportException(string message, bool isTransient, Exception? innerException = null)
        : base(message, innerException) => IsTransient = isTransient;

    public bool IsTransient { get; }
}

/// <summary>Adapts the confirmed A6O012U interface without recreating blocked A6O015U behavior.</summary>
public sealed class LegacyKitExplosionRepository(ILegacyKitExplosionClient client) : IKitExplosionRepository
{
    public const int MaximumReturnedItems = 699;

    private readonly ILegacyKitExplosionClient client = client ?? throw new ArgumentNullException(nameof(client));

    public async ValueTask<KitExplosion> ExplodeAsync(
        KitExplosionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        LegacyKitExplosionResponse response;
        try
        {
            response = await client.ExplodeAsync(Map(request), cancellationToken).ConfigureAwait(false)
                ?? throw new KitExplosionRepositoryException("The legacy kit explosion client returned no response.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (LegacyKitExplosionTransportException exception)
        {
            throw new KitExplosionRepositoryException(
                exception.Message,
                isTransient: exception.IsTransient,
                innerException: exception);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (response.Error is { } error)
        {
            throw new KitExplosionRepositoryException(
                error.Message,
                error.ErrorNumber,
                error.ResponseCode,
                error.SqlCode);
        }

        ImmutableArray<LegacyKitExplosionItem> legacyItems = response.Items.IsDefault ? [] : response.Items;
        if (legacyItems.Length > MaximumReturnedItems)
        {
            throw new KitExplosionRepositoryException(
                $"The legacy kit explosion response contained {legacyItems.Length} items; the confirmed A6O012U maximum is {MaximumReturnedItems}.");
        }

        ImmutableArray<KitExplosionItem> items = legacyItems
            .Select((item, index) => Map(item, index + 1))
            .ToImmutableArray();
        return new KitExplosion(
            request.PackProduct,
            new UnitOfMeasure(response.BaseUnitOfMeasure),
            response.ProductTypeCode,
            response.PackStatusCode,
            response.CustomSourceCode,
            response.CompleteExplosionCode,
            OptionalProduct(response.NextSubPackProductNumber),
            Map(response.Overhead),
            Map(response.ThirdPartyCost),
            Map(response.ThirdPartySell),
            items);
    }

    private static LegacyKitExplosionRequest Map(KitExplosionRequest request) => new(
        request.PackProduct.Value,
        request.EffectiveDate,
        request.View switch
        {
            KitExplosionView.Structure => "S",
            KitExplosionView.FirstLevel => "L",
            _ => "C",
        },
        request.RollupCostMode switch
        {
            KitRollupCostMode.IncludeSubPackFees => "Y",
            KitRollupCostMode.ExcludeSubPackFees => "N",
            _ => " ",
        });

    private static KitExplosionItem Map(LegacyKitExplosionItem item, int ordinal) => new(
        ordinal,
        OptionalProduct(item.ParentPackProductNumber),
        new KitProductNumber(item.ProductNumber),
        new UnitOfMeasure(item.UnitOfMeasure),
        new Quantity(item.Quantity),
        item.ProductType switch
        {
            "C" => KitExplosionItemType.Component,
            "S" => KitExplosionItemType.SubPack,
            _ => throw new KitExplosionRepositoryException($"Unknown legacy kit item type '{item.ProductType}'."),
        },
        item.Level);

    private static KitFeeWindow Map(LegacyKitFee fee) =>
        new(new Money(fee.Amount), fee.EffectiveDate, fee.ExpirationDate);

    private static KitProductNumber? OptionalProduct(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : new KitProductNumber(value);
}

public static class KitExplosionServiceCollectionExtensions
{
    public static IServiceCollection AddLegacyKitExplosionRepository(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IKitExplosionRepository, LegacyKitExplosionRepository>();
        return services;
    }
}
