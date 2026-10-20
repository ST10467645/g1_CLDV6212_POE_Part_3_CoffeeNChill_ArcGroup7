/*
 * Code Attribution:
 * The ITableEntity pattern was adapted from the lecturer's StudentQueryEntity example and from my Part 1 MenuItem class.
 * The columns are the same as the Part 1 MenuItems table, so the gateway and the Functions can read the same rows.
 *
 * Microsoft Learn (2026). Azure Tables client library for .NET - version 12.12.0. (the LINQ style query expressions example helped me)
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
 * [Accessed 20 October 2026].
 * 
 * stackoverflow (2019) C#8 what does "default!" do on generic types?
 * Available at: https://stackoverflow.com/questions/55525861/c8-what-does-default-do-on-generic-types helped me understand  = default!;
 * [Accessed 20 October 2026].
 */
using Azure;
using Azure.Data.Tables;

namespace CoffeeNChill.Gateway.Models
{
    // One row in the MenuItems Azure Table (same shape as the Part 1 and Part 2 Functions use).
    public class MenuItemEntity : ITableEntity
    {
        // Category, for example "Hot Drinks"
        //the default! is to ignore null warning because the value gets set later
        public string PartitionKey { get; set; } = default!;
        // SKU, for example "COF-001"
        public string RowKey { get; set; } = default!;       
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public double Price { get; set; }
        public bool IsAvailable { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

    }
}
