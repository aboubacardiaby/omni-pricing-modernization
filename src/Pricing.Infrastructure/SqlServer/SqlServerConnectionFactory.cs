namespace Pricing.Infrastructure.SqlServer;

using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

public sealed class SqlServerConnectionFactory : ISqlServerConnectionFactory
{
    private readonly string connectionString;

    public SqlServerConnectionFactory(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        connectionString = configuration.GetConnectionString(SqlServerOptions.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{SqlServerOptions.ConnectionStringName}' is not configured.");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{SqlServerOptions.ConnectionStringName}' must not be empty.");
        }
    }

    public DbConnection CreateConnection() => new SqlConnection(connectionString);

    public async ValueTask<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DbConnection connection = CreateConnection();
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}
