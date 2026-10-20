/*
 * Code Attribution:
 * Separating the request model from the table entity follows the lecturer-provided StudentQuery and
 * StudentQueryEntity examples (IIE, 2026). The validation attributes follow Microsoft's model validation guidance.
 *
 * Microsoft Learn (2026ad). Model validation in ASP.NET Core MVC. [Online]
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation
 * [Accessed 20 October 2026].
 */

using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.Gateway.DTOs
{
    // The JSON a person sends to POST /api/auth/register.
    public class RegisterRequest
    {
        [Required(ErrorMessage = "FullName is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "FullName must be between 2 and 100 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email is not a valid email address.")]
        [StringLength(150, ErrorMessage = "Email is too long.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters.")]
        public string Password { get; set; } = string.Empty;

        // An anonymous caller always becomes a Customer.
        // Only a logged-in Admin may ask for Staff or Admin (the rule is in AuthController).
        public string? Role { get; set; }
    }
}