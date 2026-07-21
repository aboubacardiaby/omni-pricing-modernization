namespace Pricing.Compatibility.Cobol;

using System.Text;

public static class CobolAlphanumericCodec
{
    private static readonly Encoding Ebcdic037 = CreateEncoding(37);
    private static readonly Encoding Windows1252 = CreateEncoding(1252);

    public static byte[] Encode(
        string value,
        int byteLength,
        CobolCharacterSet characterSet,
        bool lowValues = false)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(byteLength);

        if (lowValues)
        {
            if (value.Length != 0)
            {
                throw new ArgumentException("A LOW-VALUES field cannot also contain text.", nameof(value));
            }

            return new byte[byteLength];
        }

        Encoding encoding = GetEncoding(characterSet);
        byte[] encoded = encoding.GetBytes(value);
        if (encoded.Length > byteLength)
        {
            throw new OverflowException($"Encoded value requires {encoded.Length} bytes; field length is {byteLength}.");
        }

        byte[] result = new byte[byteLength];
        result.AsSpan().Fill(encoding.GetBytes(" ")[0]);
        encoded.CopyTo(result, 0);
        return result;
    }

    public static string Decode(
        ReadOnlySpan<byte> source,
        CobolCharacterSet characterSet,
        bool trimTrailingSpaces = false)
    {
        if (source.IsEmpty)
        {
            throw new ArgumentException("Source cannot be empty.", nameof(source));
        }

        Encoding encoding = GetEncoding(characterSet);
        string decoded = encoding.GetString(source);
        return trimTrailingSpaces ? decoded.TrimEnd(' ') : decoded;
    }

    private static Encoding GetEncoding(CobolCharacterSet characterSet) => characterSet switch
    {
        CobolCharacterSet.EbcdicCodePage037 => Ebcdic037,
        CobolCharacterSet.Windows1252 => Windows1252,
        _ => throw new ArgumentOutOfRangeException(nameof(characterSet)),
    };

    private static Encoding CreateEncoding(int codePage)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(
            codePage,
            EncoderFallback.ExceptionFallback,
            DecoderFallback.ExceptionFallback);
    }
}
