namespace Pricing.Api.Legacy;

using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public sealed class LegacyPriceOperationSwagger : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!string.Equals(operation.OperationId, "legacyPriceOperation", StringComparison.Ordinal))
        {
            return;
        }

        operation.RequestBody.Content["application/json"].Example = RequestExample();
        operation.Responses["200"].Content["application/json"].Example = ResponseExample();
    }

    private static OpenApiObject RequestExample() => new()
    {
        ["action"] = new OpenApiString("A"),
        ["userId"] = new OpenApiString("NC"),
        ["company"] = new OpenApiString("OM"),
        ["customerId"] = new OpenApiString("98990079"),
        ["shipTo"] = new OpenApiString(string.Empty),
        ["pricerDate"] = new OpenApiString("06-24-2026"),
        ["numberOfRequests"] = new OpenApiInteger(1),
        ["productNumbers"] = new OpenApiArray { new OpenApiString("23000J346H") },
    };

    private static OpenApiObject ResponseExample() => new()
    {
        ["errorFlag"] = new OpenApiString(string.Empty),
        ["errorDescription"] = new OpenApiString(string.Empty),
        ["rows"] = new OpenApiArray
        {
            new OpenApiObject
            {
                ["errorSwitch"] = new OpenApiString(string.Empty),
                ["errorNumber"] = new OpenApiString(string.Empty),
                ["errorMessage"] = new OpenApiString(string.Empty),
                ["partNumber"] = new OpenApiString("23000J346H"),
                ["catalogNumber"] = new OpenApiString("J346H"),
                ["partDescription"] = new OpenApiString("SUTURE CTD VICRYL 0 VIL BR CT-1"),
                ["itemIndicator"] = new OpenApiString("C"),
                ["nonStockFlag"] = new OpenApiString("N"),
                ["vendorName"] = new OpenApiString("JOHNSON & JOHNSON / ETHICON INC / S"),
                ["pricer"] = new OpenApiObject
                {
                    ["vendorContractNumber"] = new OpenApiString("NOT CONTRACTED"),
                    ["omni2Pricing"] = new OpenApiString("Y"),
                    ["sanctionedFlag"] = new OpenApiString("N"),
                },
                ["inventory"] = new OpenApiObject
                {
                    ["defaultUom"] = new OpenApiString("BX"),
                    ["quantityAvailable"] = new OpenApiString("936+"),
                    ["quantityOnOrder"] = new OpenApiString("108+"),
                },
                ["base"] = new OpenApiObject
                {
                    ["uom"] = new OpenApiString("EA"),
                    ["uomDescription"] = new OpenApiString("1"),
                    ["price"] = new OpenApiString("6.5140"),
                    ["fileCost"] = new OpenApiString("5.0108"),
                    ["acquisitionCost"] = new OpenApiString("5.0108"),
                    ["totalCost"] = new OpenApiString("5.0108"),
                    ["vendorUom"] = new OpenApiString("EA"),
                },
                ["numberOfAlternateUoms"] = new OpenApiInteger(5),
                ["alternates"] = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        ["defaultUom"] = new OpenApiString("Y"),
                        ["uom"] = new OpenApiString("BX"),
                        ["factor"] = new OpenApiString("36.0000"),
                        ["price"] = new OpenApiString("234.5054"),
                        ["fileCost"] = new OpenApiString("180.3888"),
                        ["acquisitionCost"] = new OpenApiString("180.3888"),
                        ["totalCost"] = new OpenApiString("180.3888"),
                    },
                },
            },
        },
    };
}