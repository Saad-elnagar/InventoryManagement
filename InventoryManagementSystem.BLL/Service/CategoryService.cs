using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.BLL.Mappings;
using InventoryManagementSystem.DAL.Entities;
using InventoryManagementSystem.BLL.Pagination;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace InventoryManagementSystem.BLL.Service;

public class CategoryService : ICategoryService
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
                .Select(c => c.ToDto());
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

            return category.ToDto();
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

            return category.ToDto();
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

    public async Task<PaginationResult<CategoryDTO>> SearchAsync(
        string? search,
        int page = 1,
        int pageSize = 10)
    {
        var parameters = new PaginationParams
        {
            Page = page,
            PageSize = pageSize
        };
        parameters.Normalize();

        Expression<Func<Category, bool>>? predicate = null;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var pattern = $"%{term}%";

            predicate = category =>
                EF.Functions.Like(
                    EF.Functions.Collate(
                        category.Name,
                        "SQL_Latin1_General_CP1_CI_AS"),
                    pattern)
                ||
                (category.Description != null &&
                 EF.Functions.Like(
                     EF.Functions.Collate(
                         category.Description,
                         "SQL_Latin1_General_CP1_CI_AS"),
                     pattern));
        }

        var result = await _unitOfWork
            .GenaricRepository<Category>()
            .GetPagedAsync(
                parameters.Page,
                parameters.PageSize,
                predicate,
                category => category.Name);

        var data = result.Items
            .Select(category => category.ToDto())
            .ToList();

        return PaginationHelper.Create(
            data,
            parameters,
            result.TotalCount);
    }

}