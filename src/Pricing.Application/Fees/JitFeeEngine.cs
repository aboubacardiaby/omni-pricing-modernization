namespace Pricing.Application.Fees;

using System.Collections.Immutable;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

public enum JitChargeType { Percentage, Rate, PerItem }

public sealed record JitItemCharge(
    bool Applies,
    JitChargeType ChargeType,
    decimal Rate,
    Quantity Quantity,
    RuleProvenance Provenance,
    bool LabelUnitOfMeasureFound = false,
    bool AlternateOrderUnitOfMeasureFound = false,
    bool AlternateMatchesOrderUnitOfMeasure = false,
    decimal AlternateOrderConversionFactor = 1m,
    decimal CustomerLabelConversionFactor = 1m,
    Money? FeeComponentAmount = null,
    bool UseFeeComponentAmount = false);

public sealed record JitServiceTerms(
    decimal ServicePercentage,
    decimal LowUnitOfMeasurePercentage,
    decimal ExtraDeliveryPercentage,
    decimal NonOmSelectPercentage,
    bool IsNonOmSelectItem,
    RuleProvenance Provenance);

public sealed record JitFeeInput(
    string ServiceFeeCode,
    bool IsJitCustomer,
    bool IsContractLineExempt,
    bool IsPrivateLabelO,
    Money TotalCost,
    Money SellBeforeAdjustment,
    JitItemCharge? Label,
    JitItemCharge? ApplyLabel,
    JitServiceTerms Service,
    Money BreakBulkOrLowUomFee,
    RuleProvenance BreakBulkProvenance,
    bool LowUnitOfMeasureEligible = false,
    bool IsStockOrder = false);

public sealed record JitFeeResult(
    Money ServiceFee,
    Money LabelFee,
    Money ApplyLabelFee,
    Money BreakBulkFee,
    Money LowUnitOfMeasureFee,
    Money ExtraDeliveryFee,
    Money NonOmSelectFee,
    Money CostTotal,
    Money SellTotal,
    bool RemovedFromLineItem,
    bool RequestsLowUnitOfMeasureCalculation,
    ImmutableArray<PriceComponent> Components);

/// <summary>Implements A6U01 7225 JIT A/C/P/R calculations and routing.</summary>
public static class JitFeeEngine
{
    public static JitFeeResult Calculate(JitFeeInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!input.IsJitCustomer || input.IsContractLineExempt || !IsRecognized(input.ServiceFeeCode))
        {
            return Empty();
        }

        bool costBased = (input.ServiceFeeCode is "A" or "C") && !input.IsPrivateLabelO;
        Money basis = costBased ? input.TotalCost : input.SellBeforeAdjustment;
        Money label = CalculateItem(input.Label, basis);
        Money apply = CalculateItem(input.ApplyLabel, basis);
        Money service = Percent(basis, input.Service.ServicePercentage);
        Money lowUom = Percent(basis, input.Service.LowUnitOfMeasurePercentage);
        Money extraDelivery = Percent(basis, input.Service.ExtraDeliveryPercentage);
        Money nonOm = input.Service.IsNonOmSelectItem ? Percent(basis, input.Service.NonOmSelectPercentage) : new Money(0m);
        service = Add(service, nonOm);

        Money breakBulk = input.BreakBulkOrLowUomFee;
        bool requestsLowUom = breakBulk.Value == 0m && input.LowUnitOfMeasureEligible && input.IsStockOrder;
        Money total = Sum(service, label, apply, breakBulk);
        Money costTotal = costBased ? total : new Money(0m);
        Money sellTotal = costBased ? new Money(0m) : total;
        bool removed = (input.ServiceFeeCode == "A" && costTotal.Value > 0m) || input.ServiceFeeCode == "R";

        var components = ImmutableArray.CreateBuilder<PriceComponent>();
        AddComponent("JIT service", service, input.Service.Provenance);
        AddComponent("JIT label", label, input.Label?.Provenance);
        AddComponent("JIT apply label", apply, input.ApplyLabel?.Provenance);
        AddComponent("JIT break-bulk/low-UOM", breakBulk, input.BreakBulkProvenance);
        return new(service, label, apply, breakBulk, lowUom, extraDelivery, nonOm, costTotal, sellTotal, removed, requestsLowUom, components.ToImmutable());

        void AddComponent(string name, Money amount, RuleProvenance? provenance)
        {
            if (amount.Value != 0m && provenance is not null)
            {
                components.Add(new PriceComponent(name, PriceComponentType.Fee, amount, provenance));
            }
        }
    }

    private static Money CalculateItem(JitItemCharge? charge, Money basis)
    {
        if (charge is null || !charge.Applies || charge.Rate <= 0m) return new Money(0m);
        if (charge.UseFeeComponentAmount) return charge.FeeComponentAmount ?? new Money(0m);

        decimal raw = charge.ChargeType switch
        {
            JitChargeType.Percentage => basis.Value * charge.Rate,
            JitChargeType.PerItem => charge.Rate,
            JitChargeType.Rate => Rate(charge),
            _ => 0m,
        };
        return MoneyOf(raw);
    }

    private static decimal Rate(JitItemCharge charge)
    {
        if (charge.LabelUnitOfMeasureFound && charge.CustomerLabelConversionFactor <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(charge), "Customer label conversion factor must be positive.");
        }

        decimal quantity = charge.Quantity.Value;
        if (charge.LabelUnitOfMeasureFound && charge.AlternateOrderUnitOfMeasureFound && charge.AlternateMatchesOrderUnitOfMeasure)
            return charge.Rate * charge.AlternateOrderConversionFactor * quantity / charge.CustomerLabelConversionFactor;
        if (charge.LabelUnitOfMeasureFound)
            return charge.Rate * quantity / charge.CustomerLabelConversionFactor;
        return charge.Rate * quantity;
    }

    private static bool IsRecognized(string code) => code is "A" or "C" or "P" or "R";
    private static Money Percent(Money basis, decimal rate) => rate > 0m ? MoneyOf(basis.Value * rate) : new Money(0m);
    private static Money Add(Money left, Money right) => MoneyOf(left.Value + right.Value);
    private static Money Sum(params Money[] values) => MoneyOf(values.Sum(value => value.Value));
    private static Money MoneyOf(decimal value) => new(decimal.Round(value, Money.MaximumScale, MidpointRounding.AwayFromZero));
    private static JitFeeResult Empty() => new(new(0m), new(0m), new(0m), new(0m), new(0m), new(0m), new(0m), new(0m), new(0m), false, false, []);
}
