namespace Pricing.Infrastructure.Db2;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class Db2ServiceCollectionExtensions
{
    public static IServiceCollection AddDb2DataAccess(
        this IServiceCollection services,
        IConfigurationSection configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<Db2Options>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IDb2ConnectionFactory, Db2ConnectionFactory>();
        services.TryAddSingleton<IDb2CallCounter, Db2CallCounter>();
        services.AddSingleton<IDb2QueryExecutor, DapperDb2QueryExecutor>();
        services.AddHealthChecks()
            .AddCheck<Db2ConnectionHealthCheck>("db2", tags: ["ready"]);
        return services;
    }
}
