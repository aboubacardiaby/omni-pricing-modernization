namespace Pricing.PerformanceTests;

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.Json;
using PricingLoadRunner;
using Xunit;

public sealed class LoadTestRunnerTests
{
    [Fact]
    public void CalculateReportsLatencyThroughputErrorsDatabaseCallsAndMemory()
    {
        LoadObservation[] observations =
        [
            new(200, 1, 2, 100),
            new(200, 2, 3, 120),
            new(302, 3, 4, 110),
            new(500, 4, 5, 150),
            new(200, 100, 6, 140),
        ];

        WorkloadMetrics metrics = LoadTestRunner.Calculate(Workload("representative"), observations, 2);

        Assert.Equal(5, metrics.Requests);
        Assert.Equal(2, metrics.Errors);
        Assert.Equal(0.4m, metrics.ErrorRate);
        Assert.Equal(2.5, metrics.ThroughputPerSecond);
        Assert.Equal(3, metrics.P50Milliseconds);
        Assert.Equal(100, metrics.P95Milliseconds);
        Assert.Equal(100, metrics.P99Milliseconds);
        Assert.Equal(20, metrics.TotalDbCalls);
        Assert.Equal(4, metrics.AverageDbCallsPerRequest);
        Assert.Equal(100, metrics.MinimumServerWorkingSetBytes);
        Assert.Equal(150, metrics.PeakServerWorkingSetBytes);
        Assert.Equal(50, metrics.ServerWorkingSetGrowthBytes);
        Assert.Empty(metrics.MissingMeasurements);
    }

    [Fact]
    public void CalculateDoesNotRepresentMissingTelemetryAsZero()
    {
        LoadObservation[] observations = [new(200, 10, null, null)];

        WorkloadMetrics metrics = LoadTestRunner.Calculate(Workload("missing"), observations, 1);

        Assert.Null(metrics.TotalDbCalls);
        Assert.Null(metrics.AverageDbCallsPerRequest);
        Assert.Null(metrics.PeakServerWorkingSetBytes);
        Assert.Collection(
            metrics.MissingMeasurements,
            item => Assert.Equal("dbCalls", item),
            item => Assert.Equal("serverWorkingSet", item));
    }

    [Fact]
    public async Task RunReportsKitSizeEffectRelativeToSmallestKit()
    {
        var target = new SequenceTarget(
            new(200, 10, 1, 100),
            new(200, 20, 2, 110));
        var plan = Plan(
            requests: 1,
            concurrency: 1,
            Workload("small", components: 1, depth: 1),
            Workload("large", components: 50, depth: 3));

        LoadTestReport report = await new LoadTestRunner(target).RunAsync(plan, CancellationToken.None);

        Assert.Collection(
            report.KitSizeEffects,
            small => Assert.Equal(1, small.P95RatioToSmallestKit),
            large => Assert.Equal(2, large.P95RatioToSmallestKit));
    }

    [Fact]
    public async Task RunDoesNotExceedConfiguredConcurrency()
    {
        var target = new ConcurrencyTarget();
        LoadPlan plan = Plan(requests: 12, concurrency: 3, Workload("bounded"));

        await new LoadTestRunner(target).RunAsync(plan, CancellationToken.None);

        Assert.InRange(target.PeakConcurrency, 2, 3);
    }

    private static LoadPlan Plan(int requests, int concurrency, params LoadWorkload[] workloads) => new(
        1,
        new Uri("https://pricing.example/api/v1/prices/calculate"),
        0,
        requests,
        concurrency,
        workloads.ToImmutableArray());

    private static LoadWorkload Workload(string id, int? components = null, int? depth = null)
    {
        using JsonDocument document = JsonDocument.Parse("{\"quantity\":1}");
        return new(id, "kit-size", id, components, depth, 1, document.RootElement.Clone());
    }

    private sealed class SequenceTarget(params LoadObservation[] observations) : ILoadTarget
    {
        private readonly ConcurrentQueue<LoadObservation> remaining = new(observations);

        public ValueTask<LoadObservation> ExecuteAsync(
            ReadOnlyMemory<byte> request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(remaining.TryDequeue(out LoadObservation? observation)
                ? observation
                : throw new InvalidOperationException("No observation remains."));
        }
    }

    private sealed class ConcurrencyTarget : ILoadTarget
    {
        private int active;
        private int peak;

        public int PeakConcurrency => peak;

        public async ValueTask<LoadObservation> ExecuteAsync(
            ReadOnlyMemory<byte> request,
            CancellationToken cancellationToken)
        {
            int current = Interlocked.Increment(ref active);
            int observed;
            do
            {
                observed = peak;
            }
            while (current > observed && Interlocked.CompareExchange(ref peak, current, observed) != observed);

            try
            {
                await Task.Delay(20, cancellationToken);
                return new(200, 20, 0, 100);
            }
            finally
            {
                Interlocked.Decrement(ref active);
            }
        }
    }
}
