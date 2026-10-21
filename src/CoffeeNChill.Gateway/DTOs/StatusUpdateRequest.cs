/*
 * Code Attribution:
 * The status body {"Status":"Preparing"} follows my Part 2 UpdateOrderStatus function. The validation attributes follow
 * Microsoft's model validation guidance.
 *
 * Microsoft Learn (2026ad). Model validation in ASP.NET Core MVC. [Online]
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation
 * [Accessed 20 October 2026].
 */

using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.Gateway.DTOs
{
    // The JSON staff send to PUT /api/orders/{orderDate}/{orderId}/status.
    public class StatusUpdateRequest
    {
        [Required(ErrorMessage = "Status is required.")]
        [AllowedValues("Received", "Preparing", "Ready", "Collected", ErrorMessage = "Status must be one of: Received, Preparing, Ready, Collected.")]
        public string Status { get; set; } = string.Empty;
    }
}