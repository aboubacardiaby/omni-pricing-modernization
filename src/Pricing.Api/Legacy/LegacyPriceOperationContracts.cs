namespace Pricing.Api.Legacy;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public sealed record LegacyPriceOperationRequest(
    [property: JsonPropertyName("IN_PRICE")] LegacyPriceOperationInput InPrice);

public sealed record LegacyPriceOperationInput(
    [property: JsonPropertyName("IN_ACTION"), Required] string Action,
    [property: JsonPropertyName("IN_USERID"), Required] string UserId,
    [property: JsonPropertyName("IN_CO"), Required] string Company,
    [property: JsonPropertyName("IN_CUST_ID"), Required] string CustomerId,
    [property: JsonPropertyName("IN_SHIPTO")] string? ShipTo,
    [property: JsonPropertyName("IN_PRICER_MM_DD_CCYY"), Required] string PricingDate,
    [property: JsonPropertyName("IN_NBR_REQUESTS"), Range(1, 25)] int NumberOfRequests,
    [property: JsonPropertyName("IN_PRODUCT_NO"), Required, MinLength(1), MaxLength(25)] IReadOnlyList<string> ProductNumbers);

public sealed record LegacyPriceOperationResponse(
    [property: JsonPropertyName("OUT_PRICE")] LegacyPriceOperationOutput OutPrice);

public sealed record LegacyPriceOperationOutput(
    [property: JsonPropertyName("OUT_ERROR_FLAG")] string? ErrorFlag,
    [property: JsonPropertyName("OUT_ERROR_DESC")] string? ErrorDescription,
    [property: JsonPropertyName("OUT_ROW")] IReadOnlyList<LegacyPriceRow> Rows);

public sealed record LegacyPriceRow(
    [property: JsonPropertyName("OUT_ERROR_SW")] string? ErrorSwitch,
    [property: JsonPropertyName("OUT_ERROR_NUMBER")] string? ErrorNumber,
    [property: JsonPropertyName("OUT_ERROR_MSG")] string? ErrorMessage,
    [property: JsonPropertyName("OUT_PART_NBR")] string? PartNumber,
    [property: JsonPropertyName("OUT_CATALOG_NBR")] string? CatalogNumber,
    [property: JsonPropertyName("OUT_PART_DESCRIPTION")] string? PartDescription,
    [property: JsonPropertyName("OUT_PART_EXTRA_DESC")] string? PartExtraDescription,
    [property: JsonPropertyName("OUT_ITEM_INDICATOR")] string? ItemIndicator,
    [property: JsonPropertyName("OUT_NON_STOCK_FLAG")] string? NonStockFlag,
    [property: JsonPropertyName("OUT_CUST_PART")] string? CustomerPart,
    [property: JsonPropertyName("OUT_CUST_PART2")] string? CustomerPart2,
    [property: JsonPropertyName("OUT_CUST_SUFFIX")] string? CustomerSuffix,
    [property: JsonPropertyName("OUT_VENDOR_NAME")] string? VendorName,
    [property: JsonPropertyName("OUT_PRICER")] LegacyPricerOutput Pricer,
    [property: JsonPropertyName("OUT_INB")] LegacyInventoryOutput Inventory,
    [property: JsonPropertyName("OUT_BASE")] LegacyBaseOutput Base,
    [property: JsonPropertyName("OUT_ALT_NBR_OF_UOMS")] int AlternativeUomCount,
    [property: JsonPropertyName("OUT_ALT")] IReadOnlyList<LegacyAlternativeUomOutput> AlternativeUoms);

public sealed record LegacyPricerOutput(
    [property: JsonPropertyName("OUT_VENDOR_CONTRACT_NBR")] string? VendorContractNumber,
    [property: JsonPropertyName("OUT_OMNI2_PRICING")] string? Omni2Pricing,
    [property: JsonPropertyName("OUT_BUY_GROUP_SHORT")] string? BuyingGroupShortName,
    [property: JsonPropertyName("OUT_I_GRP_CONTRACT_NBR")] string? GroupContractNumber,
    [property: JsonPropertyName("OUT_CONTRACT_TYPE")] string? ContractType,
    [property: JsonPropertyName("OUT_C_SELL_TYPE")] string? SellType,
    [property: JsonPropertyName("OUT_SANCTIONED_FLAG")] string? SanctionedFlag,
    [property: JsonPropertyName("OUT_EFFECTIVE_DATE")] string? EffectiveDate,
    [property: JsonPropertyName("OUT_EXPIRATION_DATE")] string? ExpirationDate);

public sealed record LegacyInventoryOutput(
    [property: JsonPropertyName("OUT_BR_DFLT_UOM")] string? BranchDefaultUom,
    [property: JsonPropertyName("OUT_BR_DFLT_EQUAL_BU")] string? BranchDefaultEqualsBase,
    [property: JsonPropertyName("OUT_QTY_AVAILABLE")] string? QuantityAvailable,
    [property: JsonPropertyName("OUT_QTY_ON_ORDER")] string? QuantityOnOrder,
    [property: JsonPropertyName("OUT_QTY_RESERVE")] string? QuantityReserved,
    [property: JsonPropertyName("OUT_QTY_CUST_RESERVE")] string? CustomerQuantityReserved);

public sealed record LegacyBaseOutput(
    [property: JsonPropertyName("OUT_BU_UOM")] string? Uom,
    [property: JsonPropertyName("OUT_BU_UOM_DESC")] string? UomDescription,
    [property: JsonPropertyName("OUT_BU_PRICE")] string? Price,
    [property: JsonPropertyName("OUT_BU_PRICE_UNRND")] string? UnroundedPrice,
    [property: JsonPropertyName("OUT_BU_JIT_BREAK_FEE")] string? JitBreakFee,
    [property: JsonPropertyName("OUT_BU_JIT_LABEL_FEE")] string? JitLabelFee,
    [property: JsonPropertyName("OUT_BU_JIT_SERVICE_FEE")] string? JitServiceFee,
    [property: JsonPropertyName("OUT_BU_JIT_APPLY_FEE")] string? JitApplyFee,
    [property: JsonPropertyName("OUT_BU_FILE_COST")] string? FileCost,
    [property: JsonPropertyName("OUT_BU_ACQ_COST")] string? AcquisitionCost,
    [property: JsonPropertyName("OUT_BU_TOTAL_COST")] string? TotalCost,
    [property: JsonPropertyName("OUT_BU_VENDOR_UOM")] string? VendorUom);

public sealed record LegacyAlternativeUomOutput(
    [property: JsonPropertyName("OUT_ALT_DFLT_UOM")] string? DefaultUom,
    [property: JsonPropertyName("OUT_ALT_UOM")] string? Uom,
    [property: JsonPropertyName("OUT_ALT_FACTOR")] string? Factor,
    [property: JsonPropertyName("OUT_ALT_PRICE")] string? Price,
    [property: JsonPropertyName("OUT_ALT_PRICE_UNRND")] string? UnroundedPrice,
    [property: JsonPropertyName("OUT_ALT_JIT_BREAKFEE")] string? JitBreakFee,
    [property: JsonPropertyName("OUT_ALT_JIT_LABELFEE")] string? JitLabelFee,
    [property: JsonPropertyName("OUT_ALT_JIT_SERVICEFEE")] string? JitServiceFee,
    [property: JsonPropertyName("OUT_ALT_JIT_APPLYFEE")] string? JitApplyFee,
    [property: JsonPropertyName("OUT_ALT_FILE_COST")] string? FileCost,
    [property: JsonPropertyName("OUT_ALT_ACQ_COST")] string? AcquisitionCost,
    [property: JsonPropertyName("OUT_ALT_TOTAL_COST")] string? TotalCost);
