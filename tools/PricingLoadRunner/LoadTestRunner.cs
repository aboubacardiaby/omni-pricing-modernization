namespace PricingLoadRunner;

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;

public sealed class LoadTestRunner(ILoadTarget target, TimeProvider? timeProvider = null)
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;

    public async Task<LoadTestReport> RunAsync(LoadPlan plan, CancellationToken cancellationToken)
    {
        Validate(plan);
        DateTimeOffset started = clock.GetUtcNow();
        var metrics = ImmutableArray.CreateBuilder<WorkloadMetrics>(plan.Workloads.Length);
        foreach (LoadWorkload workload in plan.Workloads)
        {
            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(workload.Request);
            for (int warmup = 0; warmup < plan.WarmupRequests; warmup++)
            {
                await target.ExecuteAsync(payload, cancellationToken).ConfigureAwait(false);
            }

            var observations = new ConcurrentBag<LoadObservation>();
            using var gate = new SemaphoreSlim(plan.Concurrency, plan.Concurrency);
            long runStarted = Stopwatch.GetTimestamp();
            Task[] requests = Enumerable.Range(0, plan.RequestsPerWorkload).Select(async _ =>
            {
                await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    observations.Add(await target.ExecuteAsync(payload, cancellationToken).ConfigureAwait(false));
                }
                finally
                {
                    gate.Release();
                }
            }).ToArray();
            await Task.WhenAll(requests).ConfigureAwait(false);
            double elapsedSeconds = Math.Max(Stopwatch.GetElapsedTime(runStarted).TotalSeconds, 0.000001d);
            metrics.Add(Calculate(workload, observations.ToArray(), elapsedSeconds));
        }

        ImmutableArray<WorkloadMetrics> workloadMetrics = metrics.ToImmutable();
        return new(
            1,
            started,
            clock.GetUtcNow(),
            plan.Endpoint,
            plan.Concurrency,
            workloadMetrics,
            KitEffects(workloadMetrics),
            "Server process working-set snapshots from X-Process-Working-Set-Bytes; unavailable measurements remain null.",
            "Per-request DB calls from X-DB-Call-Count; unavailable measurements remain null.");
    }

    public static WorkloadMetrics Calculate(
        LoadWorkload workload,
        IReadOnlyCollection<LoadObservation> observations,
        double elapsedSeconds)
    {
        double[] latencies = observations.Select(item => item.ElapsedMilliseconds).Order().ToArray();
        int errors = observations.Count(item => item.StatusCode is < 200 or >= 300);
        int[] dbCalls = observations.Where(item => item.DbCallCount.HasValue).Select(item => item.DbCallCount!.Value).ToArray();
        long[] memory = observations.Where(item => item.ServerWorkingSetBytes.HasValue).Select(item => item.ServerWorkingSetBytes!.Value).ToArray();
        var missing = ImmutableArray.CreateBuilder<string>();
        if (dbCalls.Length != observations.Count)
        {
            missing.Add("dbCalls");
        }

        if (memory.Length != observations.Count)
        {
            missing.Add("serverWorkingSet");
        }

        return new(
            workload.Id,
            workload.Dimension,
            workload.Level,
            workload.KitComponentCount,
            workload.KitDepth,
            workload.OrderLineCount,
            observations.Count,
            errors,
            observations.Count == 0 ? 0m : (decimal)errors / observations.Count,
            observations.Count / elapsedSeconds,
            Percentile(latencies, 0.50),
            Percentile(latencies, 0.95),
            Percentile(latencies, 0.99),
            dbCalls.Length == observations.Count ? dbCalls.Sum(value => (long)value) : null,
            dbCalls.Length == observations.Count ? dbCalls.Average() : null,
            memory.Length == observations.Count ? memory.Min() : null,
            memory.Length == observations.Count ? memory.Max() : null,
            memory.Length == observations.Count ? memory.Max() - memory.Min() : null,
            missing.ToImmutable());
    }

    private static double Percentile(double[] sorted, double percentile)
    {
        if (sorted.Length == 0)
        {
            return 0d;
        }

        int rank = (int)Math.Ceiling(percentile * sorted.Length) - 1;
        return sorted[Math.Clamp(rank, 0, sorted.Length - 1)];
    }

    private static ImmutableArray<KitSizeEffect> KitEffects(ImmutableArray<WorkloadMetrics> workloads)
    {
        WorkloadMetrics[] kits = workloads
            .Where(item => item.KitComponentCount.HasValue)
            .OrderBy(item => item.KitComponentCount)
            .ToArray();
        if (kits.Length == 0)
        {
            return [];
        }

        double baseline = Math.Max(kits[0].P95Milliseconds, 0.000001d);
        return kits.Select(item => new KitSizeEffect(
            item.Id,
            item.KitComponentCount!.Value,
            item.KitDepth,
            item.P95Milliseconds,
            item.P95Milliseconds / baseline,
            item.ThroughputPerSecond)).ToImmutableArray();
    }

    private static void Validate(LoadPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (plan.RequestsPerWorkload <= 0 || plan.Concurrency <= 0 || plan.WarmupRequests < 0)
        {
            throw new ArgumentException("Requests and concurrency must be positive; warmup cannot be negative.", nameof(plan));
        }

        if (plan.Workloads.IsDefaultOrEmpty)
        {
            throw new ArgumentException("At least one workload is required.", nameof(plan));
        }

        if (plan.Workloads.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() != plan.Workloads.Length)
        {
            throw new ArgumentException("Workload ids must be unique.", nameof(plan));
        }
    }
}
