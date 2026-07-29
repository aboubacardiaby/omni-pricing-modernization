namespace Pricing.IntegrationTests;

using System.Collections.Immutable;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pricing.Application.CustomerPricingContext;
using Pricing.Application.Orchestration;
using Pricing.Application.ProductClassification;
using Pricing.Application.ProductInformation;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Pricing.Infrastructure.SqlServer;
using Xunit;

public sealed class SqlServerPricingContextAdapterTests
{
    [Fact]
    public void SqlServerRegistrationExposesAllT064AdaptersAndContextStage()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{SqlServerOptions.ConnectionStringName}"] = "Server=localhost;Database=Pricing;Integrated Security=true;TrustServerCertificate=true",
        }).Build();
        var services = new ServiceCollection();
        services.AddSqlServerDataAccess(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        Assert.IsType<SqlServerProductClassificationRepository>(scope.ServiceProvider.GetRequiredService<IProductClassificationRepository>());
        Assert.IsType<SqlServerProductInformationRepository>(scope.ServiceProvider.GetRequiredService<IProductInformationRepository>());
        Assert.IsType<SqlServerCustomerPricingContextRepository>(scope.ServiceProvider.GetRequiredService<ICustomerPricingContextRepository>());
        Assert.IsType<SqlServerPricingContextStage>(scope.ServiceProvider.GetRequiredService<IPricingContextStage>());
    }

    [Fact]
    public async Task ConcreteStagePopulatesContextThroughExistingT020ToT022Services()
    {
        var dates = new PricingDateRange(new DateOnly(2026, 1, 1), null);
        var stage = new SqlServerPricingContextStage(
            new ProductClassificationService(new ClassificationRepository()),
            new ProductInformationService(new InformationRepository()),
            new CustomerPricingContextService(new CustomerRepository(dates)));

        PricingContextStageResult result = await stage.ExecuteAsync(CreateRequest(), CancellationToken.None);

        Assert.Null(result.Error);
        Assert.NotNull(result.Context);
        Assert.Equal(ProductType.Kit, result.Context.Product.ProductType);
        Assert.Equal("EA", result.Context.Product.BaseUnitOfMeasure.Value);
        Assert.Equal(42, result.Context.Product.ProductCategory);
        Assert.Equal(9001, result.Context.Customer.Customer?.Value);
        Assert.Equal(700, result.Context.Customer.BuyingGroupMemberships.Single().BuyingGroupId);
        Assert.Equal("PRODUCT_CATEGORY_NOT_FOUND", result.Warnings.Single().Code);
    }

    private static PricingRequest CreateRequest() => new(
        new DivisionId("01"), new AccountNumber("123456"), new VendorId("V001"), new ProductId("P0000001"),
        new Quantity(1m), new UnitOfMeasure("EA"), "001", "002", new DateOnly(2026, 7, 28), PricingRequestType.Full);

    private sealed class ClassificationRepository : IProductClassificationRepository
    {
        public ValueTask<string?> FindLegacyProductTypeAsync(VendorId vendor, ProductId product, CancellationToken cancellationToken) =>
            ValueTask.FromResult<string?>("O");
    }

    private sealed class InformationRepository : IProductInformationRepository
    {
        public ValueTask<ProductInformationData?> FindAsync(VendorId vendor, ProductId product, UnitOfMeasure? requestedUnitOfMeasure,
            DivisionId? division, CancellationToken cancellationToken) => ValueTask.FromResult<ProductInformationData?>(
                new ProductInformationData("R", new UnitOfMeasure("EA"), 42, null, null, "A", null,
                    ProductCategoryFound: false));
    }

    private sealed class CustomerRepository(PricingDateRange dates) : ICustomerPricingContextRepository
    {
        public ValueTask<CustomerPricingContextData?> FindAsync(DivisionId division, AccountNumber account, string? shipTo,
            string? billTo, DateOnly pricingDate, CancellationToken cancellationToken) => ValueTask.FromResult<CustomerPricingContextData?>(
                new CustomerPricingContextData(new CustomerNumber(9001),
                    [new BuyingGroupMembership(700, null, 1, dates, "CUG11")], [], [], null, null, []));
    }
}
