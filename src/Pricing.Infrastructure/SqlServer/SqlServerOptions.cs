namespace Pricing.Infrastructure.SqlServer;

using System.ComponentModel.DataAnnotations;

public sealed class SqlServerOptions
{
    public const string SectionName = "SqlServer";
    public const string ConnectionStringName = "PricingDatabase";

    [Range(1, 300)]
    public int CommandTimeoutSeconds { get; init; } = 30;
}
