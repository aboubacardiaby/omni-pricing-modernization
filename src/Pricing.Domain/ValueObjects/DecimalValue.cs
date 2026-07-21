namespace Pricing.Domain.ValueObjects;

internal static class DecimalValue
{
    public static decimal RequireScale(decimal value, int maximumScale, string parameterName)
    {
        var scale = (decimal.GetBits(value)[3] >> 16) & 0x7F;
        return scale <= maximumScale
            ? value
            : throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"Value cannot have more than {maximumScale} decimal places.");
    }

    public static decimal RequireMagnitude(decimal value, decimal maximumMagnitude, string parameterName)
    {
        return value >= -maximumMagnitude && value <= maximumMagnitude
            ? value
            : throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"Value must be between {-maximumMagnitude} and {maximumMagnitude}.");
    }
}
