namespace Pricing.Api.Pricing;

using global::Pricing.Domain.Models;
using global::Pricing.Domain.ValueObjects;

public static class PricingRequestValidator
{
    public static ValidationPricingError? Validate(CalculatePriceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Required(request.Division, "division", DivisionId.MaximumLength, "DIVISION_REQUIRED")
            ?? Required(request.Account, "account", AccountNumber.MaximumLength, "ACCOUNT_REQUIRED")
            ?? Required(request.Vendor, "vendor", VendorId.MaximumLength, "VENDOR_REQUIRED")
            ?? Required(request.Product, "product", ProductId.MaximumLength, "PRODUCT_REQUIRED")
            ?? Required(request.UnitOfMeasure, "unitOfMeasure", UnitOfMeasure.MaximumLength, "UNIT_OF_MEASURE_REQUIRED")
            ?? (request.Quantity != decimal.Truncate(request.Quantity)
                ? Error("QUANTITY_SCALE_INVALID", "Quantity must be a whole number.", "quantity")
                : null)
            ?? (decimal.Abs(request.Quantity) > Quantity.MaximumMagnitude
                ? Error("QUANTITY_OUT_OF_RANGE", "Quantity exceeds the legacy supported range.", "quantity")
                : null)
            ?? (request.PricingDate == default
                ? Error("PRICING_DATE_REQUIRED", "Pricing date is required.", "pricingDate", "102")
                : null);
    }

    private static ValidationPricingError? Required(
        string? value,
        string field,
        int maximumLength,
        string code)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            string? legacyCode = field switch
            {
                "division" => "102",
                "account" => "103",
                "vendor" => "104",
                "product" => "105",
                _ => null,
            };
            return Error(code, $"{field} is required.", field, legacyCode);
        }

        return value.Length > maximumLength
            ? Error($"{code}_TOO_LONG", $"{field} cannot exceed {maximumLength} characters.", field)
            : null;
    }

    private static ValidationPricingError Error(
        string code,
        string message,
        string field,
        string? legacyCode = null) => new(code, message, legacyCode, field);
}
