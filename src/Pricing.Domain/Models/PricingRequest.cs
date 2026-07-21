namespace Pricing.Domain.Models;

using Pricing.Domain.ValueObjects;

/// <summary>Domain input for a pricing operation, separate from the legacy OMGPR layout.</summary>
/// <remarks>
/// COBOL inputs: OMGPR.CPY fields OMGPR-I-DIVISION, OMGPR-S-ACCOUNT, OMGPR-I-VENDOR,
/// OMGPR-I-VND-PRODUCT, OMGPR-Q-ORD-LIN-ORDERED, OMGPR-C-ORD-LIN-CUST-UOM,
/// OMGPR-C-SHIP-TO-SUFFIX, OMGPR-C-BILL-TO-SUFFIX, OMGPR-D-PRICING, and OMGPR-PRICING-REQ-SW.
/// </remarks>
public sealed record PricingRequest(
    DivisionId Division,
    AccountNumber Account,
    VendorId Vendor,
    ProductId Product,
    Quantity Quantity,
    UnitOfMeasure UnitOfMeasure,
    string? ShipTo,
    string? BillTo,
    DateOnly PricingDate,
    PricingRequestType RequestType,
    bool IsSpecialContract = false);

public enum PricingRequestType
{
    Full,
    CostOnly,
    SellOnly,
    Jit
}
