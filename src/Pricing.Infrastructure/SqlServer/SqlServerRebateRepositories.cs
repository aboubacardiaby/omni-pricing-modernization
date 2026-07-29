namespace Pricing.Infrastructure.SqlServer;

using System.Collections.Immutable;
using Pricing.Application.CostAdjustments;
using Pricing.Application.Rebates;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>CCG01/03/13/14, VNG01 and VNG03 inputs for A6U01 7070-PRO-REBT-AMTS-010.</summary>
public sealed class SqlServerRebateCalculationInputRepository(ISqlServerQueryExecutor executor)
    : IRebateCalculationInputRepository
{
    private static readonly SqlServerQuery Query = new("rebate.inputs", """
        SELECT TOP (1)
          RTRIM(c.C_CNT_ENTRY_METHOD) AS ContractEntryMethod,
          l.A_CNT_LN_UNIT_COST AS ContractUnitCost, RTRIM(l.C_CNT_LN_UM) AS ContractUom,
          l.A_CNT_LN_PROT_ACQ AS ProtectedAcquisitionCost,
          l.A_CNT_LN_MAX_REBT AS MaximumRebate,
          l.D_CNT_LINE_START AS ProtectionEffective, l.D_CNT_LINE_EXPIRE AS ProtectionExpiration,
          COALESCE(b.A_CNT_LN_SUGG_SELL, p.A_CNT_LN_SUGG_SELL, 0) AS SuggestedSell,
          COALESCE(b.P_CNT_LN_BROKER, 0) AS BrokerPercentage,
          COALESCE(p.P_CNT_LN_COST_PLUS, 0) AS CostPlusPercentage,
          COALESCE(v.F_VEND_NET_CST_RBT, 'N') AS ApplyVendorNetCostRebate,
          COALESCE(v.F_VEND_BST_CST_RBT, 'N') AS UseBestCostForRebate,
          COALESCE(v.P_VEND_NET_CST_RBT, 0) AS VendorNetCostRebatePercentage,
          COALESCE(v.F_NEGATIVE_REBATE, 'N') AS AllowNegativeRebate,
          d.A_VND_PRC_DEALER AS DealerCost, d.A_VND_PRC_ACQ_COST AS AcquisitionCost,
          COALESCE(d1.A_VND_PRC_DEALER, d.A_VND_PRC_DEALER) AS Level01DealerCost,
          RTRIM(d.C_VND_PRC_UM) AS PriceUom
        FROM dbo.CCG01 c
        JOIN dbo.CCG03 l ON l.I_CONTRACT=c.I_CONTRACT
        JOIN dbo.VNG03 d ON d.I_VENDOR=l.I_VENDOR AND d.I_VND_PRODUCT=l.I_VND_PRODUCT
        LEFT JOIN dbo.VNG03 d1 ON d1.I_VENDOR=l.I_VENDOR AND d1.I_VND_PRODUCT=l.I_VND_PRODUCT
          AND d1.C_VND_PRC_LEVEL='01' AND d1.D_VND_PRC_LIST_EFF<=@PricingDate
          AND (d1.D_VND_PRC_EXPIRE>=@PricingDate OR d1.D_VND_PRC_EXPIRE IS NULL)
        LEFT JOIN dbo.VNG01 v ON v.I_VENDOR=l.I_VENDOR
        LEFT JOIN dbo.CCG13 b ON b.I_CONTRACT=l.I_CONTRACT AND b.L_CNT_LINE=l.L_CNT_LINE
        LEFT JOIN dbo.CCG14 p ON p.I_CONTRACT=l.I_CONTRACT AND p.L_CNT_LINE=l.L_CNT_LINE
        WHERE l.I_CONTRACT=@Contract AND l.I_VENDOR=@Vendor AND l.I_VND_PRODUCT=@Product
          AND d.D_VND_PRC_LIST_EFF<=@PricingDate
          AND (d.D_VND_PRC_EXPIRE>=@PricingDate OR d.D_VND_PRC_EXPIRE IS NULL)
        ORDER BY d.D_VND_PRC_LIST_EFF DESC
        """);

    public async ValueTask<RebateCalculationInput> LoadAsync(
        PricingContext context, ContractSelection contract, CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<Row> rows = await executor.QueryAsync<Row>(Query, new
            {
                Contract = contract.Contract.Value,
                Vendor = context.Request.Vendor.Value,
                Product = context.Request.Product.Value,
                PricingDate = context.Request.PricingDate.ToDateTime(TimeOnly.MinValue),
            }, cancellationToken).ConfigureAwait(false);
            Row row = rows.Count > 0 ? rows[0] : throw new RebateCalculationInputRepositoryException(
                $"No rebate inputs were found for contract {contract.Contract.Value}.", "120", "90");
            IReadOnlyDictionary<string, decimal> factors =
                await SqlServerCostUomConverter.LoadAsync(executor, context, cancellationToken).ConfigureAwait(false);

            Money Normalize(decimal amount, string uom)
            {
                if (!SqlServerCostUomConverter.TryNormalize(amount, uom, context.Request.UnitOfMeasure, factors, out Money value))
                {
                    throw new RebateCalculationInputRepositoryException(
                        $"No UOM conversion exists from {uom} to {context.Request.UnitOfMeasure.Value}.", "122", "90");
                }
                return value;
            }

            PricingDateRange? protection = row.ProtectionEffective is null
                ? null
                : new PricingDateRange(SqlServerCostMapping.Date(row.ProtectionEffective.Value),
                    SqlServerCostMapping.Date(row.ProtectionExpiration));
            return new RebateCalculationInput(true, row.ContractEntryMethod.Trim(), context.Request.PricingDate,
                Normalize(row.ContractUnitCost, row.ContractUom), Normalize(row.DealerCost, row.PriceUom),
                Normalize(row.Level01DealerCost, row.PriceUom), SqlServerCostMapping.Yes(row.UseBestCostForRebate),
                Normalize(row.AcquisitionCost, row.PriceUom), Normalize(row.SuggestedSell, row.ContractUom),
                row.BrokerPercentage, row.CostPlusPercentage, SqlServerCostMapping.Yes(row.ApplyVendorNetCostRebate),
                row.VendorNetCostRebatePercentage, SqlServerCostMapping.Yes(row.AllowNegativeRebate),
                Normalize(row.ProtectedAcquisitionCost, row.ContractUom), protection,
                Normalize(row.MaximumRebate, row.ContractUom));
        }
        catch (SqlServerAccessException exception)
        {
            throw new RebateCalculationInputRepositoryException(
                "Contract rebate SQL lookup failed.", "120", "90", exception.IsTransient, exception);
        }
    }

    private sealed record Row(string ContractEntryMethod, decimal ContractUnitCost, string ContractUom,
        decimal ProtectedAcquisitionCost, decimal MaximumRebate, DateTime? ProtectionEffective,
        DateTime? ProtectionExpiration, decimal SuggestedSell, decimal BrokerPercentage,
        decimal CostPlusPercentage, string ApplyVendorNetCostRebate, string UseBestCostForRebate,
        decimal VendorNetCostRebatePercentage, string AllowNegativeRebate, decimal DealerCost,
        decimal AcquisitionCost, decimal Level01DealerCost, string PriceUom);
}

/// <summary>A6U01 0225/0230 first-match vendor-cost-adjustment waterfall.</summary>
public sealed class SqlServerVendorCostAdjustmentRepository(ISqlServerQueryExecutor executor)
    : IVendorCostAdjustmentRepository
{
    private static readonly SqlServerQuery ContractQuery = new("cost-adjustment.contract", """
        SELECT EvaluationOrder, Percentage, SourceCode, EffectiveDate, ExpirationDate, Source
        FROM (
          SELECT 1, a.P_AC_CONT_COSTADJ, '23', a.D_AC_CONT_COSTADJ_EF, a.D_AC_CONT_COSTADJ_EX, 'CUG60'
            FROM dbo.CU_AC_CONT_COSTADJ a WHERE a.I_ACCOUNT=@Account AND a.I_CONTRACT=@Contract AND a.C_AC_CONT_FEE_TYPE=@FeeType
          UNION ALL SELECT 2, a.P_AC_VEND_COSTADJ, '24', a.D_AC_VEND_COSTADJ_EF, a.D_AC_VEND_COSTADJ_EX, 'CUG61'
            FROM dbo.CU_AC_VEND_COSTADJ a WHERE a.I_ACCOUNT=@Account AND a.I_VENDOR=@Vendor AND a.C_AC_VEND_CONT_FEE_TYPE=@FeeType
          UNION ALL SELECT 3, a.P_AC_VEND_COSTADJ, '25', a.D_AC_VEND_COSTADJ_EF, a.D_AC_VEND_COSTADJ_EX, 'CUG61'
            FROM dbo.CU_AC_VEND_COSTADJ a WHERE a.I_ACCOUNT=@Account AND a.I_VENDOR='0000' AND a.C_AC_VEND_CONT_FEE_TYPE=@FeeType
          UNION ALL SELECT 40, a.P_CO_CONT_COSTADJ, '20', a.D_CO_CONT_COSTADJ_EF, a.D_CO_CONT_COSTADJ_EX, 'CUG64'
            FROM dbo.CU_CO_CONT_COSTADJ a WHERE a.I_CONTRACT=@Contract AND a.C_CO_CONT_FEE_TYPE=@FeeType
          UNION ALL SELECT 41, a.P_CO_VEND_COSTADJ, '21', a.D_CO_VEND_COSTADJ_EF, a.D_CO_VEND_COSTADJ_EX, 'CUG65'
            FROM dbo.CU_CO_VEND_COSTADJ a WHERE a.I_VENDOR=@Vendor AND a.C_CO_VEND_CONT_FEE_TYPE=@FeeType
          UNION ALL SELECT 42, a.P_CO_VEND_COSTADJ, '22', a.D_CO_VEND_COSTADJ_EF, a.D_CO_VEND_COSTADJ_EX, 'CUG65'
            FROM dbo.CU_CO_VEND_COSTADJ a WHERE a.I_VENDOR='0000' AND a.C_CO_VEND_CONT_FEE_TYPE=@FeeType
        ) x(EvaluationOrder,Percentage,SourceCode,EffectiveDate,ExpirationDate,Source)
        WHERE EffectiveDate<=@PricingDate AND (ExpirationDate>=@PricingDate OR ExpirationDate IS NULL)
        ORDER BY EvaluationOrder
        """);

    public async ValueTask<ImmutableArray<VendorCostAdjustmentCandidate>> FindAsync(
        PricingContext context, bool hasCostContract, CancellationToken cancellationToken)
    {
        if (!hasCostContract)
        {
            // VN_AC_VEND_COSTADJ and VN_CO_VEND_COSTADJ are orphaned legacy codes with no table
            // in the migrated schema; the non-contract waterfall never finds a candidate.
            return ImmutableArray<VendorCostAdjustmentCandidate>.Empty;
        }

        try
        {
            ContractSelection? contract = context.CostSelection as ContractSelection;
            IReadOnlyList<Row> rows = await executor.QueryAsync<Row>(
                ContractQuery,
                new
                {
                    Account = context.Request.Account.Value,
                    Vendor = context.Request.Vendor.Value,
                    Contract = contract?.Contract.Value,
                    FeeType = contract?.BuyingGroupId is null ? "I" : "C",
                    PricingDate = context.Request.PricingDate.ToDateTime(TimeOnly.MinValue),
                }, cancellationToken).ConfigureAwait(false);
            return rows.Select(row =>
            {
                PricingDateRange dates = SqlServerCostMapping.Range(row.EffectiveDate, row.ExpirationDate);
                return new VendorCostAdjustmentCandidate(row.EvaluationOrder, row.Percentage, row.SourceCode,
                    dates, new RuleProvenance("vendor-cost-adjustment",
                        $"A6U01 {(hasCostContract ? "0230" : "0225")}; {row.Source}",
                        row.SourceCode, dates), false);
            }).ToImmutableArray();
        }
        catch (SqlServerAccessException exception)
        {
            throw new VendorCostAdjustmentRepositoryException(
                "Vendor cost-adjustment SQL lookup failed.", "231", "90", exception.IsTransient, exception);
        }
    }

    private sealed record Row(int EvaluationOrder, decimal Percentage, string SourceCode,
        DateTime EffectiveDate, DateTime? ExpirationDate, string Source);
}
