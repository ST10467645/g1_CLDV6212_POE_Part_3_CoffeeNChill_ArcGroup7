/*
 * Code Attribution:
 * The ITableEntity pattern (PartitionKey, RowKey, Timestamp, ETag plus my own properties) was adapted from the
 * lecturer-provided StudentQueryEntity example and from my Part 2 OrderEntity class (IIE, 2026).
 * The Users table layout (PartitionKey = Role, RowKey = email) follows the CLDV6212 Part 3 POE brief (IIE, 2026).
 *
 * Microsoft Learn (2026c). Azure Tables client library for .NET - version 12.12.0. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
 * [Accessed 04 September 2026].
 */

using Azure;
using Azure.Data.Tables;

namespace CoffeeNChill.Gateway.Models
{
    // One row in the Users Azure Table.
    public class UserEntity : ITableEntity
    {
        // The role: Admin, Staff or Customer. Grouping users by role makes "list all Staff" a fast query.
        public string PartitionKey { get; set; } = string.Empty;

        // The email address in lower case. It is unique inside the partition.
        public string RowKey { get; set; } = string.Empty;

        // The BCrypt hash of the password. The real password is never stored.
        public string PasswordHash { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        // When the account was created (UTC).
        public DateTime CreatedDate { get; set; }

        // Filled in by Azure Tables.
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}