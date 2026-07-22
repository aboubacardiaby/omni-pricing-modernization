namespace Pricing.Domain.Models;

using System.Collections.Immutable;
using Pricing.Domain.ValueObjects;

/// <summary>Explains the source and applicability window of a pricing decision or component.</summary>
public sealed record RuleProvenance(
    string RuleName,
    string Source,
    string? HierarchyLevel,
    PricingDateRange? EffectiveDates);

/// <summary>A cost-contract selection and its traceable source.</summary>
public interface ICostSourceSelection
{
    Money UnitCost { get; }
    UnitOfMeasure UnitOfMeasure { get; }
    RuleProvenance Provenance { get; }
}

public sealed record ContractSelection(
    ContractId Contract,
    string ContractType,
    Money UnitCost,
    UnitOfMeasure UnitOfMeasure,
    RuleProvenance Provenance,
    long? BuyingGroupId = null,
    int? BuyingGroupPriority = null,
    string? BuyingGroupHierarchyLevel = null,
    bool IsBuyingGroupOverride = false,
    SpecialContractSelection? SpecialContract = null) : ICostSourceSelection;

/// <summary>Confirmed early-exit outputs and controls for A6U01 special-contract processing.</summary>
public sealed record SpecialContractSelection(
    Money SuggestedSellPrice,
    bool BypassAdjustments,
    bool BypassFinalRounding,
    ImmutableArray<PricingDateRange> ExpirationSources);

/// <summary>Healthcare-selected dealer cost and its independent HC group provenance.</summary>
public sealed record HealthcareCostSelection(
    long HealthcareGroupId,
    string Scope,
    Money UnitCost,
    UnitOfMeasure UnitOfMeasure,
    Money AcquisitionCost,
    DateOnly PriceListEffectiveDate,
    RuleProvenance Provenance,
    HealthcareSellOverrideTerms? SellOverride) : ICostSourceSelection;

/// <summary>The terminal VNG03 dealer-cost fallback and its explicit no-contract defaults.</summary>
public sealed record AcquisitionCostSelection(
    Money UnitCost,
    UnitOfMeasure UnitOfMeasure,
    Money AcquisitionCost,
    string PriceLevel,
    DateOnly PriceListEffectiveDate,
    bool IsJitExempt,
    bool IsFreightExempt,
    RuleProvenance Provenance) : ICostSourceSelection;

public sealed record HealthcareSellOverrideTerms
{
    public HealthcareSellOverrideTerms(
        HealthcareSellOverrideType type,
        decimal? percentage,
        Money? statedPrice,
        UnitOfMeasure unitOfMeasure,
        PricingDateRange effectiveDates,
        string source,
        long healthcareGroupId = 0,
        string? scope = null,
        string? comment = null)
    {
        if (type == HealthcareSellOverrideType.CostPlus && percentage is null)
        {
            throw new ArgumentException("A healthcare COST+ override requires a percentage.", nameof(percentage));
        }

        if (type == HealthcareSellOverrideType.CostPlus && statedPrice is not null)
        {
            throw new ArgumentException("A healthcare COST+ override cannot also contain a stated price.", nameof(statedPrice));
        }

        if (type == HealthcareSellOverrideType.StatedPrice && statedPrice is null)
        {
            throw new ArgumentException("A stated healthcare override requires a fixed price.", nameof(statedPrice));
        }

        if (type == HealthcareSellOverrideType.StatedPrice && percentage is not null)
        {
            throw new ArgumentException("A stated healthcare override cannot also contain a percentage.", nameof(percentage));
        }

        Type = type;
        Percentage = percentage;
        StatedPrice = statedPrice;
        UnitOfMeasure = unitOfMeasure;
        EffectiveDates = effectiveDates;
        Source = string.IsNullOrWhiteSpace(source)
            ? throw new ArgumentException("Healthcare override source is required.", nameof(source))
            : source;
        HealthcareGroupId = healthcareGroupId;
        Scope = scope;
        Comment = comment;
    }

    public HealthcareSellOverrideType Type { get; }
    public decimal? Percentage { get; }
    public Money? StatedPrice { get; }
    public UnitOfMeasure UnitOfMeasure { get; }
    public PricingDateRange EffectiveDates { get; }
    public string Source { get; }
    public long HealthcareGroupId { get; }
    public string? Scope { get; }
    public string? Comment { get; }
}

public enum HealthcareSellOverrideType
{
    CostPlus,
    StatedPrice,
}

/// <summary>A sell-arrangement selection and its traceable source.</summary>
public sealed record SellArrangementSelection(
    string ArrangementIdentifier,
    string ArrangementType,
    RuleProvenance Provenance,
    long? BuyingGroupId = null,
    string? BuyingGroupMember = null,
    int? PreferredTierLevel = null,
    DateOnly? TierStartDate = null,
    int? ParentDepth = null);
