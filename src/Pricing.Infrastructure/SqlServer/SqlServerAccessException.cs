namespace Pricing.Infrastructure.SqlServer;

public sealed class SqlServerAccessException : Exception
{
    public SqlServerAccessException(
        string operation,
        int errorNumber,
        bool isTransient,
        Exception innerException)
        : base($"SQL Server operation '{operation}' failed.", innerException)
    {
        Operation = operation;
        ErrorNumber = errorNumber;
        IsTransient = isTransient;
    }

    public string Operation { get; }
    public int ErrorNumber { get; }
    public bool IsTransient { get; }
}
