namespace Pricing.Api.Legacy;

using System.Globalization;
using global::Pricing.Application.Legacy;
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

public sealed class LegacyPriceOperationService(
    IPricingCalculationService pricing,
    ILegacyPricingDetailsRepository legacyDetails)
    : ILegacyPriceOperationService
{
    public async ValueTask<PriceResponse> ExecuteAsync(
        PriceRequest input,
        CancellationToken cancellationToken)
    {
        (DivisionId division, AccountNumber account, DateOnly pricingDate) = LegacyPriceRequestParser.ParseHeader(input);
        var rows = new List<PriceRow>(input.ProductNumbers.Count);
        foreach (string distributorProductNumber in input.ProductNumbers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (VendorId vendor, ProductId product) = LegacyPriceRequestParser.ParseProduct(distributorProductNumber);
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
            if (result.Errors.IsEmpty)
            {
                LegacyPricingDetails? details = await legacyDetails
                    .FindAsync(division, vendor, product, pricingDate, result.Cost, result.SellPrice, cancellationToken)
                    .ConfigureAwait(false);
                result = result with { LegacyDetails = details };
            }
            rows.Add(ToRow(distributorProductNumber, result, input.ShipTo));
        }

        bool hasErrors = rows.Any(row => row.ErrorSwitch == "Y");
        return new PriceResponse
        {
            ErrorFlag = hasErrors ? "Y" : string.Empty,
            ErrorDescription = hasErrors ? "One or more products could not be priced." : string.Empty,
            Rows = rows,
        };
    }

    private static PriceRow ToRow(string distributorProductNumber, PricingResult result, string shipTo)
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
            CustomerSuffix = shipTo,
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
                SellType = MapSellType(result.SellArrangementSelection),
                SanctionedFlag = contract is { ContractType: "PRIMARY_GROUP", GroupHasContractFees: true } ? "Y" : "N",
                EffectiveDate = FormatDate(contract?.Provenance.EffectiveDates?.EffectiveDate),
                ExpirationDate = FormatDate(contract?.Provenance.EffectiveDates?.ExpirationDate ?? result.ExpirationDate),
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

    private static string MapSellType(SellArrangementSelection? selection) => selection?.Provenance.HierarchyLevel switch
    {
        "Account" => "A",
        "CustomerNumber" => "N",
        "SUBGROUP" or "PARENT" => "G",
        "CORPORATE" => "C",
        _ => string.Empty,
    };
}