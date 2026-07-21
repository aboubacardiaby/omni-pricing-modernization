namespace Pricing.Application.CustomerPricingContext;

using System.Collections.Immutable;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>Loads the business-oriented customer aggregate required by CUP100.</summary>
public interface ICustomerPricingContextRepository
{
    ValueTask<CustomerPricingContextData?> FindAsync(
        DivisionId division,
        AccountNumber account,
        string? shipTo,
        string? billTo,
        DateOnly pricingDate,
        CancellationToken cancellationToken);
}

public sealed record CustomerPricingContextData(
    CustomerNumber? Customer,
    ImmutableArray<BuyingGroupMembership> BuyingGroupMemberships,
    ImmutableArray<ContractExclusion> ContractExclusions,
    ImmutableArray<CustomerFeeConfiguration> Fees,
    LowUnitOfMeasureConfiguration? LowUnitOfMeasure,
    CustomerFreightConfiguration? Freight,
    ImmutableArray<CustomerPriceComponentConfiguration> PriceComponents,
    bool ActiveCustomerFound = true);

public sealed class CustomerPricingContextRepositoryException : Exception
{
    public CustomerPricingContextRepositoryException(
        string message,
        string? legacyErrorCode = null,
        string? legacySeverityCode = null,
        bool isTransient = false,
        Exception? innerException = null)
        : base(message, innerException)
    {
        LegacyErrorCode = legacyErrorCode;
        LegacySeverityCode = legacySeverityCode;
        IsTransient = isTransient;
    }

    public string? LegacyErrorCode { get; }
    public string? LegacySeverityCode { get; }
    public bool IsTransient { get; }
}
