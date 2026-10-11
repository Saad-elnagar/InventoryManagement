using InventoryManagementSystem.DAL.Entities;

namespace InventoryManagementSystem.BLL.Pagination;

public static class PaginationHelper
{
    public static PaginationResult<T> Create<T>(
        IReadOnlyList<T> data,
        PaginationParams parameters,
        int totalCount)
    {
        parameters.Normalize();

        var totalPages = (int)Math.Ceiling(
            totalCount / (double)parameters.PageSize);

        return new PaginationResult<T>
        {
            Data = data,
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }
}