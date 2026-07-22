namespace Pricing.Api.Pricing;

using global::Pricing.Application.Orchestration;
using global::Pricing.Domain.Models;
using global::Pricing.Domain.ValueObjects;
using System.Text.Json;

public static class PricingEndpoint
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Action<ILogger, string, PricingRequestType, ProductType, Exception?> AuditSuccess =
        LoggerMessage.Define<string, PricingRequestType, ProductType>(
            LogLevel.Information,
            new EventId(53001, "PricingCalculationCompleted"),
            "Pricing calculation completed with outcome {Outcome}, request type {RequestType}, and product type {ProductType}");
    private static readonly Action<ILogger, string, PricingRequestType, Exception?> AuditFailure =
        LoggerMessage.Define<string, PricingRequestType>(
            LogLevel.Warning,
            new EventId(53002, "PricingCalculationRejected"),
            "Pricing calculation rejected with error {ErrorCode} and request type {RequestType}");
    private static readonly Action<ILogger, PricingRequestType, Exception?> AuditUnexpectedFailure =
        LoggerMessage.Define<PricingRequestType>(
            LogLevel.Error,
            new EventId(53003, "PricingCalculationFailed"),
            "Pricing calculation failed unexpectedly for request type {RequestType}");

    public static IEndpointRouteBuilder MapPricingEndpoint(
        this IEndpointRouteBuilder endpoints,
        bool requireAuthentication = false)
    {
        RouteHandlerBuilder endpoint = endpoints.MapPost("/api/v1/prices/calculate", CalculateAsync)
            .WithName("calculatePrice")
            .WithTags("Pricing")
            .Produces<CalculatePriceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .RequireRateLimiting("pricing");
        if (requireAuthentication)
        {
            endpoint.RequireAuthorization("PricingCalculate");
        }

        return endpoints;
    }

    private static async Task<IResult> CalculateAsync(
        CalculatePriceRequest request,
        IPricingCalculationService pricing,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        ValidationPricingError? validationError = PricingRequestValidator.Validate(request);
        if (validationError is not null)
        {
            WriteFailureAudit(context, validationError.Code, request.RequestType);
            return PricingProblemWriter.Result(context, validationError);
        }

        PricingRequest domainRequest = ToDomain(request);
        PricingResult result;
        try
        {
            result = await pricing.CalculateAsync(domainRequest, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            AuditUnexpectedFailure(
                context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("PricingAudit"),
                request.RequestType,
                null);
            return PricingProblemWriter.Result(
                context,
                StatusCodes.Status500InternalServerError,
                "Pricing failed unexpectedly.",
                null,
                "UNEXPECTED_ERROR");
        }
        if (!result.Errors.IsEmpty)
        {
            WriteFailureAudit(context, result.Errors[0].Code, request.RequestType);
            return PricingProblemWriter.Result(context, result.Errors[0]);
        }

        AuditSuccess(
            context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("PricingAudit"),
            "Success",
            request.RequestType,
            result.ProductType,
            null);
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(
            ToResponse(result),
            ResponseJsonOptions);
        return Results.Bytes(payload, "application/json");
    }

    private static PricingRequest ToDomain(CalculatePriceRequest request) => new(
        new DivisionId(request.Division),
        new AccountNumber(request.Account),
        new VendorId(request.Vendor),
        new ProductId(request.Product),
        new Quantity(request.Quantity),
        new UnitOfMeasure(request.UnitOfMeasure),
        request.ShipTo,
        request.BillTo,
        request.PricingDate,
        request.RequestType);

    private static CalculatePriceResponse ToResponse(PricingResult result) => new(
        result.ProductType == ProductType.Kit ? "Kit" : "Regular",
        result.Cost?.Value,
        result.SellPrice?.Value,
        result.ExpirationDate,
        result.ContractSelection?.Contract.Value,
        result.ContractSelection?.BuyingGroupId?.ToString(System.Globalization.CultureInfo.InvariantCulture),
        result.SellArrangementSelection?.ArrangementType,
        result.Components.Select(component => new PriceComponentResponse(
            component.Name,
            component.Amount.Value,
            ToResponse(component.Provenance))).ToArray(),
        result.Provenance.Select(ToResponse).ToArray(),
        result.Warnings.Select(warning => new PricingWarningResponse(warning.Code, warning.Message)).ToArray());

    private static RuleProvenanceResponse ToResponse(RuleProvenance provenance) => new(
        provenance.RuleName,
        provenance.Source,
        provenance.HierarchyLevel,
        provenance.EffectiveDates?.EffectiveDate,
        provenance.EffectiveDates?.ExpirationDate);

    private static void WriteFailureAudit(
        HttpContext context,
        string errorCode,
        PricingRequestType requestType) => AuditFailure(
            context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("PricingAudit"),
            errorCode,
            requestType,
            null);
}
