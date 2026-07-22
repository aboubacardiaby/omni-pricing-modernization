namespace Pricing.Infrastructure.SqlServer;

using System.Data.Common;

public interface ISqlServerConnectionFactory
{
    DbConnection CreateConnection();

    ValueTask<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken);
}
