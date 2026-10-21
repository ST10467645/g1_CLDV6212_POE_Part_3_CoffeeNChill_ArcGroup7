/*
 * Code Attribution:
 * The mapper keeps every conversion between the table entity and the DTOs in one place, so the controllers stay short
 * and the password hash can never be returned by accident. The idea follows the lecturer-provided mapper examples
 * (IIE, 2026) and my Part 2 MenuItemMapper and OrderMapper.
 *
 * Microsoft Learn (2026c). Azure Tables client library for .NET - version 12.12.0. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
 * [Accessed 04 September 2026].
 */

using CoffeeNChill.Gateway.Models;

namespace CoffeeNChill.Gateway.DTOs
{
    // Converts between the Users table row (UserEntity) and the DTOs.
    public static class UserMapper
    {
        // Builds the table row for a new user. The role becomes the PartitionKey and the email (lower case) the RowKey.
        public static UserEntity ToEntity(string fullName, string email, string role, string passwordHash) => new UserEntity
        {
            PartitionKey = role,
            RowKey = email.Trim().ToLowerInvariant(),
            FullName = fullName.Trim(),
            PasswordHash = passwordHash,
            CreatedDate = DateTime.UtcNow
        };

        // Turns a table row into what the client may see. The password hash is hidden for security purposes.
        public static UserResponse ToResponse(UserEntity entity) => new UserResponse
        {
            FullName = entity.FullName,
            Email = entity.RowKey,
            Role = entity.PartitionKey,
            CreatedDate = entity.CreatedDate
        };
    }
}