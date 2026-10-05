using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Service;
using InventoryManagementSystem.DAL.Entities;
using InventoryManagementSystem.Tests.TestDoubles;
using Moq;

namespace InventoryManagementSystem.Tests;

public class ProductServiceTests
{
    private readonly UnitOfWorkMock _uow = new();
    private ProductService Sut => new(_uow.Object);

    [Fact]
    public async Task CreateAsync_ValidProduct_CreatesAndReturnsDto()
    {
        var categoryRepo = _uow.Repository<Category>();
        var productRepo = _uow.Repository<Product>();

        categoryRepo
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>()))
            .ReturnsAsync(true);

        productRepo
            .Setup(r => r.AddAsync(It.IsAny<Product>()))
            .Callback<Product>(p => p.Id = 25)
            .Returns(Task.CompletedTask);

        var dto = new ProductDTO
        {
            SKU = "SKU-25",
            Name = "Keyboard",
            Description = "Mechanical keyboard",
            Price = 1200m,
            Quantity = 10,
            ReorderLevel = 3,
            CategoryId = 2
        };

        var result = await Sut.CreateAsync(dto);

        Assert.Equal(25, result.Id);
        Assert.Equal("Keyboard", result.Name);
        Assert.Equal(1200m, result.Price);
        Assert.Equal(10, result.Quantity);

        productRepo.Verify(r => r.AddAsync(It.Is<Product>(p =>
            p.Sku == "SKU-25" &&
            p.Name == "Keyboard" &&
            p.UnitPrice == 1200m &&
            p.StockQuantity == 10 &&
            p.CategoryId == 2)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_MissingCategory_ThrowsWrappedException()
    {
        _uow
            .Repository<Category>()
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>()))
            .ReturnsAsync(false);

        var dto = new ProductDTO
        {
            Name = "Keyboard",
            Price = 1200m,
            Quantity = 10,
            ReorderLevel = 3,
            CategoryId = 99
        };

        var ex = await Assert.ThrowsAsync<Exception>(() => Sut.CreateAsync(dto));

        Assert.Equal("Error while creating product.", ex.Message);
        Assert.Equal("Category not found.", ex.InnerException?.Message);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingProduct_MapsProductAndCategory()
    {
        var productRepo = _uow.Repository<Product>();
        var categoryRepo = _uow.Repository<Category>();

        productRepo
            .Setup(r => r.GetByIdAsync(7))
            .ReturnsAsync(new Product
            {
                Id = 7,
                Sku = "P-7",
                Name = "Mouse",
                Description = "Wireless",
                UnitPrice = 350m,
                StockQuantity = 6,
                LowStockThreshold = 2,
                CategoryId = 4
            });

        categoryRepo
            .Setup(r => r.GetByIdAsync(4))
            .ReturnsAsync(new Category
            {
                Id = 4,
                Name = "Accessories"
            });

        var result = await Sut.GetByIdAsync(7);

        Assert.NotNull(result);
        Assert.Equal(7, result!.Id);
        Assert.Equal("P-7", result.SKU);
        Assert.Equal("Mouse", result.Name);
        Assert.Equal("Accessories", result.CategoryName);
        Assert.Equal(6, result.Quantity);
    }

    [Fact]
    public async Task GetLowStockAsync_MapsLowStockProducts()
    {
        _uow
            .Repository<Product>()
            .Setup(r => r.GetWhereAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>()))
            .ReturnsAsync(new[]
            {
                new Product
                {
                    Id = 1,
                    Name = "Low",
                    UnitPrice = 10,
                    StockQuantity = 2,
                    LowStockThreshold = 3,
                    CategoryId = 1
                }
            });

        var result = (await Sut.GetLowStockAsync()).ToList();

        Assert.Single(result);
        Assert.Equal("Low", result[0].Name);
        Assert.Equal(2, result[0].Quantity);
        Assert.Equal(3, result[0].ReorderLevel);
    }

    [Fact]
    public async Task UpdateAsync_ExistingProduct_UpdatesAndSaves()
    {
        var product = new Product
        {
            Id = 5,
            Name = "Old",
            UnitPrice = 100,
            StockQuantity = 4,
            LowStockThreshold = 2,
            CategoryId = 1
        };

        _uow.Repository<Product>()
            .Setup(r => r.GetByIdAsync(5))
            .ReturnsAsync(product);

        _uow.Repository<Category>()
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>()))
            .ReturnsAsync(true);

        var dto = new ProductDTO
        {
            SKU = "SKU-5",
            Name = "Updated",
            Description = "New description",
            Price = 150m,
            Quantity = 8,
            ReorderLevel = 3,
            CategoryId = 2
        };

        var result = await Sut.UpdateAsync(5, dto);

        Assert.True(result);
        Assert.Equal("Updated", product.Name);
        Assert.Equal("New description", product.Description);
        Assert.Equal(150m, product.UnitPrice);
        Assert.Equal(8, product.StockQuantity);
        Assert.Equal(3, product.LowStockThreshold);
        Assert.Equal(2, product.CategoryId);

        _uow.Repository<Product>().Verify(r => r.Update(product), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ProductWithSaleHistory_Throws()
    {
        _uow.Repository<Product>()
            .Setup(r => r.GetByIdAsync(8))
            .ReturnsAsync(new Product { Id = 8, Name = "Tracked" });

        _uow.Repository<PurchaseItem>()
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PurchaseItem, bool>>>()))
            .ReturnsAsync(false);

        _uow.Repository<SaleItem>()
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SaleItem, bool>>>()))
            .ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<Exception>(() => Sut.DeleteAsync(8));

        Assert.Equal("Error while deleting product.", ex.Message);
        Assert.Contains("sales history", ex.InnerException?.Message);
        _uow.Repository<Product>().Verify(r => r.Delete(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsCorrectMetadata()
    {
        _uow.Repository<Product>()
            .Setup(r => r.GetPagedAsync(2, 3, null))
            .ReturnsAsync((
                (IEnumerable<Product>)new[]
                {
                    new Product { Id = 4, Name = "P4", CategoryId = 1 },
                    new Product { Id = 5, Name = "P5", CategoryId = 1 },
                    new Product { Id = 6, Name = "P6", CategoryId = 1 }
                },
                8));

        var result = await Sut.GetPagedAsync(2, 3);

        Assert.Equal(2, result.Page);
        Assert.Equal(3, result.PageSize);
        Assert.Equal(8, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(3, result.Data.Count);
        Assert.True(result.HasPrevious);
        Assert.True(result.HasNext);
    }
}
