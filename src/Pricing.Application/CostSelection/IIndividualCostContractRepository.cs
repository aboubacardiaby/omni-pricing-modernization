namespace Pricing.Application.CostSelection;

using System.Collections.Immutable;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>Loads active individual-customer cost-contract candidates as one business query.</summary>
public interface IIndividualCostContractRepository
{
    /// <remarks>
    /// A6U01 9037/9040 and the MIN_INDV cursor join CCG01/03/04/06/09/27 by customer,
    /// vendor, product, division, and inclusive effective dates. Implementations must preserve
    /// cursor row order and apply 7080 UOM conversion with the approved rounding policy before
    /// returning <see cref="IndividualCostContractCandidate.NormalizedUnitCost"/>. When
    /// <see cref="PricingRequest.IsSpecialContract"/> is true, CCG01.F_BYPASS must be restricted
    /// to B exactly as A6U01 0235 does.
    /// </remarks>
    ValueTask<ImmutableArray<IndividualCostContractCandidate>> FindCandidatesAsync(
        PricingContext context,
        CancellationToken cancellationToken);
}

public sealed record IndividualCostContractCandidate(
    ContractId Contract,
    string ContractType,
    Money NormalizedUnitCost,
    UnitOfMeasure NormalizedUnitOfMeasure,
    ImmutableArray<PricingDateRange> EligibilityDates,
    bool IsExcluded,
    RuleProvenance Provenance,
    Money? SuggestedSellPrice = null);

public sealed class IndividualCostContractRepositoryException : Exception
{
    public IndividualCostContractRepositoryException(
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
