namespace Pricing.Api.Legacy;

using global::Pricing.Api.Pricing;
using System.Text.Json;

public static class LegacyPriceOperationEndpoint
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapLegacyPriceOperationEndpoint(
        this IEndpointRouteBuilder endpoints,
        bool requireAuthentication = false)
    {
        RouteHandlerBuilder endpoint = endpoints
            .MapPost("/api/v1/legacy/price-operation", ExecuteAsync)
            .WithName("legacyPriceOperation")
            .WithSummary("Execute the legacy Price operation using JSON")
            .WithDescription("JSON compatibility facade for the Micro Focus Price.wsdl contract. COBOL remains authoritative.")
            .WithTags("Legacy compatibility")
            .Accepts<LegacyPriceOperationRequest>("application/json")
            .Produces<LegacyPriceOperationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireRateLimiting("pricing");
        if (requireAuthentication)
        {
            endpoint.RequireAuthorization("PricingCalculate");
        }

        return endpoints;
    }

    private static async Task<IResult> ExecuteAsync(
        LegacyPriceOperationRequest request,
        ILegacyPriceOperationService operation,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            LegacyPriceOperationResponse response = await operation
                .ExecuteAsync(request.InPrice, cancellationToken)
                .ConfigureAwait(false);
            return Results.Bytes(
                JsonSerializer.SerializeToUtf8Bytes(response, ResponseJsonOptions),
                "application/json");
        }
        catch (ArgumentException exception)
        {
            return PricingProblemWriter.Result(
                context,
                StatusCodes.Status400BadRequest,
                exception.Message,
                null,
                "INVALID_LEGACY_PRICE_OPERATION");
        }
    }
}
