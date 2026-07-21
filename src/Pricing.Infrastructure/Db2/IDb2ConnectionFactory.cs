namespace Pricing.Infrastructure.Db2;

using System.Data.Common;

public interface IDb2ConnectionFactory
{
    ValueTask<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken);
}
