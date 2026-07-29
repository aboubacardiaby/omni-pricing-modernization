namespace Pricing.Api.Pricing;

using global::Pricing.Domain.Models;
using global::Pricing.Domain.ValueObjects;

public static class PricingRequestValidator
{
    public static ValidationPricingError? Validate(CalculatePricesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Required(request.Division, "division", DivisionId.MaximumLength, "DIVISION_REQUIRED")
            ?? Required(request.Account, "account", AccountNumber.MaximumLength, "ACCOUNT_REQUIRED")
            ?? Required(request.UnitOfMeasure, "unitOfMeasure", UnitOfMeasure.MaximumLength, "UNIT_OF_MEASURE_REQUIRED")
            ?? (request.Products is null || request.Products.Count is < 1 or > 25
                ? Error("PRODUCTS_COUNT_INVALID", "products must contain between one and 25 items.", "products")
                : null)
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

    public static ValidationPricingError? Validate(CalculateProductRequest product)
    {
        if (product is null)
        {
            return Error("PRODUCT_REQUIRED", "A product entry is required.", "products", "105");
        }

        return Required(product.Vendor, "vendor", VendorId.MaximumLength, "VENDOR_REQUIRED")
            ?? Required(product.Product, "product", ProductId.MaximumLength, "PRODUCT_REQUIRED");
    }

    private static ValidationPricingError? Required(string? value, string field, int maximumLength, string code)
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
        string code, string message, string field, string? legacyCode = null) =>
        new(code, message, legacyCode, field);
}