/*
 * Code Attribution:
 * The mapper converts between the request, the queue message and the response in one place, so the controller stays short.
 * Adapted from my Part 2 OrderMapper and the lecturer-provided mapper examples (IIE, 2026).
 *
 * Microsoft Learn (2026c). Azure Tables client library for .NET - version 12.12.0. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
 * [Accessed 04 September 2026].
 */

using CoffeeNChill.Gateway.Models;

namespace CoffeeNChill.Gateway.DTOs
{
    // Converts between the order DTOs and the queue message.
    public static class OrderMapper
    {
        // Request > queue message. The order number and the UTC time are generated here.
        public static OrderQueueMessage ToQueueMessage(CreateOrderRequest dto) => new OrderQueueMessage
        {
            OrderId = GenerateOrderId(),
            CustomerName = dto.CustomerName.Trim(),
            SelectedItemSKUs = dto.SelectedItemSKUs.Select(sku => sku.Trim()).ToList(),
            TotalPrice = dto.TotalPrice,
            OrderTimestamp = DateTime.UtcNow
        };

        // Makes an order number that looks like ORD-2026-A1B2C3.
        public static string GenerateOrderId() =>
            $"ORD-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

        // Queue message > the reply that is sent to the customer.
        public static OrderQueuedResponse ToQueuedResponse(OrderQueueMessage message) => new OrderQueuedResponse
        {
            Message = "Order accepted and queued for processing.",
            OrderId = message.OrderId,
            OrderDate = message.OrderTimestamp.ToString("yyyy-MM-dd")
        };
    }
}