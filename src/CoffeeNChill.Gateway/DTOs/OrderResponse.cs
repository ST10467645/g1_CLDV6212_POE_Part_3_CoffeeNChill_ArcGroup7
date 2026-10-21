/*
 * Code Attribution:
 * The order response DTO follows my Part 2 OrderResponse. The gateway reads this shape from the Functions app and
 * sends it on to the client, so table details like ETag and Timestamp never reach the client.
 *
 * Microsoft Learn (2026c). Azure Tables client library for .NET - version 12.12.0. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
 * [Accessed 19 October 2026].
 */

namespace CoffeeNChill.Gateway.DTOs
{
    // One full order as staff see it.
    public class OrderResponse
    {
        public string OrderDate { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public List<string> SelectedItemSKUs { get; set; } = new();
        public double TotalPrice { get; set; }
        public string OrderTimestamp { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string LastUpdated { get; set; } = string.Empty;
    }
}