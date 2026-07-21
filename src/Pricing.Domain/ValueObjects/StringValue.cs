namespace Pricing.Domain.ValueObjects;

internal static class StringValue
{
    public static string Require(string? value, int maximumLength, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Length <= maximumLength
            ? value
            : throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"Value cannot exceed {maximumLength} characters.");
    }
}
