using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Enums;
using InventoryManagementSystem.BLL.Service;
using InventoryManagementSystem.DAL.Entities;
using InventoryManagementSystem.Tests.TestDoubles;
using Moq;

namespace InventoryManagementSystem.Tests;

public class SaleServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidSale_DecreasesStockAndCreatesMovement()
    {
        var uow = new UnitOfWorkMock();

        uow.Repository<Customer>()
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Customer, bool>>>()))
            .ReturnsAsync(true);

        var product = new Product
        {
            Id = 10,
            Name = "Laptop",
            UnitPrice = 1000m,
            StockQuantity = 10,
            CategoryId = 1
        };

        uow.Repository<Product>()
            .Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(product);

        uow.Repository<Sale>()
            .Setup(r => r.AddAsync(It.IsAny<Sale>()))
            .Callback<Sale>(s => s.Id = 100)
            .Returns(Task.CompletedTask);

        SaleItem? addedItem = null;
        uow.Repository<SaleItem>()
            .Setup(r => r.AddAsync(It.IsAny<SaleItem>()))
            .Callback<SaleItem>(x => addedItem = x)
            .Returns(Task.CompletedTask);

        StockMovement? movement = null;
        uow.Repository<StockMovement>()
            .Setup(r => r.AddAsync(It.IsAny<StockMovement>()))
            .Callback<StockMovement>(x => movement = x)
            .Returns(Task.CompletedTask);

        var dto = new SaleDTO
        {
            CustomerId = 3,
            SaleDate = DateTime.UtcNow,
            Items =
            {
                new SaleItemDTO
                {
                    ProductId = 10,
                    Quantity = 2,
                    UnitPrice = 1200m
                }
            }
        };

        var result = await new SaleService(uow.Object).CreateAsync(dto);

        Assert.Equal(100, result.Id);
        Assert.Equal(2400m, result.TotalAmount);
        Assert.Equal(8, product.StockQuantity);

        Assert.NotNull(addedItem);
        Assert.Equal(100, addedItem!.SaleId);
        Assert.Equal(2, addedItem.Quantity);

        Assert.NotNull(movement);
        Assert.Equal(-2, movement!.Quantity);
        Assert.Equal(StockMovementType.Sale.ToString(), movement.MovementType);
        Assert.Equal("Sale", movement.ReferenceType);
        Assert.Equal(100, movement.ReferenceId);

        uow.Repository<Product>().Verify(r => r.Update(product), Times.Once);
        uow.VerifyTransactionStarted(Times.Once());
        uow.VerifyCommitted(Times.Once());
        uow.VerifyRolledBack(Times.Never());
    }

    [Fact]
    public async Task CreateAsync_InsufficientStock_RollsBackAndDoesNotCommit()
    {
        var uow = new UnitOfWorkMock();

        uow.Repository<Customer>()
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Customer, bool>>>()))
            .ReturnsAsync(true);

        uow.Repository<Product>()
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Product
            {
                Id = 1,
                Name = "Keyboard",
                StockQuantity = 1,
                UnitPrice = 50m
            });

        var dto = new SaleDTO
        {
            CustomerId = 1,
            Items =
            {
                new SaleItemDTO
                {
                    ProductId = 1,
                    Quantity = 5,
                    UnitPrice = 50m
                }
            }
        };

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            new SaleService(uow.Object).CreateAsync(dto));

        Assert.Contains("Not enough stock", ex.Message);
        uow.VerifyCommitted(Times.Never());
        uow.VerifyRolledBack(Times.Once());
    }

    [Fact]
    public async Task CreateAsync_InvalidCustomer_RollsBack()
    {
        var uow = new UnitOfWorkMock();

        uow.Repository<Customer>()
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Customer, bool>>>()))
            .ReturnsAsync(false);

        var dto = new SaleDTO
        {
            CustomerId = 404,
            Items =
            {
                new SaleItemDTO
                {
                    ProductId = 1,
                    Quantity = 1,
                    UnitPrice = 100m
                }
            }
        };

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            new SaleService(uow.Object).CreateAsync(dto));

        Assert.Equal("Customer not found.", ex.Message);
        uow.VerifyCommitted(Times.Never());
        uow.VerifyRolledBack(Times.Once());
    }
}
