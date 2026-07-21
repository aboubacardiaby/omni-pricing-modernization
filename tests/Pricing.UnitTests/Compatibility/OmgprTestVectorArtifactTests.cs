namespace Pricing.UnitTests.Compatibility;

using System.Globalization;
using System.Text.Json;
using Pricing.Compatibility.Cobol;
using Xunit;

public sealed class OmgprTestVectorArtifactTests
{
    [Fact]
    public void EveryT016VectorIsLoadedAndMatchesThePrimitiveCodecs()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "TestData", "omgpr-test-vectors.json");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement[] vectors = document.RootElement.GetProperty("vectors").EnumerateArray().ToArray();

        Assert.Equal(23, vectors.Length);
        Assert.Equal(8, vectors.Select(vector => vector.GetProperty("field").GetString()).Distinct().Count());
        foreach (JsonElement vector in vectors)
        {
            VerifyVector(vector, "mainframe_ebcdic", CobolCharacterSet.EbcdicCodePage037, CobolByteOrder.BigEndian);
            VerifyVector(vector, "microfocus_ascii_native", CobolCharacterSet.Windows1252, CobolByteOrder.LittleEndian);
        }
    }

    private static void VerifyVector(
        JsonElement vector,
        string profileName,
        CobolCharacterSet characterSet,
        CobolByteOrder byteOrder)
    {
        string id = vector.GetProperty("id").GetString()!;
        string field = vector.GetProperty("field").GetString()!;
        byte[] actual = field switch
        {
            "OMGPR-I-VENDOR" or "OMGPR-D-PRICING" or "OMGPR-F-PRICER-ERROR" =>
                EncodeTextVector(vector, characterSet),
            "OMGPR-I-CONTRACT" or "OMGPR-Q-ERROR-NBR" =>
                CobolCompCodec.EncodeSigned(
                    vector.GetProperty("logical_value").GetInt64(),
                    vector.GetProperty("copybook_citation").GetProperty("byte_length").GetInt32(),
                    byteOrder),
            "OMGPR-A-CNT-LN-UNIT-COST" =>
                CobolPackedDecimalCodec.Encode(ReadDecimal(vector), 13, 8, true),
            "OMGPR-PRICING-PERCENTAGE" =>
                CobolPackedDecimalCodec.Encode(ReadDecimal(vector), 5, 4, false),
            "OMGPR-ERROR-MESSAGE" => EncodeErrorMessageVector(vector, characterSet),
            _ => throw new InvalidOperationException($"Unhandled T016 vector field {field}."),
        };

        Assert.Equal(ExpectedHex(vector, profileName), Convert.ToHexString(actual));
        Assert.False(string.IsNullOrWhiteSpace(id));
    }

    private static byte[] EncodeTextVector(JsonElement vector, CobolCharacterSet characterSet)
    {
        int length = vector.GetProperty("copybook_citation").GetProperty("byte_length").GetInt32();
        string category = vector.GetProperty("category").GetString()!;
        if (category.Contains("low_values", StringComparison.Ordinal)) return new byte[length];
        return CobolAlphanumericCodec.Encode(vector.GetProperty("logical_value").GetString()!, length, characterSet);
    }

    private static byte[] EncodeErrorMessageVector(JsonElement vector, CobolCharacterSet characterSet)
    {
        string category = vector.GetProperty("category").GetString()!;
        string value = category.Contains("spaces", StringComparison.Ordinal) ? string.Empty : "DB ERROR";
        return CobolAlphanumericCodec.Encode(value, 76, characterSet);
    }

    private static decimal ReadDecimal(JsonElement vector) =>
        decimal.Parse(vector.GetProperty("logical_value").GetString()!, CultureInfo.InvariantCulture);

    private static string ExpectedHex(JsonElement vector, string profileName)
    {
        if (vector.TryGetProperty("encoded_hex", out JsonElement encodedHex))
        {
            return encodedHex.GetProperty(profileName).GetString()!;
        }

        if (vector.TryGetProperty("encoded", out JsonElement encoded))
        {
            return encoded.GetProperty(profileName).GetString()!;
        }

        JsonElement padding = vector.GetProperty("padding");
        int byteCount = padding.GetProperty("byte_count").GetInt32();
        string fillName = profileName == "mainframe_ebcdic"
            ? "mainframe_ebcdic_fill_byte"
            : "microfocus_ascii_native_fill_byte";
        string fill = padding.GetProperty(fillName).GetString()!;
        if (!vector.TryGetProperty("encoded_message_text_only", out JsonElement message))
        {
            return string.Concat(Enumerable.Repeat(fill, byteCount));
        }

        return message.GetProperty(profileName).GetString()! + string.Concat(Enumerable.Repeat(fill, byteCount));
    }
}
