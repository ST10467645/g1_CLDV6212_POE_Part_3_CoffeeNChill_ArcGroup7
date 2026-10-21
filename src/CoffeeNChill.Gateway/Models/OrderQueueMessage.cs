/*
 * Code Attribution:
 * The queue message shape is the same as the Order class in my Part 2 Functions project, so the existing
 * ProcessOrderQueue function can read it. Structure adapted from the StudentQuery model in the
 * lecturer-provided Student Relations example (IIE, 2026).
 *
 * Microsoft Learn (2026q). Azure Queue Storage client library for .NET. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/storage.queues-readme?view=azure-dotnet
 * [Accessed 04 September 2026].
 */

namespace CoffeeNChill.Gateway.Models
{
    // The JSON that is placed on order-processing-queue. The names must stay exactly like this, because the Functions app reads them.
    public class OrderQueueMessage
    {
        public string OrderId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public List<string> SelectedItemSKUs { get; set; } = new();
        public double TotalPrice { get; set; }
        public DateTime OrderTimestamp { get; set; }
    }
}