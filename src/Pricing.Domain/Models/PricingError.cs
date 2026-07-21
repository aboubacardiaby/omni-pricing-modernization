namespace Pricing.Domain.Models;

using System.Text.Json.Serialization;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "errorType")]
[JsonDerivedType(typeof(ValidationPricingError), "validation")]
[JsonDerivedType(typeof(MissingDataPricingError), "missingData")]
[JsonDerivedType(typeof(DependencyPricingError), "dependency")]
[JsonDerivedType(typeof(UnsupportedBehaviorPricingError), "unsupportedBehavior")]
public abstract record PricingError(
    string Code,
    string Message,
    string? LegacyErrorCode,
    string? LegacySeverityCode);

public sealed record ValidationPricingError(
    string Code,
    string Message,
    string? LegacyErrorCode = null,
    string? Field = null,
    string? LegacySeverityCode = null)
    : PricingError(Code, Message, LegacyErrorCode, LegacySeverityCode);

public sealed record MissingDataPricingError(
    string Code,
    string Message,
    string? LegacyErrorCode = null,
    string? Entity = null,
    string? LegacySeverityCode = null)
    : PricingError(Code, Message, LegacyErrorCode, LegacySeverityCode);

public sealed record DependencyPricingError(
    string Code,
    string Message,
    string? LegacyErrorCode = null,
    bool IsTransient = false,
    string? LegacySeverityCode = null)
    : PricingError(Code, Message, LegacyErrorCode, LegacySeverityCode);

public sealed record UnsupportedBehaviorPricingError(
    string Code,
    string Message,
    string? LegacyErrorCode = null,
    string? Blocker = null,
    string? LegacySeverityCode = null)
    : PricingError(Code, Message, LegacyErrorCode, LegacySeverityCode);
