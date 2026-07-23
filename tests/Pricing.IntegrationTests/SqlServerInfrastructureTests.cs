namespace Pricing.IntegrationTests;

using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Pricing.Infrastructure.SqlServer;
using Pricing.Application.ProductClassification;
using Pricing.Application.ProductInformation;
using Pricing.Application.CostSelection;
using Pricing.Application.CustomerPricingContext;
using Xunit;

public sealed class SqlServerInfrastructureTests
{
    private const string TestConnectionString =
        "Server=localhost;Database=Pricing;User ID=test-user;Password=test-password;" +
        "Encrypt=True;TrustServerCertificate=True";

    [Fact]
    public void ConnectionFactoryCreatesSqlClientConnectionWithoutOpeningIt()
    {
        IConfiguration configuration = CreateConfiguration(TestConnectionString);
        var factory = new SqlServerConnectionFactory(configuration);

        using var connection = factory.CreateConnection();

        var sqlConnection = Assert.IsType<SqlConnection>(connection);
        Assert.Equal(ConnectionState.Closed, sqlConnection.State);
        Assert.Equal("Pricing", sqlConnection.Database);
        Assert.Equal("localhost", sqlConnection.DataSource);
    }

    [Fact]
    public void RegistrationProvidesFactoryExecutorAndHealthCheck()
    {
        IConfiguration configuration = CreateConfiguration(TestConnectionString);
        var services = new ServiceCollection();

        services.AddSqlServerDataAccess(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsType<SqlServerConnectionFactory>(provider.GetRequiredService<ISqlServerConnectionFactory>());
        Assert.IsType<DapperSqlServerQueryExecutor>(provider.GetRequiredService<ISqlServerQueryExecutor>());
        using IServiceScope scope = provider.CreateScope();
        IProductClassificationRepository classification =
            scope.ServiceProvider.GetRequiredService<IProductClassificationRepository>();
        IProductInformationRepository information =
            scope.ServiceProvider.GetRequiredService<IProductInformationRepository>();
        Assert.IsType<SqlServerProductRepository>(classification);
        Assert.Same(classification, information);
        Assert.IsType<SqlServerAcquisitionCostRepository>(
            scope.ServiceProvider.GetRequiredService<IAcquisitionCostRepository>());
        Assert.IsType<SqlServerCustomerPricingContextRepository>(
            scope.ServiceProvider.GetRequiredService<ICustomerPricingContextRepository>());
        Assert.IsType<SqlServerIndividualCostContractRepository>(
            scope.ServiceProvider.GetRequiredService<IIndividualCostContractRepository>());
        Assert.IsType<SqlServerBuyingGroupCostContractRepository>(
            scope.ServiceProvider.GetRequiredService<IBuyingGroupCostContractRepository>());
        Assert.Contains(
            provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations,
            registration => registration.Name == "sqlserver");
    }

    [Fact]
    public void RegistrationRejectsMissingConnectionString()
    {
        IConfiguration configuration = CreateConfiguration(null);
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddSqlServerDataAccess(configuration));

        Assert.Contains(SqlServerOptions.ConnectionStringName, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void QueryRequiresSafeOperationAndSqlText()
    {
        Assert.Throws<ArgumentException>(() => new SqlServerQuery("", "SELECT 1"));
        Assert.Throws<ArgumentException>(() => new SqlServerQuery("health.probe", ""));
    }

    private static IConfiguration CreateConfiguration(string? connectionString)
    {
        var values = new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{SqlServerOptions.ConnectionStringName}"] = connectionString,
            [$"{SqlServerOptions.SectionName}:CommandTimeoutSeconds"] = "15",
        };
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
