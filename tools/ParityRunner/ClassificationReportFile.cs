namespace ParityRunner;

using System.Collections.Immutable;
using System.Text.Json;

public static class ClassificationReportFile
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    public static async Task<ParityRunReport> LoadRunAsync(
        string path,
        CancellationToken cancellationToken)
    {
        await using FileStream stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<ParityRunReport>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidDataException("The parity run report is empty or invalid.");
    }

    public static ClassifiedParityReport Classify(
        ParityRunReport run,
        decimal roundingTolerance,
        TimeProvider? timeProvider = null)
    {
        var classifier = new PricingDifferenceClassifier(roundingTolerance);
        ImmutableArray<ClassifiedParityCase> cases = run.Cases.Select(classifier.Classify).ToImmutableArray();
        return new(
            1,
            (timeProvider ?? TimeProvider.System).GetUtcNow(),
            roundingTolerance,
            cases);
    }

    public static async Task SaveAsync(
        string path,
        ClassifiedParityReport report,
        CancellationToken cancellationToken)
    {
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        string temporaryPath = fullPath + ".tmp";
        await using (FileStream stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, report, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }

        File.Move(temporaryPath, fullPath, overwrite: true);
    }
}
