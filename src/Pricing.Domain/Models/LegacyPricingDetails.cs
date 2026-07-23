namespace Pricing.Domain.Models;

using System.Collections.Immutable;
using Pricing.Domain.ValueObjects;

/// <summary>Legacy PriceOperation output facts populated by the COBOL-compatible SQL path.</summary>
public sealed record LegacyPricingDetails(
    string? CatalogNumber,
    string? Description,
    string? ExtraDescription,
    string? ItemIndicator,
    string? NonStockFlag,
    string? VendorName,
    string? VendorContractNumber,
    string? Omni2Pricing,
    string? SanctionedFlag,
    string? BranchDefaultUom,
    string? BranchDefaultEqualsBase,
    int? QuantityAvailable,
    int? QuantityOnOrder,
    int? QuantityReserved,
    int? CustomerQuantityReserved,
    string? BaseUom,
    string? BaseUomDescription,
    Money? FileCost,
    Money? AcquisitionCost,
    string? VendorUom,
    ImmutableArray<LegacyAlternateUomDetails> AlternateUoms);

public sealed record LegacyAlternateUomDetails(
    bool IsBranchDefault,
    string UnitOfMeasure,
    decimal Factor,
    Money Price,
    Money Cost);
