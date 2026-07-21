namespace Pricing.Compatibility.Cobol;

using System.Globalization;

public static class CobolPackedDecimalCodec
{
    // Fail-loud validation is intentional: accepting invalid BCD digits, padding, or sign
    // nibbles would silently convert corrupt legacy bytes into authoritative domain values.
    public static byte[] Encode(decimal value, int precision, int scale, bool isSigned)
    {
        ValidateFormat(precision, scale);
        if (!isSigned && value < 0)
        {
            throw new OverflowException("An unsigned COMP-3 field cannot contain a negative value.");
        }

        if (decimal.Round(value, scale, MidpointRounding.ToEven) != value)
        {
            throw new OverflowException($"Value {value} has more than {scale} fractional digits.");
        }

        string fixedValue = decimal.Abs(value).ToString($"F{scale}", CultureInfo.InvariantCulture);
        string digits = fixedValue.Replace(".", string.Empty, StringComparison.Ordinal);
        if (digits.Length > precision)
        {
            throw new OverflowException($"Value {value} exceeds COMP-3 precision {precision} with scale {scale}.");
        }

        digits = digits.PadLeft(precision, '0');
        int byteLength = (precision + 2) / 2;
        string nibbles = precision % 2 == 0 ? $"0{digits}" : digits;
        nibbles += value < 0 ? "D" : isSigned ? "C" : "F";

        byte[] result = new byte[byteLength];
        for (int index = 0; index < result.Length; index++)
        {
            result[index] = Convert.ToByte(nibbles.Substring(index * 2, 2), 16);
        }

        return result;
    }

    public static decimal Decode(ReadOnlySpan<byte> source, int precision, int scale, bool isSigned)
    {
        ValidateFormat(precision, scale);
        int expectedLength = (precision + 2) / 2;
        if (source.Length != expectedLength)
        {
            throw new ArgumentException($"COMP-3 source must be exactly {expectedLength} bytes.", nameof(source));
        }

        Span<char> nibbles = stackalloc char[source.Length * 2];
        for (int index = 0; index < source.Length; index++)
        {
            nibbles[index * 2] = ToHex(source[index] >> 4);
            nibbles[(index * 2) + 1] = ToHex(source[index] & 0x0F);
        }

        int digitStart = precision % 2 == 0 ? 1 : 0;
        if (digitStart == 1 && nibbles[0] != '0')
        {
            throw new FormatException("The unused leading COMP-3 nibble must be zero.");
        }

        ReadOnlySpan<char> digits = nibbles.Slice(digitStart, precision);
        foreach (char digit in digits)
        {
            if (digit is < '0' or > '9')
            {
                throw new FormatException("COMP-3 contains a non-decimal digit nibble.");
            }
        }

        char signNibble = nibbles[^1];
        bool negative = signNibble == 'D';
        if (isSigned ? signNibble is not ('C' or 'D' or 'F') : signNibble != 'F')
        {
            throw new FormatException($"Invalid COMP-3 sign nibble '{signNibble}'.");
        }

        decimal unscaled = decimal.Parse(digits, NumberStyles.None, CultureInfo.InvariantCulture);
        decimal value = unscaled / Pow10(scale);
        return negative ? -value : value;
    }

    private static void ValidateFormat(int precision, int scale)
    {
        if (precision is < 1 or > 28) throw new ArgumentOutOfRangeException(nameof(precision));
        if (scale < 0 || scale > precision) throw new ArgumentOutOfRangeException(nameof(scale));
    }

    private static char ToHex(int nibble) => (char)(nibble < 10 ? '0' + nibble : 'A' + nibble - 10);

    private static decimal Pow10(int scale)
    {
        decimal result = 1;
        for (int index = 0; index < scale; index++) result *= 10;
        return result;
    }
}
