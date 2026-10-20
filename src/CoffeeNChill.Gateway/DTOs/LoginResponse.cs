/*
 * Code Attribution:
 * The response DTO pattern follows the lecturer-provided examples (IIE, 2026) and the Azure Functions
 * HTTP trigger documentation that my Part 2 DTOs were based on.
 *
 * Microsoft Learn (2025b). Azure Functions HTTP trigger. [Online]
 * Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger?tabs=python-v2%2Cisolated-process%2Cnodejsv4%2Cfunctionsv2&pivots=programming-language-csharp
 * [Accessed 04 September 2026].
 */

namespace CoffeeNChill.Gateway.DTOs
{
    // Message after a successful login for the server.
    public class LoginResponse
    {
        // The signed JWT. It goes into the Authorization header as: Bearer <token>
        public string Token { get; set; } = string.Empty;

        // When the token stops working (UTC).
        public DateTime ExpiresAtUtc { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}