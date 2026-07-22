namespace ParityRunner;

using System.Collections.Immutable;
using System.Text.Json;

public sealed class ParityExecutionRunner(
    IPricingEngineClient cobolClient,
    IPricingEngineClient csharpClient,
    TimeProvider? timeProvider = null)
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;

    public async Task<ParityRunReport> RunAsync(
        ImmutableArray<ParityCase> cases,
        string cobolAdapterEndpoint,
        string csharpEndpoint,
        CancellationToken cancellationToken)
    {
        if (cases.IsDefaultOrEmpty)
        {
            throw new ArgumentException("At least one parity case is required.", nameof(cases));
        }

        DateTimeOffset started = clock.GetUtcNow();
        var results = ImmutableArray.CreateBuilder<ParityCaseResult>(cases.Length);
        foreach (ParityCase parityCase in cases)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte[] identicalInput = JsonSerializer.SerializeToUtf8Bytes(parityCase.Input);
            EngineObservation cobol = await cobolClient
                .ExecuteAsync(identicalInput, cancellationToken)
                .ConfigureAwait(false);
            EngineObservation csharp = await csharpClient
                .ExecuteAsync(identicalInput, cancellationToken)
                .ConfigureAwait(false);
            results.Add(new ParityCaseResult(
                parityCase.Id,
                parityCase.Input.Clone(),
                cobol,
                csharp));
        }

        return new ParityRunReport(
            1,
            started,
            clock.GetUtcNow(),
            cobolAdapterEndpoint,
            csharpEndpoint,
            results.ToImmutable());
    }
}
