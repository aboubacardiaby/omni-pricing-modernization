namespace PricingLoadRunner;

using System.Collections.Immutable;
using System.Text.Json;

public sealed record LoadPlan(
    int SchemaVersion,
    Uri Endpoint,
    int WarmupRequests,
    int RequestsPerWorkload,
    int Concurrency,
    ImmutableArray<LoadWorkload> Workloads);

public sealed record LoadWorkload(
    string Id,
    string Dimension,
    string Level,
    int? KitComponentCount,
    int? KitDepth,
    int? OrderLineCount,
    JsonElement Request);

public sealed record LoadObservation(
    int StatusCode,
    double ElapsedMilliseconds,
    int? DbCallCount,
    long? ServerWorkingSetBytes);

public sealed record WorkloadMetrics(
    string Id,
    string Dimension,
    string Level,
    int? KitComponentCount,
    int? KitDepth,
    int? OrderLineCount,
    int Requests,
    int Errors,
    decimal ErrorRate,
    double ThroughputPerSecond,
    double P50Milliseconds,
    double P95Milliseconds,
    double P99Milliseconds,
    long? TotalDbCalls,
    double? AverageDbCallsPerRequest,
    long? MinimumServerWorkingSetBytes,
    long? PeakServerWorkingSetBytes,
    long? ServerWorkingSetGrowthBytes,
    ImmutableArray<string> MissingMeasurements);

public sealed record KitSizeEffect(
    string WorkloadId,
    int ComponentCount,
    int? Depth,
    double P95Milliseconds,
    double P95RatioToSmallestKit,
    double ThroughputPerSecond);

public sealed record LoadTestReport(
    int SchemaVersion,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    Uri Endpoint,
    int Concurrency,
    ImmutableArray<WorkloadMetrics> Workloads,
    ImmutableArray<KitSizeEffect> KitSizeEffects,
    string MemoryMeasurement,
    string DbCallMeasurement);
