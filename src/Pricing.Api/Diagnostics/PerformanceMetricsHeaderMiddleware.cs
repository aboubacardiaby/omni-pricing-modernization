namespace Pricing.Api.Diagnostics;

using global::Pricing.Infrastructure.Db2;

public sealed class PerformanceMetricsHeaderMiddleware(
    RequestDelegate next,
    IDb2CallCounter callCounter)
{
    public const string DbCallCountHeader = "X-DB-Call-Count";
    public const string WorkingSetHeader = "X-Process-Working-Set-Bytes";

    public async Task InvokeAsync(HttpContext context)
    {
        using IDisposable request = callCounter.BeginRequest();
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[DbCallCountHeader] = callCounter.Count.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            context.Response.Headers[WorkingSetHeader] = Environment.WorkingSet.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            return Task.CompletedTask;
        });
        await next(context);
    }
}
