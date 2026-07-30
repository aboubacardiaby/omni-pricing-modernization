namespace Pricing.Api.Legacy;

using System.Globalization;
using global::Pricing.Domain.ValueObjects;

/// <summary>Shared header/product parsing for any endpoint that accepts the legacy PriceRequest shape.</summary>
public static class LegacyPriceRequestParser
{
    public static (DivisionId Division, AccountNumber Account, DateOnly PricingDate) ParseHeader(
        PriceRequest input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!string.Equals(input.Action, "A", StringComparison.Ordinal))
        {
            throw new ArgumentException("action must be 'A'.", nameof(input));
        }

        if (!string.Equals(input.Company, "OM", StringComparison.Ordinal))
        {
            throw new ArgumentException("company must be 'OM'.", nameof(input));
        }

        if (string.IsNullOrWhiteSpace(input.UserId))
        {
            throw new ArgumentException("userId is required.", nameof(input));
        }

        if (input.CustomerId.Length != 8)
        {
            throw new ArgumentException("customerId must contain a two-character division and six-character account.", nameof(input));
        }

        if (input.ProductNumbers is null || input.ProductNumbers.Count is < 1 or > 25)
        {
            throw new ArgumentException("productNumbers must contain between one and 25 products.", nameof(input));
        }

        if (input.NumberOfRequests != input.ProductNumbers.Count)
        {
            throw new ArgumentException("numberOfRequests must equal the number of productNumbers values.", nameof(input));
        }

        if (!DateOnly.TryParseExact(
                input.PricerDate,
                "MM-dd-yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly pricingDate))
        {
            throw new ArgumentException("pricerDate must use MM-dd-yyyy.", nameof(input));
        }

        return (
            new DivisionId(input.CustomerId[..2]),
            new AccountNumber(input.CustomerId[2..]),
            pricingDate);
    }

    public static (VendorId Vendor, ProductId Product) ParseProduct(string distributorProductNumber)
    {
        if (string.IsNullOrWhiteSpace(distributorProductNumber) || distributorProductNumber.Length is < 5 or > 12)
        {
            throw new ArgumentException("Each product number must contain a four-character vendor and one-to-eight-character vendor product.");
        }

        return (new VendorId(distributorProductNumber[..4]), new ProductId(distributorProductNumber[4..]));
    }
}
