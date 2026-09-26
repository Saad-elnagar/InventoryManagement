using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.BLL.Services;
using InventoryManagementSystem.DAL.Entities;
using InventoryManagementSystem.DTOs;
using Moq;
using Xunit;

namespace InventoryManagementSystem.Tests.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<IProductRepository> _productRepositoryMock;
        private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
        private readonly ProductService _sut; // system under test

        public ProductServiceTests()
        {
            _productRepositoryMock = new Mock<IProductRepository>();
            _categoryRepositoryMock = new Mock<ICategoryRepository>();
            _sut = new ProductService(_productRepositoryMock.Object, _categoryRepositoryMock.Object);
        }

        // ---------- Happy path ----------

        [Fact]
        public async Task CreateProductAsync_ValidInput_ReturnsMappedProductDto()
        {
            // Arrange
            var input = new CreateProductDto
            {
                Name = "  Wireless Mouse  ",
                Price = 25.50m,
                CategoryId = 1
            };

            var category = new Category { ID = 1, CategoryName = "Electronics" };

            _categoryRepositoryMock
                .Setup(r => r.GetByIdAsync(input.CategoryId))
                .ReturnsAsync(category);

            _productRepositoryMock
                .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
                .ReturnsAsync(false);

            Product? addedProduct = null;
            _productRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Product>()))
                .Callback<Product>(p =>
                {
                    p.ID = 100; // simulate DB-generated id
                    addedProduct = p;
                })
                .Returns(Task.CompletedTask);

            _productRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _sut.CreateProductAsync(input);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100, result.Id);
            Assert.Equal("Wireless Mouse", result.Name); // trimmed
            Assert.Equal(25.50m, result.Price);
            Assert.Equal(1, result.CategoryId);

            Assert.NotNull(addedProduct);
            Assert.Equal("Wireless Mouse", addedProduct!.ProductName);
            Assert.Equal(25.50m, addedProduct.UnitPrice);
            Assert.Equal(1, addedProduct.CategoryId);

            _productRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Once);
            _productRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        // ---------- 1. Input validation ----------

        [Fact]
        public async Task CreateProductAsync_NullInput_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.CreateProductAsync(null!));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateProductAsync_InvalidName_ThrowsArgumentException(string? name)
        {
            var input = new CreateProductDto { Name = name, Price = 10m, CategoryId = 1 };

            await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateProductAsync(input));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task CreateProductAsync_PriceNotPositive_ThrowsArgumentException(decimal price)
        {
            var input = new CreateProductDto { Name = "Valid Name", Price = price, CategoryId = 1 };

            await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateProductAsync(input));
        }

        [Fact]
        public async Task CreateProductAsync_InvalidInput_DoesNotHitRepositories()
        {
            var input = new CreateProductDto { Name = "", Price = 10m, CategoryId = 1 };

            await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateProductAsync(input));

            _categoryRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
            _productRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Never);
        }

        // ---------- 2. Category existence ----------

        [Fact]
        public async Task CreateProductAsync_CategoryDoesNotExist_ThrowsKeyNotFoundException()
        {
            var input = new CreateProductDto { Name = "Valid Name", Price = 10m, CategoryId = 99 };

            _categoryRepositoryMock
                .Setup(r => r.GetByIdAsync(input.CategoryId))
                .ReturnsAsync((Category?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.CreateProductAsync(input));

            _productRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Never);
        }

        // ---------- 3. Duplicate name rule ----------

        [Fact]
        public async Task CreateProductAsync_DuplicateNameInSameCategory_ThrowsInvalidOperationException()
        {
            var input = new CreateProductDto { Name = "Wireless Mouse", Price = 10m, CategoryId = 1 };

            _categoryRepositoryMock
                .Setup(r => r.GetByIdAsync(input.CategoryId))
                .ReturnsAsync(new Category { ID = 1, CategoryName = "Electronics" });

            _productRepositoryMock
                .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
                .ReturnsAsync(true);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateProductAsync(input));

            _productRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Never);
            _productRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        // ---------- Edge case: name trimming / case sensitivity is handled upstream ----------

        [Fact]
        public async Task CreateProductAsync_NameWithSurroundingWhitespace_IsTrimmedBeforeSaving()
        {
            var input = new CreateProductDto { Name = "   Keyboard   ", Price = 15m, CategoryId = 2 };

            _categoryRepositoryMock
                .Setup(r => r.GetByIdAsync(input.CategoryId))
                .ReturnsAsync(new Category { ID = 2, CategoryName = "Accessories" });

            _productRepositoryMock
                .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
                .ReturnsAsync(false);

            Product? addedProduct = null;
            _productRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Product>()))
                .Callback<Product>(p => addedProduct = p)
                .Returns(Task.CompletedTask);

            _productRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            var result = await _sut.CreateProductAsync(input);

            Assert.Equal("Keyboard", result.Name);
            Assert.Equal("Keyboard", addedProduct!.ProductName);
        }
    }
}