namespace Pricing.ParityTests;

using System.Collections.Immutable;
using System.Text.Json;
using ParityRunner;
using Xunit;

public sealed class ParityExecutionRunnerTests
{
    [Fact]
    public async Task SendsByteIdenticalInputToBothEnginesAndRetainsBothObservations()
    {
        using JsonDocument inputDocument = JsonDocument.Parse("""
            {"division":"01","account":"123456","vendor":"1234","product":"ABC123","quantity":1,"unitOfMeasure":"EA","pricingDate":"2026-07-21","requestType":"Full"}
            """);
        var cobol = new StubClient(200, """{"cost":10.00}""");
        var csharp = new StubClient(200, """{"cost":10.00}""");
        var runner = new ParityExecutionRunner(cobol, csharp);

        ParityRunReport report = await runner.RunAsync(
            [new ParityCase("SCN-COST-001", inputDocument.RootElement.Clone())],
            "https://cobol.example/prices",
            "https://csharp.example/api/v1/prices/calculate",
            CancellationToken.None);

        Assert.Equal(cobol.Inputs.Single(), csharp.Inputs.Single());
        ParityCaseResult result = Assert.Single(report.Cases);
        Assert.Equal("SCN-COST-001", result.Id);
        Assert.Equal(200, result.Cobol.StatusCode);
        Assert.Equal(200, result.CSharp.StatusCode);
        Assert.Equal(10.00m, result.Cobol.JsonBody?.GetProperty("cost").GetDecimal());
        Assert.Equal(10.00m, result.CSharp.JsonBody?.GetProperty("cost").GetDecimal());
    }

    [Fact]
    public async Task LoadsCasesAndPersistsAuditableReport()
    {
        string directory = Path.Combine(Path.GetTempPath(), "parity-runner-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string casesPath = Path.Combine(directory, "cases.json");
        string reportPath = Path.Combine(directory, "report.json");
        try
        {
            await File.WriteAllTextAsync(casesPath, """
                [{"id":"CASE-1","input":{"requestType":"Full"}}]
                """);

            ImmutableArray<ParityCase> cases = await ParityCaseFile.LoadAsync(casesPath, CancellationToken.None);
            var runner = new ParityExecutionRunner(
                new StubClient(200, """{"cost":1}"""),
                new StubClient(503, "dependency unavailable"));
            ParityRunReport report = await runner.RunAsync(
                cases,
                "https://cobol.example/prices",
                "https://csharp.example/prices",
                CancellationToken.None);
            await ParityCaseFile.SaveReportAsync(reportPath, report, CancellationToken.None);

            using JsonDocument persisted = JsonDocument.Parse(await File.ReadAllTextAsync(reportPath));
            JsonElement persistedCase = persisted.RootElement.GetProperty("cases")[0];
            Assert.Equal("CASE-1", persistedCase.GetProperty("id").GetString());
            Assert.Equal(200, persistedCase.GetProperty("cobol").GetProperty("statusCode").GetInt32());
            Assert.Equal(503, persistedCase.GetProperty("cSharp").GetProperty("statusCode").GetInt32());
            Assert.Equal(
                "dependency unavailable",
                persistedCase.GetProperty("cSharp").GetProperty("nonJsonBody").GetString());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task RejectsDuplicateCaseIdentifiersBeforeExecution()
    {
        string path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, """
                [
                  {"id":"DUPLICATE","input":{}},
                  {"id":"DUPLICATE","input":{}}
                ]
                """);

            InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(
                () => ParityCaseFile.LoadAsync(path, CancellationToken.None));

            Assert.Contains("Duplicate parity case id", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RequiresExplicitEndpointsCasesAndOutput()
    {
        RunnerOptions options = RunnerOptions.Parse([
            "--cases", "cases.json",
            "--cobol-url", "https://cobol.example/prices",
            "--csharp-url", "https://csharp.example/prices",
            "--output", "report.json",
            "--timeout-seconds", "45",
        ]);

        Assert.Equal("cases.json", options.CasesPath);
        Assert.Equal(TimeSpan.FromSeconds(45), options.Timeout);
        Assert.Equal("https://cobol.example/prices", options.CobolAdapterEndpoint.ToString());
    }

    private sealed class StubClient(int statusCode, string response) : IPricingEngineClient
    {
        public List<byte[]> Inputs { get; } = [];

        public ValueTask<EngineObservation> ExecuteAsync(
            ReadOnlyMemory<byte> input,
            CancellationToken cancellationToken)
        {
            Inputs.Add(input.ToArray());
            try
            {
                using JsonDocument document = JsonDocument.Parse(response);
                return ValueTask.FromResult(new EngineObservation(
                    statusCode,
                    document.RootElement.Clone(),
                    null,
                    1,
                    "test-correlation"));
            }
            catch (JsonException)
            {
                return ValueTask.FromResult(new EngineObservation(
                    statusCode,
                    null,
                    response,
                    1,
                    "test-correlation"));
            }
        }
    }
}
