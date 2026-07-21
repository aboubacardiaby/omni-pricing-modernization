namespace Pricing.UnitTests.Rebates;

using Pricing.Application.Rebates;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class RebateCalculatorTests
{
    [Fact]
    public void NormalRebateUsesDealerCostAndRoundsAtCobolOutputStage()
    {
        RebateCalculationResult result = RebateCalculator.Calculate(Input(
            dealerCost: 12.34567m,
            contractCost: 10.11111m));

        Assert.True(result.WasCalculated);
        Assert.Equal(12.34567m, result.RebateBaseCost.Value);
        Assert.Equal(2.2346m, result.ContractRebate.Value);
        Assert.Equal(2.2346m, result.TotalRebate.Value);
        Assert.Equal(2, result.Components.Length);
        Assert.Equal("A6U01 7070-PRO-REBT-AMTS-010", result.Provenance?.Source);
    }

    [Theory]
    [InlineData("06")]
    [InlineData("08")]
    public void FixedRebateEntryMethodsBypassDerivedCalculation(string entryMethod)
    {
        RebateCalculationResult result = RebateCalculator.Calculate(Input(entryMethod: entryMethod));

        Assert.False(result.WasCalculated);
        Assert.Contains("fixed rebate", result.SkipReason, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.Components);
    }

    [Fact]
    public void BestCostFlagUsesLevel01DealerCost()
    {
        RebateCalculationResult result = RebateCalculator.Calculate(Input(
            dealerCost: 20m,
            level01DealerCost: 15m,
            contractCost: 10m,
            useBestCost: true));

        Assert.Equal(15m, result.RebateBaseCost.Value);
        Assert.Equal(5m, result.TotalRebate.Value);
    }

    [Fact]
    public void VendorNetCostPercentageAppliesOnlyToStandardBranch()
    {
        RebateCalculationResult standard = RebateCalculator.Calculate(Input(
            dealerCost: 20m,
            contractCost: 10m,
            applyNetRebate: true,
            netRebatePercentage: 0.25m));
        RebateCalculationResult broker = RebateCalculator.Calculate(Input(
            dealerCost: 20m,
            contractCost: 10m,
            suggestedSell: 14m,
            brokerPercentage: 0.1m,
            applyNetRebate: true,
            netRebatePercentage: 0.25m));

        Assert.Equal(7.5m, standard.TotalRebate.Value);
        Assert.Equal(10m, broker.TotalRebate.Value);
        Assert.Equal(4m, broker.VendorDistributorRebate.Value);
        Assert.Equal(6m, broker.SellCostDifference.Value);
    }

    [Fact]
    public void CostPlusRunsAfterBrokerWhenBothApply()
    {
        RebateCalculationResult result = RebateCalculator.Calculate(Input(
            dealerCost: 20m,
            contractCost: 10m,
            suggestedSell: 14m,
            brokerPercentage: 0.1m,
            costPlusPercentage: 0.2m));

        Assert.Equal(10m, result.ContractRebate.Value);
        Assert.Equal(4m, result.VendorDistributorRebate.Value);
        Assert.Equal(6m, result.SellCostDifference.Value);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, -5)]
    public void NegativeRebateRequiresVendorOptIn(bool allowNegative, decimal expected)
    {
        RebateCalculationResult result = RebateCalculator.Calculate(Input(
            dealerCost: 5m,
            contractCost: 10m,
            allowNegative: allowNegative));

        Assert.Equal(expected, result.TotalRebate.Value);
    }

    [Fact]
    public void ZeroMaximumMeansUncappedAndPositiveMaximumCapsTotal()
    {
        RebateCalculationResult uncapped = RebateCalculator.Calculate(Input(
            dealerCost: 20m,
            contractCost: 10m,
            maximumRebate: 0m));
        RebateCalculationResult capped = RebateCalculator.Calculate(Input(
            dealerCost: 20m,
            contractCost: 10m,
            maximumRebate: 3m));

        Assert.Equal(10m, uncapped.TotalRebate.Value);
        Assert.Equal(3m, capped.TotalRebate.Value);
        Assert.Equal(10m, capped.ContractRebate.Value);
    }

    [Theory]
    [InlineData("2026-01-01", -2)]
    [InlineData("2026-12-31", -2)]
    [InlineData("2025-12-31", 0)]
    [InlineData("2027-01-01", 0)]
    public void ProtectedAcquisitionUsesInclusiveWindowAndMayBeNegative(
        string pricingDate,
        decimal expected)
    {
        RebateCalculationResult result = RebateCalculator.Calculate(Input(
            pricingDate: DateOnly.Parse(pricingDate, System.Globalization.CultureInfo.InvariantCulture),
            dealerCost: 12m,
            acquisitionCost: 8m,
            protectedAcquisitionCost: 10m,
            protectionDates: new PricingDateRange(
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 12, 31))));

        Assert.Equal(expected, result.PriceProtection.Value);
        Assert.Equal(expected == 0 ? null : new DateOnly(2026, 12, 31), result.PriceProtectionExpirationDate);
        Assert.Equal(2m, result.TotalRebate.Value);
    }

    [Fact]
    public void ProtectionDoesNotApplyWhenProtectedCostEqualsRebateBase()
    {
        RebateCalculationResult result = RebateCalculator.Calculate(Input(
            dealerCost: 10m,
            acquisitionCost: 15m,
            protectedAcquisitionCost: 10m,
            protectionDates: ActiveDates()));

        Assert.Equal(0m, result.PriceProtection.Value);
        Assert.Null(result.PriceProtectionExpirationDate);
    }

    [Fact]
    public void NoCostContractDoesNotCalculateRebate()
    {
        RebateCalculationResult result = RebateCalculator.Calculate(Input(hasContract: false));

        Assert.False(result.WasCalculated);
        Assert.Contains("No cost contract", result.SkipReason, StringComparison.Ordinal);
    }

    private static RebateCalculationInput Input(
        bool hasContract = true,
        string entryMethod = "01",
        DateOnly? pricingDate = null,
        decimal contractCost = 10m,
        decimal dealerCost = 12m,
        decimal level01DealerCost = 12m,
        bool useBestCost = false,
        decimal acquisitionCost = 12m,
        decimal suggestedSell = 0m,
        decimal brokerPercentage = 0m,
        decimal costPlusPercentage = 0m,
        bool applyNetRebate = false,
        decimal netRebatePercentage = 0m,
        bool allowNegative = false,
        decimal protectedAcquisitionCost = 0m,
        PricingDateRange? protectionDates = null,
        decimal maximumRebate = 0m) => new(
        hasContract,
        entryMethod,
        pricingDate ?? new DateOnly(2026, 7, 21),
        new Money(contractCost),
        new Money(dealerCost),
        new Money(level01DealerCost),
        useBestCost,
        new Money(acquisitionCost),
        new Money(suggestedSell),
        brokerPercentage,
        costPlusPercentage,
        applyNetRebate,
        netRebatePercentage,
        allowNegative,
        new Money(protectedAcquisitionCost),
        protectionDates,
        new Money(maximumRebate));

    private static PricingDateRange ActiveDates() =>
        new(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
}
