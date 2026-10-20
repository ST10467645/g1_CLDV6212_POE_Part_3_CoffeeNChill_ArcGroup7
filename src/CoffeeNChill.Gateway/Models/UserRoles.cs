/*
 * Code Attribution:
 * Role names are kept in one place so the controllers never contain typing mistakes. The role names are
 * the ones in the CLDV6212 Part 3 POE brief (IIE, 2026). They are used with [Authorize(Roles = "...")].
 *
 * Microsoft Learn (2026af). Role-based authorization in ASP.NET Core. [Online]
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles
 * [Accessed 20 October 2026].
 */

namespace CoffeeNChill.Gateway.Models
{
    // The only three roles the system knows.
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Staff = "Staff";
        public const string Customer = "Customer";

        // Used when we need to check "is this one of the real roles?".
        public static readonly string[] All = { Admin, Staff, Customer };

        // Returns the correctly written role name, or null when the text is not a real role. It ignores capital letters.
        public static string? Normalise(string? role)
        {
            return All.FirstOrDefault(r => string.Equals(r, role?.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }
}