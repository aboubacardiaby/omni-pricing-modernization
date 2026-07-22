namespace Pricing.Infrastructure.SqlServer;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pricing.Infrastructure.Db2;

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
        services.AddHealthChecks()
            .AddCheck<SqlServerConnectionHealthCheck>("sqlserver", tags: ["ready"]);
        return services;
    }
}
