using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Enums;
using InventoryManagementSystem.DAL.Entities;
using InventoryManagementSystem.BLL.Service;
using InventoryManagementSystem.Tests.TestDoubles;
using Moq;

namespace InventoryManagementSystem.Tests;

public class PurchaseServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidPurchase_IncreasesStockAndCreatesMovement()
    {
        var uow = new UnitOfWorkMock();

        uow.Repository<Supplier>()
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Supplier, bool>>>()))
            .ReturnsAsync(true);

        var product = new Product
        {
            Id = 20,
            Name = "Monitor",
            UnitPrice = 5000m,
            StockQuantity = 4,
            CategoryId = 1
        };

        uow.Repository<Product>()
            .Setup(r => r.GetByIdAsync(20))
            .ReturnsAsync(product);

        uow.Repository<Purchase>()
            .Setup(r => r.AddAsync(It.IsAny<Purchase>()))
            .Callback<Purchase>(p => p.Id = 200)
            .Returns(Task.CompletedTask);

        PurchaseItem? addedItem = null;
        uow.Repository<PurchaseItem>()
            .Setup(r => r.AddAsync(It.IsAny<PurchaseItem>()))
            .Callback<PurchaseItem>(x => addedItem = x)
            .Returns(Task.CompletedTask);

        StockMovement? movement = null;
        uow.Repository<StockMovement>()
            .Setup(r => r.AddAsync(It.IsAny<StockMovement>()))
            .Callback<StockMovement>(x => movement = x)
            .Returns(Task.CompletedTask);

        var dto = new PurchaseDTO
        {
            SupplierId = 9,
            PurchaseDate = DateTime.UtcNow,
            Items =
            {
                new PurchaseItemDTO
                {
                    ProductId = 20,
                    Quantity = 6,
                    UnitPrice = 4500m
                }
            }
        };

        var result = await new PurchaseService(uow.Object).CreateAsync(dto);

        Assert.Equal(200, result.Id);
        Assert.Equal(27000m, result.TotalAmount);
        Assert.Equal(10, product.StockQuantity);

        Assert.NotNull(addedItem);
        Assert.Equal(200, addedItem!.PurchaseId);
        Assert.Equal(6, addedItem.Quantity);
        Assert.Equal(4500m, addedItem.UnitCost);

        Assert.NotNull(movement);
        Assert.Equal(6, movement!.Quantity);
        Assert.Equal(StockMovementType.Purchase.ToString(), movement.MovementType);
        Assert.Equal("Purchase", movement.ReferenceType);
        Assert.Equal(200, movement.ReferenceId);

        uow.Repository<Product>().Verify(r => r.Update(product), Times.Once);
        uow.VerifyTransactionStarted(Times.Once());
        uow.VerifyCommitted(Times.Once());
        uow.VerifyRolledBack(Times.Never());
    }

    [Fact]
    public async Task CreateAsync_InvalidSupplier_RollsBack()
    {
        var uow = new UnitOfWorkMock();

        uow.Repository<Supplier>()
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Supplier, bool>>>()))
            .ReturnsAsync(false);

        var dto = new PurchaseDTO
        {
            SupplierId = 404,
            Items =
            {
                new PurchaseItemDTO
                {
                    ProductId = 1,
                    Quantity = 1,
                    UnitPrice = 100m
                }
            }
        };

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            new PurchaseService(uow.Object).CreateAsync(dto));

        Assert.Equal("Supplier not found.", ex.Message);
        uow.VerifyCommitted(Times.Never());
        uow.VerifyRolledBack(Times.Once());
    }

    [Fact]
    public async Task CreateAsync_EmptyItems_RollsBack()
    {
        var uow = new UnitOfWorkMock();

        uow.Repository<Supplier>()
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Supplier, bool>>>()))
            .ReturnsAsync(true);

        var dto = new PurchaseDTO { SupplierId = 1 };

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            new PurchaseService(uow.Object).CreateAsync(dto));

        Assert.Equal("Purchase must contain at least one item.", ex.Message);
        uow.VerifyRolledBack(Times.Once());
    }
}
