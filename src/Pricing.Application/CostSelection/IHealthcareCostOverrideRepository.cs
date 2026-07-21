namespace Pricing.Application.CostSelection;

using System.Collections.Immutable;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>Loads the HC group/account/CID/product aggregate and eligible VNG03 rows.</summary>
public interface IHealthcareCostOverrideRepository
{
    ValueTask<HealthcareOverrideData> FindAsync(
        PricingContext context,
        CancellationToken cancellationToken);
}

public sealed record HealthcareOverrideData(
    HealthcareCostEligibility? AccountCostEligibility,
    HealthcareCostEligibility? CustomerCostEligibility,
    HealthcareSellOverrideTerms? AccountSellOverride,
    HealthcareSellOverrideTerms? CustomerSellOverride,
    ImmutableArray<HealthcarePriceListCandidate> PriceListCandidates);

public sealed record HealthcareCostEligibility(
    long HealthcareGroupId,
    HealthcareOverrideScope Scope,
    PricingDateRange HeaderDates,
    PricingDateRange AssignmentDates,
    PricingDateRange GroupDetailDates,
    PricingDateRange ProductCostDates,
    bool GroupCostOverridden,
    bool? ProductCostOverridden,
    string Source);

public sealed record HealthcarePriceListCandidate(
    Money AcquisitionCost,
    Money DealerCost,
    UnitOfMeasure UnitOfMeasure,
    DateOnly ListEffectiveDate,
    DateOnly ActiveDate,
    DateOnly? ExpirationDate,
    RuleProvenance Provenance);

public enum HealthcareOverrideScope
{
    Account,
    Customer,
}

public sealed class HealthcareCostOverrideRepositoryException : Exception
{
    public HealthcareCostOverrideRepositoryException(
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
