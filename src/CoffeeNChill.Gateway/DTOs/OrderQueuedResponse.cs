/*
 * Code Attribution:
 * Response DTO pattern based on the Azure Functions HTTP trigger documentation that my Part 2 DTOs were based on.
 *
 * Microsoft Learn (2025b). Azure Functions HTTP trigger. [Online]
 * Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger?tabs=python-v2%2Cisolated-process%2Cnodejsv4%2Cfunctionsv2&pivots=programming-language-csharp
 * [Accessed 04 September 2026].
 */

using System.Text.Json.Serialization;

namespace CoffeeNChill.Gateway.DTOs
{
    // What the customer gets back after placing an order (202 Accepted).
    public class OrderQueuedResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        // Needed later to look the order up, because the order date is the PartitionKey of the Orders table.
        [JsonPropertyName("orderDate")]
        public string OrderDate { get; set; } = string.Empty;
    }
}