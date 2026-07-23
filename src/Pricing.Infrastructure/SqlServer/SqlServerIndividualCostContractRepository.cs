namespace Pricing.Infrastructure.SqlServer;

using System.Collections.Immutable;
using Pricing.Application.CostSelection;
using Pricing.Application.Rounding;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>Loads the A6U01 individual contract cursor without the unavailable CCG27 join.</summary>
/// <remarks>COBOL: A6U01 0235/0270/7080 and SQL 9040; CCG27 intentionally omitted by decision.</remarks>
public sealed class SqlServerIndividualCostContractRepository(ISqlServerQueryExecutor queries) :
    IIndividualCostContractRepository
{
    private static readonly SqlServerQuery Contracts = new("contract.ccg01-03-04-06-09", """
        SELECT CCG01.I_CONTRACT AS Contract,
               CCG01.C_CNT_TYPE AS ContractType,
               CCG03.C_CNT_LN_UM AS UnitOfMeasure,
               CCG03.A_CNT_LN_UNIT_COST AS UnitCost,
               CCG01.D_CNT_START AS HeaderEffective,
               CCG01.D_CNT_PROT_END AS HeaderExpiration,
               CCG03.D_CNT_LINE_START AS LineEffective,
               CCG03.D_CNT_LINE_EXPIRE AS LineExpiration,
               CCG06.D_ASGN_EFFECT AS AssignmentEffective,
               CCG06.D_ASGN_EXPIRE AS AssignmentExpiration,
               CCG09.D_IND_VND_CNT_EFF AS VendorEffective,
               CCG09.D_IND_VND_CNT_EXP AS VendorExpiration
        FROM dbo.CCG01
        JOIN dbo.CCG03 ON CCG03.I_CONTRACT = CCG01.I_CONTRACT
        JOIN dbo.CCG04 ON CCG04.I_CONTRACT = CCG01.I_CONTRACT
        JOIN dbo.CCG06 ON CCG06.I_CONTRACT = CCG01.I_CONTRACT
        JOIN dbo.CCG09 ON CCG09.I_CONTRACT = CCG01.I_CONTRACT
                      AND CCG09.I_DIVISION = CCG04.I_DIVISION
        WHERE CCG06.I_CUSTOMER = @Customer
          AND CCG01.I_VENDOR = COALESCE(
              (SELECT TOP (1) I_PARENT_VENDOR FROM dbo.VNG01 WHERE I_VENDOR = @Vendor), @Vendor)
          AND CCG01.D_CNT_START <= @PricingDate
          AND CCG01.D_CNT_PROT_END >= @PricingDate
          AND CCG01.F_BYPASS = @Bypass
          AND CCG03.D_CNT_LINE_START <= @PricingDate
          AND (CCG03.D_CNT_LINE_EXPIRE >= @PricingDate OR CCG03.D_CNT_LINE_EXPIRE IS NULL)
          AND CCG06.D_ASGN_EFFECT <= @PricingDate
          AND (CCG06.D_ASGN_EXPIRE >= @PricingDate OR CCG06.D_ASGN_EXPIRE IS NULL)
          AND CCG09.D_IND_VND_CNT_EFF <= @PricingDate
          AND (CCG09.D_IND_VND_CNT_EXP >= @PricingDate OR CCG09.D_IND_VND_CNT_EXP IS NULL)
          AND CCG03.I_VENDOR = @Vendor
          AND CCG03.I_VND_PRODUCT = @Product
          AND CCG04.I_DIVISION = @Division
        ORDER BY CCG01.I_CONTRACT, CCG03.L_CNT_LINE;
        """);
    private static readonly SqlServerQuery Factors = new("contract.vng05-factors", """
        SELECT C_VD_PRD_ALT_UM AS UnitOfMeasure, A_VD_PRD_ALT_UMF AS ConversionFactor
        FROM dbo.VNG05 WHERE I_VENDOR = @Vendor AND I_VND_PRODUCT = @Product;
        """);
    private static readonly SqlServerQuery Exclusion = new("contract.individual-ccg25-exclusion", """
        SELECT TOP (1) 1 FROM dbo.CCG25
         WHERE I_ACCOUNT=(SELECT TOP (1) I_ACCOUNT FROM dbo.CUG03 WHERE S_ACCOUNT=@Account AND I_DIVISION=@Division)
           AND I_CONTRACT=@Contract
           AND C_SHIP_TO_SUFFIX IN ('000', @ShipTo)
           AND D_EFFECT<=@PricingDate AND (D_EXPIRE>=@PricingDate OR D_EXPIRE IS NULL);
        """);

    private readonly ISqlServerQueryExecutor queries = queries ?? throw new ArgumentNullException(nameof(queries));

    public async ValueTask<ImmutableArray<IndividualCostContractCandidate>> FindCandidatesAsync(
        PricingContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Customer.Customer is null) return [];
        try
        {
            var parameters = new
            {
                Customer = context.Customer.Customer.Value.Value,
                Vendor = context.Request.Vendor.Value,
                Product = context.Request.Product.Value,
                Division = context.Request.Division.Value,
                PricingDate = context.Request.PricingDate.ToDateTime(TimeOnly.MinValue),
                Bypass = context.Request.IsSpecialContract ? "B" : "N",
            };
            IReadOnlyList<Row> rows = await queries.QueryAsync<Row>(Contracts, parameters, cancellationToken)
                .ConfigureAwait(false);
            IReadOnlyList<FactorRow> factorRows = await queries.QueryAsync<FactorRow>(Factors, parameters, cancellationToken)
                .ConfigureAwait(false);
            Dictionary<string, decimal> factors = factorRows
                .Where(row => !string.IsNullOrWhiteSpace(row.UnitOfMeasure) && row.ConversionFactor > 0m)
                .ToDictionary(row => row.UnitOfMeasure!.Trim(), row => row.ConversionFactor, StringComparer.Ordinal);
            factors[context.Product.BaseUnitOfMeasure.Value] = 1m;
            var candidates = ImmutableArray.CreateBuilder<IndividualCostContractCandidate>();
            foreach (Row row in rows)
            {
                int? excluded = await queries.QuerySingleOrDefaultAsync<int?>(Exclusion, new
                {
                    Account = context.Request.Account.Value, Division = context.Request.Division.Value,
                    Contract = row.Contract, ShipTo = string.IsNullOrWhiteSpace(context.Request.ShipTo) ? "000" : context.Request.ShipTo,
                    PricingDate = context.Request.PricingDate.ToDateTime(TimeOnly.MinValue),
                }, cancellationToken).ConfigureAwait(false);
                candidates.Add(Map(row, context, factors, excluded is not null));
            }
            return candidates.ToImmutable();
        }
        catch (SqlServerAccessException exception)
        {
            throw new IndividualCostContractRepositoryException(
                $"SQL Server individual-contract lookup '{exception.Operation}' failed.", "23", "70",
                exception.IsTransient, exception);
        }
    }

    private static IndividualCostContractCandidate Map(Row row, PricingContext context,
        IReadOnlyDictionary<string, decimal> factors, bool isExcluded)
    {
        string fileUom = Required(row.UnitOfMeasure, "CCG03.C_CNT_LN_UM");
        decimal up = GetFactor(context.Request.UnitOfMeasure.Value, context.Product.BaseUnitOfMeasure.Value, factors, "95");
        decimal down = GetFactor(fileUom, context.Product.BaseUnitOfMeasure.Value, factors, "96");
        var dates = ImmutableArray.Create(
            Range(row.HeaderEffective, row.HeaderExpiration), Range(row.LineEffective, row.LineExpiration),
            Range(row.AssignmentEffective, row.AssignmentExpiration), Range(row.VendorEffective, row.VendorExpiration));
        return new IndividualCostContractCandidate(
            new ContractId(row.Contract.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            row.ContractType?.Trim() ?? string.Empty,
            new Money(CobolRoundingPolicy.RoundIntermediate(row.UnitCost * up / down)),
            context.Request.UnitOfMeasure, dates, isExcluded,
            new RuleProvenance("individual-customer-cost-contract", "CCG01/03/04/06/09", "Customer", dates[0]));
    }

    private static PricingDateRange Range(DateTime effective, DateTime? expiration) => new(
        DateOnly.FromDateTime(effective), expiration is null ? null : DateOnly.FromDateTime(expiration.Value));
    private static decimal GetFactor(string uom, string baseUom, IReadOnlyDictionary<string, decimal> factors, string code) =>
        StringComparer.Ordinal.Equals(uom, baseUom) ? 1m : factors.TryGetValue(uom, out decimal factor) ? factor
        : throw new IndividualCostContractRepositoryException($"UOM '{uom}' was not found in VNG05.", code, "60");
    private static string Required(string? value, string field) => string.IsNullOrWhiteSpace(value)
        ? throw new IndividualCostContractRepositoryException($"Required field {field} was empty.") : value.Trim();

    private sealed class Row
    {
        public long Contract { get; init; }
        public string? ContractType { get; init; }
        public string? UnitOfMeasure { get; init; }
        public decimal UnitCost { get; init; }
        public DateTime HeaderEffective { get; init; }
        public DateTime? HeaderExpiration { get; init; }
        public DateTime LineEffective { get; init; }
        public DateTime? LineExpiration { get; init; }
        public DateTime AssignmentEffective { get; init; }
        public DateTime? AssignmentExpiration { get; init; }
        public DateTime VendorEffective { get; init; }
        public DateTime? VendorExpiration { get; init; }
    }
    private sealed class FactorRow { public string? UnitOfMeasure { get; init; } public decimal ConversionFactor { get; init; } }
}
