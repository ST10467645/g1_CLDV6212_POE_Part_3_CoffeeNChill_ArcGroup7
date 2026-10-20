/*
 * Code Attribution:
 * Keeping the response model separate from the table entity follows the lecturer-provided model examples (IIE, 2026).
 * The point of this class is that the PasswordHash, ETag and Timestamp are never sent to the client.
 *
 * Microsoft Learn (2026c). Azure Tables client library for .NET - version 12.12.0. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
 * [Accessed 04 September 2026].
 */

namespace CoffeeNChill.Gateway.DTOs
{
    // The response the user will see (as a client), therefore, there is no password and no hash here on purpose.
    public class UserResponse
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}