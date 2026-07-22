namespace Pricing.UnitTests.Rounding;

using System.Globalization;
using Pricing.Application.Rounding;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class CobolRoundingPolicyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("N")]
    public void NoRoundingCodesPreserveFullPrecision(string? code)
    {
        AccountRoundingConfiguration configuration = AccountRoundingConfiguration.FromLegacyCode(code);

        FinalSellRoundingResult result = CobolRoundingPolicy.ApplyFinal(
            new Money(12.34567891m),
            new Money(24.69135782m),
            configuration);

        Assert.True(configuration.IsRecognized);
        Assert.Equal(AccountRoundingMode.None, configuration.Mode);
        Assert.Equal(12.34567891m, result.UnitPrice.Value);
        Assert.Equal(24.69135782m, result.TotalSell.Value);
        Assert.Equal(result.UnroundedUnitPrice, result.UnitPrice);
        Assert.Equal(result.UnroundedTotalSell, result.TotalSell);
    }

    [Fact]
    public void NormalModeRoundsEachHeadlineValueIndependentlyToTwoDecimals()
    {
        FinalSellRoundingResult result = Apply("R", 1.234m, 3.455m);

        Assert.Equal(1.23m, result.UnitPrice.Value);
        Assert.Equal(3.46m, result.TotalSell.Value);
        Assert.Equal(1.234m, result.UnroundedUnitPrice.Value);
        Assert.Equal(3.455m, result.UnroundedTotalSell.Value);
    }

    [Theory]
    [InlineData("1.23000001", "1.23")]
    [InlineData("1.2301", "1.24")]
    [InlineData("1.2300", "1.23")]
    [InlineData("1.2399", "1.24")]
    public void UpModeUsesExactCobolPointZeroZeroFourNineBias(string input, string expected)
    {
        FinalSellRoundingResult result = Apply("Y", Parse(input), Parse(input));

        Assert.Equal(Parse(expected), result.UnitPrice.Value);
        Assert.Equal(Parse(expected), result.TotalSell.Value);
    }

    [Fact]
    public void UpModePreservesCobolFormulaForNegativeValuesRatherThanSubstitutingCeiling()
    {
        FinalSellRoundingResult result = Apply("Y", -1.235m, -1.2301m);

        Assert.Equal(-1.23m, result.UnitPrice.Value);
        Assert.Equal(-1.23m, result.TotalSell.Value);
    }

    [Fact]
    public void UnknownCodeSilentlyUsesNoRoundingButRemainsObservable()
    {
        AccountRoundingConfiguration configuration = AccountRoundingConfiguration.FromLegacyCode("X");
        FinalSellRoundingResult result = CobolRoundingPolicy.ApplyFinal(
            new Money(1.23456789m),
            new Money(2.34567891m),
            configuration);

        Assert.False(configuration.IsRecognized);
        Assert.Equal(AccountRoundingMode.None, configuration.Mode);
        Assert.Equal(1.23456789m, result.UnitPrice.Value);
        Assert.Equal("X", result.Configuration.LegacyCode);
    }

    [Theory]
    [InlineData("1.234567885", "1.23456789")]
    [InlineData("-1.234567885", "-1.23456789")]
    public void IntermediateRoundingUsesApprovedHalfAwayFromZeroAssumption(string input, string expected)
    {
        decimal actual = CobolRoundingPolicy.RoundIntermediate(Parse(input));

        Assert.Equal(Parse(expected), actual);
    }

    [Theory]
    [InlineData("1.234567899", "1.23456789")]
    [InlineData("-1.234567899", "-1.23456789")]
    public void ExplicitTruncationPreservesSurchargeException(string input, string expected)
    {
        decimal actual = CobolRoundingPolicy.TruncateIntermediate(Parse(input));

        Assert.Equal(Parse(expected), actual);
    }

    [Fact]
    public void IntermediateThenFinalRoundingCanDifferFromRoundingAnUnstagedFormula()
    {
        decimal staged = CobolRoundingPolicy.RoundIntermediate(1.2349m, 3);
        FinalSellRoundingResult stagedResult = Apply("R", staged, staged);
        FinalSellRoundingResult directResult = Apply("R", 1.2349m, 1.2349m);

        Assert.Equal(1.235m, staged);
        Assert.Equal(1.24m, stagedResult.UnitPrice.Value);
        Assert.Equal(1.23m, directResult.UnitPrice.Value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(9)]
    public void UnsupportedIntermediateScaleIsRejected(int scale)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CobolRoundingPolicy.RoundIntermediate(1m, scale));
        Assert.Throws<ArgumentOutOfRangeException>(() => CobolRoundingPolicy.TruncateIntermediate(1m, scale));
    }

    private static FinalSellRoundingResult Apply(string code, decimal unitPrice, decimal totalSell) =>
        CobolRoundingPolicy.ApplyFinal(
            new Money(unitPrice),
            new Money(totalSell),
            AccountRoundingConfiguration.FromLegacyCode(code));

    private static decimal Parse(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);
}
