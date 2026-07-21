namespace Pricing.Infrastructure.Db2;

using System.ComponentModel.DataAnnotations;

public sealed class Db2Options
{
    public const string SectionName = "Db2";

    [Required]
    public string ProviderInvariantName { get; init; } = string.Empty;

    [Required]
    public string ConnectionString { get; init; } = string.Empty;

    [Range(1, 300)]
    public int CommandTimeoutSeconds { get; init; } = 30;
}
