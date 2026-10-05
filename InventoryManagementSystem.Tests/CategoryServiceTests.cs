using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Service;
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

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            new CategoryService(uow.Object).CreateAsync(
                new CategoryDTO { Name = "Electronics" }));

        Assert.Equal("Error while creating category.", ex.Message);
        Assert.Equal("Category already exists.", ex.InnerException?.Message);
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

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            new CategoryService(uow.Object).DeleteAsync(5));

        Assert.Contains("contains products", ex.InnerException?.Message);
        uow.Repository<Category>().Verify(r => r.Delete(It.IsAny<Category>()), Times.Never);
    }
}
