/*
 * Code Attribution:
 * Separating the response model from the table entity follows the lecturer's model examples and my Part 2 MenuItemResponse (hiding the ETag and Timestamp.)
 * 
 * similar to MenuItemResponse in CoffeeNChillFunctions
 *
 * Microsoft Learn (2026c). Azure Tables client library for .NET - version 12.12.0. (the ITableEntity example helped me)
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
 * [Accessed 20 October 2026].
 */
namespace CoffeeNChill.Gateway.DTOs
{
    //A menu item as the client sees it. It uses clear names (Category, Sku) instead of PartitionKey and RowKey.
    public class MenuItemResponse
    {
        public string Category { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
        public bool IsAvailable { get; set; }

    }
}
