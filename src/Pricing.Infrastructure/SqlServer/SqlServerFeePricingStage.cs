namespace Pricing.Infrastructure.SqlServer;

using System.Collections.Immutable;
using Pricing.Application.ExpirationDates;
using Pricing.Application.Fees;
using Pricing.Application.Orchestration;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>Concrete A6U01 7195/7200/7210 fee composition stage.</summary>
public sealed class SqlServerFeePricingStage(
    FreightEngine freight,
    LowUomBreakBulkEngine lowUom,
    PandacEngine pandac,
    SurchargeEngine surcharge) : IFeePricingStage
{
    public async ValueTask<FeePricingStageResult> ExecuteAsync(
        PricingContext context, Money totalCost, Money unitPrice, Money totalSell,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        var components = ImmutableArray.CreateBuilder<PriceComponent>();
        var expirations = ImmutableArray.CreateBuilder<ExpirationDateSource>();
        Money cost = totalCost;
        Money sell = unitPrice;

        FreightResult freightResult = await freight.CalculateAsync(new FreightInput(
            context, context.CostSelection?.UnitCost ?? totalCost, totalCost, null,
            FreightApplicationMode.Cost,
            new FreightExemptionFacts(false, true, ContractClass(context), true, true, true, true),
            FreightDisabled: context.Customer.Freight is { InboundFreight: false }),
            cancellationToken).ConfigureAwait(false);
        if (freightResult.Error is not null) return Failed(cost, sell, totalSell, freightResult.Error);
        Add(freightResult.Component);
        if (freightResult.FoldIntoCost) cost = Sum(cost, freightResult.Amount);
        if (freightResult.FoldIntoSell) sell = Sum(sell, freightResult.Amount);
        AddExpiration("Inbound freight", freightResult.EffectiveDates, freightResult.Component?.Provenance);

        LowUnitOfMeasureConfiguration? low = context.Customer.LowUnitOfMeasure;
        if (low is not null)
        {
            LowUomChargeSource source = new(LowUomSourceScope.Account, low.Percentage, low.Percentage,
                new RuleProvenance("low-uom", low.Source, "ACCOUNT", DateRange(low.EffectiveDate)));
            LowUomResult lowResult = await lowUom.CalculateAsync(new LowUomInput(
                context, low.IsEligible, true, false, context.Request.Quantity,
                context.Request.UnitOfMeasure, null, 1m, LowUomDesignator.LowUnitOfMeasure,
                [source], cost, sell, "", false), cancellationToken).ConfigureAwait(false);
            if (lowResult.Error is not null) return Failed(cost, sell, totalSell, lowResult.Error);
            Add(lowResult.Component);
            sell = Sum(sell, lowResult.Amount);
        }

        PandacResult pandacResult = await pandac.CalculateAsync(new PandacInput(
            context, true, true, !string.IsNullOrWhiteSpace(context.Request.ShipTo),
            cost, sell), cancellationToken).ConfigureAwait(false);
        if (pandacResult.Error is not null) return Failed(cost, sell, totalSell, pandacResult.Error);
        if (pandacResult.FoldIntoLine) sell = Sum(sell, pandacResult.Amount);
        Add(pandacResult.Component);
        AddExpiration("PANDAC", pandacResult.EffectiveDates, pandacResult.Component?.Provenance);

        SurchargeResult surchargeResult = await surcharge.CalculateAsync(new SurchargeInput(
            context, cost, false, context.Product.ProductCategory is not null, null),
            cancellationToken).ConfigureAwait(false);
        if (surchargeResult.SoftError is not null) return Failed(cost, sell, totalSell, surchargeResult.SoftError);
        if (surchargeResult.FoldIntoLine) sell = Sum(sell, surchargeResult.Amount);
        Add(surchargeResult.Component);

        Money extended = new(decimal.Round(sell.Value * context.Request.Quantity.Value,
            Money.MaximumScale, MidpointRounding.AwayFromZero));
        return new FeePricingStageResult(cost, sell, extended, components.ToImmutable(), expirations.ToImmutable(), []);

        void Add(PriceComponent? component)
        {
            if (component is not null) components.Add(component);
        }
        void AddExpiration(string name, PricingDateRange? dates, RuleProvenance? provenance)
        {
            if (dates?.ExpirationDate is { } expiration && provenance is not null)
                expirations.Add(new ExpirationDateSource(name, expiration, provenance));
        }
    }

    private static FreightContractClass ContractClass(PricingContext context) =>
        context.CostSelection switch
        {
            ContractSelection { BuyingGroupId: not null } => FreightContractClass.SanctionedGroup,
            ContractSelection => FreightContractClass.Individual,
            _ => FreightContractClass.NonContract,
        };
    private static PricingDateRange? DateRange(DateOnly? effective) =>
        effective is { } date ? new PricingDateRange(date, null) : null;
    private static Money Sum(Money left, Money right) =>
        new(decimal.Round(left.Value + right.Value, Money.MaximumScale, MidpointRounding.AwayFromZero));
    private static FeePricingStageResult Failed(Money cost, Money unitPrice, Money totalSell, PricingError error) =>
        new(cost, unitPrice, totalSell, [], [], [], error);
}
