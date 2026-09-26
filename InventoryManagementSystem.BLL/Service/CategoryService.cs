using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.DAL.Entities;
using InventoryManagementSystem.DAL.Repository;

namespace InventoryManagementSystem.BLL.Service;

public class CategoryService : ICategoryService
{
    private readonly IGenaricRepository<Category> _categoryRepository;

    public CategoryService(
        IGenaricRepository<Category> categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IEnumerable<CategoryDTO>> GetAllAsync()
    {
        try
        {
            var categories = await _categoryRepository.GetAllAsync();

            return categories.Select(c => new CategoryDTO
            {
                Id = c.ID,
                Name = c.CategoryName,
                Description = c.Description
            });
        }
        catch (Exception ex)
        {
            throw new Exception("Error while getting categories.", ex);
        }
    }

    public async Task<CategoryDTO?> GetByIdAsync(int id)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id);

            if (category == null)
                return null;

            return new CategoryDTO
            {
                Id = category.ID,
                Name = category.CategoryName,
                Description = category.Description
            };
        }
        catch (Exception ex)
        {
            throw new Exception("Error while getting category.", ex);
        }
    }

    public async Task<CategoryDTO> CreateAsync(CategoryDTO dto)
    {
        try
        {
            var exists = await _categoryRepository.AnyAsync(
                c => c.CategoryName == dto.Name);

            if (exists)
                throw new Exception("Category already exists.");

            var category = new Category
            {
                CategoryName = dto.Name,
                Description = dto.Description
            };

            await _categoryRepository.AddAsync(category);

            dto.Id = category.ID;

            return dto;
        }
        catch (Exception ex)
        {
            throw new Exception("Error while creating category.", ex);
        }
    }

    public async Task<bool> UpdateAsync(int id, CategoryDTO dto)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id);

            if (category == null)
                return false;

            var exists = await _categoryRepository.AnyAsync(
                c => c.ID != id &&
                     c.CategoryName == dto.Name);

            if (exists)
                throw new Exception("Category already exists.");

            category.CategoryName = dto.Name;
            category.Description = dto.Description;

            _categoryRepository.Update(category);

            return true;
        }
        catch (Exception ex)
        {
            throw new Exception("Error while updating category.", ex);
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id);

            if (category == null)
                return false;

            _categoryRepository.Delete(category);

            return true;
        }
        catch (Exception ex)
        {
            throw new Exception("Error while deleting category.", ex);
        }
    }
}