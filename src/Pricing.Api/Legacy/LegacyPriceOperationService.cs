using System.Globalization;
using System.Collections.Immutable;
using global::Pricing.Domain.Models;
using global::Pricing.Domain.ValueObjects;
using global::Pricing.Application.Orchestration;

namespace Pricing.Api.Legacy;
public interface ILegacyPriceOperationService
{
    ValueTask<LegacyPriceOperationResponse> ExecuteAsync(
        LegacyPriceOperationInput input,
        CancellationToken cancellationToken);
}

public sealed class LegacyPriceOperationService(IPricingCalculationService pricing)
    : ILegacyPriceOperationService
{
    public async ValueTask<LegacyPriceOperationResponse> ExecuteAsync(
        LegacyPriceOperationInput input,
        CancellationToken cancellationToken)
    {
        (DivisionId division, AccountNumber account, DateOnly pricingDate) = ParseHeader(input);
        var rows = ImmutableArray.CreateBuilder<LegacyPriceRow>(input.ProductNumbers.Count);
        foreach (string distributorProductNumber in input.ProductNumbers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (VendorId vendor, ProductId product) = ParseProduct(distributorProductNumber);
            var request = new PricingRequest(
                division,
                account,
                vendor,
                product,
                new Quantity(1m),
                new UnitOfMeasure("EA"),
                input.ShipTo,
                null,
                pricingDate,
                PricingRequestType.Full);
            PricingResult result = await pricing.CalculateAsync(request, cancellationToken).ConfigureAwait(false);
            rows.Add(ToRow(distributorProductNumber, result));
        }

        bool hasErrors = rows.Any(row => row.ErrorSwitch == "Y");
        return new LegacyPriceOperationResponse(new LegacyPriceOperationOutput(
            hasErrors ? "Y" : null,
            hasErrors ? "One or more products could not be priced." : null,
            rows.ToImmutable()));
    }

    private static (DivisionId Division, AccountNumber Account, DateOnly PricingDate) ParseHeader(
        LegacyPriceOperationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!string.Equals(input.Action, "A", StringComparison.Ordinal))
        {
            throw new ArgumentException("IN_ACTION must be 'A'.", nameof(input));
        }

        if (!string.Equals(input.Company, "OM", StringComparison.Ordinal))
        {
            throw new ArgumentException("IN_CO must be 'OM'.", nameof(input));
        }

        if (string.IsNullOrWhiteSpace(input.UserId))
        {
            throw new ArgumentException("IN_USERID is required.", nameof(input));
        }

        if (input.CustomerId.Length != 8)
        {
            throw new ArgumentException("IN_CUST_ID must contain a two-character division and six-character account.", nameof(input));
        }

        if (input.ProductNumbers is null || input.ProductNumbers.Count is < 1 or > 25)
        {
            throw new ArgumentException("IN_PRODUCT_NO must contain between one and 25 products.", nameof(input));
        }

        if (input.NumberOfRequests != input.ProductNumbers.Count)
        {
            throw new ArgumentException("IN_NBR_REQUESTS must equal the number of IN_PRODUCT_NO values.", nameof(input));
        }

        if (!DateOnly.TryParseExact(
                input.PricingDate,
                "MM-dd-yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly pricingDate))
        {
            throw new ArgumentException("IN_PRICER_MM_DD_CCYY must use MM-dd-yyyy.", nameof(input));
        }

        return (
            new DivisionId(input.CustomerId[..2]),
            new AccountNumber(input.CustomerId[2..]),
            pricingDate);
    }

    private static (VendorId Vendor, ProductId Product) ParseProduct(string distributorProductNumber)
    {
        if (string.IsNullOrWhiteSpace(distributorProductNumber) || distributorProductNumber.Length is < 5 or > 12)
        {
            throw new ArgumentException("Each IN_PRODUCT_NO must contain a four-character vendor and one-to-eight-character vendor product.");
        }

        return (new VendorId(distributorProductNumber[..4]), new ProductId(distributorProductNumber[4..]));
    }

    private static LegacyPriceRow ToRow(string distributorProductNumber, PricingResult result)
    {
        PricingError? error = result.Errors.IsEmpty ? null : result.Errors[0];
        ContractSelection? contract = result.ContractSelection;
        LegacyPricingDetails? details = result.LegacyDetails;
        string? expiration = FormatDate(result.ExpirationDate);
        IReadOnlyList<LegacyAlternativeUomOutput> alternatives = details?.AlternateUoms.Select(alternate =>
            new LegacyAlternativeUomOutput(
                alternate.IsBranchDefault ? "Y" : null,
                alternate.UnitOfMeasure,
                alternate.Factor.ToString("F4", CultureInfo.InvariantCulture),
                FormatAmount(alternate.Price, 4),
                FormatAmount(alternate.Price, 8),
                "0.0000",
                "0.0000",
                "0.0000",
                "0.0000",
                FormatAmount(alternate.Cost, 4),
                FormatAmount(alternate.Cost, 4),
                FormatAmount(alternate.Cost, 4))).ToImmutableArray() ?? [];

        return new LegacyPriceRow(
            error is null ? null : "Y",
            error?.LegacyErrorCode,
            error?.Message,
            distributorProductNumber,
            details?.CatalogNumber,
            details?.Description,
            details?.ExtraDescription,
            details?.ItemIndicator,
            details?.NonStockFlag,
            null,
            null,
            null,
            details?.VendorName,
            new LegacyPricerOutput(
                contract?.Contract.Value ?? details?.VendorContractNumber,
                details?.Omni2Pricing,
                null,
                null,
                contract?.ContractType,
                result.SellArrangementSelection?.ArrangementType,
                details?.SanctionedFlag,
                FormatDate(contract?.Provenance.EffectiveDates?.EffectiveDate),
                expiration),
            new LegacyInventoryOutput(
                details?.BranchDefaultUom,
                details?.BranchDefaultEqualsBase,
                FormatQuantity(details?.QuantityAvailable, showPositiveSign: true),
                FormatQuantity(details?.QuantityOnOrder, showPositiveSign: true),
                FormatQuantity(details?.QuantityReserved),
                FormatQuantity(details?.CustomerQuantityReserved)),
            new LegacyBaseOutput(
                contract?.UnitOfMeasure.Value ?? details?.BaseUom,
                details?.BaseUomDescription,
                FormatAmount(result.SellPrice, 4),
                FormatAmount(result.SellPrice, 8),
                "0.0000",
                "0.0000",
                "0.0000",
                "0.0000",
                FormatAmount(details?.FileCost, 4),
                FormatAmount(details?.AcquisitionCost, 4),
                FormatAmount(result.Cost, 4),
                contract?.UnitOfMeasure.Value ?? details?.VendorUom),
            alternatives.Count,
            alternatives);
    }

    private static string? FormatQuantity(int? quantity, bool showPositiveSign = false) => quantity is null
        ? null
        : showPositiveSign && quantity >= 0
            ? $"{quantity}+"
            : quantity.Value.ToString(CultureInfo.InvariantCulture);
    private static string? FormatAmount(Money? amount, int scale) => amount is null
        ? null
        : amount.Value.Value.ToString($"F{scale}", CultureInfo.InvariantCulture);

    private static string? FormatDate(DateOnly? date) => date?.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);
}
