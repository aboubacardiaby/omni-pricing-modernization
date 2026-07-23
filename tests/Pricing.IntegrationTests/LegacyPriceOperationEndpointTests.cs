namespace Pricing.IntegrationTests;

using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using global::Pricing.Application.Orchestration;
using global::Pricing.Domain.Models;
using global::Pricing.Domain.ValueObjects;
using Xunit;

public sealed class LegacyPriceOperationEndpointTests
{
    [Fact]
    public async Task UnconfiguredDatabaseReturnsDependencyProblemInsteadOfDiFailure()
    {
        await using var factory = new UnconfiguredApiFactory();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/prices/calculate",
            new
            {
                division = "98",
                account = "990079",
                vendor = "2300",
                product = "0J346H",
                quantity = 1,
                unitOfMeasure = "EA",
                pricingDate = "2026-06-24",
                requestType = "Full",
            },
            CancellationToken.None);
        using JsonDocument problem = JsonDocument.Parse(
            await response.Content.ReadAsStreamAsync(CancellationToken.None));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(
            "PRICING_DATA_ACCESS_NOT_CONFIGURED",
            problem.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task ExecutesLegacyJsonContractAndMapsSupportedResponseFields()
    {
        var pricing = new RecordingPricingService(SuccessResult());
        await using var factory = new PricingApiFactory(pricing);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/legacy/price-operation",
            Request(),
            CancellationToken.None);
        using JsonDocument document = JsonDocument.Parse(
            await response.Content.ReadAsStreamAsync(CancellationToken.None));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement output = document.RootElement.GetProperty("OUT_PRICE");
        JsonElement row = output.GetProperty("OUT_ROW")[0];
        Assert.Equal("23000J346H", row.GetProperty("OUT_PART_NBR").GetString());
        Assert.Equal("6.5140", row.GetProperty("OUT_BASE").GetProperty("OUT_BU_PRICE").GetString());
        Assert.Equal("6.51400000", row.GetProperty("OUT_BASE").GetProperty("OUT_BU_PRICE_UNRND").GetString());
        Assert.Equal("5.0108", row.GetProperty("OUT_BASE").GetProperty("OUT_BU_TOTAL_COST").GetString());
        Assert.Equal(0, row.GetProperty("OUT_ALT_NBR_OF_UOMS").GetInt32());

        PricingRequest mapped = Assert.IsType<PricingRequest>(pricing.Request);
        Assert.Equal("98", mapped.Division.Value);
        Assert.Equal("990079", mapped.Account.Value);
        Assert.Equal("2300", mapped.Vendor.Value);
        Assert.Equal("0J346H", mapped.Product.Value);
        Assert.Equal(new DateOnly(2026, 6, 24), mapped.PricingDate);
    }

    [Fact]
    public async Task RejectsRequestCountThatDoesNotMatchProductArray()
    {
        var pricing = new RecordingPricingService(SuccessResult());
        await using var factory = new PricingApiFactory(pricing);
        using HttpClient client = factory.CreateClient();
        object request = new
        {
            IN_PRICE = new
            {
                IN_ACTION = "A",
                IN_USERID = "NC",
                IN_CO = "OM",
                IN_CUST_ID = "98990079",
                IN_SHIPTO = "",
                IN_PRICER_MM_DD_CCYY = "06-24-2026",
                IN_NBR_REQUESTS = 2,
                IN_PRODUCT_NO = new[] { "23000J346H" },
            },
        };

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/legacy/price-operation",
            request,
            CancellationToken.None);
        using JsonDocument problem = JsonDocument.Parse(
            await response.Content.ReadAsStreamAsync(CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("INVALID_LEGACY_PRICE_OPERATION", problem.RootElement.GetProperty("errorCode").GetString());
        Assert.Null(pricing.Request);
    }

    [Fact]
    public async Task SwaggerPublishesLegacyJsonOperationAndWirePropertyNames()
    {
        await using var factory = new PricingApiFactory(new RecordingPricingService(SuccessResult()));
        using HttpClient client = factory.CreateClient();

        using JsonDocument swagger = JsonDocument.Parse(
            await client.GetStreamAsync("/openapi/v1.json", CancellationToken.None));

        JsonElement operation = swagger.RootElement
            .GetProperty("paths")
            .GetProperty("/api/v1/legacy/price-operation")
            .GetProperty("post");
        Assert.Equal("legacyPriceOperation", operation.GetProperty("operationId").GetString());
        string swaggerJson = swagger.RootElement.GetRawText();
        Assert.Contains("IN_PRICE", swaggerJson, StringComparison.Ordinal);
        Assert.Contains("OUT_PRICE", swaggerJson, StringComparison.Ordinal);
        Assert.Contains("OUT_BU_PRICE_UNRND", swaggerJson, StringComparison.Ordinal);
    }

    private static object Request() => new
    {
        IN_PRICE = new
        {
            IN_ACTION = "A",
            IN_USERID = "NC",
            IN_CO = "OM",
            IN_CUST_ID = "98990079",
            IN_SHIPTO = "",
            IN_PRICER_MM_DD_CCYY = "06-24-2026",
            IN_NBR_REQUESTS = 1,
            IN_PRODUCT_NO = new[] { "23000J346H" },
        },
    };

    private static PricingResult SuccessResult()
    {
        var provenance = new RuleProvenance(
            "Legacy compatibility test",
            "Price.wsdl",
            "Account",
            new PricingDateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)));
        return new PricingResult(
            ProductType.Regular,
            new Money(5.0108m),
            new Money(6.514m),
            new DateOnly(2026, 12, 31),
            new ContractSelection(
                new ContractId("CONTRACT-1"),
                "Individual",
                new Money(5.0108m),
                new UnitOfMeasure("EA"),
                provenance),
            new SellArrangementSelection("SELL-1", "Fixed", provenance),
            ImmutableArray<PriceComponent>.Empty,
            [provenance],
            ImmutableArray<PricingWarning>.Empty,
            ImmutableArray<PricingError>.Empty);
    }

    private sealed class PricingApiFactory(IPricingCalculationService pricing)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPricingCalculationService>();
            services.AddSingleton(pricing);
        });
    }

    private sealed class UnconfiguredApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.UseEnvironment("Testing");
    }
    private sealed class RecordingPricingService(PricingResult result) : IPricingCalculationService
    {
        public PricingRequest? Request { get; private set; }

        public ValueTask<PricingResult> CalculateAsync(
            PricingRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Request = request;
            return ValueTask.FromResult(result);
        }
    }
}
