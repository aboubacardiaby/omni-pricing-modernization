namespace Pricing.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public sealed class ApiFoundationTests : IClassFixture<ApiFoundationTests.UnconfiguredApiFactory>
{
    private readonly HttpClient client;

    public ApiFoundationTests(UnconfiguredApiFactory factory)
    {
        client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpointsReturnHealthy(string path)
    {
        using var response = await client.GetAsync(path, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(CancellationToken.None));
    }

    [Fact]
    public async Task OpenApiDocumentIsAvailable()
    {
        using var response = await client.GetAsync("/openapi/v1.json", CancellationToken.None);
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStreamAsync(CancellationToken.None));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("3.0.1", document.RootElement.GetProperty("openapi").GetString());
    }

    [Fact]
    public async Task MissingRouteReturnsProblemDetailsWithCorrelationId()
    {
        const string correlationId = "integration-test-correlation";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/does-not-exist");
        request.Headers.Add("X-Correlation-ID", correlationId);

        using var response = await client.SendAsync(request, CancellationToken.None);
        var problem = Assert.IsType<ProblemDetails>(
            await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken.None));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-ID").Single());
        Assert.Equal(404, problem.Status);
        Assert.True(problem.Extensions.TryGetValue("correlationId", out var problemCorrelationId));
        Assert.Equal(correlationId, problemCorrelationId?.ToString());
    }

    public sealed class UnconfiguredApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.UseEnvironment("Testing");
    }}
