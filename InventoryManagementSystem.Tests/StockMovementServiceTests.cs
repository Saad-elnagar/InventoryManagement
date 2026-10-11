using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Enums;
using InventoryManagementSystem.BLL.Service;
using InventoryManagementSystem.DAL.Entities;
using InventoryManagementSystem.Tests.TestDoubles;
using Moq;

namespace InventoryManagementSystem.Tests;

public class StockMovementServiceTests
{
    [Fact]
    public async Task CreateAsync_Damage_DecreasesStockAndStoresNegativeDelta()
    {
        var uow = new UnitOfWorkMock();

        var product = new Product
        {
            Id = 1,
            Name = "Phone",
            StockQuantity = 10,
            UnitPrice = 9000m
        };

        uow.Repository<Product>()
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(product);

        StockMovement? movement = null;
        uow.Repository<StockMovement>()
            .Setup(r => r.AddAsync(It.IsAny<StockMovement>()))
            .Callback<StockMovement>(m =>
            {
                m.Id = 50;
                movement = m;
            })
            .Returns(Task.CompletedTask);

        var dto = new CreateStockMovementDTO
        {
            ProductId = 1,
            MovementType = StockMovementType.Damage,
            Quantity = 3,
            Reason = "  Broken screen  ",
            Notes = "  Warehouse check  "
        };

        var result = await new StockMovementService(uow.Object).CreateAsync(dto);

        Assert.Equal(7, product.StockQuantity);
        Assert.Equal(-3, result.Quantity);
        Assert.Equal(-3, movement!.Quantity);
        Assert.Equal("Broken screen", result.Reason);
        Assert.Equal("Warehouse check", result.Notes);
        uow.Repository<Product>().Verify(r => r.Update(product), Times.Once);
        uow.VerifyCommitted(Times.Once());
    }

    [Fact]
    public async Task CreateAsync_Found_IncreasesStock()
    {
        var uow = new UnitOfWorkMock();

        var product = new Product
        {
            Id = 2,
            Name = "Keyboard",
            StockQuantity = 4
        };

        uow.Repository<Product>()
            .Setup(r => r.GetByIdAsync(2))
            .ReturnsAsync(product);

        uow.Repository<StockMovement>()
            .Setup(r => r.AddAsync(It.IsAny<StockMovement>()))
            .Callback<StockMovement>(m => m.Id = 60)
            .Returns(Task.CompletedTask);

        var dto = new CreateStockMovementDTO
        {
            ProductId = 2,
            MovementType = StockMovementType.Found,
            Quantity = 5,
            Reason = "Found during audit"
        };

        var result = await new StockMovementService(uow.Object).CreateAsync(dto);

        Assert.Equal(9, product.StockQuantity);
        Assert.Equal(5, result.Quantity);
    }

    [Fact]
    public async Task CreateAsync_AdjustmentWithIncrease_AddsStock()
    {
        var uow = new UnitOfWorkMock();

        var product = new Product
        {
            Id = 3,
            Name = "Mouse",
            StockQuantity = 4
        };

        uow.Repository<Product>()
            .Setup(r => r.GetByIdAsync(3))
            .ReturnsAsync(product);

        uow.Repository<StockMovement>()
            .Setup(r => r.AddAsync(It.IsAny<StockMovement>()))
            .Callback<StockMovement>(m => m.Id = 70)
            .Returns(Task.CompletedTask);

        var dto = new CreateStockMovementDTO
        {
            ProductId = 3,
            MovementType = StockMovementType.Adjustment,
            Direction = StockMovementDirection.Increase,
            Quantity = 2,
            Reason = "Inventory correction"
        };

        var result = await new StockMovementService(uow.Object).CreateAsync(dto);

        Assert.Equal(6, product.StockQuantity);
        Assert.Equal(2, result.Quantity);
    }

    [Fact]
    public async Task CreateAsync_SaleType_IsRejectedAsManualMovement()
    {
        var uow = new UnitOfWorkMock();

        var dto = new CreateStockMovementDTO
        {
            ProductId = 1,
            MovementType = StockMovementType.Sale,
            Quantity = 1,
            Reason = "Should not be manual"
        };

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            new StockMovementService(uow.Object).CreateAsync(dto));

        Assert.Contains("created automatically", ex.Message);
        uow.VerifyTransactionStarted(Times.Never());
    }

    [Fact]
    public async Task CreateAsync_DamageGreaterThanStock_IsRejectedWithoutTransaction()
    {
        var uow = new UnitOfWorkMock();

        uow.Repository<Product>()
            .Setup(r => r.GetByIdAsync(5))
            .ReturnsAsync(new Product
            {
                Id = 5,
                Name = "Camera",
                StockQuantity = 2
            });

        var dto = new CreateStockMovementDTO
        {
            ProductId = 5,
            MovementType = StockMovementType.Damage,
            Quantity = 5,
            Reason = "Damage"
        };

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            new StockMovementService(uow.Object).CreateAsync(dto));

        Assert.Contains("Not enough stock", ex.Message);
        uow.VerifyTransactionStarted(Times.Never());
    }

    [Fact]
    public async Task CreateAsync_MissingReason_IsRejected()
    {
        var uow = new UnitOfWorkMock();

        var dto = new CreateStockMovementDTO
        {
            ProductId = 1,
            MovementType = StockMovementType.Damage,
            Quantity = 1,
            Reason = "   "
        };

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            new StockMovementService(uow.Object).CreateAsync(dto));

        Assert.Equal("Reason is required.", ex.Message);
        uow.VerifyTransactionStarted(Times.Never());
    }
}
