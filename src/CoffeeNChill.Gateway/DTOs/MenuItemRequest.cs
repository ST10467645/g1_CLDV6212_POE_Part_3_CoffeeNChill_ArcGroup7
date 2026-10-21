/*
 * Code Attribution:
 * Separating the request model from the table entity follows the lecturer's StudentQuery and StudentQueryEntity examples and my Part 2 MenuItemRequest.
 * The validation attributes follow microsoft's model validation guidance so every problem is reported together and Swagger shows the rules.
 * 
 * Microsoft Learn (2026ad). Model validation in ASP.NET Core MVC and Razor Pages
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0#validationresult helped me with Validation attributes and Error messages sections helped me with Required, StringLength and Range.
 * [Accessed 20 October 2026].
 * 
 * Microsoft Learn (2023). Understanding the Table service data model
 * Available at: https://learn.microsoft.com/en-us/rest/api/storageservices/understanding-the-table-service-data-model this explains which Characters Disallowed in Key Fields (so i can make sure RegularExpression wont accept them).
 * [Accessed 20 October 2026].
 * 
 * riptutorial (2024) RegularExpression Attribute
 * Available at: https://riptutorial.com/asp-net-mvc/example/16150/regularexpression-attribute shows me what a RegularExpression should look like
 * [Accessed 20 October 2026].
 * 
 * similar to MenuItemRequest in CoffeeNChillFunctions but a few changes were made.
 */
using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.Gateway.DTOs
{
    // The JSON sent to POST /api/menu to create a menu item.
    public class MenuItemRequest
    {
        [Required(ErrorMessage = "Category is required.")]
        [StringLength(60, ErrorMessage = "Category is too long (60 characters at most).")]
        //Table keys cannot contain / \ # or ? so we refuse them here.
        [RegularExpression(@"^[^/\\#?]+$", ErrorMessage = "Category cannot contain / \\ # or ?.")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sku is required.")]
        [StringLength(30, ErrorMessage = "Sku is too long (30 characters at most).")]
        [RegularExpression(@"^[^/\\#?]+$", ErrorMessage = "Sku cannot contain / \\ # or ?.")]
        public string Sku { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, ErrorMessage = "Name is too long (100 characters at most).")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description is too long (500 characters at most).")]
        public string? Description { get; set; }

        [Range(0, 100000, ErrorMessage = "Price must be between 0 and 100000.")]
        public double Price { get; set; }

        public bool IsAvailable { get; set; } = true;

    }

    //The JSON sent to PUT /api/menu/{category}/{sku}. Every value is optional only the values sent are updated.
    public class MenuItemUpdateRequest
    {
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters.")]
        public string? Name { get; set; }

        [StringLength(500, ErrorMessage = "Description is too long (500 characters at most).")]
        public string? Description { get; set; }

        [Range(0, 100000, ErrorMessage = "Price must be between 0 and 100000.")]
        public double? Price { get; set; }

        public bool? IsAvailable { get; set; }

    }
}

