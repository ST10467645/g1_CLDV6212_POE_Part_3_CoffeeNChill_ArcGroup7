/*
 * Code Attribution:
 * The update request with optional properties follows the MenuItemUpdateRequest idea from my Part 2 DTOs,
 * where only the values that are sent are changed (IIE, 2026). Asking for the current password before a new one
 * is set is my own design choice for account safety.
 *
 * Microsoft Learn (2026ad). Model validation in ASP.NET Core MVC. [Online]
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation
 * [Accessed 20 October 2026].
 */

using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.Gateway.DTOs
{
    // The JSON a logged-in user sends to PUT /api/profile/mine.
    public class UpdateProfileRequest
    {
        [StringLength(100, MinimumLength = 2, ErrorMessage = "FullName must be between 2 and 100 characters.")]
        public string? FullName { get; set; }

        // Only when NewPassword is sent.
        [StringLength(100, ErrorMessage = "CurrentPassword is too long.")]
        public string? CurrentPassword { get; set; }

        [StringLength(100, MinimumLength = 8, ErrorMessage = "NewPassword must be between 8 and 100 characters.")]
        public string? NewPassword { get; set; }
    }
}