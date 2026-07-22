namespace Pricing.Infrastructure.SqlServer;

using System.Data.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public sealed class SqlServerConnectionHealthCheck : IHealthCheck
{
    private readonly ISqlServerConnectionFactory connectionFactory;

    public SqlServerConnectionHealthCheck(ISqlServerConnectionFactory connectionFactory) =>
        this.connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using DbConnection connection = await connectionFactory
                .OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            _ = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy("SQL Server connection and probe succeeded.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("SQL Server connection or probe failed.", exception);
        }
    }
}
