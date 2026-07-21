namespace Pricing.Compatibility.Omgpr;

using System.Globalization;
using Pricing.Compatibility.Cobol;
using Pricing.Domain.Models;

public sealed class OmgprEncoder
{
    private readonly OmgprEncodingProfile profile;

    public OmgprEncoder(OmgprEncodingProfile profile) =>
        this.profile = profile ?? throw new ArgumentNullException(nameof(profile));

    public byte[] Encode(DecodedOmgprRecord original, PricingResult result)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(result);

        byte[] encoded = original.OriginalBytes.ToArray();
        WritePackedDecimal(encoded, OmgprLayout.Output.TotalCost, result.Cost?.Value ?? 0m);
        WritePackedDecimal(encoded, OmgprLayout.Output.SellPrice, result.SellPrice?.Value ?? 0m);
        WriteText(
            encoded,
            OmgprLayout.Output.ExpirationDate,
            result.ExpirationDate?.ToString(profile.PricingDateFormat, CultureInfo.InvariantCulture) ?? string.Empty);
        WriteError(encoded, result.Errors);

        original.OpaqueTrailingBytes.CopyTo(encoded, OmgprLayout.OpaqueTrailingOffset);
        return encoded;
    }

    private void WriteError(Span<byte> destination, System.Collections.Immutable.ImmutableArray<PricingError> errors)
    {
        PricingError? error = errors.FirstOrDefault(candidate => candidate.LegacyErrorCode is not null)
            ?? errors.FirstOrDefault();
        if (error is null)
        {
            WriteText(destination, OmgprLayout.Output.PricerErrorFlag, string.Empty);
            WriteText(destination, OmgprLayout.Output.ErrorMessage, string.Empty);
            WriteCompNumber(destination, OmgprLayout.Output.ErrorNumber, 0);
            WriteDisplayNumber(destination, OmgprLayout.Output.ErrorSeverityCode, 0);
            return;
        }

        WriteText(destination, OmgprLayout.Output.PricerErrorFlag, "Y");
        WriteText(destination, OmgprLayout.Output.ErrorMessage, error.Message);
        int errorNumber = ParseLegacyErrorCode(error.LegacyErrorCode);
        int severityCode = ParseLegacySeverityCode(error.LegacySeverityCode);
        WriteCompNumber(
            destination,
            OmgprLayout.Output.ErrorNumber,
            errorNumber);
        WriteDisplayNumber(destination, OmgprLayout.Output.ErrorSeverityCode, severityCode);
    }

    private static int ParseLegacyErrorCode(string? legacyErrorCode)
    {
        if (legacyErrorCode is null)
        {
            throw new InvalidOperationException("A legacy OMGPR error requires a specific error number.");
        }
        if (!int.TryParse(legacyErrorCode, NumberStyles.None, CultureInfo.InvariantCulture, out int value) ||
            value is < -9999 or > 9999)
        {
            throw new FormatException($"Legacy error code '{legacyErrorCode}' must fit OMGPR-Q-ERROR-NBR PIC S9(4). ");
        }

        return value;
    }

    private static int ParseLegacySeverityCode(string? legacySeverityCode)
    {
        if (legacySeverityCode is null)
        {
            throw new InvalidOperationException("A legacy OMGPR error requires an independent severity code.");
        }

        if (!int.TryParse(legacySeverityCode, NumberStyles.None, CultureInfo.InvariantCulture, out int value) ||
            value is < 0 or > 99)
        {
            throw new FormatException(
                $"Legacy severity code '{legacySeverityCode}' must fit OMGPR-Q-ERROR-CODE's confirmed 0-99 domain.");
        }

        return value;
    }

    private void WriteDisplayNumber(Span<byte> destination, OmgprLayout.Field field, int value) =>
        WriteText(destination, field, value.ToString($"D{field.Length}", CultureInfo.InvariantCulture));

    private void WriteCompNumber(Span<byte> destination, OmgprLayout.Field field, int value) =>
        CobolCompCodec.EncodeSigned(value, field.Length, profile.CompByteOrder).CopyTo(field.Write(destination));

    private static void WritePackedDecimal(Span<byte> destination, OmgprLayout.Field field, decimal value) =>
        CobolPackedDecimalCodec.Encode(value, 13, 8, true).CopyTo(field.Write(destination));

    private void WriteText(Span<byte> destination, OmgprLayout.Field field, string value) =>
        CobolAlphanumericCodec.Encode(value, field.Length, profile.CharacterSet).CopyTo(field.Write(destination));
}
