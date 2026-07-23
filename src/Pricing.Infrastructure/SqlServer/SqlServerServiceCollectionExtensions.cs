namespace Pricing.Infrastructure.SqlServer;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pricing.Infrastructure.Db2;
using Pricing.Application.ProductClassification;
using Pricing.Application.ProductInformation;
using Pricing.Application.SellSelection;
using Pricing.Application.Fees;
using Pricing.Application.CostSelection;
using Pricing.Application.CustomerPricingContext;

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
        services.TryAddSingleton<IDb2CallCounter, Db2CallCounter>();
        services.AddSingleton<ISqlServerQueryExecutor, DapperSqlServerQueryExecutor>();
        services.AddScoped<SqlServerProductRepository>();
        services.AddScoped<IProductClassificationRepository>(provider =>
            provider.GetRequiredService<SqlServerProductRepository>());
        services.AddScoped<IProductInformationRepository>(provider =>
            provider.GetRequiredService<SqlServerProductRepository>());
        services.AddScoped<IPriceLockRepository, SqlServerPriceLockRepository>();
        services.AddScoped<ILowUomRepository, SqlServerLowUomRepository>();
        services.AddScoped<IAcquisitionCostRepository, SqlServerAcquisitionCostRepository>();
        services.AddScoped<ICustomerPricingContextRepository, SqlServerCustomerPricingContextRepository>();
        services.AddScoped<IIndividualCostContractRepository, SqlServerIndividualCostContractRepository>();
        services.AddScoped<IBuyingGroupCostContractRepository, SqlServerBuyingGroupCostContractRepository>();
        services.AddHealthChecks()
            .AddCheck<SqlServerConnectionHealthCheck>("sqlserver", tags: ["ready"]);
        return services;
    }
}
