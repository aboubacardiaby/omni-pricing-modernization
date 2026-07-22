namespace Pricing.Application.Kits;

using System.Collections.Immutable;
using Pricing.Domain.ValueObjects;

public enum KitExplosionView
{
    Components,
    FirstLevel,
    Structure,
}

public enum KitRollupCostMode
{
    LegacyDefault,
    IncludeSubPackFees,
    ExcludeSubPackFees,
}

public enum KitExplosionItemType
{
    Component,
    SubPack,
}

public sealed record KitExplosionRequest(
    KitProductNumber PackProduct,
    DateOnly EffectiveDate,
    KitExplosionView View = KitExplosionView.Components,
    KitRollupCostMode RollupCostMode = KitRollupCostMode.LegacyDefault);

public sealed record KitFeeWindow(
    Money Amount,
    DateOnly? EffectiveDate,
    DateOnly? ExpirationDate);

public sealed record KitExplosionItem(
    int LegacyOrdinal,
    KitProductNumber? ParentPack,
    KitProductNumber Product,
    UnitOfMeasure UnitOfMeasure,
    Quantity Quantity,
    KitExplosionItemType Type,
    int? Level);

/// <summary>Legacy A6O012U output with still-unknown A6O015U fields retained as opaque codes.</summary>
public sealed record KitExplosion(
    KitProductNumber PackProduct,
    UnitOfMeasure BaseUnitOfMeasure,
    string LegacyProductTypeCode,
    string LegacyPackStatusCode,
    string LegacyCustomSourceCode,
    string LegacyCompleteExplosionCode,
    KitProductNumber? NextSubPackProduct,
    KitFeeWindow Overhead,
    KitFeeWindow ThirdPartyCost,
    KitFeeWindow ThirdPartySell,
    ImmutableArray<KitExplosionItem> Items);

/// <summary>Business boundary for kit explosion; the initial implementation delegates to A6O012U.</summary>
public interface IKitExplosionRepository
{
    ValueTask<KitExplosion> ExplodeAsync(
        KitExplosionRequest request,
        CancellationToken cancellationToken);
}

public sealed class KitExplosionRepositoryException : Exception
{
    public KitExplosionRepositoryException(
        string message,
        string? legacyErrorCode = null,
        string? legacyResponseCode = null,
        string? legacySqlCode = null,
        bool isTransient = false,
        Exception? innerException = null)
        : base(message, innerException)
    {
        LegacyErrorCode = legacyErrorCode;
        LegacyResponseCode = legacyResponseCode;
        LegacySqlCode = legacySqlCode;
        IsTransient = isTransient;
    }

    public string? LegacyErrorCode { get; }
    public string? LegacyResponseCode { get; }
    public string? LegacySqlCode { get; }
    public bool IsTransient { get; }
}
