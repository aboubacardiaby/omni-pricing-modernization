namespace Pricing.Api.Diagnostics;

using global::Pricing.Api.Pricing;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";
    public const int MaximumLength = 128;

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string? supplied = context.Request.Headers[HeaderName].FirstOrDefault();
        if (supplied?.Length > MaximumLength)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";
            byte[] payload = PricingProblemWriter.Payload(
                context,
                StatusCodes.Status400BadRequest,
                "The pricing request is invalid.",
                $"{HeaderName} cannot exceed {MaximumLength} characters.",
                "CORRELATION_ID_TOO_LONG");
            await context.Response.Body.WriteAsync(payload, context.RequestAborted);
            return;
        }

        var correlationId = GetCorrelationId(context, supplied);
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

    private static string GetCorrelationId(HttpContext context, string? supplied)
    {
        return !string.IsNullOrWhiteSpace(supplied)
            ? supplied
            : context.TraceIdentifier;
    }
}
