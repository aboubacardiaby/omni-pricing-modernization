namespace Pricing.Infrastructure.Db2;

using System.Data.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public sealed class Db2ConnectionHealthCheck : IHealthCheck
{
    private const string ProbeSql = "SELECT 1 FROM SYSIBM.SYSDUMMY1";
    private readonly IDb2ConnectionFactory connectionFactory;

    public Db2ConnectionHealthCheck(IDb2ConnectionFactory connectionFactory) =>
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
            command.CommandText = ProbeSql;
            _ = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy("DB2 connection and probe succeeded.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("DB2 connection or probe failed.", exception);
        }
    }
}
