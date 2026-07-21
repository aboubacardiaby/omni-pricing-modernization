namespace Pricing.Infrastructure.Db2;

public sealed class Db2AccessException : Exception
{
    public Db2AccessException(
        string operation,
        string message,
        string? sqlState,
        int providerErrorCode,
        bool isTransient,
        Exception innerException)
        : base(message, innerException)
    {
        Operation = operation;
        SqlState = sqlState;
        ProviderErrorCode = providerErrorCode;
        IsTransient = isTransient;
    }

    public string Operation { get; }
    public string? SqlState { get; }
    public int ProviderErrorCode { get; }
    public bool IsTransient { get; }
}
