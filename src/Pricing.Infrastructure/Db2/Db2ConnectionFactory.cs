namespace Pricing.Infrastructure.Db2;

using System.Data.Common;
using Microsoft.Extensions.Options;

public sealed class Db2ConnectionFactory : IDb2ConnectionFactory
{
    private readonly Db2Options options;

    public Db2ConnectionFactory(IOptions<Db2Options> options) =>
        this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    public async ValueTask<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DbProviderFactory provider = DbProviderFactories.GetFactory(options.ProviderInvariantName);
        DbConnection connection = provider.CreateConnection()
            ?? throw new InvalidOperationException("The configured DB2 provider did not create a connection.");
        connection.ConnectionString = options.ConnectionString;

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
