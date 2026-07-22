using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Pricing.Api.Diagnostics;
using Pricing.Api.Legacy;
using Pricing.Api.Pricing;
using Pricing.Application.Kits;
using Pricing.Application.Orchestration;
using Pricing.Application.ProductClassification;
using Pricing.Application.ProductInformation;
using Pricing.Infrastructure.Db2;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var problemJsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;
        context.ProblemDetails.Extensions.TryAdd(
            "errorCode",
            context.ProblemDetails.Status == StatusCodes.Status400BadRequest
                ? "INVALID_REQUEST"
                : "UNEXPECTED_ERROR");
    };
});
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
});
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
builder.Services.AddAuthentication();
builder.Services.AddAuthorization(options => options.AddPolicy(
    "PricingCalculate",
    policy => policy.RequireAuthenticatedUser()));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("pricing", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true,
        }));
    options.OnRejected = async (rejected, cancellationToken) =>
    {
        rejected.HttpContext.Response.ContentType = "application/problem+json";
        byte[] payload = PricingProblemWriter.Payload(
            rejected.HttpContext,
            StatusCodes.Status429TooManyRequests,
            "The pricing request rate limit was exceeded.",
            "Retry the pricing request after the current rate-limit window.",
            "RATE_LIMIT_EXCEEDED");
        await rejected.HttpContext.Response.Body.WriteAsync(payload, cancellationToken);
    };
});
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live", "ready"]);
builder.Services.AddSingleton<IDb2CallCounter, Db2CallCounter>();
IConfigurationSection db2Configuration = builder.Configuration.GetSection(Db2Options.SectionName);
if (db2Configuration.Exists())
{
    builder.Services.AddDb2DataAccess(db2Configuration);
}
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<IPricingCalculationService>(services =>
{
    IPricingOrchestrator? regularPricing = services.GetService<IPricingOrchestrator>();
    IProductClassificationRepository? classificationRepository =
        services.GetService<IProductClassificationRepository>();
    IProductInformationRepository? informationRepository = services.GetService<IProductInformationRepository>();
    IKitExplosionRepository? kitRepository = services.GetService<IKitExplosionRepository>();
    if (regularPricing is null || classificationRepository is null || informationRepository is null || kitRepository is null)
    {
        return new UnavailablePricingCalculationService();
    }

    return new PricingCalculationService(
        new ProductClassificationService(classificationRepository),
        new ProductInformationService(informationRepository),
        regularPricing,
        new KitComponentPricingService(
            kitRepository,
            regularPricing));
});
builder.Services.AddScoped<ILegacyPriceOperationService, LegacyPriceOperationService>();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OMNI Pricing API",
        Version = "v1",
        Description = "Explainable pricing service foundation. COBOL remains authoritative."
    });
    options.OperationFilter<LegacyPriceOperationSwagger>();
});
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Pricing.Api"))
    .WithTracing(tracing => tracing
        .AddSource(DapperDb2QueryExecutor.ActivitySourceName)
        .AddAspNetCoreInstrumentation(options =>
        {
            options.Filter = context => !context.Request.Path.StartsWithSegments("/health");
            options.RecordException = true;
        }));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages(async statusCodeContext =>
{
    var context = statusCodeContext.HttpContext;
    var problem = new ProblemDetails
    {
        Status = context.Response.StatusCode,
        Title = "HTTP request failed"
    };
    problem.Extensions["correlationId"] = context.TraceIdentifier;

    context.Response.ContentType = "application/problem+json";
    var payload = JsonSerializer.SerializeToUtf8Bytes(problem, problemJsonOptions);
    await context.Response.Body.WriteAsync(payload, context.RequestAborted);
});
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestBodyLimitMiddleware>();
if (builder.Configuration.GetValue<bool>("Performance:ExposeMetricsHeaders"))
{
    app.UseMiddleware<PerformanceMetricsHeaderMiddleware>();
}
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger(options => options.RouteTemplate = "openapi/{documentName}.json");
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "OMNI Pricing API v1"));

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live")
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});
app.MapPricingEndpoint(builder.Configuration.GetValue<bool>("Security:RequireAuthentication"));
app.MapLegacyPriceOperationEndpoint(builder.Configuration.GetValue<bool>("Security:RequireAuthentication"));

app.Run();

public partial class Program;
