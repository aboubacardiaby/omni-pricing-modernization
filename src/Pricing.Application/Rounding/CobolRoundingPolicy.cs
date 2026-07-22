namespace Pricing.Application.Rounding;

using Pricing.Domain.ValueObjects;

/// <summary>Account-selected final rounding modes from OMGPR-C-ACCT-ROUNDING.</summary>
public enum AccountRoundingMode
{
    None,
    NormalTwoDecimals,
    UpTwoDecimals,
}

/// <summary>Parsed caller-supplied account rounding configuration.</summary>
public sealed record AccountRoundingConfiguration(
    string LegacyCode,
    AccountRoundingMode Mode,
    bool IsRecognized)
{
    /// <remarks>
    /// OMGPR.CPY defines space/N, R, and Y. A6U01 7715 treats every other value as a silent no-op.
    /// </remarks>
    public static AccountRoundingConfiguration FromLegacyCode(string? legacyCode)
    {
        string code = legacyCode ?? string.Empty;
        return code switch
        {
            "" or " " or "N" => new(code, AccountRoundingMode.None, true),
            "R" => new(code, AccountRoundingMode.NormalTwoDecimals, true),
            "Y" => new(code, AccountRoundingMode.UpTwoDecimals, true),
            _ => new(code, AccountRoundingMode.None, false),
        };
    }
}

/// <summary>Final sell values with the pre-rounding values retained for explainability.</summary>
public sealed record FinalSellRoundingResult(
    Money UnitPrice,
    Money TotalSell,
    Money UnroundedUnitPrice,
    Money UnroundedTotalSell,
    AccountRoundingConfiguration Configuration);

/// <summary>COBOL-compatible intermediate and final-stage decimal policies.</summary>
/// <remarks>
/// A6U01 uses bare ROUNDED without an explicit mode. Until runtime capture resolves G3's
/// compiler blocker, this policy uses the T010-approved IBM-compatible half-away-from-zero assumption.
/// </remarks>
public static class CobolRoundingPolicy
{
    public static decimal RoundIntermediate(decimal value, int scale = Money.MaximumScale)
    {
        ValidateScale(scale);
        return decimal.Round(value, scale, MidpointRounding.AwayFromZero);
    }

    public static decimal TruncateIntermediate(decimal value, int scale = Money.MaximumScale)
    {
        ValidateScale(scale);
        decimal factor = PowerOfTen(scale);
        return decimal.Truncate(value * factor) / factor;
    }

    /// <summary>
    /// Applies A6U01 7715 independently to unit price and total sell after preserving both originals.
    /// </summary>
    public static FinalSellRoundingResult ApplyFinal(
        Money unitPrice,
        Money totalSell,
        AccountRoundingConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        decimal roundedUnit = ApplyFinalValue(unitPrice.Value, configuration.Mode);
        decimal roundedTotal = ApplyFinalValue(totalSell.Value, configuration.Mode);
        return new FinalSellRoundingResult(
            new Money(roundedUnit),
            new Money(roundedTotal),
            unitPrice,
            totalSell,
            configuration);
    }

    private static decimal ApplyFinalValue(decimal value, AccountRoundingMode mode) => mode switch
    {
        AccountRoundingMode.NormalTwoDecimals => decimal.Round(value, 2, MidpointRounding.AwayFromZero),
        AccountRoundingMode.UpTwoDecimals => decimal.Round(0.0049m + value, 2, MidpointRounding.AwayFromZero),
        _ => value,
    };

    private static void ValidateScale(int scale)
    {
        if (scale is < 0 or > Money.MaximumScale)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), scale, "Scale must be between zero and eight.");
        }
    }

    private static decimal PowerOfTen(int scale)
    {
        decimal factor = 1m;
        for (int index = 0; index < scale; index++)
        {
            factor *= 10m;
        }

        return factor;
    }
}
