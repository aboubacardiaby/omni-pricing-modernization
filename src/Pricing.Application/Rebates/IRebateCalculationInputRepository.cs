namespace Pricing.Application.Rebates;

using Pricing.Domain.Models;

/// <summary>Loads the inputs consumed by A6U01 7070 for the selected cost contract.</summary>
public interface IRebateCalculationInputRepository
{
    ValueTask<RebateCalculationInput> LoadAsync(
        PricingContext context,
        ContractSelection contract,
        CancellationToken cancellationToken);
}

public sealed class RebateCalculationInputRepositoryException : Exception
{
    public RebateCalculationInputRepositoryException(
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
