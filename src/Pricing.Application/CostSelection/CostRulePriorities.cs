namespace Pricing.Application.CostSelection;

/// <summary>Relative slots for the confirmed A6U01 cost cascade; gaps permit later rule insertion.</summary>
public static class CostRulePriorities
{
    /// <summary>Special requests use the restricted individual path and stop normal processing.</summary>
    public const int SpecialContract = 50;

    /// <summary>A6U01 0195 tries 0235 individual-customer contracts before every group path.</summary>
    public const int IndividualCustomerContract = 100;

    /// <summary>A6U01 0195 reaches 0240 group selection only after individual selection misses.</summary>
    public const int BuyingGroupContract = 200;

    /// <summary>A6U01 7190 checks healthcare only after individual and group contracts miss.</summary>
    public const int HealthcareOverride = 300;

    /// <summary>A6U01 7190 uses normalized VNG03 dealer cost only after every higher cost source misses.</summary>
    public const int AcquisitionDealerFallback = 400;
}
