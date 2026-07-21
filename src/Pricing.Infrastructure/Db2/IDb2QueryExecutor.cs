namespace Pricing.Infrastructure.Db2;

public interface IDb2QueryExecutor
{
    ValueTask<T?> QuerySingleOrDefaultAsync<T>(
        Db2Query query,
        object? parameters,
        CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<T>> QueryAsync<T>(
        Db2Query query,
        object? parameters,
        CancellationToken cancellationToken);
}

public sealed record Db2Query
{
    public Db2Query(string operation, string commandText)
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
