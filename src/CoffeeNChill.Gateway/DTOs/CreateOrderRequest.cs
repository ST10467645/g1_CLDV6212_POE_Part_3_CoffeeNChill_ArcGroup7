/*
 * Code Attribution:
 * Separating the request model from the queue message follows the lecturer-provided StudentQuery example and my
 * Part 2 CreateOrderRequest (IIE, 2026). The validation attributes follow Microsoft's model validation guidance.
 *
 * Microsoft Learn (2026ad). Model validation in ASP.NET Core MVC. [Online]
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation
 * [Accessed 20 October 2026].
 */

using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.Gateway.DTOs
{
    // The JSON a customer sends to POST /api/orders/queue. The order number and the time are made by the server.
    public class CreateOrderRequest
    {
        [Required(ErrorMessage = "CustomerName is required.")]
        [StringLength(100, ErrorMessage = "CustomerName is too long (100 characters at most).")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "SelectedItemSKUs is required.")]
        [MinLength(1, ErrorMessage = "SelectedItemSKUs must contain at least one item.")]
        public List<string> SelectedItemSKUs { get; set; } = new();

        [Range(0.01, 1000000, ErrorMessage = "TotalPrice must be greater than 0.")]
        public double TotalPrice { get; set; }
    }
}