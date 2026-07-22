namespace Pricing.Application.Orchestration;

using System.Collections.Immutable;
using Pricing.Application.ExpirationDates;
using Pricing.Application.Rounding;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

public sealed record PricingOperation(
    PricingRequest Request,
    AccountRoundingConfiguration RoundingConfiguration);

public interface IPricingOrchestrator
{
    ValueTask<PricingResult> PriceAsync(PricingOperation operation, CancellationToken cancellationToken);
}

public sealed record PricingContextStageResult(
    PricingContext? Context,
    ImmutableArray<PricingWarning> Warnings,
    PricingError? Error = null);

public sealed record CostPricingStageResult(
    PricingContext Context,
    Money TotalCost,
    ImmutableArray<PriceComponent> Components,
    ImmutableArray<ExpirationDateSource> ExpirationSources,
    ImmutableArray<PricingWarning> Warnings,
    PricingError? Error = null);

public sealed record RebatePricingStageResult(
    Money TotalCost,
    ImmutableArray<PriceComponent> Components,
    ImmutableArray<ExpirationDateSource> ExpirationSources,
    ImmutableArray<PricingWarning> Warnings,
    PricingError? Error = null);

public sealed record SellPricingStageResult(
    PricingContext Context,
    Money UnitPrice,
    Money TotalSell,
    ImmutableArray<PriceComponent> Components,
    ImmutableArray<ExpirationDateSource> ExpirationSources,
    ImmutableArray<PricingWarning> Warnings,
    PricingError? Error = null);

public sealed record FeePricingStageResult(
    Money TotalCost,
    Money UnitPrice,
    Money TotalSell,
    ImmutableArray<PriceComponent> Components,
    ImmutableArray<ExpirationDateSource> ExpirationSources,
    ImmutableArray<PricingWarning> Warnings,
    PricingError? Error = null);

public interface IPricingContextStage
{
    ValueTask<PricingContextStageResult> ExecuteAsync(PricingRequest request, CancellationToken cancellationToken);
}

public interface ICostPricingStage
{
    ValueTask<CostPricingStageResult> ExecuteAsync(PricingContext context, CancellationToken cancellationToken);
}

public interface IRebatePricingStage
{
    ValueTask<RebatePricingStageResult> ExecuteAsync(
        PricingContext context,
        Money totalCost,
        CancellationToken cancellationToken);
}

public interface ISellPricingStage
{
    ValueTask<SellPricingStageResult> ExecuteAsync(
        PricingContext context,
        Money totalCost,
        CancellationToken cancellationToken);
}

public interface IFeePricingStage
{
    ValueTask<FeePricingStageResult> ExecuteAsync(
        PricingContext context,
        Money totalCost,
        Money unitPrice,
        Money totalSell,
        CancellationToken cancellationToken);
}

/// <summary>Immutable inputs accumulated by the ordered regular-item workflow.</summary>
public sealed record PricingResultSnapshot(
    PricingRequest Request,
    PricingContext? Context,
    Money? Cost,
    Money? UnitPrice,
    Money? TotalSell,
    AccountRoundingConfiguration RoundingConfiguration,
    ImmutableArray<PriceComponent> Components,
    ImmutableArray<ExpirationDateSource> ExpirationSources,
    ImmutableArray<PricingWarning> Warnings,
    ImmutableArray<PricingError> Errors);

/// <summary>Builds the explainable domain result after calculation stages finish or fail.</summary>
public static class PricingResultFactory
{
    public static PricingResult Create(PricingResultSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        ImmutableArray<PriceComponent> components = Normalize(snapshot.Components);
        ImmutableArray<ExpirationDateSource> expirationSources = Normalize(snapshot.ExpirationSources);
        ImmutableArray<PricingWarning> warnings = Normalize(snapshot.Warnings);
        ImmutableArray<PricingError> errors = Normalize(snapshot.Errors);

        Money? sellPrice = snapshot.TotalSell;
        if (errors.IsEmpty && snapshot.UnitPrice is { } unitPrice && snapshot.TotalSell is { } totalSell)
        {
            FinalSellRoundingResult rounded = CobolRoundingPolicy.ApplyFinal(
                unitPrice,
                totalSell,
                snapshot.RoundingConfiguration);
            sellPrice = rounded.TotalSell;
        }

        ExpirationDateCollectionResult expiration = ExpirationDateCollector.Collect(
            snapshot.Request.PricingDate,
            expirationSources);
        if (expiration.Error is not null)
        {
            errors = errors.Add(expiration.Error);
        }

        ImmutableArray<RuleProvenance> provenance = components
            .Select(component => component.Provenance)
            .Concat(expirationSources.Select(source => source.Provenance))
            .Concat(Selections(snapshot.Context))
            .Distinct()
            .ToImmutableArray();

        return new PricingResult(
            snapshot.Context?.Product.ProductType ?? ProductType.Regular,
            snapshot.Cost,
            sellPrice,
            expiration.ExpirationDate,
            snapshot.Context?.CostSelection as ContractSelection,
            snapshot.Context?.SellArrangementSelection,
            components,
            provenance,
            warnings,
            errors);
    }

    private static IEnumerable<RuleProvenance> Selections(PricingContext? context)
    {
        if (context?.CostSelection is { } cost)
        {
            yield return cost.Provenance;
        }

        if (context?.SellArrangementSelection is { } sell)
        {
            yield return sell.Provenance;
        }
    }

    private static ImmutableArray<T> Normalize<T>(ImmutableArray<T> values) => values.IsDefault ? [] : values;
}

/// <summary>
/// Coordinates A6U01's regular-item order: context, cost, rebate, sell, fees, then rounding/date resolution.
/// </summary>
public sealed class PricingOrchestrator : IPricingOrchestrator
{
    private readonly IPricingContextStage contextStage;
    private readonly ICostPricingStage costStage;
    private readonly IRebatePricingStage rebateStage;
    private readonly ISellPricingStage sellStage;
    private readonly IFeePricingStage feeStage;

    public PricingOrchestrator(
        IPricingContextStage contextStage,
        ICostPricingStage costStage,
        IRebatePricingStage rebateStage,
        ISellPricingStage sellStage,
        IFeePricingStage feeStage)
    {
        this.contextStage = contextStage ?? throw new ArgumentNullException(nameof(contextStage));
        this.costStage = costStage ?? throw new ArgumentNullException(nameof(costStage));
        this.rebateStage = rebateStage ?? throw new ArgumentNullException(nameof(rebateStage));
        this.sellStage = sellStage ?? throw new ArgumentNullException(nameof(sellStage));
        this.feeStage = feeStage ?? throw new ArgumentNullException(nameof(feeStage));
    }

    public async ValueTask<PricingResult> PriceAsync(
        PricingOperation operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();

        var accumulator = new Accumulator(operation);
        PricingContextStageResult contextResult = await contextStage
            .ExecuteAsync(operation.Request, cancellationToken)
            .ConfigureAwait(false);
        accumulator.Add(contextResult.Warnings, contextResult.Error);
        if (contextResult.Context is null || contextResult.Error is not null)
        {
            return PricingResultFactory.Create(accumulator.Snapshot());
        }

        PricingContext context = contextResult.Context;
        accumulator.Context = context;
        CostPricingStageResult costResult = await costStage.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
        accumulator.Context = costResult.Context;
        accumulator.Cost = costResult.TotalCost;
        accumulator.Add(costResult.Components, costResult.ExpirationSources, costResult.Warnings, costResult.Error);
        if (costResult.Error is not null)
        {
            return PricingResultFactory.Create(accumulator.Snapshot());
        }

        context = costResult.Context;
        RebatePricingStageResult rebateResult = await rebateStage
            .ExecuteAsync(context, costResult.TotalCost, cancellationToken)
            .ConfigureAwait(false);
        accumulator.Cost = rebateResult.TotalCost;
        accumulator.Add(rebateResult.Components, rebateResult.ExpirationSources, rebateResult.Warnings, rebateResult.Error);
        if (rebateResult.Error is not null)
        {
            return PricingResultFactory.Create(accumulator.Snapshot());
        }

        SellPricingStageResult sellResult = await sellStage
            .ExecuteAsync(context, rebateResult.TotalCost, cancellationToken)
            .ConfigureAwait(false);
        accumulator.Context = sellResult.Context;
        accumulator.UnitPrice = sellResult.UnitPrice;
        accumulator.TotalSell = sellResult.TotalSell;
        accumulator.Add(sellResult.Components, sellResult.ExpirationSources, sellResult.Warnings, sellResult.Error);
        if (sellResult.Error is not null)
        {
            return PricingResultFactory.Create(accumulator.Snapshot());
        }

        FeePricingStageResult feeResult = await feeStage.ExecuteAsync(
            sellResult.Context,
            rebateResult.TotalCost,
            sellResult.UnitPrice,
            sellResult.TotalSell,
            cancellationToken).ConfigureAwait(false);
        accumulator.Cost = feeResult.TotalCost;
        accumulator.UnitPrice = feeResult.UnitPrice;
        accumulator.TotalSell = feeResult.TotalSell;
        accumulator.Add(feeResult.Components, feeResult.ExpirationSources, feeResult.Warnings, feeResult.Error);

        return PricingResultFactory.Create(accumulator.Snapshot());
    }

    private sealed class Accumulator(PricingOperation operation)
    {
        private readonly ImmutableArray<PriceComponent>.Builder components = ImmutableArray.CreateBuilder<PriceComponent>();
        private readonly ImmutableArray<ExpirationDateSource>.Builder expirations = ImmutableArray.CreateBuilder<ExpirationDateSource>();
        private readonly ImmutableArray<PricingWarning>.Builder warnings = ImmutableArray.CreateBuilder<PricingWarning>();
        private readonly ImmutableArray<PricingError>.Builder errors = ImmutableArray.CreateBuilder<PricingError>();

        public PricingContext? Context { get; set; }
        public Money? Cost { get; set; }
        public Money? UnitPrice { get; set; }
        public Money? TotalSell { get; set; }

        public void Add(ImmutableArray<PricingWarning> stageWarnings, PricingError? error)
        {
            warnings.AddRange(Normalize(stageWarnings));
            if (error is not null)
            {
                errors.Add(error);
            }
        }

        public void Add(
            ImmutableArray<PriceComponent> stageComponents,
            ImmutableArray<ExpirationDateSource> stageExpirations,
            ImmutableArray<PricingWarning> stageWarnings,
            PricingError? error)
        {
            components.AddRange(Normalize(stageComponents));
            expirations.AddRange(Normalize(stageExpirations));
            Add(stageWarnings, error);
        }

        public PricingResultSnapshot Snapshot() => new(
            operation.Request,
            Context,
            Cost,
            UnitPrice,
            TotalSell,
            operation.RoundingConfiguration,
            components.ToImmutable(),
            expirations.ToImmutable(),
            warnings.ToImmutable(),
            errors.ToImmutable());

        private static ImmutableArray<T> Normalize<T>(ImmutableArray<T> values) => values.IsDefault ? [] : values;
    }
}
