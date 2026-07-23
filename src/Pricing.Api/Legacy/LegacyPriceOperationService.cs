namespace Pricing.Api.Legacy;

using System.Globalization;
using global::Pricing.Application.Orchestration;
using global::Pricing.Domain.Models;
using global::Pricing.Domain.ValueObjects;
using PricerApi.Models;

public interface ILegacyPriceOperationService
{
    ValueTask<PriceResponse> ExecuteAsync(
        PriceRequest input,
        CancellationToken cancellationToken);
}

public sealed class LegacyPriceOperationService(IPricingCalculationService pricing)
    : ILegacyPriceOperationService
{
    public async ValueTask<PriceResponse> ExecuteAsync(
        PriceRequest input,
        CancellationToken cancellationToken)
    {
        (DivisionId division, AccountNumber account, DateOnly pricingDate) = ParseHeader(input);
        var rows = new List<PriceRow>(input.ProductNumbers.Count);
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
        return new PriceResponse
        {
            ErrorFlag = hasErrors ? "Y" : string.Empty,
            ErrorDescription = hasErrors ? "One or more products could not be priced." : string.Empty,
            Rows = rows,
        };
    }

    private static (DivisionId Division, AccountNumber Account, DateOnly PricingDate) ParseHeader(
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

    private static (VendorId Vendor, ProductId Product) ParseProduct(string distributorProductNumber)
    {
        if (string.IsNullOrWhiteSpace(distributorProductNumber) || distributorProductNumber.Length is < 5 or > 12)
        {
            throw new ArgumentException("Each product number must contain a four-character vendor and one-to-eight-character vendor product.");
        }

        return (new VendorId(distributorProductNumber[..4]), new ProductId(distributorProductNumber[4..]));
    }

    private static PriceRow ToRow(string distributorProductNumber, PricingResult result)
    {
        PricingError? error = result.Errors.IsEmpty ? null : result.Errors[0];
        ContractSelection? contract = result.ContractSelection;
        LegacyPricingDetails? details = result.LegacyDetails;
        List<AlternateUomPricing> alternatives = details?.AlternateUoms.Select(alternate =>
            new AlternateUomPricing
            {
                DefaultUom = alternate.IsBranchDefault ? "Y" : string.Empty,
                Uom = alternate.UnitOfMeasure,
                Factor = alternate.Factor.ToString("F4", CultureInfo.InvariantCulture),
                Price = FormatAmount(alternate.Price, 4),
                JitBreakFee = "0.0000",
                JitLabelFee = "0.0000",
                JitServiceFee = "0.0000",
                JitApplyFee = "0.0000",
                FileCost = FormatAmount(alternate.Cost, 4),
                AcquisitionCost = FormatAmount(alternate.Cost, 4),
                TotalCost = FormatAmount(alternate.Cost, 4),
            }).ToList() ?? [];

        return new PriceRow
        {
            ErrorSwitch = error is null ? string.Empty : "Y",
            ErrorNumber = error?.LegacyErrorCode ?? string.Empty,
            ErrorMessage = error?.Message ?? string.Empty,
            PartNumber = distributorProductNumber,
            CatalogNumber = details?.CatalogNumber ?? string.Empty,
            PartDescription = details?.Description ?? string.Empty,
            PartExtraDescription = details?.ExtraDescription ?? string.Empty,
            ItemIndicator = details?.ItemIndicator ?? string.Empty,
            NonStockFlag = details?.NonStockFlag ?? string.Empty,
            VendorName = details?.VendorName ?? string.Empty,
            Pricer = new PricerInfo
            {
                VendorContractNumber = contract?.Contract.Value ?? details?.VendorContractNumber ?? string.Empty,
                Omni2Pricing = details?.Omni2Pricing ?? string.Empty,
                ContractType = contract?.ContractType ?? string.Empty,
                SellType = result.SellArrangementSelection?.ArrangementType ?? string.Empty,
                SanctionedFlag = details?.SanctionedFlag ?? string.Empty,
                EffectiveDate = FormatDate(contract?.Provenance.EffectiveDates?.EffectiveDate),
                ExpirationDate = FormatDate(result.ExpirationDate),
            },
            Inventory = new InboundInventory
            {
                DefaultUom = details?.BranchDefaultUom ?? string.Empty,
                DefaultEqualBaseUnit = details?.BranchDefaultEqualsBase ?? string.Empty,
                QuantityAvailable = FormatQuantity(details?.QuantityAvailable, showPositiveSign: true),
                QuantityOnOrder = FormatQuantity(details?.QuantityOnOrder, showPositiveSign: true),
                QuantityReserved = FormatQuantity(details?.QuantityReserved),
                QuantityCustomerReserved = FormatQuantity(details?.CustomerQuantityReserved),
            },
            Base = new BaseUomPricing
            {
                Uom = contract?.UnitOfMeasure.Value ?? details?.BaseUom ?? string.Empty,
                UomDescription = details?.BaseUomDescription ?? string.Empty,
                Price = FormatAmount(result.SellPrice, 4),
                JitBreakFee = "0.0000",
                JitLabelFee = "0.0000",
                JitServiceFee = "0.0000",
                JitApplyFee = "0.0000",
                FileCost = FormatAmount(details?.FileCost, 4),
                AcquisitionCost = FormatAmount(details?.AcquisitionCost, 4),
                TotalCost = FormatAmount(result.Cost, 4),
                VendorUom = contract?.UnitOfMeasure.Value ?? details?.VendorUom ?? string.Empty,
            },
            NumberOfAlternateUoms = alternatives.Count,
            Alternates = alternatives,
        };
    }

    private static string FormatQuantity(int? quantity, bool showPositiveSign = false) => quantity is null
        ? string.Empty
        : showPositiveSign && quantity >= 0
            ? $"{quantity}+"
            : quantity.Value.ToString(CultureInfo.InvariantCulture);

    private static string FormatAmount(Money? amount, int scale) => amount is null
        ? string.Empty
        : amount.Value.Value.ToString($"F{scale}", CultureInfo.InvariantCulture);

    private static string FormatDate(DateOnly? date) =>
        date?.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture) ?? string.Empty;
}