using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.DAL.Entities;

namespace InventoryManagementSystem.BLL.Mappings;

public static class CategoryMappingExtensions
{
    public static CategoryDTO ToDto(this Category category)
    {
        return new CategoryDTO
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        };
    }
    public static Category ToEntity(this CategoryDTO dto)
    {
        return new Category
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim()
        };
    }

    public static void ApplyTo(this CategoryDTO dto, Category category)
    {
        category.Name = dto.Name.Trim();
        category.Description = dto.Description?.Trim();
    }

}
