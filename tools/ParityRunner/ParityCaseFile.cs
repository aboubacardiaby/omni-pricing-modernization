namespace ParityRunner;

using System.Collections.Immutable;
using System.Text.Json;

public static class ParityCaseFile
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public static async Task<ImmutableArray<ParityCase>> LoadAsync(
        string path,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        await using FileStream stream = File.OpenRead(path);
        ParityCase[]? cases = await JsonSerializer.DeserializeAsync<ParityCase[]>(
            stream,
            JsonOptions,
            cancellationToken).ConfigureAwait(false);
        if (cases is null || cases.Length == 0)
        {
            throw new InvalidDataException("The parity case file must contain at least one case.");
        }

        var identifiers = new HashSet<string>(StringComparer.Ordinal);
        foreach (ParityCase parityCase in cases)
        {
            if (string.IsNullOrWhiteSpace(parityCase.Id))
            {
                throw new InvalidDataException("Every parity case requires a non-blank id.");
            }

            if (!identifiers.Add(parityCase.Id))
            {
                throw new InvalidDataException($"Duplicate parity case id '{parityCase.Id}'.");
            }

            if (parityCase.Input.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException($"Parity case '{parityCase.Id}' input must be a JSON object.");
            }
        }

        return [.. cases];
    }

    public static async Task SaveReportAsync(
        string path,
        ParityRunReport report,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(report);
        string fullPath = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string temporaryPath = fullPath + ".tmp";
        await using (FileStream stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, report, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }

        File.Move(temporaryPath, fullPath, overwrite: true);
    }
}
