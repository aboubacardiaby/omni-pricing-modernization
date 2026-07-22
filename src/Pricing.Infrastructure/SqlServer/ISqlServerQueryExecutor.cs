namespace Pricing.Infrastructure.SqlServer;

public interface ISqlServerQueryExecutor
{
    ValueTask<T?> QuerySingleOrDefaultAsync<T>(
        SqlServerQuery query,
        object? parameters,
        CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<T>> QueryAsync<T>(
        SqlServerQuery query,
        object? parameters,
        CancellationToken cancellationToken);
}

public sealed record SqlServerQuery
{
    public SqlServerQuery(string operation, string commandText)
    {
        Operation = string.IsNullOrWhiteSpace(operation)
            ? throw new ArgumentException("A safe query operation name is required.", nameof(operation))
            : operation;
        CommandText = string.IsNullOrWhiteSpace(commandText)
            ? throw new ArgumentException("SQL command text is required.", nameof(commandText))
            : commandText;
    }

    public string Operation { get; }
    public string CommandText { get; }
}
