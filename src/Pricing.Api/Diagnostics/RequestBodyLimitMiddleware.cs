namespace Pricing.Api.Diagnostics;

using global::Pricing.Api.Pricing;

public sealed class RequestBodyLimitMiddleware(RequestDelegate next)
{
    public const long MaximumBodyBytes = 64 * 1024;

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Request.ContentLength is > MaximumBodyBytes)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            context.Response.ContentType = "application/problem+json";
            byte[] payload = PricingProblemWriter.Payload(
                context,
                StatusCodes.Status413PayloadTooLarge,
                "The request body is too large.",
                $"Request bodies cannot exceed {MaximumBodyBytes} bytes.",
                "REQUEST_BODY_TOO_LARGE");
            await context.Response.Body.WriteAsync(payload, context.RequestAborted);
            return;
        }

        await next(context);
    }
}
