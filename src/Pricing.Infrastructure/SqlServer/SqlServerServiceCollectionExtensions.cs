namespace Pricing.Infrastructure.SqlServer;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pricing.Application.CustomerPricingContext;
using Pricing.Application.CostSelection;
using Pricing.Application.CostAdjustments;
using Pricing.Application.Legacy;
using Pricing.Application.Orchestration;
using Pricing.Application.ProductClassification;
using Pricing.Application.ProductInformation;
using Pricing.Application.Rebates;
using Pricing.Application.SellSelection;
using Pricing.Application.Fees;
using Pricing.Infrastructure.Diagnostics;

public static class SqlServerServiceCollectionExtensions
{
    public static IServiceCollection AddSqlServerDataAccess(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string? connectionString = configuration.GetConnectionString(SqlServerOptions.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{SqlServerOptions.ConnectionStringName}' must be configured.");
        }

        services.AddOptions<SqlServerOptions>()
            .Bind(configuration.GetSection(SqlServerOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.TryAddSingleton(configuration);
        services.AddSingleton<ISqlServerConnectionFactory, SqlServerConnectionFactory>();
        services.TryAddSingleton<IDatabaseCallCounter, DatabaseCallCounter>();
        services.AddSingleton<ISqlServerQueryExecutor, DapperSqlServerQueryExecutor>();
        services.AddScoped<IProductClassificationRepository, SqlServerProductClassificationRepository>();
        services.AddScoped<IProductInformationRepository, SqlServerProductInformationRepository>();
        services.AddScoped<ICustomerPricingContextRepository, SqlServerCustomerPricingContextRepository>();
        services.AddScoped<ProductClassificationService>();
        services.AddScoped<ProductInformationService>();
        services.AddScoped<CustomerPricingContextService>();
        services.AddScoped<IPricingContextStage, SqlServerPricingContextStage>();
        services.AddScoped<IIndividualCostContractRepository, SqlServerIndividualCostContractRepository>();
        services.AddScoped<IBuyingGroupCostContractRepository, SqlServerBuyingGroupCostContractRepository>();
        services.AddScoped<IHealthcareCostOverrideRepository, SqlServerHealthcareCostOverrideRepository>();
        services.AddScoped<IAcquisitionCostRepository, SqlServerAcquisitionCostRepository>();
        services.AddScoped<ICostRule, SpecialContractCostRule>();
        services.AddScoped<ICostRule, IndividualCustomerCostContractRule>();
        services.AddScoped<ICostRule, BuyingGroupCostContractRule>();
        services.AddScoped<ICostRule, HealthcareCostOverrideRule>();
        services.AddScoped<ICostRule, AcquisitionDealerCostFallbackRule>();
        services.AddScoped<CostRuleEvaluator>();
        services.AddScoped<ICostPricingStage, SqlServerCostPricingStage>();
        services.AddScoped<IRebateCalculationInputRepository, SqlServerRebateCalculationInputRepository>();
        services.AddScoped<IVendorCostAdjustmentRepository, SqlServerVendorCostAdjustmentRepository>();
        services.AddScoped<VendorCostAdjustmentService>();
        services.AddScoped<IRebatePricingStage, SqlServerRebatePricingStage>();
        services.AddScoped<IAccountCustomerSellArrangementRepository, SqlServerAccountCustomerSellArrangementRepository>();
        services.AddScoped<IBuyingGroupSellArrangementRepository, SqlServerBuyingGroupSellArrangementRepository>();
        services.AddScoped<ICorporateSellArrangementRepository, SqlServerCorporateSellArrangementRepository>();
        services.AddScoped<IPriceLockRepository, SqlServerPriceLockRepository>();
        services.AddScoped<PriceLockService>();
        services.AddScoped<ISellPriceCalculationStrategy, GrossMarginCalculationStrategy>();
        services.AddScoped<ISellPriceCalculationStrategy, CostPlusCalculationStrategy>();
        services.AddScoped<ISellPriceCalculationStrategy, ListPriceCalculationStrategy>();
        services.AddScoped<ISellPriceCalculationStrategy, CostDiscountCalculationStrategy>();
        services.AddScoped<ISellPriceCalculationStrategy, SuggestedSellCalculationStrategy>();
        services.AddScoped<ISellPriceCalculationStrategy, SuggestedSellMarkupCalculationStrategy>();
        services.AddScoped<ISellPriceCalculationStrategy, SuggestedSellMarkdownCalculationStrategy>();
        services.AddScoped<ISellPriceCalculationStrategy, StatedPriceCalculationStrategy>();
        services.AddScoped<SellPriceCalculationDispatcher>();
        services.AddScoped(provider => new SellArrangementRuleEvaluator(SellArrangementRuleSet.Create(
            provider.GetRequiredService<IAccountCustomerSellArrangementRepository>(),
            provider.GetRequiredService<IBuyingGroupSellArrangementRepository>(),
            provider.GetRequiredService<ICorporateSellArrangementRepository>())));
        services.AddScoped<ISellPricingStage, SqlServerSellPricingStage>();
        services.AddScoped<IFreightRepository, SqlServerFreightRepository>();
        services.AddScoped<ILowUomRepository, SqlServerLowUomRepository>();
        services.AddScoped<IPandacRepository, SqlServerPandacRepository>();
        services.AddScoped<ISurchargeRepository, SqlServerSurchargeRepository>();
        services.AddScoped<FreightEngine>();
        services.AddScoped<LowUomBreakBulkEngine>();
        services.AddScoped<PandacEngine>();
        services.AddScoped<SurchargeEngine>();
        services.AddScoped<IFeePricingStage, SqlServerFeePricingStage>();
        services.AddScoped<PricingOrchestrator>();
        services.AddScoped<IPricingOrchestrator>(provider => provider.GetRequiredService<PricingOrchestrator>());
        services.AddScoped<IPricingCalculationService, PricingCalculationService>();
        services.AddScoped<ILegacyPricingDetailsRepository, SqlServerLegacyPricingDetailsRepository>();
        services.AddHealthChecks()
            .AddCheck<SqlServerConnectionHealthCheck>("sqlserver", tags: ["ready"]);
        return services;
    }
}
