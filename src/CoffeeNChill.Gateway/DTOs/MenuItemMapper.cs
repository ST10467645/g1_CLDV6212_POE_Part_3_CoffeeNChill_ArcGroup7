/*
 * Code Attribution:
 * The mapper keeps all conversions between the DTOs and the table entity in one place so the controller stays short.
 * The idea follows the lecturer's mapper examples and my Part 2 MenuItemMapper.
 * 
 * similar to MenuItemMapper in CoffeeNChillFunctions but a few changes were made.
 *
 * Microsoft Learn (2026). Azure Tables client library for .NET - version 12.12.0. (the  ITableEntity example helped me)
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
 * [Accessed 20 October 2026].
 */
using CoffeeNChill.Gateway.Models;

namespace CoffeeNChill.Gateway.DTOs
{
    //Converts between the menu DTOs and the MenuItemEntity.
    public class MenuItemMapper
    {
         //Request DTO then entity. Used when a menu item is created.
        public static MenuItemEntity ToEntity(MenuItemRequest dto) => new MenuItemEntity
        {
            PartitionKey = dto.Category.Trim(),
            RowKey = dto.Sku.Trim(),
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            Price = dto.Price,
            IsAvailable = dto.IsAvailable
        };

        //Entity then response DTO. Used for every reply that returns a menu item.
        public static MenuItemResponse ToResponse(MenuItemEntity entity) => new MenuItemResponse
        {
            Category = entity.PartitionKey,
            Sku = entity.RowKey,
            Name = entity.Name ?? string.Empty,
            Description = entity.Description ?? string.Empty,
            Price = entity.Price,
            IsAvailable = entity.IsAvailable
        };

        //Copies only the values that were sent in an update onto the existing entity. The other values stay as they were.
        public static void ApplyUpdate(MenuItemEntity entity, MenuItemUpdateRequest dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.Name)) entity.Name = dto.Name.Trim();
            if (dto.Description != null) entity.Description = dto.Description.Trim();
            if (dto.Price.HasValue) entity.Price = dto.Price.Value;
            if (dto.IsAvailable.HasValue) entity.IsAvailable = dto.IsAvailable.Value;
        }
    }
}