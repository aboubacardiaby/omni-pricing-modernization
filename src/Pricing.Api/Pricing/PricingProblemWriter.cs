namespace Pricing.Api.Pricing;

using System.Text.Json;
using System.Text;
using global::Pricing.Domain.Models;
using Microsoft.AspNetCore.Mvc;

public static class PricingProblemWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IResult Result(HttpContext context, PricingError error)
    {
        (int status, string title) = error switch
        {
            ValidationPricingError => (StatusCodes.Status400BadRequest, "The pricing request is invalid."),
            MissingDataPricingError => (StatusCodes.Status404NotFound, "Required pricing data was not found."),
            UnsupportedBehaviorPricingError => (StatusCodes.Status422UnprocessableEntity, "The request cannot be priced."),
            DependencyPricingError => (StatusCodes.Status503ServiceUnavailable, "A pricing dependency is unavailable."),
            _ => (StatusCodes.Status500InternalServerError, "Pricing failed unexpectedly."),
        };
        return Result(context, status, title, error.Message, error.Code, error.LegacyErrorCode);
    }

    public static IResult Result(
        HttpContext context,
        int status,
        string title,
        string? detail,
        string errorCode,
        string? legacyErrorCode = null)
    {
        byte[] payload = Payload(context, status, title, detail, errorCode, legacyErrorCode);
        return Results.Content(
            Encoding.UTF8.GetString(payload),
            "application/problem+json",
            Encoding.UTF8,
            status);
    }

    public static byte[] Payload(
        HttpContext context,
        int status,
        string title,
        string? detail,
        string errorCode,
        string? legacyErrorCode = null)
    {
        var problem = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{status}",
            Title = title,
            Status = status,
            Detail = detail,
            Instance = context.Request.Path,
        };
        problem.Extensions["errorCode"] = errorCode;
        if (legacyErrorCode is not null)
        {
            problem.Extensions["legacyErrorCode"] = legacyErrorCode;
        }

        problem.Extensions["correlationId"] = context.TraceIdentifier;
        return JsonSerializer.SerializeToUtf8Bytes(problem, JsonOptions);
    }
}
