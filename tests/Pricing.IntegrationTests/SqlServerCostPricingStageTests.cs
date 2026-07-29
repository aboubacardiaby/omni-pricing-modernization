namespace Pricing.IntegrationTests;

using System.Collections.Immutable;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pricing.Application.CostSelection;
using Pricing.Application.CostAdjustments;
using Pricing.Application.Rebates;
using Pricing.Application.SellSelection;
using Pricing.Application.Fees;
using Pricing.Application.Orchestration;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Pricing.Infrastructure.SqlServer;
using Xunit;

public sealed class SqlServerCostPricingStageTests
{
    [Fact]
    public void RegistrationExposesFourRepositoriesAndConcreteCostStage()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{SqlServerOptions.ConnectionStringName}"] = "Server=localhost;Database=Pricing;Integrated Security=true;TrustServerCertificate=true",
        }).Build();
        var services = new ServiceCollection();
        services.AddSqlServerDataAccess(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        Assert.IsType<SqlServerIndividualCostContractRepository>(scope.ServiceProvider.GetRequiredService<IIndividualCostContractRepository>());
        Assert.IsType<SqlServerBuyingGroupCostContractRepository>(scope.ServiceProvider.GetRequiredService<IBuyingGroupCostContractRepository>());
        Assert.IsType<SqlServerHealthcareCostOverrideRepository>(scope.ServiceProvider.GetRequiredService<IHealthcareCostOverrideRepository>());
        Assert.IsType<SqlServerAcquisitionCostRepository>(scope.ServiceProvider.GetRequiredService<IAcquisitionCostRepository>());
        Assert.IsType<SqlServerCostPricingStage>(scope.ServiceProvider.GetRequiredService<ICostPricingStage>());
        Assert.IsType<SqlServerVendorCostAdjustmentRepository>(scope.ServiceProvider.GetRequiredService<IVendorCostAdjustmentRepository>());
        Assert.IsType<SqlServerRebateCalculationInputRepository>(scope.ServiceProvider.GetRequiredService<IRebateCalculationInputRepository>());
        Assert.IsType<SqlServerRebatePricingStage>(scope.ServiceProvider.GetRequiredService<IRebatePricingStage>());
        Assert.IsType<SqlServerAccountCustomerSellArrangementRepository>(scope.ServiceProvider.GetRequiredService<IAccountCustomerSellArrangementRepository>());
        Assert.IsType<SqlServerBuyingGroupSellArrangementRepository>(scope.ServiceProvider.GetRequiredService<IBuyingGroupSellArrangementRepository>());
        Assert.IsType<SqlServerCorporateSellArrangementRepository>(scope.ServiceProvider.GetRequiredService<ICorporateSellArrangementRepository>());
        Assert.IsType<SqlServerPriceLockRepository>(scope.ServiceProvider.GetRequiredService<IPriceLockRepository>());
        Assert.IsType<SqlServerSellPricingStage>(scope.ServiceProvider.GetRequiredService<ISellPricingStage>());
        Assert.IsType<SqlServerFreightRepository>(scope.ServiceProvider.GetRequiredService<IFreightRepository>());
        Assert.IsType<SqlServerLowUomRepository>(scope.ServiceProvider.GetRequiredService<ILowUomRepository>());
        Assert.IsType<SqlServerPandacRepository>(scope.ServiceProvider.GetRequiredService<IPandacRepository>());
        Assert.IsType<SqlServerSurchargeRepository>(scope.ServiceProvider.GetRequiredService<ISurchargeRepository>());
        Assert.IsType<SqlServerFeePricingStage>(scope.ServiceProvider.GetRequiredService<IFeePricingStage>());
        Assert.IsType<PricingOrchestrator>(scope.ServiceProvider.GetRequiredService<IPricingOrchestrator>());
        Assert.IsType<PricingCalculationService>(scope.ServiceProvider.GetRequiredService<IPricingCalculationService>());
    }

    [Fact]
    public async Task KnownIndividualContractPopulatesContractSelectionEndToEnd()
    {
        PricingContext context = Context();
        PricingDateRange dates = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        var provenance = new RuleProvenance("individual-customer-cost-contract", "CCG01/03/04/06/09/27", "INDIVIDUAL", dates);
        var individual = new IndividualRepository([
            new IndividualCostContractCandidate(new ContractId("CNT-100"), "INDIVIDUAL", new Money(7.25m),
                new UnitOfMeasure("EA"), [dates], false, provenance),
        ]);
        var evaluator = new CostRuleEvaluator([
            new SpecialContractCostRule(individual),
            new IndividualCustomerCostContractRule(individual),
            new BuyingGroupCostContractRule(new EmptyGroupRepository()),
            new HealthcareCostOverrideRule(new EmptyHealthcareRepository()),
            new AcquisitionDealerCostFallbackRule(new FallbackRepository()),
        ]);
        var stage = new SqlServerCostPricingStage(evaluator);

        CostPricingStageResult result = await stage.ExecuteAsync(context, CancellationToken.None);

        Assert.Null(result.Error);
        Assert.Equal(7.25m, result.TotalCost.Value);
        ContractSelection contract = Assert.IsType<ContractSelection>(result.Context.CostSelection);
        Assert.Same(contract, result.Context.ContractSelection);
        Assert.Equal("CNT-100", contract.Contract.Value);
        Assert.Equal("INDIVIDUAL", contract.ContractType);
        Assert.Equal(PriceComponentType.BaseCost, result.Components.Single().Type);
        Assert.Equal(new DateOnly(2026, 12, 31), result.ExpirationSources.Single().ExpirationDate);
    }

    private static PricingContext Context()
    {
        var request = new PricingRequest(new DivisionId("01"), new AccountNumber("123456"), new VendorId("V001"),
            new ProductId("P0000001"), new Quantity(1m), new UnitOfMeasure("EA"), "001", "002",
            new DateOnly(2026, 7, 28), PricingRequestType.Full);
        return new PricingContext(request,
            new ProductInformation(request.Vendor, request.Product, ProductType.Regular, request.UnitOfMeasure, null, null, 10, "A"),
            new CustomerInformation(request.Account, new CustomerNumber(42), []), null, null);
    }

    private sealed class IndividualRepository(ImmutableArray<IndividualCostContractCandidate> candidates) : IIndividualCostContractRepository
    {
        public ValueTask<ImmutableArray<IndividualCostContractCandidate>> FindCandidatesAsync(PricingContext context, CancellationToken cancellationToken) =>
            ValueTask.FromResult(candidates);
    }
    private sealed class EmptyGroupRepository : IBuyingGroupCostContractRepository
    {
        public ValueTask<BuyingGroupCostSearchData> FindCandidatesAsync(PricingContext context, CancellationToken cancellationToken) =>
            ValueTask.FromResult(new BuyingGroupCostSearchData(new(BuyingGroupScope.Account, [], [], []), new(BuyingGroupScope.Customer, [], [], [])));
    }
    private sealed class EmptyHealthcareRepository : IHealthcareCostOverrideRepository
    {
        public ValueTask<HealthcareOverrideData> FindAsync(PricingContext context, CancellationToken cancellationToken) =>
            ValueTask.FromResult(new HealthcareOverrideData(null, null, null, null, []));
    }
    private sealed class FallbackRepository : IAcquisitionCostRepository
    {
        public ValueTask<AcquisitionCostData> FindAsync(PricingContext context, CancellationToken cancellationToken) =>
            ValueTask.FromResult(new AcquisitionCostData(null, []));
    }
}
