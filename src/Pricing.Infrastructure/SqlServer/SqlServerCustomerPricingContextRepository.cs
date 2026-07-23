namespace Pricing.Infrastructure.SqlServer;

using System.Collections.Immutable;
using Pricing.Application.CustomerPricingContext;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>Loads CUP100 account/customer and active buying-group priority data.</summary>
/// <remarks>COBOL: CUP100 A200, A810, A820, and A850.</remarks>
public sealed class SqlServerCustomerPricingContextRepository(ISqlServerQueryExecutor queries) :
    ICustomerPricingContextRepository
{
    private static readonly SqlServerQuery Account = new("customer.cug03", """
        SELECT I_CUSTOMER AS Customer, I_ACCOUNT AS InternalAccount
        FROM dbo.CUG03
        WHERE S_ACCOUNT = @Account AND I_DIVISION = @Division;
        """);
    private static readonly SqlServerQuery AccountGroups = new("customer.cug10-cug11", """
        SELECT CUG11.I_BUY_GROUP AS BuyingGroupId,
               CUG11.Q_ACCT_BG_PRIORITY AS Priority,
               CUG10.D_ACCT_BG_PRI_EFF AS EffectiveDate,
               CUG10.D_ACCT_BG_PRI_EXP AS ExpirationDate
        FROM dbo.CUG10
        JOIN dbo.CUG11 ON CUG11.I_ACCOUNT = CUG10.I_ACCOUNT
                      AND CUG11.D_ACCT_BG_PRI_EFF = CUG10.D_ACCT_BG_PRI_EFF
        WHERE CUG10.I_ACCOUNT = @InternalAccount
          AND CUG10.D_ACCT_BG_PRI_EFF <= @PricingDate
          AND (CUG10.D_ACCT_BG_PRI_EXP >= @PricingDate OR CUG10.D_ACCT_BG_PRI_EXP IS NULL);
        """);
    private static readonly SqlServerQuery CustomerGroups = new("customer.cug06-cug07", """
        SELECT CUG07.I_BUY_GROUP AS BuyingGroupId,
               CUG07.Q_CUST_BG_PRIORITY AS Priority,
               CUG06.D_CUST_BG_PRI_EFF AS EffectiveDate,
               CUG06.D_CUST_BG_PRI_EXP AS ExpirationDate
        FROM dbo.CUG06
        JOIN dbo.CUG07 ON CUG07.I_CUSTOMER = CUG06.I_CUSTOMER
                      AND CUG07.D_CUST_BG_PRI_EFF = CUG06.D_CUST_BG_PRI_EFF
        WHERE CUG06.I_CUSTOMER = @Customer
          AND CUG06.D_CUST_BG_PRI_EFF <= @PricingDate
          AND (CUG06.D_CUST_BG_PRI_EXP >= @PricingDate OR CUG06.D_CUST_BG_PRI_EXP IS NULL);
        """);
    private static readonly SqlServerQuery Parent = new("customer.bgg03-parent", """
        SELECT TOP (1) I_BUY_GROUP_PARENT
        FROM dbo.BGG03
        WHERE I_BUY_GROUP = @BuyingGroup
          AND D_PARTNER_START <= @PricingDate
          AND (D_PARTNER_EXPIRE >= @PricingDate OR D_PARTNER_EXPIRE IS NULL)
        ORDER BY D_PARTNER_START DESC;
        """);

    private readonly ISqlServerQueryExecutor queries = queries ?? throw new ArgumentNullException(nameof(queries));

    public async ValueTask<CustomerPricingContextData?> FindAsync(
        DivisionId division, AccountNumber account, string? shipTo, string? billTo,
        DateOnly pricingDate, CancellationToken cancellationToken)
    {
        try
        {
            AccountRow? accountRow = await queries.QuerySingleOrDefaultAsync<AccountRow>(Account,
                new { Division = division.Value, Account = account.Value }, cancellationToken).ConfigureAwait(false);
            if (accountRow is null) return null;

            var parameters = new
            {
                InternalAccount = accountRow.InternalAccount,
                Customer = accountRow.Customer,
                PricingDate = pricingDate.ToDateTime(TimeOnly.MinValue),
            };
            IReadOnlyList<GroupRow> accountGroups = await queries.QueryAsync<GroupRow>(
                AccountGroups, parameters, cancellationToken).ConfigureAwait(false);
            IReadOnlyList<GroupRow> customerGroups = await queries.QueryAsync<GroupRow>(
                CustomerGroups, parameters, cancellationToken).ConfigureAwait(false);
            var memberships = ImmutableArray.CreateBuilder<BuyingGroupMembership>();
            foreach ((GroupRow row, string source) in accountGroups.Select(row => (row, "CUG10/CUG11"))
                         .Concat(customerGroups.Select(row => (row, "CUG06/CUG07"))))
            {
                long? parent = await queries.QuerySingleOrDefaultAsync<long?>(Parent,
                    new { BuyingGroup = row.BuyingGroupId, PricingDate = parameters.PricingDate }, cancellationToken)
                    .ConfigureAwait(false);
                memberships.Add(new BuyingGroupMembership(row.BuyingGroupId, parent, row.Priority,
                    new PricingDateRange(DateOnly.FromDateTime(row.EffectiveDate),
                        row.ExpirationDate is null ? null : DateOnly.FromDateTime(row.ExpirationDate.Value)), source));
            }

            return new CustomerPricingContextData(
                new CustomerNumber(accountRow.Customer), memberships.ToImmutable(), [], [], null, null, [], true);
        }
        catch (SqlServerAccessException exception)
        {
            throw new CustomerPricingContextRepositoryException(
                $"SQL Server customer-context lookup '{exception.Operation}' failed.", "10", "70",
                exception.IsTransient, exception);
        }
    }

    private sealed class AccountRow
    {
        public long Customer { get; init; }
        public long InternalAccount { get; init; }
    }
    private sealed class GroupRow
    {
        public long BuyingGroupId { get; init; }
        public int Priority { get; init; }
        public DateTime EffectiveDate { get; init; }
        public DateTime? ExpirationDate { get; init; }
    }
}
