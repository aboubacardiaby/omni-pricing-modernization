namespace Pricing.Infrastructure.SqlServer;

using System.Data.Common;
using System.Diagnostics;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Pricing.Infrastructure.Diagnostics;

public sealed class DapperSqlServerQueryExecutor : ISqlServerQueryExecutor
{
    public const string ActivitySourceName = "Pricing.Infrastructure.SqlServer";
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private static readonly HashSet<int> TransientErrorNumbers =
    [
        -2, 20, 64, 233, 1205, 4060, 10928, 10929, 40197, 40501, 40613, 49918, 49919, 49920,
    ];

    private readonly ISqlServerConnectionFactory connectionFactory;
    private readonly int commandTimeoutSeconds;
    private readonly IDatabaseCallCounter? callCounter;

    public DapperSqlServerQueryExecutor(
        ISqlServerConnectionFactory connectionFactory,
        IOptions<SqlServerOptions> options,
        IDatabaseCallCounter? callCounter = null)
    {
        this.connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        commandTimeoutSeconds = options?.Value.CommandTimeoutSeconds
            ?? throw new ArgumentNullException(nameof(options));
        this.callCounter = callCounter;
    }

    public async ValueTask<T?> QuerySingleOrDefaultAsync<T>(
        SqlServerQuery query,
        object? parameters,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        callCounter?.Increment();
        using Activity? activity = StartActivity(query.Operation);
        try
        {
            await using DbConnection connection = await connectionFactory
                .OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var command = new CommandDefinition(
                query.CommandText,
                parameters,
                commandTimeout: commandTimeoutSeconds,
                cancellationToken: cancellationToken);
            return await connection.QuerySingleOrDefaultAsync<T>(command).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "cancelled");
            throw;
        }
        catch (DbException exception)
        {
            throw MapFailure(query.Operation, exception, activity);
        }
    }

    public async ValueTask<IReadOnlyList<T>> QueryAsync<T>(
        SqlServerQuery query,
        object? parameters,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        callCounter?.Increment();
        using Activity? activity = StartActivity(query.Operation);
        try
        {
            await using DbConnection connection = await connectionFactory
                .OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var command = new CommandDefinition(
                query.CommandText,
                parameters,
                commandTimeout: commandTimeoutSeconds,
                cancellationToken: cancellationToken);
            IEnumerable<T> rows = await connection.QueryAsync<T>(command).ConfigureAwait(false);
            return rows.AsList();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "cancelled");
            throw;
        }
        catch (DbException exception)
        {
            throw MapFailure(query.Operation, exception, activity);
        }
    }

    private static Activity? StartActivity(string operation)
    {
        Activity? activity = ActivitySource.StartActivity("sqlserver.query", ActivityKind.Client);
        activity?.SetTag("db.system.name", "microsoft.sql_server");
        activity?.SetTag("db.operation.name", operation);
        return activity;
    }

    private static SqlServerAccessException MapFailure(
        string operation,
        DbException exception,
        Activity? activity)
    {
        int errorNumber = exception is SqlException sqlException
            ? sqlException.Number
            : exception.ErrorCode;
        bool transient = exception is SqlException sql &&
            sql.Errors.Cast<SqlError>().Any(error => TransientErrorNumbers.Contains(error.Number));
        activity?.SetTag("error.type", exception.GetType().FullName);
        activity?.SetStatus(ActivityStatusCode.Error, "SQL Server query failed");
        return new SqlServerAccessException(operation, errorNumber, transient, exception);
    }
}
