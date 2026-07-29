namespace Pricing.IntegrationTests;

using System.Collections.Immutable;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pricing.Application.Orchestration;
using Pricing.Domain.Models;
using Pricing.Domain.ValueObjects;
using Xunit;

public sealed class PricingEndpointTests
{
    [Fact]
    public async Task InvalidRequiredFieldReturnsCorrelatedProblemWithLegacyCode()
    {
        var service = new StubPricingCalculationService(Result(ProductType.Regular));
        await using var factory = new PricingApiFactory(service);
        using HttpClient client = factory.CreateClient();
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/prices/calculate")
        {
            Content = JsonContent.Create(new
            {
                division = "",
                account = "123456",
                products = new[] { new { vendor = "1234", product = "ABC123" } },
                quantity = 2,
                unitOfMeasure = "EA",
                pricingDate = "2026-07-21",
                requestType = "Full",
            }),
        };
        message.Headers.Add("X-Correlation-ID", "validation-correlation");

        using HttpResponseMessage response = await client.SendAsync(message, CancellationToken.None);
        using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("DIVISION_REQUIRED", problem.RootElement.GetProperty("errorCode").GetString());
        Assert.Equal("102", problem.RootElement.GetProperty("legacyErrorCode").GetString());
        Assert.Equal("validation-correlation", problem.RootElement.GetProperty("correlationId").GetString());
        Assert.Null(service.Request);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("unsupported")]
    [InlineData("dependency")]
    [InlineData("validation")]
    public async Task MapsTypedPricingErrorsToProductRows(string errorType)
    {
        PricingError error = errorType switch
        {
            "missing" => new MissingDataPricingError("PRODUCT_NOT_FOUND", "Not found", "61603"),
            "unsupported" => new UnsupportedBehaviorPricingError("BLOCKED_BEHAVIOR", "Blocked"),
            "dependency" => new DependencyPricingError("DATABASE_UNAVAILABLE", "Unavailable", "70", true),
            _ => new ValidationPricingError("INVALID_INPUT", "Invalid", "102"),
        };
        var service = new StubPricingCalculationService(Result(ProductType.Regular) with { Errors = [error] });
        await using var factory = new PricingApiFactory(service);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/prices/calculate",
            Request("Full"),
            CancellationToken.None);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(CancellationToken.None));
        JsonElement productError = document.RootElement.GetProperty("results")[0].GetProperty("error");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(error.Code, productError.GetProperty("code").GetString());
        Assert.Equal(error.LegacyErrorCode, productError.GetProperty("legacyErrorCode").GetString());
    }

    [Fact]
    public async Task RejectsOversizedRequestBeforePricing()
    {
        var service = new StubPricingCalculationService(Result(ProductType.Regular));
        await using var factory = new PricingApiFactory(service);
        using HttpClient client = factory.CreateClient();
        using var content = new ByteArrayContent(new byte[64 * 1024 + 1]);
        content.Headers.ContentType = new("application/json");

        using HttpResponseMessage response = await client.PostAsync(
            "/api/v1/prices/calculate",
            content,
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Null(service.Request);
    }

    [Fact]
    public async Task RejectsCorrelationIdentifierOverContractLimit()
    {
        var service = new StubPricingCalculationService(Result(ProductType.Regular));
        await using var factory = new PricingApiFactory(service);
        using HttpClient client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/prices/calculate")
        {
            Content = JsonContent.Create(Request("Full")),
        };
        request.Headers.Add("X-Correlation-ID", new string('x', 129));

        using HttpResponseMessage response = await client.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(service.Request);
    }

    [Fact]
    public async Task RejectsPropertiesOutsideOpenApiContract()
    {
        var service = new StubPricingCalculationService(Result(ProductType.Regular));
        await using var factory = new PricingApiFactory(service);
        using HttpClient client = factory.CreateClient();
        const string json = """
            {"division":"01","account":"123456","products":[{"vendor":"1234","product":"ABC123"}],"quantity":2,"unitOfMeasure":"EA","pricingDate":"2026-07-21","requestType":"Full","unexpected":true}
            """;

        using HttpResponseMessage response = await client.PostAsync(
            "/api/v1/prices/calculate",
            new StringContent(json, Encoding.UTF8, "application/json"),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(service.Request);
    }

    [Fact]
    public async Task EnforcesPerCallerPricingRateLimit()
    {
        var service = new StubPricingCalculationService(Result(ProductType.Regular));
        await using var factory = new PricingApiFactory(service);
        using HttpClient client = factory.CreateClient();

        for (int attempt = 0; attempt < 100; attempt++)
        {
            using HttpResponseMessage accepted = await client.PostAsJsonAsync(
                "/api/v1/prices/calculate",
                Request("Full"),
                CancellationToken.None);
            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        }

        using HttpResponseMessage rejected = await client.PostAsJsonAsync(
            "/api/v1/prices/calculate",
            Request("Full"),
            CancellationToken.None);
        using JsonDocument problem = JsonDocument.Parse(
            await rejected.Content.ReadAsStreamAsync(CancellationToken.None));

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal("RATE_LIMIT_EXCEEDED", problem.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task UnexpectedFailureReturnsSafeCorrelatedProblem()
    {
        var service = new StubPricingCalculationService(
            Result(ProductType.Regular),
            new InvalidOperationException("sensitive internal detail"));
        await using var factory = new PricingApiFactory(service);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/prices/calculate",
            Request("Full"),
            CancellationToken.None);
        string body = await response.Content.ReadAsStringAsync(CancellationToken.None);
        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement productError = document.RootElement.GetProperty("results")[0].GetProperty("error");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("UNEXPECTED_ERROR", productError.GetProperty("code").GetString());
        Assert.DoesNotContain("sensitive internal detail", body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Full", PricingRequestType.Full)]
    [InlineData("CostOnly", PricingRequestType.CostOnly)]
    [InlineData("SellOnly", PricingRequestType.SellOnly)]
    [InlineData("Jit", PricingRequestType.Jit)]
    public async Task MapsEveryContractRequestMode(string wireValue, PricingRequestType expected)
    {
        var service = new StubPricingCalculationService(Result(ProductType.Regular));
        await using var factory = new PricingApiFactory(service);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/prices/calculate",
            Request(wireValue),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expected, Assert.IsType<PricingRequest>(service.Request).RequestType);
    }

    [Theory]
    [InlineData(ProductType.Regular, "Regular")]
    [InlineData(ProductType.SupplierKit, "Regular")]
    [InlineData(ProductType.Kit, "Kit")]
    public async Task MapsRegularAndKitResultsToTheOpenApiShape(ProductType type, string expectedWireType)
    {
        var service = new StubPricingCalculationService(Result(type));
        await using var factory = new PricingApiFactory(service);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/prices/calculate",
            Request("Full"),
            CancellationToken.None);
        ApiBatchResult? batch = await response.Content.ReadFromJsonAsync<ApiBatchResult>(CancellationToken.None);
        ApiResult? result = batch?.Results.Single();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(expectedWireType, result.ProductType);
        Assert.Equal(12.34m, result.Cost);
        Assert.Equal(15.67m, result.SellPrice);
        Assert.Equal("CONTRACT-1", result.ContractIdentifier);
        Assert.Equal("42", result.BuyingGroupIdentifier);
        Assert.Equal("Fixed", result.RuleType);
        Assert.Single(result.Components);
        Assert.Single(result.Provenance);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public async Task PricesMultipleProductsInOrderAndKeepsProductErrorsInRows()
    {
        PricingResult failed = Result(ProductType.Regular) with
        {
            Errors = [new MissingDataPricingError("PRODUCT_NOT_FOUND", "Not found", "61603")],
        };
        var service = new SequencePricingCalculationService(Result(ProductType.Regular), failed);
        await using var factory = new PricingApiFactory(service);
        using HttpClient client = factory.CreateClient();
        object request = new
        {
            division = "01",
            account = "123456",
            products = new[]
            {
                new { vendor = "1234", product = "ABC123" },
                new { vendor = "5678", product = "XYZ789" },
            },
            quantity = 2,
            unitOfMeasure = "EA",
            pricingDate = "2026-07-21",
            requestType = "Full",
        };

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/prices/calculate",
            request,
            CancellationToken.None);
        using JsonDocument document = JsonDocument.Parse(
            await response.Content.ReadAsStreamAsync(CancellationToken.None));
        JsonElement results = document.RootElement.GetProperty("results");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, results.GetArrayLength());
        Assert.Equal("1234", results[0].GetProperty("vendor").GetString());
        Assert.Equal("ABC123", results[0].GetProperty("product").GetString());
        Assert.Equal(JsonValueKind.Null, results[0].GetProperty("error").ValueKind);
        Assert.Equal("5678", results[1].GetProperty("vendor").GetString());
        Assert.Equal("XYZ789", results[1].GetProperty("product").GetString());
        Assert.Equal("PRODUCT_NOT_FOUND", results[1].GetProperty("error").GetProperty("code").GetString());
        Assert.Collection(
            service.Requests,
            item => Assert.Equal("ABC123", item.Product.Value),
            item => Assert.Equal("XYZ789", item.Product.Value));
    }
    private static object Request(string requestType) => new
    {
        division = "01",
        account = "123456",
        products = new[] { new { vendor = "1234", product = "ABC123" } },
        quantity = 2,
        unitOfMeasure = "EA",
        shipTo = "01",
        billTo = "02",
        pricingDate = "2026-07-21",
        requestType,
    };

    private static PricingResult Result(ProductType type)
    {
        var dates = new PricingDateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        var provenance = new RuleProvenance("Test rule", "TEST", "Account", dates);
        var contract = new ContractSelection(
            new ContractId("CONTRACT-1"),
            "Individual",
            new Money(12.34m),
            new UnitOfMeasure("EA"),
            provenance,
            BuyingGroupId: 42);
        var sell = new SellArrangementSelection("SELL-1", "Fixed", provenance);
        return new PricingResult(
            type,
            new Money(12.34m),
            new Money(15.67m),
            new DateOnly(2026, 12, 31),
            contract,
            sell,
            [new PriceComponent("Base", PriceComponentType.BaseCost, new Money(12.34m), provenance)],
            [provenance],
            [new PricingWarning("TEST_WARNING", "Test warning")],
            ImmutableArray<PricingError>.Empty);
    }

    private sealed class PricingApiFactory(IPricingCalculationService service) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPricingCalculationService>();
            services.AddSingleton(service);
        });
    }

    private sealed class SequencePricingCalculationService(params PricingResult[] results) : IPricingCalculationService
    {
        private int index;

        public List<PricingRequest> Requests { get; } = [];

        public ValueTask<PricingResult> CalculateAsync(PricingRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(request);
            return ValueTask.FromResult(results[index++]);
        }
    }
    private sealed class StubPricingCalculationService(
        PricingResult result,
        Exception? exception = null) : IPricingCalculationService
    {
        public PricingRequest? Request { get; private set; }

        public ValueTask<PricingResult> CalculateAsync(PricingRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Request = request;
            if (exception is not null)
            {
                return ValueTask.FromException<PricingResult>(exception);
            }

            return ValueTask.FromResult(result);
        }
    }

    private sealed record ApiBatchResult(ApiResult[] Results);

    private sealed record ApiResult(
        string ProductType,
        decimal? Cost,
        decimal? SellPrice,
        string? ContractIdentifier,
        string? BuyingGroupIdentifier,
        string? RuleType,
        ApiComponent[] Components,
        ApiProvenance[] Provenance,
        ApiWarning[] Warnings);

    private sealed record ApiComponent(string Name, decimal Amount, ApiProvenance Provenance);
    private sealed record ApiProvenance(string RuleName, string Source);
    private sealed record ApiWarning(string Code, string Message);
}
