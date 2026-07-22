namespace ParityRunner;

using System.Collections.Immutable;
using System.Text.Json;

public sealed record ParityCase(string Id, JsonElement Input);

public sealed record EngineObservation(
    int StatusCode,
    JsonElement? JsonBody,
    string? NonJsonBody,
    long ElapsedMilliseconds,
    string? CorrelationId);

public sealed record ParityCaseResult(
    string Id,
    JsonElement Input,
    EngineObservation Cobol,
    EngineObservation CSharp);

public sealed record ParityRunReport(
    int SchemaVersion,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    string CobolAdapterEndpoint,
    string CSharpEndpoint,
    ImmutableArray<ParityCaseResult> Cases);
