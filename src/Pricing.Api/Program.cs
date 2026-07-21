using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Pricing.Api.Diagnostics;
using Pricing.Infrastructure.Db2;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var problemJsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live", "ready"]);
IConfigurationSection db2Configuration = builder.Configuration.GetSection(Db2Options.SectionName);
if (db2Configuration.Exists())
{
    builder.Services.AddDb2DataAccess(db2Configuration);
}
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OMNI Pricing API",
        Version = "v1",
        Description = "Explainable pricing service foundation. COBOL remains authoritative."
    });
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

app.Run();

public partial class Program;
