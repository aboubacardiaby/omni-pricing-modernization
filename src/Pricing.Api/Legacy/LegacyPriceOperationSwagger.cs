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
        ["IN_PRICE"] = new OpenApiObject
        {
            ["IN_ACTION"] = new OpenApiString("A"),
            ["IN_USERID"] = new OpenApiString("NC"),
            ["IN_CO"] = new OpenApiString("OM"),
            ["IN_CUST_ID"] = new OpenApiString("98990079"),
            ["IN_SHIPTO"] = new OpenApiString(string.Empty),
            ["IN_PRICER_MM_DD_CCYY"] = new OpenApiString("06-24-2026"),
            ["IN_NBR_REQUESTS"] = new OpenApiInteger(1),
            ["IN_PRODUCT_NO"] = new OpenApiArray { new OpenApiString("23000J346H") },
        },
    };

    private static OpenApiObject ResponseExample() => new()
    {
        ["OUT_PRICE"] = new OpenApiObject
        {
            ["OUT_ERROR_FLAG"] = new OpenApiString(string.Empty),
            ["OUT_ERROR_DESC"] = new OpenApiString(string.Empty),
            ["OUT_ROW"] = new OpenApiArray
            {
                new OpenApiObject
                {
                    ["OUT_ERROR_SW"] = new OpenApiString(string.Empty),
                    ["OUT_ERROR_NUMBER"] = new OpenApiString(string.Empty),
                    ["OUT_ERROR_MSG"] = new OpenApiString(string.Empty),
                    ["OUT_PART_NBR"] = new OpenApiString("23000J346H"),
                    ["OUT_CATALOG_NBR"] = new OpenApiString("J346H"),
                    ["OUT_PART_DESCRIPTION"] = new OpenApiString("SUTURE CTD VICRYL 0 VIL BR CT-1"),
                    ["OUT_ITEM_INDICATOR"] = new OpenApiString("C"),
                    ["OUT_NON_STOCK_FLAG"] = new OpenApiString("N"),
                    ["OUT_VENDOR_NAME"] = new OpenApiString("JOHNSON & JOHNSON / ETHICON INC / S"),
                    ["OUT_PRICER"] = new OpenApiObject
                    {
                        ["OUT_VENDOR_CONTRACT_NBR"] = new OpenApiString("NOT CONTRACTED"),
                        ["OUT_OMNI2_PRICING"] = new OpenApiString("Y"),
                        ["OUT_SANCTIONED_FLAG"] = new OpenApiString("N"),
                    },
                    ["OUT_INB"] = new OpenApiObject
                    {
                        ["OUT_BR_DFLT_UOM"] = new OpenApiString("BX"),
                        ["OUT_QTY_AVAILABLE"] = new OpenApiString("936+"),
                        ["OUT_QTY_ON_ORDER"] = new OpenApiString("108+"),
                    },
                    ["OUT_BASE"] = new OpenApiObject
                    {
                        ["OUT_BU_UOM"] = new OpenApiString("EA"),
                        ["OUT_BU_UOM_DESC"] = new OpenApiString("1"),
                        ["OUT_BU_PRICE"] = new OpenApiString("6.5140"),
                        ["OUT_BU_PRICE_UNRND"] = new OpenApiString("6.51404000"),
                        ["OUT_BU_FILE_COST"] = new OpenApiString("5.0108"),
                        ["OUT_BU_ACQ_COST"] = new OpenApiString("5.0108"),
                        ["OUT_BU_TOTAL_COST"] = new OpenApiString("5.0108"),
                        ["OUT_BU_VENDOR_UOM"] = new OpenApiString("EA"),
                    },
                    ["OUT_ALT_NBR_OF_UOMS"] = new OpenApiInteger(5),
                    ["OUT_ALT"] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["OUT_ALT_DFLT_UOM"] = new OpenApiString("Y"),
                            ["OUT_ALT_UOM"] = new OpenApiString("BX"),
                            ["OUT_ALT_FACTOR"] = new OpenApiString("36.0000"),
                            ["OUT_ALT_PRICE"] = new OpenApiString("234.5054"),
                            ["OUT_ALT_PRICE_UNRND"] = new OpenApiString("234.50544000"),
                            ["OUT_ALT_FILE_COST"] = new OpenApiString("180.3888"),
                            ["OUT_ALT_ACQ_COST"] = new OpenApiString("180.3888"),
                            ["OUT_ALT_TOTAL_COST"] = new OpenApiString("180.3888"),
                        },
                    },
                },
            },
        },
    };
}
