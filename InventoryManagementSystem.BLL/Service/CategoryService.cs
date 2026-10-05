using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.DAL.Entities;

namespace InventoryManagementSystem.BLL.Service;

public class CategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<CategoryDTO>> GetAllAsync()
    {
        try
        {
            var categories =
                await _unitOfWork
                    .GenaricRepository<Category>()
                    .GetAllAsync();

            return categories
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description
                });
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Error while getting categories.",
                ex);
        }
    }

    public async Task<CategoryDTO?> GetByIdAsync(int id)
    {
        try
        {
            var category =
                await _unitOfWork
                    .GenaricRepository<Category>()
                    .GetByIdAsync(id);

            if (category == null)
                return null;

            return new CategoryDTO
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            };
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Error while getting category.",
                ex);
        }
    }

    public async Task<CategoryDTO> CreateAsync(
        CategoryDTO dto)
    {
        try
        {
            dto.Name = dto.Name.Trim();
            dto.Description = dto.Description?.Trim();

            var exists =
                await _unitOfWork
                    .GenaricRepository<Category>()
                    .AnyAsync(
                        c => c.Name == dto.Name);

            if (exists)
                throw new Exception(
                    "Category already exists.");

            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description
            };

            await _unitOfWork
                .GenaricRepository<Category>()
                .AddAsync(category);

            await _unitOfWork.SaveChangesAsync();

            dto.Id = category.Id;

            return dto;
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Error while creating category.",
                ex);
        }
    }

    public async Task<bool> UpdateAsync(
        int id,
        CategoryDTO dto)
    {
        try
        {
            dto.Name = dto.Name.Trim();

            var category =
                await _unitOfWork
                    .GenaricRepository<Category>()
                    .GetByIdAsync(id);

            if (category == null)
                return false;

            var exists =
                await _unitOfWork
                    .GenaricRepository<Category>()
                    .AnyAsync(
                        c => c.Id != id &&
                             c.Name == dto.Name);

            if (exists)
                throw new Exception(
                    "Category already exists.");

            category.Name = dto.Name;
            category.Description =
                dto.Description?.Trim();

            _unitOfWork
                .GenaricRepository<Category>()
                .Update(category);

            await _unitOfWork.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Error while updating category.",
                ex);
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var category =
                await _unitOfWork
                    .GenaricRepository<Category>()
                    .GetByIdAsync(id);

            if (category == null)
                return false;

            var hasProducts =
                await _unitOfWork
                    .GenaricRepository<Product>()
                    .AnyAsync(
                        p => p.CategoryId == id);

            if (hasProducts)
            {
                throw new Exception(
                    "Cannot delete category because it contains products.");
            }

            _unitOfWork
                .GenaricRepository<Category>()
                .Delete(category);

            await _unitOfWork.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Error while deleting category.",
                ex);
        }
    }
}