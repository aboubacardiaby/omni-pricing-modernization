namespace PricerApi.Models;

public class PriceResponse
{
    public string ErrorFlag { get; set; } = string.Empty;
    public string ErrorDescription { get; set; } = string.Empty;
    public List<PriceRow> Rows { get; set; } = [];
}

public class PriceRow
{
    public string ErrorSwitch { get; set; } = string.Empty;
    public string ErrorNumber { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public string CatalogNumber { get; set; } = string.Empty;
    public string PartDescription { get; set; } = string.Empty;
    public string PartExtraDescription { get; set; } = string.Empty;
    public string ItemIndicator { get; set; } = string.Empty;
    public string NonStockFlag { get; set; } = string.Empty;
    public string CustomerPart { get; set; } = string.Empty;
    public string CustomerPart2 { get; set; } = string.Empty;
    public string CustomerSuffix { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public PricerInfo Pricer { get; set; } = new();
    public InboundInventory Inventory { get; set; } = new();
    public BaseUomPricing Base { get; set; } = new();
    public int NumberOfAlternateUoms { get; set; }
    public List<AlternateUomPricing> Alternates { get; set; } = [];
}

public class PricerInfo
{
    public string VendorContractNumber { get; set; } = string.Empty;
    public string Omni2Pricing { get; set; } = string.Empty;
    public string BuyGroupShort { get; set; } = string.Empty;
    public string GroupContractNumber { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty;
    public string SellType { get; set; } = string.Empty;
    public string SanctionedFlag { get; set; } = string.Empty;
    public string EffectiveDate { get; set; } = string.Empty;
    public string ExpirationDate { get; set; } = string.Empty;
}

public class InboundInventory
{
    public string DefaultUom { get; set; } = string.Empty;
    public string DefaultEqualBaseUnit { get; set; } = string.Empty;
    public string QuantityAvailable { get; set; } = string.Empty;
    public string QuantityOnOrder { get; set; } = string.Empty;
    public string QuantityReserved { get; set; } = string.Empty;
    public string QuantityCustomerReserved { get; set; } = string.Empty;
}

public class BaseUomPricing
{
    public string Uom { get; set; } = string.Empty;
    public string UomDescription { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string JitBreakFee { get; set; } = string.Empty;
    public string JitLabelFee { get; set; } = string.Empty;
    public string JitServiceFee { get; set; } = string.Empty;
    public string JitApplyFee { get; set; } = string.Empty;
    public string FileCost { get; set; } = string.Empty;
    public string AcquisitionCost { get; set; } = string.Empty;
    public string TotalCost { get; set; } = string.Empty;
    public string VendorUom { get; set; } = string.Empty;
}

public class AlternateUomPricing
{
    public string DefaultUom { get; set; } = string.Empty;
    public string Uom { get; set; } = string.Empty;
    public string Factor { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string JitBreakFee { get; set; } = string.Empty;
    public string JitLabelFee { get; set; } = string.Empty;
    public string JitServiceFee { get; set; } = string.Empty;
    public string JitApplyFee { get; set; } = string.Empty;
    public string FileCost { get; set; } = string.Empty;
    public string AcquisitionCost { get; set; } = string.Empty;
    public string TotalCost { get; set; } = string.Empty;
}