namespace Pricing.Infrastructure.SqlServer;

using Pricing.Application.CostSelection;
using Pricing.Application.ExpirationDates;
using Pricing.Application.Orchestration;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;

/// <summary>Runs A6U01's ordered cost-source cascade over the SQL Server repositories.</summary>
public sealed class SqlServerCostPricingStage(CostRuleEvaluator evaluator) : ICostPricingStage
{
    public async ValueTask<CostPricingStageResult> ExecuteAsync(PricingContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        CostSelectionResult result = await evaluator.EvaluateAsync(context, cancellationToken).ConfigureAwait(false);
        if (result.Error is not null || result.Selection is null)
        {
            return new CostPricingStageResult(context, new Money(0m), [], [], [], result.Error ??
                new MissingDataPricingError("COST_SOURCE_NOT_FOUND", "No cost source was selected.", "130", "cost"));
        }

        ICostSourceSelection selection = result.Selection;
        PricingContext selectedContext = context with
        {
            CostSelection = selection,
            ContractSelection = selection as ContractSelection,
        };
        var component = new PriceComponent("Selected base cost", PriceComponentType.BaseCost,
            selection.UnitCost, selection.Provenance);
        DateOnly? expiration = selection.Provenance.EffectiveDates?.ExpirationDate;
        ExpirationDateSource[] expirations = expiration is null ? [] :
            [new ExpirationDateSource("Selected base cost", expiration, selection.Provenance)];
        return new CostPricingStageResult(selectedContext, selection.UnitCost, [component], [.. expirations], []);
    }
}
