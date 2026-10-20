/*
 * Code Attribution:
 * The Table Storage create, read, update (with ETag) and delete calls were adapted from my Part 1 menu Functions
 * (which were based on the lecturers example) and from the Azure Tables client library documentation. 
 * The controller structure, [HttpGet]/[HttpPost] attributes and the ActionResult return types follow
 * Microsoft's ASP.NET Core web API documentation. [AllowAnonymous] and [Authorize(Roles = "...")] follow the role-based authorization documentation.
 * 
 * C# Corner (2025) Mastering [Produces("application/json")] in ASP.NET Core
 * Available at: https://www.c-sharpcorner.com/blogs/mastering-producesapplicationjson-in-asp-net-core explains [Produces("application/json")] (make the output always JSON)
 * [Accessed 20 October 2026].
 * 
 * stackoverflow (2015) Response model for specific status codes using Swagger
 * Available at: https://stackoverflow.com/questions/29489304/response-model-for-specific-status-codes-using-swagger showed me an example of how i could do /// <summary> /// </summary> and /// <response code="...">....</response> (these will be comment visable to the user)
 * [Accessed 20 October 2026].
 *
 * Microsoft Learn (2026). Azure Tables client library for .NET - version 12.12.0. 
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
 * [Accessed 20 October 2026].
 *
 * Microsoft Learn (2026). Create web APIs with ASP.NET Core. 
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/web-api/
 * [Accessed 20 October 2026].
 *
 * Microsoft Learn (2026). Role-based authorization in ASP.NET Core. (Pages/RequireAdminOrSuperUserRoleWithAuthorizeAttribute.razor example helped me)
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles
 * [Accessed 20 October 2026].
 *
 * Microsoft Learn (2026). Controller action return types in ASP.NET Core web API. (helped me with ProducesResponseType)
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/web-api/action-return-types
 * [Accessed 20 October 2026].
 * 
 * Microsoft Learn (2026).TableUpdateMode Enum (helped me with TableUpdateMode.Replace) 
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/azure.data.tables.tableupdatemode?view=azure-dotnet
 * [Accessed 20 October 2026].
 * 
 * GitHub (2023) Updating and Upserting Table Entities (guiding me with this line:await table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);)
 * Available at: https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/tables/Azure.Data.Tables/samples/Sample5UpdateUpsertEntities.md
 * [Accessed 20 October 2026].
 */
using Azure;
using Azure.Data.Tables;
using CoffeeNChill.Gateway.DTOs;
using CoffeeNChill.Gateway.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers
{
    //the following /// comments are so the user can see them when using swagger
    /// <summary>
    /// The CoffeeNChill menu. Anyone can read it. Only Admin and Staff can change it.
    /// </summary>
    [ApiController]
    [Route("api/menu")]
    [Produces("application/json")]
    public class MenuController : ControllerBase
    {
        private const string TableName = "MenuItems";

        //One shared client for the whole table service provided by a dependency injection (found in Program.cs).
        private readonly TableServiceClient _tables;

        public MenuController(TableServiceClient tables)
        {
            _tables = tables;
        }

        //Gets the MenuItems table and checks if it exists so it shows an empty list instead of an error.
        private async Task<TableClient> GetTableAsync()
        {
            TableClient table = _tables.GetTableClient(TableName);
            await table.CreateIfNotExistsAsync();
            return table;
        }

        /// <summary>
        /// Gets the whole menu. No login needed.
        /// </summary>
        /// <response code="200">The list of menu items (an empty list when there are none).</response>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType<List<MenuItemResponse>>(StatusCodes.Status200OK)]
        public async Task<ActionResult<List<MenuItemResponse>>> GetAll()
        {
            TableClient table = await GetTableAsync();
            var items = new List<MenuItemResponse>();
            await foreach (MenuItemEntity entity in table.QueryAsync<MenuItemEntity>())
            {
                items.Add(MenuItemMapper.ToResponse(entity));
            }
            return Ok(items);
        }

        /// <summary>
        /// Gets the menu items of one category, for example "Hot Drinks". No login needed.
        /// </summary>
        /// <param name="category">The category name (it must match exactly, for example Hot Drinks).</param>
        /// <response code="200">The items in that category (an empty list when there are none).</response>
        [HttpGet("category/{category}")]
        [AllowAnonymous]
        [ProducesResponseType<List<MenuItemResponse>>(StatusCodes.Status200OK)]
        public async Task<ActionResult<List<MenuItemResponse>>> GetByCategory(string category)
        {
            TableClient table = await GetTableAsync();
            var items = new List<MenuItemResponse>();
            // The category is the PartitionKey, so this filter runs on the table side and is fast.
            await foreach (MenuItemEntity entity in table.QueryAsync<MenuItemEntity>(x => x.PartitionKey == category))
            {
                items.Add(MenuItemMapper.ToResponse(entity));
            }
            return Ok(items);
        }

        /// <summary>
        /// Creates a new menu item. Admin and Staff only.
        /// </summary>
        /// <param name="request">The new menu item.</param>
        /// <response code="201">The item was created.</response>
        /// <response code="400">One or more fields are missing or not valid.</response>
        /// <response code="401">No token, or the token is not valid.</response>
        /// <response code="403">The token is valid but the role is not Admin or Staff.</response>
        /// <response code="409">An item with this category and SKU already exists.</response>
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        [ProducesResponseType<MenuItemResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationErrorResponse>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<MenuItemResponse>> Create([FromBody] MenuItemRequest request)
        {
            //Request DTO then entity (the mapper trims the text and sets the keys).
            MenuItemEntity entity = MenuItemMapper.ToEntity(request);
            TableClient table = await GetTableAsync();

            try
            {
                //AddEntityAsync fails with 409 when the same PartitionKey and RowKey already exist.
                await table.AddEntityAsync(entity);
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                return Conflict(new ErrorResponse(
                    $"Menu item '{entity.RowKey}' already exists in category '{entity.PartitionKey}'.", HttpContext.TraceIdentifier));
            }

            // 201 Created and a Location header that points to the category list the item is in.
            return CreatedAtAction(nameof(GetByCategory), new { category = entity.PartitionKey }, MenuItemMapper.ToResponse(entity));
        }

        /// <summary>
        /// Changes a menu item. Send only the values you want to change. Admin and Staff only.
        /// </summary>
        /// <param name="category">The category of the item.</param>
        /// <param name="sku">The SKU of the item, for example COF-001.</param>
        /// <param name="request">The values to change.</param>
        /// <response code="200">The item was updated.</response>
        /// <response code="400">A value is not valid, for example a negative price.</response>
        /// <response code="401">No token, or the token is not valid.</response>
        /// <response code="403">The token is valid but the role is not Admin or Staff.</response>
        /// <response code="404">That menu item does not exist.</response>
        /// <response code="409">Someone else changed the item at the same time. Try again.</response>
        [HttpPut("{category}/{sku}")]
        [Authorize(Roles = "Admin,Staff")]
        [ProducesResponseType<MenuItemResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationErrorResponse>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<MenuItemResponse>> Update(string category, string sku, [FromBody] MenuItemUpdateRequest request)
        {
            TableClient table = await GetTableAsync();
            Response<MenuItemEntity> existing;

            try
            {
                // Read the current item first so we only change what was sent.
                existing = await table.GetEntityAsync<MenuItemEntity>(category, sku);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return NotFound(new ErrorResponse($"Menu item '{sku}' in category '{category}' was not found.", HttpContext.TraceIdentifier));
            }

            MenuItemEntity entity = existing.Value;
            MenuItemMapper.ApplyUpdate(entity, request);

            try
            {
                // The ETag makes sure nobody else changed the row while we were working on it.
                await table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);
            }
            catch (RequestFailedException ex) when (ex.Status == 412)
            {
                return Conflict(new ErrorResponse("The menu item was changed by someone else. Please try again.", HttpContext.TraceIdentifier));
            }

            return Ok(MenuItemMapper.ToResponse(entity));
        }

        /// <summary>
        /// Deletes a menu item. Admin and Staff only.
        /// </summary>
        /// <param name="category">The category of the item.</param>
        /// <param name="sku">The SKU of the item.</param>
        /// <response code="204">The item was deleted.</response>
        /// <response code="401">No token, or the token is not valid.</response>
        /// <response code="403">The token is valid but the role is not Admin or Staff.</response>
        /// <response code="404">That menu item does not exist.</response>
        [HttpDelete("{category}/{sku}")]
        [Authorize(Roles = "Admin,Staff")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(string category, string sku)
        {
            TableClient table = await GetTableAsync();

            try
            {
                //Check it exists first, so we can return a proper 404.
                await table.GetEntityAsync<MenuItemEntity>(category, sku);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return NotFound(new ErrorResponse($"Menu item '{sku}' in category '{category}' was not found.", HttpContext.TraceIdentifier));
            }

            await table.DeleteEntityAsync(category, sku);
            return NoContent();
        }
    }
}