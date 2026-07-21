namespace Pricing.Application.Rebates;

using System.Collections.Immutable;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>Calculates contract rebate outputs in A6U01 source order.</summary>
/// <remarks>
/// COBOL: A6U01 0030 skips methods 06/08; 7070-PRO-REBT-AMTS-010 selects the
/// rebate base, runs standard/broker/cost-plus branches sequentially, calculates
/// price protection, and caps total contract rebate.
/// </remarks>
public static class RebateCalculator
{
    private const int OutputScale = 4;

    public static RebateCalculationResult Calculate(RebateCalculationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!input.HasCostContract)
        {
            return RebateCalculationResult.NotCalculated("No cost contract was selected.");
        }

        if (input.ContractEntryMethod is "06" or "08")
        {
            return RebateCalculationResult.NotCalculated(
                $"Contract entry method {input.ContractEntryMethod} carries a fixed rebate and bypasses derived rebate processing.");
        }

        Money rebateBase = input.UseBestCostForRebate ? input.Level01DealerCost : input.DealerCost;
        decimal contractRebate = 0m;
        decimal vendorDistributorRebate = 0m;
        decimal sellCostDifference = 0m;

        bool standardApplies = input.BrokerPercentage == 0m
            || input.SuggestedSell.Value == 0m
            || input.CostPlusPercentage == 0m;
        if (standardApplies)
        {
            contractRebate = Round(rebateBase.Value - input.ContractUnitCost.Value);
            if (input.ApplyVendorNetCostRebate)
            {
                contractRebate *= 1m - input.VendorNetCostRebatePercentage;
            }

            contractRebate = ApplyNegativePolicy(contractRebate, input.AllowNegativeRebate);
        }

        if (input.BrokerPercentage > 0m && input.SuggestedSell.Value > 0m)
        {
            (vendorDistributorRebate, sellCostDifference, contractRebate) =
                CalculateNegotiated(input, rebateBase);
        }

        if (input.CostPlusPercentage > 0m && input.SuggestedSell.Value > 0m)
        {
            (vendorDistributorRebate, sellCostDifference, contractRebate) =
                CalculateNegotiated(input, rebateBase);
        }

        decimal priceProtection = 0m;
        DateOnly? priceProtectionExpiration = null;
        if (input.ProtectedAcquisitionCost.Value != 0m
            && input.PriceProtectionDates is { } dates
            && dates.Contains(input.PricingDate)
            && rebateBase.Value != input.ProtectedAcquisitionCost.Value)
        {
            priceProtection = Round(input.AcquisitionCost.Value - input.ProtectedAcquisitionCost.Value);
            priceProtectionExpiration = dates.ExpirationDate;
        }

        decimal totalRebate = Round(contractRebate);
        if (input.MaximumRebate.Value > 0m && input.MaximumRebate.Value < totalRebate)
        {
            totalRebate = input.MaximumRebate.Value;
        }

        var provenance = new RuleProvenance(
            "contract-rebate",
            "A6U01 7070-PRO-REBT-AMTS-010",
            "cost-contract",
            input.PriceProtectionDates);
        ImmutableArray<PriceComponent> components =
        [
            new("Contract rebate", PriceComponentType.Rebate, new Money(totalRebate), provenance),
            new("Price protection", PriceComponentType.Rebate, new Money(priceProtection), provenance),
        ];

        return new RebateCalculationResult(
            true,
            null,
            rebateBase,
            new Money(contractRebate),
            new Money(vendorDistributorRebate),
            new Money(sellCostDifference),
            new Money(priceProtection),
            new Money(totalRebate),
            priceProtectionExpiration,
            components,
            provenance);
    }

    private static (decimal VendorDistributor, decimal SellCostDifference, decimal ContractRebate)
        CalculateNegotiated(RebateCalculationInput input, Money rebateBase)
    {
        decimal vendorDistributor = Round(input.SuggestedSell.Value - input.ContractUnitCost.Value);
        decimal sellCostDifference = Round(rebateBase.Value - input.SuggestedSell.Value);
        decimal contractRebate = Round(vendorDistributor + sellCostDifference);
        contractRebate = ApplyNegativePolicy(contractRebate, input.AllowNegativeRebate);
        return (vendorDistributor, sellCostDifference, contractRebate);
    }

    private static decimal ApplyNegativePolicy(decimal value, bool allowNegative) =>
        value < 0m && !allowNegative ? 0m : value;

    private static decimal Round(decimal value) =>
        decimal.Round(value, OutputScale, MidpointRounding.AwayFromZero);
}

public sealed record RebateCalculationInput(
    bool HasCostContract,
    string ContractEntryMethod,
    DateOnly PricingDate,
    Money ContractUnitCost,
    Money DealerCost,
    Money Level01DealerCost,
    bool UseBestCostForRebate,
    Money AcquisitionCost,
    Money SuggestedSell,
    decimal BrokerPercentage,
    decimal CostPlusPercentage,
    bool ApplyVendorNetCostRebate,
    decimal VendorNetCostRebatePercentage,
    bool AllowNegativeRebate,
    Money ProtectedAcquisitionCost,
    PricingDateRange? PriceProtectionDates,
    Money MaximumRebate);

public sealed record RebateCalculationResult(
    bool WasCalculated,
    string? SkipReason,
    Money RebateBaseCost,
    Money ContractRebate,
    Money VendorDistributorRebate,
    Money SellCostDifference,
    Money PriceProtection,
    Money TotalRebate,
    DateOnly? PriceProtectionExpirationDate,
    ImmutableArray<PriceComponent> Components,
    RuleProvenance? Provenance)
{
    public static RebateCalculationResult NotCalculated(string reason) => new(
        false,
        reason,
        new Money(0m),
        new Money(0m),
        new Money(0m),
        new Money(0m),
        new Money(0m),
        new Money(0m),
        null,
        [],
        null);
}
