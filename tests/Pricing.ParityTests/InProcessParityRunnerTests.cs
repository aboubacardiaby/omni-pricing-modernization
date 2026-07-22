namespace Pricing.ParityTests;

using System.Collections.Immutable;
using System.Text.Json;
using ParityRunner;
using Pricing.Application.Orchestration;
using Pricing.Application.Rounding;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class InProcessParityRunnerTests
{
    private const string Catalog = """
        | ID | Scenario | Input conditions | Expected decision path / output | Governing rule | Confidence |
        |---|---|---|---|---|---|
        | SCN-COST-001 | individual | conditions | expected | R-COST-001 | CONFIRMED |
        """;

    [Fact]
    public async Task RunsFixtureAdapterAndActualOrchestratorAndCapturesDecisionPathFields()
    {
        PricingOperation operation = Operation();
        PricingResult expected = ErrorResult("601");
        var adapter = new FixtureBackedCobolPricingAdapter(Catalog,
        [
            new("SCN-COST-001", expected, "R-COST-001; A6U01/7160"),
        ]);
        PricingOrchestrator orchestrator = ErrorOrchestrator("602");
        var runner = new InProcessParityRunner(adapter, orchestrator);

        InProcessParityReport report = await runner.RunAsync(
            [new InProcessParityCase("SCN-COST-001", operation)],
            CancellationToken.None);

        InProcessParityCaseResult result = Assert.Single(report.Cases);
        Assert.Same(operation, result.Input);
        Assert.Equal(CobolObservationSource.FixtureBackedDocumentedExpectation, result.CobolSource);
        Assert.Contains("not live COBOL evidence", result.CobolEvidence, StringComparison.Ordinal);
        ComparedField legacyError = Assert.Single(result.Fields, field => field.Path == "$.errors[0].legacyErrorCode");
        Assert.Equal("601", legacyError.Cobol?.GetString());
        Assert.Equal("602", legacyError.CSharp?.GetString());
        Assert.False(legacyError.ExactMatch);
        Assert.Contains(result.Fields, field => field.Path.StartsWith("$.provenance", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PersistsBothSidesAndAllFieldPairsAtomically()
    {
        PricingOperation operation = Operation();
        PricingResult expected = ErrorResult("601");
        var runner = new InProcessParityRunner(
            new FixtureBackedCobolPricingAdapter(Catalog, [new("SCN-COST-001", expected, "R-COST-001")]),
            ErrorOrchestrator("601"));
        InProcessParityReport report = await runner.RunAsync(
            [new InProcessParityCase("SCN-COST-001", operation)], CancellationToken.None);
        string directory = Path.Combine(Path.GetTempPath(), "in-process-parity", Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "report.json");
        try
        {
            await InProcessParityReportFile.SaveAsync(path, report, CancellationToken.None);
            using JsonDocument document = JsonDocument.Parse(await File.ReadAllTextAsync(path));
            JsonElement persisted = document.RootElement.GetProperty("cases")[0];
            Assert.Equal("fixtureBackedDocumentedExpectation", persisted.GetProperty("cobolSource").GetString());
            Assert.Equal("601", persisted.GetProperty("cobol").GetProperty("errors")[0].GetProperty("legacyErrorCode").GetString());
            Assert.NotEmpty(persisted.GetProperty("fields").EnumerateArray());
            Assert.False(File.Exists(path + ".tmp"));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void FixtureAdapterRejectsScenarioNotPresentInT055Catalog()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(() =>
            new FixtureBackedCobolPricingAdapter(Catalog, [new("SCN-MADE-UP-999", ErrorResult("601"), "invented")]));
        Assert.Contains("not documented", error.Message, StringComparison.Ordinal);
    }

    private static PricingOperation Operation() => new(
        new PricingRequest(
            new DivisionId("01"), new AccountNumber("123456"), new VendorId("1234"),
            new ProductId("ABC123"), new Quantity(1), new UnitOfMeasure("EA"), "001", "001",
            new DateOnly(2026, 7, 21), PricingRequestType.Full),
        AccountRoundingConfiguration.FromLegacyCode("N"));

    private static PricingOrchestrator ErrorOrchestrator(string legacyCode) => new(
        new ErrorContextStage(legacyCode), new UnusedCostStage(), new UnusedRebateStage(),
        new UnusedSellStage(), new UnusedFeeStage());

    private static PricingResult ErrorResult(string legacyCode) => new(
        ProductType.Regular, null, null, null, null, null, [],
        [new RuleProvenance("R-COST-001", "A6U01/7160", "Individual", null)],
        [], [new ValidationPricingError("SPECIAL_NOT_FOUND", "Special contract not found", legacyCode)]);

    private sealed class ErrorContextStage(string legacyCode) : IPricingContextStage
    {
        public ValueTask<PricingContextStageResult> ExecuteAsync(PricingRequest request, CancellationToken cancellationToken) =>
            ValueTask.FromResult(new PricingContextStageResult(null, [],
                new ValidationPricingError("SPECIAL_NOT_FOUND", "Special contract not found", legacyCode)));
    }

    private sealed class UnusedCostStage : ICostPricingStage
    {
        public ValueTask<CostPricingStageResult> ExecuteAsync(PricingContext context, CancellationToken cancellationToken) => throw new InvalidOperationException();
    }
    private sealed class UnusedRebateStage : IRebatePricingStage
    {
        public ValueTask<RebatePricingStageResult> ExecuteAsync(PricingContext context, Money totalCost, CancellationToken cancellationToken) => throw new InvalidOperationException();
    }
    private sealed class UnusedSellStage : ISellPricingStage
    {
        public ValueTask<SellPricingStageResult> ExecuteAsync(PricingContext context, Money totalCost, CancellationToken cancellationToken) => throw new InvalidOperationException();
    }
    private sealed class UnusedFeeStage : IFeePricingStage
    {
        public ValueTask<FeePricingStageResult> ExecuteAsync(PricingContext context, Money totalCost, Money unitPrice, Money totalSell, CancellationToken cancellationToken) => throw new InvalidOperationException();
    }
}
