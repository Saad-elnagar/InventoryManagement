using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Exceptions;
using InventoryManagementSystem.BLL.Service;
using InventoryManagementSystem.BLL.Pagination;
using System.Linq.Expressions;
using InventoryManagementSystem.DAL.Entities;
using InventoryManagementSystem.Tests.TestDoubles;
using Moq;

namespace InventoryManagementSystem.Tests;

public class CategoryServiceTests
{
    [Fact]
    public async Task CreateAsync_TrimsValuesAndCreatesCategory()
    {
        var uow = new UnitOfWorkMock();
        var repo = uow.Repository<Category>();

        repo.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>()))
            .ReturnsAsync(false);

        repo.Setup(r => r.AddAsync(It.IsAny<Category>()))
            .Callback<Category>(c => c.Id = 11)
            .Returns(Task.CompletedTask);

        var dto = new CategoryDTO
        {
            Name = "  Electronics  ",
            Description = "  Devices  "
        };

        var result = await new CategoryService(uow.Object).CreateAsync(dto);

        Assert.Equal(11, result.Id);
        Assert.Equal("Electronics", result.Name);
        Assert.Equal("Devices", result.Description);
        repo.Verify(r => r.AddAsync(It.Is<Category>(c =>
            c.Name == "Electronics" && c.Description == "Devices")), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateCategory_Throws()
    {
        var uow = new UnitOfWorkMock();

        uow.Repository<Category>()
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>()))
            .ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            new CategoryService(uow.Object).CreateAsync(
                new CategoryDTO { Name = "Electronics" }));

        Assert.Equal("Category already exists.", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_CategoryWithProducts_IsRejected()
    {
        var uow = new UnitOfWorkMock();

        uow.Repository<Category>()
            .Setup(r => r.GetByIdAsync(5))
            .ReturnsAsync(new Category { Id = 5, Name = "Office" });

        uow.Repository<Product>()
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>()))
            .ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            new CategoryService(uow.Object).DeleteAsync(5));

        Assert.Contains("contains products", ex.Message);
        uow.Repository<Category>().Verify(r => r.Delete(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task SearchAsync_ReturnsFilteredPaginatedCategories()
    {
        var uow = new UnitOfWorkMock();
        var repo = uow.Repository<Category>();

        repo.Setup(r => r.GetPagedAsync(
                1,
                100,
                It.IsAny<Expression<Func<Category, bool>>?>(),
                It.IsAny<Expression<Func<Category, string>>>()))
            .ReturnsAsync((
                (IEnumerable<Category>)new[]
                {
                    new Category
                    {
                        Id = 3,
                        Name = "Electronics",
                        Description = "Devices"
                    }
                },
                12));

        var result = await new CategoryService(uow.Object)
            .SearchAsync("  Electronics  ", page: 0, pageSize: 500);

        Assert.Single(result.Data);
        Assert.Equal("Electronics", result.Data[0].Name);
        Assert.Equal(12, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(100, result.PageSize);
        Assert.Equal(1, result.TotalPages);

        repo.Verify(r => r.GetPagedAsync(
                1,
                100,
                It.Is<Expression<Func<Category, bool>>?>(predicate => predicate != null),
                It.IsAny<Expression<Func<Category, string>>>()),
            Times.Once);
    }

}
