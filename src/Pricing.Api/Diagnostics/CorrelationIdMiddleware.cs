namespace Pricing.Api.Diagnostics;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";
    public const int MaximumLength = 128;

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = GetCorrelationId(context);
        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        var supplied = context.Request.Headers[HeaderName].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(supplied) && supplied.Length <= MaximumLength
            ? supplied
            : context.TraceIdentifier;
    }
}
