namespace Pricing.Application.SellSelection;

using Pricing.Domain.Models;

/// <summary>Queries one account or customer-number sell-arrangement level.</summary>
public interface IAccountCustomerSellArrangementRepository
{
    ValueTask<SellArrangementSelection?> FindAsync(
        PricingContext context,
        SellArrangementScope scope,
        SellArrangementLevel level,
        CancellationToken cancellationToken);
}

public enum SellArrangementScope
{
    Account,
    CustomerNumber,
}

public enum SellArrangementLevel
{
    Product,
    VendorContract,
    ProductCategory,
    SpecialServiceCode,
    Vendor,
    Default,
}

public enum SellCostCascade
{
    IndividualContract,
    GroupContract,
    AcquisitionCost,
}

public sealed class SellArrangementRepositoryException : Exception
{
    public SellArrangementRepositoryException(
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
