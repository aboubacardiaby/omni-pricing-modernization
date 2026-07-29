namespace Pricing.Infrastructure.SqlServer;

using System.Collections.Immutable;
using Pricing.Application.CustomerPricingContext;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>
/// Loads the CUP100 customer aggregate from SQL Server without applying pricing rules.
/// </summary>
/// <remarks>
/// COBOL: CUP100 A200/A310, A425, A800-A850, 1000, 2000 and 3000.
/// </remarks>
public sealed class SqlServerCustomerPricingContextRepository(ISqlServerQueryExecutor executor)
    : ICustomerPricingContextRepository
{
    private static readonly SqlServerQuery AccountQuery = new("customer.account", """
        SELECT a.I_ACCOUNT AS InternalAccount, a.I_CUSTOMER AS Customer, CASE WHEN c.I_CUSTOMER IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS ActiveCustomerFound
        FROM dbo.CUG03 a
        LEFT JOIN dbo.CUG02 c ON c.I_CUSTOMER=a.I_CUSTOMER
        WHERE a.I_DIVISION=@Division AND a.S_ACCOUNT=@Account
        """);
    private static readonly SqlServerQuery MembershipQuery = new("customer.buying-groups", """
        WITH memberships AS (
          SELECT m.I_BUY_GROUP AS BuyingGroupId, m.Q_ACCT_BG_PRIORITY AS Priority,
                 p.D_ACCT_BG_PRI_EFF AS EffectiveDate, p.D_ACCT_BG_PRI_EXP AS ExpirationDate, 'CUG11' AS Source
          FROM dbo.CUG10 p JOIN dbo.CUG11 m ON m.I_ACCOUNT=p.I_ACCOUNT AND m.D_ACCT_BG_PRI_EFF=p.D_ACCT_BG_PRI_EFF
          WHERE p.I_ACCOUNT=@InternalAccount AND p.D_ACCT_BG_PRI_EFF<=@PricingDate
            AND (p.D_ACCT_BG_PRI_EXP>=@PricingDate OR p.D_ACCT_BG_PRI_EXP IS NULL)
          UNION ALL
          SELECT m.I_BUY_GROUP, m.Q_CUST_BG_PRIORITY, p.D_CUST_BG_PRI_EFF, p.D_CUST_BG_PRI_EXP, 'CUG07'
          FROM dbo.CUG06 p JOIN dbo.CUG07 m ON m.I_CUSTOMER=p.I_CUSTOMER AND m.D_CUST_BG_PRI_EFF=p.D_CUST_BG_PRI_EFF
          WHERE p.I_CUSTOMER=@Customer AND p.D_CUST_BG_PRI_EFF<=@PricingDate
            AND (p.D_CUST_BG_PRI_EXP>=@PricingDate OR p.D_CUST_BG_PRI_EXP IS NULL)
        )
        SELECT m.BuyingGroupId, parent.I_BUY_GROUP_PARENT AS ParentBuyingGroupId,
               m.Priority, m.EffectiveDate, m.ExpirationDate, m.Source
        FROM memberships m
        OUTER APPLY (SELECT TOP (1) b.I_BUY_GROUP_PARENT FROM dbo.BGG03 b
          WHERE b.I_BUY_GROUP=m.BuyingGroupId AND b.D_PARTNER_START<=@PricingDate
            AND (b.D_PARTNER_EXPIRE>=@PricingDate OR b.D_PARTNER_EXPIRE IS NULL)
          ORDER BY b.D_PARTNER_START DESC) parent
        ORDER BY m.Priority, m.BuyingGroupId
        """);
    private static readonly SqlServerQuery ExclusionQuery = new("customer.contract-exclusions", """
        SELECT CAST(1 AS bit) AS IsExcluded, 'account' AS Scope, 'CCG25' AS Source
        FROM dbo.CCG25 WHERE I_ACCOUNT=@InternalAccount
        """);
    private static readonly SqlServerQuery FreightQuery = new("customer.freight", """
        SELECT TOP (1) F_FRT_FLAG, F_EXEMPT_SANC_FLAG, F_EXEMPT_NON_SANC_FLAG,
          F_EXEMPT_IND_FLAG, F_EXEMPT_NON_CONT_FLAG, F_EXEMPT_CUSTOM_FLAG, I_BUY_GROUP
        FROM dbo.CUG53 WHERE I_ACCOUNT=@InternalAccount AND D_EFF_DATE<=@PricingDate
          AND (D_EXP_DATE>=@PricingDate OR D_EXP_DATE IS NULL)
        ORDER BY D_EFF_DATE DESC
        """);
    private static readonly SqlServerQuery LowUomQuery = new("customer.low-uom", """
        SELECT TOP (1) CAST(1 AS bit) AS IsEligible, b.I_BUY_GROUP AS BuyingGroupId,
          m.Priority, b.P_BG_LOW_UOM AS Percentage, b.F_LUOM_VEND_EXCL AS VendorExcluded,
          b.D_BG_LOW_UOM_EFF AS EffectiveDate, 'BGG23' AS Source
        FROM (
          SELECT I_BUY_GROUP, Q_ACCT_BG_PRIORITY AS Priority FROM dbo.CUG11 WHERE I_ACCOUNT=@InternalAccount
          UNION ALL
          SELECT I_BUY_GROUP, Q_CUST_BG_PRIORITY FROM dbo.CUG07 WHERE I_CUSTOMER=@Customer
        ) m JOIN dbo.BGG23 b ON b.I_BUY_GROUP=m.I_BUY_GROUP
        WHERE b.D_BG_LOW_UOM_EFF<=@PricingDate
          AND NOT EXISTS (SELECT 1 FROM dbo.BGG24 x WHERE x.I_BUY_GROUP=b.I_BUY_GROUP
            AND x.D_BG_LOW_UOM_EFF=b.D_BG_LOW_UOM_EFF AND x.I_CUSTOMER=@Customer
            AND x.D_BG_LUOM_EXCL_EFF<=@PricingDate
            AND (x.D_BG_LUOM_EXCL_EXP>=@PricingDate OR x.D_BG_LUOM_EXCL_EXP IS NULL))
        ORDER BY m.Priority, b.D_BG_LOW_UOM_EFF DESC
        """);
    private static readonly SqlServerQuery FeeQuery = new("customer.fees", """
        SELECT FEE_SHORT_CODE AS ShortCode, FEE_SHORT_CODE AS Name, FEE_TYPE AS FeeType,
          FEE_PCT AS Percentage, FEE_AMT AS Amount, SKU_CODE AS SkuCode, BILLING_FREQ AS BillingFrequency,
          D_FEE_EFFECTIVE AS EffectiveDate, D_FEE_EXPIRE AS ExpirationDate
        FROM dbo.CUTFEE_PRICE WHERE COMPANY_ID='OM' AND CUST_ID=@CustId AND SHIP_TO_SUFFIX=@ShipToSuffix
          AND D_FEE_EFFECTIVE<=@PricingDate AND (D_FEE_EXPIRE>=@PricingDate OR D_FEE_EXPIRE IS NULL)
        """);
    private static readonly SqlServerQuery ComponentQuery = new("customer.price-components", """
        SELECT COMPONENT_SHORT_CODE AS ShortCode, COMPONENT_SHORT_CODE AS Name, SKU_CODE AS SkuCode,
          BILLING_FREQ AS BillingFrequency, D_FEE_EFFECTIVE AS EffectiveDate, D_FEE_EXPIRE AS ExpirationDate
        FROM dbo.CUTPRICE_COMPONENT WHERE COMPANY_ID='OM' AND CUST_ID=@CustId AND SHIP_TO_SUFFIX=@ShipToSuffix
          AND D_FEE_EFFECTIVE<=@PricingDate AND (D_FEE_EXPIRE>=@PricingDate OR D_FEE_EXPIRE IS NULL)
        """);

    public async ValueTask<CustomerPricingContextData?> FindAsync(
        DivisionId division, AccountNumber account, string? shipTo, string? billTo,
        DateOnly pricingDate, CancellationToken cancellationToken)
    {
        try
        {
            var key = new { Division = division.Value, Account = account.Value };
            AccountRow? accountRow = await executor.QuerySingleOrDefaultAsync<AccountRow>(AccountQuery, key, cancellationToken).ConfigureAwait(false);
            if (accountRow is null)
            {
                return null;
            }

            var parameters = new
            {
                InternalAccount = accountRow.InternalAccount,
                Customer = accountRow.Customer,
                PricingDate = pricingDate.ToDateTime(TimeOnly.MinValue),
                CustId = division.Value + account.Value,
                ShipToSuffix = "0" + (shipTo ?? string.Empty).Trim(),
            };
            IReadOnlyList<MembershipRow> memberships = await executor.QueryAsync<MembershipRow>(MembershipQuery, parameters, cancellationToken).ConfigureAwait(false);
            IReadOnlyList<ExclusionRow> exclusions = await executor.QueryAsync<ExclusionRow>(ExclusionQuery, parameters, cancellationToken).ConfigureAwait(false);
            FreightRow? freight = await executor.QuerySingleOrDefaultAsync<FreightRow>(FreightQuery, parameters, cancellationToken).ConfigureAwait(false);
            LowUomRow? lowUom = await executor.QuerySingleOrDefaultAsync<LowUomRow>(LowUomQuery, parameters, cancellationToken).ConfigureAwait(false);
            IReadOnlyList<FeeRow> fees = await executor.QueryAsync<FeeRow>(FeeQuery, parameters, cancellationToken).ConfigureAwait(false);
            IReadOnlyList<ComponentRow> components = await executor.QueryAsync<ComponentRow>(ComponentQuery, parameters, cancellationToken).ConfigureAwait(false);

            return new CustomerPricingContextData(
                new CustomerNumber(accountRow.Customer),
                memberships.Select(Map).ToImmutableArray(),
                exclusions.Select(x => new ContractExclusion(x.IsExcluded, x.Scope, x.Source)).ToImmutableArray(),
                fees.Select(Map).ToImmutableArray(),
                lowUom is null ? null : new LowUnitOfMeasureConfiguration(lowUom.IsEligible, lowUom.BuyingGroupId,
                    lowUom.Priority, lowUom.Percentage, Yes(lowUom.VendorExcluded), ToDateOnly(lowUom.EffectiveDate), lowUom.Source),
                freight is null ? null : new CustomerFreightConfiguration(Yes(freight.F_FRT_FLAG), Yes(freight.F_EXEMPT_SANC_FLAG),
                    Yes(freight.F_EXEMPT_NON_SANC_FLAG), Yes(freight.F_EXEMPT_IND_FLAG), Yes(freight.F_EXEMPT_NON_CONT_FLAG),
                    Yes(freight.F_EXEMPT_CUSTOM_FLAG), freight.I_BUY_GROUP, "CUG53"),
                components.Select(Map).ToImmutableArray(),
                accountRow.ActiveCustomerFound);
        }
        catch (SqlServerAccessException exception)
        {
            throw new CustomerPricingContextRepositoryException(
                $"SQL Server customer context operation '{exception.Operation}' failed.",
                LegacyError(exception.Operation), "90", exception.IsTransient, exception);
        }
    }

    private static BuyingGroupMembership Map(MembershipRow row) => new(row.BuyingGroupId, row.ParentBuyingGroupId,
        row.Priority, new PricingDateRange(ToDateOnly(row.EffectiveDate)!.Value, ToDateOnly(row.ExpirationDate)), row.Source);
    private static CustomerFeeConfiguration Map(FeeRow row) => new(row.ShortCode.Trim(), row.Name.Trim(), MapFeeType(row.FeeType),
        row.Percentage / 100m, new Money(row.Amount), row.SkuCode?.Trim(), row.BillingFrequency.Trim(), null,
        new PricingDateRange(ToDateOnly(row.EffectiveDate)!.Value, ToDateOnly(row.ExpirationDate)), new Money(0m), 0m, "CUTFEE_PRICE");
    private static CustomerPriceComponentConfiguration Map(ComponentRow row) => new(row.ShortCode.Trim(), row.Name.Trim(),
        row.SkuCode?.Trim(), row.BillingFrequency.Trim(), null,
        new PricingDateRange(ToDateOnly(row.EffectiveDate)!.Value, ToDateOnly(row.ExpirationDate)), "CUTPRICE_COMPONENT");
    private static CustomerFeeType MapFeeType(string? value) => value?.Trim() switch
    {
        "L" => CustomerFeeType.PerLine, "Q" => CustomerFeeType.PerQuantity, "O" => CustomerFeeType.PerOrder,
        "S" => CustomerFeeType.PercentageOfSales, "C" => CustomerFeeType.PercentageOfCost, _ => CustomerFeeType.Unknown,
    };
    private static bool Yes(string? value) => string.Equals(value?.Trim(), "Y", StringComparison.OrdinalIgnoreCase);
    private static DateOnly? ToDateOnly(DateTime? value) => value is null ? null : DateOnly.FromDateTime(value.Value);
    private static string LegacyError(string operation) => operation switch
    {
        "customer.account" => "10", "customer.freight" => "30", "customer.low-uom" => "70",
        "customer.contract-exclusions" => "100", "customer.fees" => "220", "customer.price-components" => "230", _ => "20",
    };

    // Property-based rows let Dapper perform provider numeric conversions (for example SQL int to long).
    private sealed class AccountRow
    {
        public long InternalAccount { get; init; }
        public long Customer { get; init; }
        public bool ActiveCustomerFound { get; init; }
    }
    private sealed class MembershipRow
    {
        public long BuyingGroupId { get; init; }
        public long? ParentBuyingGroupId { get; init; }
        public int Priority { get; init; }
        public DateTime EffectiveDate { get; init; }
        public DateTime? ExpirationDate { get; init; }
        public string Source { get; init; } = string.Empty;
    }
    private sealed class ExclusionRow
    {
        public bool IsExcluded { get; init; }
        public string Scope { get; init; } = string.Empty;
        public string Source { get; init; } = string.Empty;
    }
    private sealed class FreightRow
    {
        public string? F_FRT_FLAG { get; init; }
        public string? F_EXEMPT_SANC_FLAG { get; init; }
        public string? F_EXEMPT_NON_SANC_FLAG { get; init; }
        public string? F_EXEMPT_IND_FLAG { get; init; }
        public string? F_EXEMPT_NON_CONT_FLAG { get; init; }
        public string? F_EXEMPT_CUSTOM_FLAG { get; init; }
        public long? I_BUY_GROUP { get; init; }
    }
    private sealed class LowUomRow
    {
        public bool IsEligible { get; init; }
        public long? BuyingGroupId { get; init; }
        public int? Priority { get; init; }
        public decimal Percentage { get; init; }
        public string? VendorExcluded { get; init; }
        public DateTime? EffectiveDate { get; init; }
        public string Source { get; init; } = string.Empty;
    }
    private sealed class FeeRow
    {
        public string ShortCode { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? FeeType { get; init; }
        public decimal Percentage { get; init; }
        public decimal Amount { get; init; }
        public string? SkuCode { get; init; }
        public string BillingFrequency { get; init; } = string.Empty;
        public DateTime EffectiveDate { get; init; }
        public DateTime? ExpirationDate { get; init; }
    }
    private sealed class ComponentRow
    {
        public string ShortCode { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? SkuCode { get; init; }
        public string BillingFrequency { get; init; } = string.Empty;
        public DateTime EffectiveDate { get; init; }
        public DateTime? ExpirationDate { get; init; }
    }
}
