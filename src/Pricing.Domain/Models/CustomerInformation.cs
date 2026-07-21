namespace Pricing.Domain.Models;

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Pricing.Domain.ValueObjects;

/// <summary>Customer facts and hierarchy memberships used to build pricing context.</summary>
public sealed record CustomerInformation
{
    [JsonConstructor]
    public CustomerInformation(
        AccountNumber account,
        CustomerNumber? customer,
        ImmutableArray<BuyingGroupMembership> buyingGroupMemberships,
        ImmutableArray<ContractExclusion> contractExclusions,
        ImmutableArray<CustomerFeeConfiguration> fees,
        LowUnitOfMeasureConfiguration? lowUnitOfMeasure,
        CustomerFreightConfiguration? freight,
        ImmutableArray<CustomerPriceComponentConfiguration> priceComponents)
    {
        Account = account;
        Customer = customer;
        BuyingGroupMemberships = Normalize(buyingGroupMemberships);
        ContractExclusions = Normalize(contractExclusions);
        Fees = Normalize(fees);
        LowUnitOfMeasure = lowUnitOfMeasure;
        Freight = freight;
        PriceComponents = Normalize(priceComponents);
    }

    public CustomerInformation(
        AccountNumber account,
        CustomerNumber? customer,
        ImmutableArray<BuyingGroupMembership> buyingGroupMemberships)
        : this(account, customer, buyingGroupMemberships, [], [], null, null, [])
    {
    }

    public AccountNumber Account { get; }
    public CustomerNumber? Customer { get; }
    public ImmutableArray<BuyingGroupMembership> BuyingGroupMemberships { get; }
    public ImmutableArray<ContractExclusion> ContractExclusions { get; }
    public ImmutableArray<CustomerFeeConfiguration> Fees { get; }
    public LowUnitOfMeasureConfiguration? LowUnitOfMeasure { get; }
    public CustomerFreightConfiguration? Freight { get; }
    public ImmutableArray<CustomerPriceComponentConfiguration> PriceComponents { get; }

    private static ImmutableArray<T> Normalize<T>(ImmutableArray<T> values) => values.IsDefault ? [] : values;
}

public sealed record BuyingGroupMembership(
    long BuyingGroupId,
    long? ParentBuyingGroupId,
    int Priority,
    PricingDateRange EffectiveDates,
    string Source);

/// <summary>An account/customer contract exclusion loaded by CUP100 1000-VERFIFY-CONTRACT-EXCL.</summary>
public sealed record ContractExclusion(bool IsExcluded, string Scope, string Source);

/// <summary>Customer fee configuration; calculation is intentionally deferred to fee engines.</summary>
/// <remarks>COBOL: CUP100 2000-GET-PRICE-FEE-DATA and CUVFEEP.</remarks>
public sealed record CustomerFeeConfiguration(
    string ShortCode,
    string Name,
    CustomerFeeType Type,
    decimal Percentage,
    Money Amount,
    string? SkuCode,
    string BillingFrequency,
    string? CustomerOrderNumber,
    PricingDateRange EffectiveDates,
    Money LowUnitOfMeasureAmount,
    decimal LowUnitOfMeasurePercentage,
    string Source);

public enum CustomerFeeType
{
    PerLine,
    PerQuantity,
    PerOrder,
    PercentageOfSales,
    PercentageOfCost,
    Unknown,
}

/// <summary>Low-UOM facts selected through account/group priority and parent traversal.</summary>
/// <remarks>COBOL: CUP100 A800-GET-LOW-UOM-PCT through A850-SEL-PARENTS.</remarks>
public sealed record LowUnitOfMeasureConfiguration(
    bool IsEligible,
    long? BuyingGroupId,
    int? Priority,
    decimal Percentage,
    bool VendorExcluded,
    DateOnly? EffectiveDate,
    string Source);

/// <summary>Account and buying-group freight flags loaded by CUP100 A425-PRO-CUG53.</summary>
public sealed record CustomerFreightConfiguration(
    bool InboundFreight,
    bool SanctionedGroupExempt,
    bool NonSanctionedGroupExempt,
    bool IndividualExempt,
    bool NonContractExempt,
    bool CustomerExempt,
    long? BuyingGroupId,
    string Source);

/// <summary>Configured customer price-component metadata; application is deferred.</summary>
/// <remarks>COBOL: CUP100 3000-GET-PRICE-COMPONENT-DATA and CUVPRCMP.</remarks>
public sealed record CustomerPriceComponentConfiguration(
    string ShortCode,
    string Name,
    string? SkuCode,
    string BillingFrequency,
    string? CustomerOrderNumber,
    PricingDateRange EffectiveDates,
    string Source);
