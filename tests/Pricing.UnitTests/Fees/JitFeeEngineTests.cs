namespace Pricing.UnitTests.Fees;

using Pricing.Application.Fees;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class JitFeeEngineTests
{
    [Theory]
    [InlineData("A", 29, 0, true)]
    [InlineData("C", 29, 0, false)]
    [InlineData("P", 0, 57, false)]
    [InlineData("R", 0, 57, true)]
    public void RoutesRecognizedCodesToConfirmedCostOrSellBucket(string code, decimal cost, decimal sell, bool removed)
    {
        JitFeeResult result = JitFeeEngine.Calculate(Input(code));
        Assert.Equal(cost, result.CostTotal.Value);
        Assert.Equal(sell, result.SellTotal.Value);
        Assert.Equal(removed, result.RemovedFromLineItem);
    }

    [Fact]
    public void PrivateLabelOForcesCostCodesOntoSellBasis()
    {
        JitFeeResult result = JitFeeEngine.Calculate(Input("C") with { IsPrivateLabelO = true });
        Assert.Equal(0m, result.CostTotal.Value);
        Assert.Equal(57m, result.SellTotal.Value);
    }

    [Theory]
    [InlineData(false, false, "P")]
    [InlineData(true, true, "P")]
    [InlineData(true, false, "X")]
    public void CustomerExemptionAndUnknownCodeAreSilentNoOps(bool customer, bool exempt, string code)
    {
        JitFeeResult result = JitFeeEngine.Calculate(Input(code) with { IsJitCustomer = customer, IsContractLineExempt = exempt });
        Assert.Equal(0m, result.CostTotal.Value);
        Assert.Equal(0m, result.SellTotal.Value);
        Assert.Empty(result.Components);
    }

    [Theory]
    [InlineData(JitChargeType.Percentage, 0.1, 10)]
    [InlineData(JitChargeType.Rate, 2, 6)]
    [InlineData(JitChargeType.PerItem, 7, 7)]
    public void LabelSupportsPercentageRateAndPerItem(JitChargeType type, decimal rate, decimal expected)
    {
        JitItemCharge label = Charge(type, rate);
        JitFeeResult result = JitFeeEngine.Calculate(Input("C") with { Label = label, Service = EmptyService() });
        Assert.Equal(expected, result.LabelFee.Value);
    }

    [Fact]
    public void RateUsesConfirmedLabelAndAlternateUomFormula()
    {
        JitItemCharge label = Charge(JitChargeType.Rate, 2m) with
        {
            LabelUnitOfMeasureFound = true,
            AlternateOrderUnitOfMeasureFound = true,
            AlternateMatchesOrderUnitOfMeasure = true,
            AlternateOrderConversionFactor = 4m,
            CustomerLabelConversionFactor = 2m,
        };
        JitFeeResult result = JitFeeEngine.Calculate(Input("P") with { Label = label, Service = EmptyService() });
        Assert.Equal(12m, result.LabelFee.Value);
    }

    [Fact]
    public void ApplyLabelFeeComponentAmountSupersedesLegacyFormula()
    {
        JitItemCharge apply = Charge(JitChargeType.Percentage, 0.5m) with { UseFeeComponentAmount = true, FeeComponentAmount = new Money(7m) };
        JitFeeResult result = JitFeeEngine.Calculate(Input("P") with { ApplyLabel = apply, Service = EmptyService() });
        Assert.Equal(7m, result.ApplyLabelFee.Value);
    }

    [Fact]
    public void ServiceFamilyUsesSameBasisAndFoldsNonOmIntoServiceOnly()
    {
        JitFeeResult result = JitFeeEngine.Calculate(Input("C") with { Label = null, ApplyLabel = null, BreakBulkOrLowUomFee = new Money(0m) });
        Assert.Equal(13m, result.ServiceFee.Value);
        Assert.Equal(2m, result.LowUnitOfMeasureFee.Value);
        Assert.Equal(3m, result.ExtraDeliveryFee.Value);
        Assert.Equal(3m, result.NonOmSelectFee.Value);
        Assert.Equal(13m, result.CostTotal.Value);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public void RequestsT041CalculationOnlyForEligibleStockWithZeroBreakFee(bool eligible, bool stock, bool expected)
    {
        JitFeeInput input = Input("P") with { LowUnitOfMeasureEligible = eligible, IsStockOrder = stock, BreakBulkOrLowUomFee = new Money(0m) };
        Assert.Equal(expected, JitFeeEngine.Calculate(input).RequestsLowUnitOfMeasureCalculation);
    }

    [Fact]
    public void PreservesEightDecimalComputeRoundingAndProvenance()
    {
        JitFeeInput input = Input("C") with { Label = Charge(JitChargeType.Percentage, 0.3333m), ApplyLabel = null, Service = EmptyService() };
        JitFeeResult result = JitFeeEngine.Calculate(input);
        Assert.Equal(33.33m, result.LabelFee.Value);
        Assert.Contains(result.Components, component => component.Name == "JIT label" && component.Provenance.Source == "A6U01 7225");
    }

    private static JitFeeInput Input(string code) => new(
        code, true, false, false, new Money(100m), new Money(200m),
        Charge(JitChargeType.Percentage, 0.1m), Charge(JitChargeType.Percentage, 0.05m),
        new JitServiceTerms(0.1m, 0.02m, 0.03m, 0.03m, true, Provenance()),
        new Money(1m), Provenance());

    private static JitItemCharge Charge(JitChargeType type, decimal rate) =>
        new(true, type, rate, new Quantity(3m), Provenance());

    private static JitServiceTerms EmptyService() => new(0m, 0m, 0m, 0m, false, Provenance());
    private static RuleProvenance Provenance() => new("JIT", "A6U01 7225", "ACCOUNT", null);
}
