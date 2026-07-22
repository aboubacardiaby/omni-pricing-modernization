namespace Pricing.Infrastructure.Db2;

using System.Data.Common;
using System.Diagnostics;
using Dapper;
using Microsoft.Extensions.Options;

public sealed class DapperDb2QueryExecutor : IDb2QueryExecutor
{
    public const string ActivitySourceName = "Pricing.Infrastructure.Db2";
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    private readonly IDb2ConnectionFactory connectionFactory;
    private readonly int commandTimeoutSeconds;
    private readonly IDb2CallCounter? callCounter;

    public DapperDb2QueryExecutor(
        IDb2ConnectionFactory connectionFactory,
        IOptions<Db2Options> options,
        IDb2CallCounter? callCounter = null)
    {
        this.connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        commandTimeoutSeconds = options?.Value.CommandTimeoutSeconds
            ?? throw new ArgumentNullException(nameof(options));
        this.callCounter = callCounter;
    }

    public async ValueTask<T?> QuerySingleOrDefaultAsync<T>(
        Db2Query query,
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
        Db2Query query,
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
        Activity? activity = ActivitySource.StartActivity("db2.query", ActivityKind.Client);
        activity?.SetTag("db.system.name", "db2");
        activity?.SetTag("db.operation.name", operation);
        return activity;
    }

    private static Db2AccessException MapFailure(string operation, DbException exception, Activity? activity)
    {
        bool transient = Db2TransientErrorDetector.IsTransient(exception);
        activity?.SetTag("error.type", exception.GetType().FullName);
        activity?.SetStatus(ActivityStatusCode.Error, "DB2 query failed");
        return new Db2AccessException(
            operation,
            $"DB2 operation '{operation}' failed.",
            exception.SqlState,
            exception.ErrorCode,
            transient,
            exception);
    }
}
