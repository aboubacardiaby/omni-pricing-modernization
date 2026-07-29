namespace Pricing.Api.Pricing;

using System.Text.Json;
using global::Pricing.Application.Orchestration;
using global::Pricing.Domain.Models;
using global::Pricing.Domain.ValueObjects;

public static class PricingEndpoint
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Action<ILogger, string, PricingRequestType, ProductType, Exception?> AuditSuccess =
        LoggerMessage.Define<string, PricingRequestType, ProductType>(
            LogLevel.Information, new EventId(53001, "PricingCalculationCompleted"),
            "Pricing calculation completed with outcome {Outcome}, request type {RequestType}, and product type {ProductType}");
    private static readonly Action<ILogger, string, PricingRequestType, Exception?> AuditFailure =
        LoggerMessage.Define<string, PricingRequestType>(
            LogLevel.Warning, new EventId(53002, "PricingCalculationRejected"),
            "Pricing calculation rejected with error {ErrorCode} and request type {RequestType}");
    private static readonly Action<ILogger, PricingRequestType, Exception?> AuditUnexpectedFailure =
        LoggerMessage.Define<PricingRequestType>(
            LogLevel.Error, new EventId(53003, "PricingCalculationFailed"),
            "Pricing calculation failed unexpectedly for request type {RequestType}");

    public static IEndpointRouteBuilder MapPricingEndpoint(
        this IEndpointRouteBuilder endpoints,
        bool requireAuthentication = false)
    {
        RouteHandlerBuilder endpoint = endpoints.MapPost("/api/v1/prices/calculate", CalculateAsync)
            .WithName("calculatePrices")
            .WithTags("Pricing")
            .Produces<CalculatePricesResponse>(StatusCodes.Status200OK)
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

    private static async Task<IResult> CalculateAsync(
        CalculatePricesRequest request,
        IPricingCalculationService pricing,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        ValidationPricingError? requestError = PricingRequestValidator.Validate(request);
        if (requestError is not null)
        {
            WriteFailureAudit(context, requestError.Code, request.RequestType);
            return PricingProblemWriter.Result(context, requestError);
        }

        var responses = new List<CalculateProductResponse>(request.Products.Count);
        foreach (CalculateProductRequest product in request.Products)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ValidationPricingError? productError = PricingRequestValidator.Validate(product);
            if (productError is not null)
            {
                WriteFailureAudit(context, productError.Code, request.RequestType);
                responses.Add(ToErrorResponse(product, productError));
                continue;
            }

            PricingResult result;
            try
            {
                result = await pricing.CalculateAsync(ToDomain(request, product), cancellationToken).ConfigureAwait(false);
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
                responses.Add(ToErrorResponse(product, new DependencyPricingError(
                    "UNEXPECTED_ERROR", "Pricing failed unexpectedly.", IsTransient: false)));
                continue;
            }

            if (!result.Errors.IsEmpty)
            {
                WriteFailureAudit(context, result.Errors[0].Code, request.RequestType);
                responses.Add(ToErrorResponse(product, result.Errors[0]));
                continue;
            }

            AuditSuccess(
                context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("PricingAudit"),
                "Success", request.RequestType, result.ProductType, null);
            responses.Add(ToResponse(product, result));
        }

        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(
            new CalculatePricesResponse(responses), ResponseJsonOptions);
        return Results.Bytes(payload, "application/json");
    }

    private static PricingRequest ToDomain(CalculatePricesRequest request, CalculateProductRequest product) => new(
        new DivisionId(request.Division),
        new AccountNumber(request.Account),
        new VendorId(product.Vendor),
        new ProductId(product.Product),
        new Quantity(request.Quantity),
        new UnitOfMeasure(request.UnitOfMeasure),
        request.ShipTo,
        request.BillTo,
        request.PricingDate,
        request.RequestType);

    private static CalculateProductResponse ToResponse(CalculateProductRequest product, PricingResult result) => new(
        product.Vendor,
        product.Product,
        null,
        result.ProductType == ProductType.Kit ? "Kit" : "Regular",
        result.Cost?.Value,
        result.SellPrice?.Value,
        result.ExpirationDate,
        result.ContractSelection?.Contract.Value,
        result.ContractSelection?.BuyingGroupId?.ToString(System.Globalization.CultureInfo.InvariantCulture),
        result.SellArrangementSelection?.ArrangementType,
        result.Components.Select(component => new PriceComponentResponse(
            component.Name, component.Amount.Value, ToResponse(component.Provenance))).ToArray(),
        result.Provenance.Select(ToResponse).ToArray(),
        result.Warnings.Select(warning => new PricingWarningResponse(warning.Code, warning.Message)).ToArray());

    private static CalculateProductResponse ToErrorResponse(CalculateProductRequest? product, PricingError error) => new(
        product?.Vendor ?? string.Empty,
        product?.Product ?? string.Empty,
        new PricingErrorResponse(error.Code, error.Message, error.LegacyErrorCode),
        null, null, null, null, null, null, null, [], [], []);

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