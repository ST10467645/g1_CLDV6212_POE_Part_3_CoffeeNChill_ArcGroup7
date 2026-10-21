/*
 * Code Attribution:
 * Response DTO pattern based on the Azure Functions HTTP trigger documentation (Microsoft Learn, 2025b); the reply shape
 * follows the optional status endpoint example given by the lecturer (IIE, 2026) and my Part 2 OrderStatusResponse.
 *
 * Microsoft Learn (2025b). Azure Functions HTTP trigger. [Online]
 * Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger?tabs=python-v2%2Cisolated-process%2Cnodejsv4%2Cfunctionsv2&pivots=programming-language-csharp
 * [Accessed 19 October 2026].
 */

using System.Text.Json.Serialization;

namespace CoffeeNChill.Gateway.DTOs
{
    // What a customer sees when they check an order: only the order number and the status.
    public class OrderStatusResponse
    {
        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}