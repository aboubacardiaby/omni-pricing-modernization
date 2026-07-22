namespace Pricing.UnitTests.Orchestration;

using System.Collections.Immutable;
using Pricing.Application.ExpirationDates;
using Pricing.Application.Orchestration;
using Pricing.Application.Rounding;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class PricingOrchestratorTests
{
    [Fact]
    public async Task FullRegularItemScenarioRunsCobolOrderAndBuildsExplainableResult()
    {
        var calls = new List<string>();
        Scenario scenario = Scenario.Create(calls);

        PricingResult result = await scenario.Orchestrator.PriceAsync(
            new PricingOperation(scenario.Request, AccountRoundingConfiguration.FromLegacyCode("R")),
            CancellationToken.None);

        Assert.Equal(["context", "cost", "rebate", "sell", "fees"], calls);
        Assert.Equal(8.25m, result.Cost?.Value);
        Assert.Equal(13.46m, result.SellPrice?.Value);
        Assert.Equal(new DateOnly(2028, 6, 30), result.ExpirationDate);
        Assert.Equal("CNT-1", result.ContractSelection?.Contract.Value);
        Assert.Equal("SELL-1", result.SellArrangementSelection?.ArrangementIdentifier);
        Assert.Collection(
            result.Components,
            component => Assert.Equal(PriceComponentType.BaseCost, component.Type),
            component => Assert.Equal(PriceComponentType.Rebate, component.Type),
            component => Assert.Equal(PriceComponentType.BaseSell, component.Type),
            component => Assert.Equal(PriceComponentType.Fee, component.Type));
        Assert.Equal(4, result.Provenance.Length);
        Assert.Equal(["CONTEXT_WARNING", "FEE_WARNING"], result.Warnings.Select(warning => warning.Code));
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task CostFailureReturnsPartialExplainableResultAndStopsLaterStages()
    {
        var calls = new List<string>();
        Scenario scenario = Scenario.Create(calls);
        scenario.Cost.Result = scenario.Cost.Result with
        {
            Error = new DependencyPricingError("COST_FAILED", "Cost dependency failed", "42"),
        };

        PricingResult result = await scenario.Orchestrator.PriceAsync(
            new PricingOperation(scenario.Request, AccountRoundingConfiguration.FromLegacyCode("R")),
            CancellationToken.None);

        Assert.Equal(["context", "cost"], calls);
        Assert.Equal("COST_FAILED", Assert.Single(result.Errors).Code);
        Assert.Equal(10m, result.Cost?.Value);
        Assert.Null(result.SellPrice);
        Assert.Single(result.Components);
    }

    [Fact]
    public async Task FeeFailurePreservesRawSellAndAllPriorEvidence()
    {
        Scenario scenario = Scenario.Create([]);
        scenario.Fees.Result = scenario.Fees.Result with
        {
            Error = new DependencyPricingError("FEE_FAILED", "Fee dependency failed", "707"),
        };

        PricingResult result = await scenario.Orchestrator.PriceAsync(
            new PricingOperation(scenario.Request, AccountRoundingConfiguration.FromLegacyCode("R")),
            CancellationToken.None);

        Assert.Equal(13.456m, result.SellPrice?.Value);
        Assert.Equal("FEE_FAILED", Assert.Single(result.Errors).Code);
        Assert.Equal(4, result.Components.Length);
    }

    [Fact]
    public void ResultFactoryAddsExpirationOverflowAsTypedError()
    {
        PricingRequest request = Request();
        ExpirationDateSource[] sources = Enumerable.Range(0, 51)
            .Select(index => Expiration("Source " + index, request.PricingDate.AddDays(index)))
            .ToArray();
        var snapshot = new PricingResultSnapshot(
            request,
            null,
            new Money(1m),
            null,
            null,
            AccountRoundingConfiguration.FromLegacyCode("N"),
            [],
            [.. sources],
            [],
            []);

        PricingResult result = PricingResultFactory.Create(snapshot);

        ValidationPricingError error = Assert.IsType<ValidationPricingError>(Assert.Single(result.Errors));
        Assert.Equal("145", error.LegacyErrorCode);
        Assert.Null(result.ExpirationDate);
    }

    [Fact]
    public async Task CancellationIsObservedBeforeAnyStageRuns()
    {
        var calls = new List<string>();
        Scenario scenario = Scenario.Create(calls);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await scenario.Orchestrator.PriceAsync(
                new PricingOperation(scenario.Request, AccountRoundingConfiguration.FromLegacyCode("N")),
                cancellation.Token));

        Assert.Empty(calls);
    }

    private sealed record Scenario(
        PricingRequest Request,
        PricingOrchestrator Orchestrator,
        CostStage Cost,
        FeeStage Fees)
    {
        public static Scenario Create(List<string> calls)
        {
            PricingRequest request = PricingOrchestratorTests.Request();
            RuleProvenance costProvenance = Provenance("Cost", "A6U01/7190", new DateOnly(2028, 12, 31));
            RuleProvenance rebateProvenance = Provenance("Rebate", "A6U01/7070", new DateOnly(2028, 9, 30));
            RuleProvenance sellProvenance = Provenance("Sell", "A6U01/7090", new DateOnly(2028, 7, 31));
            RuleProvenance feeProvenance = Provenance("Fee", "A6U01/7195", new DateOnly(2028, 6, 30));
            ProductInformation product = new(
                request.Vendor,
                request.Product,
                ProductType.Regular,
                request.UnitOfMeasure,
                null,
                null,
                1234,
                "A");
            CustomerInformation customer = new(request.Account, new CustomerNumber(123), []);
            PricingContext initial = new(request, product, customer, null, null);
            var contract = new ContractSelection(
                new ContractId("CNT-1"),
                "Individual",
                new Money(10m),
                request.UnitOfMeasure,
                costProvenance);
            PricingContext withCost = initial with { ContractSelection = contract, CostSelection = contract };
            var sell = new SellArrangementSelection("SELL-1", "CostPlus", sellProvenance);
            PricingContext complete = withCost with { SellArrangementSelection = sell };

            var contextStage = new ContextStage(calls, new(
                initial,
                [new PricingWarning("CONTEXT_WARNING", "Context warning")]));
            var costStage = new CostStage(calls, new(
                withCost,
                new Money(10m),
                [Component("Base cost", PriceComponentType.BaseCost, 10m, costProvenance)],
                [Expiration("Cost", new DateOnly(2028, 12, 31), costProvenance)],
                []));
            var rebateStage = new RebateStage(calls, new(
                new Money(8m),
                [Component("Rebate", PriceComponentType.Rebate, -2m, rebateProvenance)],
                [Expiration("Rebate", new DateOnly(2028, 9, 30), rebateProvenance)],
                []));
            var sellStage = new SellStage(calls, new(
                complete,
                new Money(12.345m),
                new Money(12.345m),
                [Component("Base sell", PriceComponentType.BaseSell, 12.345m, sellProvenance)],
                [Expiration("Sell", new DateOnly(2028, 7, 31), sellProvenance)],
                []));
            var feeStage = new FeeStage(calls, new(
                new Money(8.25m),
                new Money(13.456m),
                new Money(13.456m),
                [Component("Fee", PriceComponentType.Fee, 1.111m, feeProvenance)],
                [Expiration("Fee", new DateOnly(2028, 6, 30), feeProvenance)],
                [new PricingWarning("FEE_WARNING", "Fee warning")]));
            return new(
                request,
                new PricingOrchestrator(contextStage, costStage, rebateStage, sellStage, feeStage),
                costStage,
                feeStage);
        }
    }

    private sealed class ContextStage(List<string> calls, PricingContextStageResult result) : IPricingContextStage
    {
        public ValueTask<PricingContextStageResult> ExecuteAsync(PricingRequest request, CancellationToken cancellationToken)
        {
            calls.Add("context");
            return ValueTask.FromResult(result);
        }
    }

    private sealed class CostStage(List<string> calls, CostPricingStageResult result) : ICostPricingStage
    {
        public CostPricingStageResult Result { get; set; } = result;
        public ValueTask<CostPricingStageResult> ExecuteAsync(PricingContext context, CancellationToken cancellationToken)
        {
            calls.Add("cost");
            return ValueTask.FromResult(Result);
        }
    }

    private sealed class RebateStage(List<string> calls, RebatePricingStageResult result) : IRebatePricingStage
    {
        public ValueTask<RebatePricingStageResult> ExecuteAsync(PricingContext context, Money totalCost, CancellationToken cancellationToken)
        {
            calls.Add("rebate");
            return ValueTask.FromResult(result);
        }
    }

    private sealed class SellStage(List<string> calls, SellPricingStageResult result) : ISellPricingStage
    {
        public ValueTask<SellPricingStageResult> ExecuteAsync(PricingContext context, Money totalCost, CancellationToken cancellationToken)
        {
            calls.Add("sell");
            return ValueTask.FromResult(result);
        }
    }

    private sealed class FeeStage(List<string> calls, FeePricingStageResult result) : IFeePricingStage
    {
        public FeePricingStageResult Result { get; set; } = result;
        public ValueTask<FeePricingStageResult> ExecuteAsync(PricingContext context, Money totalCost, Money unitPrice, Money totalSell, CancellationToken cancellationToken)
        {
            calls.Add("fees");
            return ValueTask.FromResult(Result);
        }
    }

    private static PricingRequest Request() => new(
        new DivisionId("01"),
        new AccountNumber("123456"),
        new VendorId("1234"),
        new ProductId("ABC123"),
        new Quantity(1),
        new UnitOfMeasure("EA"),
        "001",
        "001",
        new DateOnly(2028, 1, 1),
        PricingRequestType.Full);

    private static PriceComponent Component(string name, PriceComponentType type, decimal amount, RuleProvenance provenance) =>
        new(name, type, new Money(amount), provenance);

    private static ExpirationDateSource Expiration(string component, DateOnly date, RuleProvenance? provenance = null) =>
        new(component, date, provenance ?? Provenance(component, component, date));

    private static RuleProvenance Provenance(string rule, string source, DateOnly expiration) =>
        new(rule, source, "Regular", new PricingDateRange(new DateOnly(2028, 1, 1), expiration));
}
