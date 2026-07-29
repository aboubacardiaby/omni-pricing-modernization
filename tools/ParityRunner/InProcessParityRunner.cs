namespace ParityRunner;

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Pricing.Application.Orchestration;
using Pricing.Domain.Models;

/// <summary>A boundary for the unavailable CICS/COBOL pricing transport.</summary>
/// <remarks>A live implementation must call COBOL. Fixture implementations are not parity evidence.</remarks>
public interface ICobolPricingAdapter
{
    ValueTask<ParityPricingObservation> PriceAsync(
        string scenarioId,
        PricingOperation operation,
        CancellationToken cancellationToken);
}

public enum CobolObservationSource
{
    LiveCobol,
    CapturedCobol,
    FixtureBackedDocumentedExpectation,
}

public sealed record ParityPricingObservation(
    CobolObservationSource Source,
    PricingResult Result,
    string Evidence);

public sealed record FixturePricingObservation(
    string ScenarioId,
    PricingResult ExpectedResult,
    string GoverningRule,
    string CatalogSource = "docs/characterization/pricing-scenario-catalog.md");

/// <summary>
/// Supplies hand-derived T055 expectations. It deliberately does not represent a live COBOL run.
/// A fixture is accepted only when its scenario id occurs in the supplied T055 catalog text.
/// </summary>
public sealed class FixtureBackedCobolPricingAdapter : ICobolPricingAdapter
{
    private readonly ImmutableDictionary<string, FixturePricingObservation> fixtures;

    public FixtureBackedCobolPricingAdapter(
        string t055CatalogMarkdown,
        IEnumerable<FixturePricingObservation> fixtures)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(t055CatalogMarkdown);
        ArgumentNullException.ThrowIfNull(fixtures);
        ImmutableHashSet<string> documentedIds = PricingScenarioCatalog.ReadScenarioIds(t055CatalogMarkdown);
        this.fixtures = fixtures.ToImmutableDictionary(item => item.ScenarioId, StringComparer.Ordinal);
        if (this.fixtures.Count == 0)
        {
            throw new ArgumentException("At least one T055 fixture is required.", nameof(fixtures));
        }

        string? undocumented = this.fixtures.Keys.FirstOrDefault(id => !documentedIds.Contains(id));
        if (undocumented is not null)
        {
            throw new InvalidDataException($"Fixture '{undocumented}' is not documented in the T055 catalog.");
        }
    }

    public ValueTask<ParityPricingObservation> PriceAsync(
        string scenarioId,
        PricingOperation operation,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();
        if (!fixtures.TryGetValue(scenarioId, out FixturePricingObservation? fixture))
        {
            throw new KeyNotFoundException($"No fixture-backed COBOL expectation exists for '{scenarioId}'.");
        }

        return ValueTask.FromResult(new ParityPricingObservation(
            CobolObservationSource.FixtureBackedDocumentedExpectation,
            fixture.ExpectedResult,
            $"{fixture.CatalogSource}; {fixture.GoverningRule}; hand-derived, not live COBOL evidence"));
    }
}

public static class PricingScenarioCatalog
{
    public static ImmutableHashSet<string> ReadScenarioIds(string markdown)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(markdown);
        return markdown.Split('\n')
            .Select(line => line.TrimStart())
            .Where(line => line.StartsWith("| SCN-", StringComparison.Ordinal))
            .Select(line => line.Split('|', StringSplitOptions.TrimEntries)[1])
            .ToImmutableHashSet(StringComparer.Ordinal);
    }
}

public sealed record InProcessParityCase(string Id, PricingOperation Operation);

public sealed record ComparedField(string Path, JsonElement? Cobol, JsonElement? CSharp, bool ExactMatch);

public sealed record InProcessParityCaseResult(
    string Id,
    PricingOperation Input,
    CobolObservationSource CobolSource,
    string CobolEvidence,
    PricingResult Cobol,
    PricingResult CSharp,
    ImmutableArray<ComparedField> Fields,
    long CobolElapsedMilliseconds,
    long CSharpElapsedMilliseconds);

public sealed record InProcessParityReport(
    int SchemaVersion,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    ImmutableArray<InProcessParityCaseResult> Cases);

/// <summary>Runs the COBOL boundary and the C# orchestrator for the identical immutable operation.</summary>
public sealed class InProcessParityRunner(
    ICobolPricingAdapter cobolAdapter,
    IPricingOrchestrator csharpOrchestrator,
    TimeProvider? timeProvider = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;

    public async Task<InProcessParityReport> RunAsync(
        IEnumerable<InProcessParityCase> cases,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cases);
        InProcessParityCase[] materialized = cases.ToArray();
        if (materialized.Length == 0 || materialized.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() != materialized.Length)
        {
            throw new ArgumentException("Parity cases must be non-empty and have unique ids.", nameof(cases));
        }

        DateTimeOffset started = clock.GetUtcNow();
        var results = ImmutableArray.CreateBuilder<InProcessParityCaseResult>(materialized.Length);
        foreach (InProcessParityCase item in materialized)
        {
            cancellationToken.ThrowIfCancellationRequested();
            long beforeCobol = Stopwatch.GetTimestamp();
            ParityPricingObservation cobol = await cobolAdapter.PriceAsync(item.Id, item.Operation, cancellationToken).ConfigureAwait(false);
            long cobolElapsed = (long)Stopwatch.GetElapsedTime(beforeCobol).TotalMilliseconds;
            long beforeCSharp = Stopwatch.GetTimestamp();
            PricingResult csharp = await csharpOrchestrator.PriceAsync(item.Operation, cancellationToken).ConfigureAwait(false);
            long csharpElapsed = (long)Stopwatch.GetElapsedTime(beforeCSharp).TotalMilliseconds;
            results.Add(new InProcessParityCaseResult(
                item.Id,
                item.Operation,
                cobol.Source,
                cobol.Evidence,
                cobol.Result,
                csharp,
                Compare(cobol.Result, csharp),
                cobolElapsed,
                csharpElapsed));
        }

        return new InProcessParityReport(1, started, clock.GetUtcNow(), results.ToImmutable());
    }

    private static ImmutableArray<ComparedField> Compare(PricingResult cobol, PricingResult csharp)
    {
        using JsonDocument left = JsonDocument.Parse(JsonSerializer.Serialize(cobol, JsonOptions));
        using JsonDocument right = JsonDocument.Parse(JsonSerializer.Serialize(csharp, JsonOptions));
        var fields = ImmutableArray.CreateBuilder<ComparedField>();
        Flatten("$", left.RootElement, right.RootElement, fields);
        return fields.ToImmutable();
    }

    private static void Flatten(string path, JsonElement? left, JsonElement? right, ImmutableArray<ComparedField>.Builder fields)
    {
        if (left is { ValueKind: JsonValueKind.Object } || right is { ValueKind: JsonValueKind.Object })
        {
            var names = (left?.ValueKind == JsonValueKind.Object ? left.Value.EnumerateObject().Select(p => p.Name) : [])
                .Concat(right?.ValueKind == JsonValueKind.Object ? right.Value.EnumerateObject().Select(p => p.Name) : [])
                .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
            foreach (string name in names)
            {
                JsonElement? l = left is { ValueKind: JsonValueKind.Object } && left.Value.TryGetProperty(name, out JsonElement lv) ? lv : null;
                JsonElement? r = right is { ValueKind: JsonValueKind.Object } && right.Value.TryGetProperty(name, out JsonElement rv) ? rv : null;
                Flatten($"{path}.{name}", l, r, fields);
            }
            return;
        }

        if (left is { ValueKind: JsonValueKind.Array } || right is { ValueKind: JsonValueKind.Array })
        {
            int count = Math.Max(left?.ValueKind == JsonValueKind.Array ? left.Value.GetArrayLength() : 0, right?.ValueKind == JsonValueKind.Array ? right.Value.GetArrayLength() : 0);
            for (int index = 0; index < count; index++)
            {
                JsonElement? l = left is { ValueKind: JsonValueKind.Array } && index < left.Value.GetArrayLength() ? left.Value[index] : null;
                JsonElement? r = right is { ValueKind: JsonValueKind.Array } && index < right.Value.GetArrayLength() ? right.Value[index] : null;
                Flatten($"{path}[{index}]", l, r, fields);
            }
            return;
        }

        JsonElement? leftClone = left?.Clone();
        JsonElement? rightClone = right?.Clone();
        fields.Add(new ComparedField(path, leftClone, rightClone, left?.GetRawText() == right?.GetRawText()));
    }
}

public static class InProcessParityReportFile
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    public static async Task SaveAsync(string path, InProcessParityReport report, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(report);
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        string temporary = fullPath + ".tmp";
        await using (FileStream stream = File.Create(temporary))
        {
            await JsonSerializer.SerializeAsync(stream, report, Options, cancellationToken).ConfigureAwait(false);
        }
        File.Move(temporary, fullPath, true);
    }
}
