namespace InventoryManagementSystem.BLL.Pagination;

public class PaginationParams
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public void Normalize()
    {
        if (Page < 1)
            Page = 1;

        if (PageSize < 1)
            PageSize = 10;

        if (PageSize > 100)
            PageSize = 100;
    }
}