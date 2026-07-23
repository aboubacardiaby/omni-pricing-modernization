namespace Pricing.Api.Legacy;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public sealed class PriceRequest
{
    [Required]
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("company")]
    public string Company { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("shipTo")]
    public string ShipTo { get; set; } = string.Empty;

    /// <summary>Date in MM-DD-CCYY format (e.g. 04-21-2026).</summary>
    [Required]
    [JsonPropertyName("pricerDate")]
    public string PricerDate { get; set; } = string.Empty;

    [Range(1, 25)]
    [JsonPropertyName("numberOfRequests")]
    public int NumberOfRequests { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(25)]
    [JsonPropertyName("productNumbers")]
    public List<string> ProductNumbers { get; set; } = [];
}
