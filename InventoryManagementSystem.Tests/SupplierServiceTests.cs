using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Service;
using InventoryManagementSystem.DAL.Entities;
using InventoryManagementSystem.Tests.TestDoubles;
using Moq;

namespace InventoryManagementSystem.Tests;

public class SupplierServiceTests
{
    [Fact]
    public async Task CreateAsync_DuplicateSupplier_IsRejected()
    {
        var uow = new UnitOfWorkMock();

        uow.Repository<Supplier>()
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Supplier, bool>>>()))
            .ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            new SupplierService(uow.Object).CreateAsync(
                new SupplierDTO
                {
                    SupplierName = "Acme"
                }));

        Assert.Equal("Supplier already exists.", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_SupplierWithPurchases_IsRejected()
    {
        var uow = new UnitOfWorkMock();

        uow.Repository<Supplier>()
            .Setup(r => r.GetByIdAsync(4))
            .ReturnsAsync(new Supplier { Id = 4, SupplierName = "Acme" });

        uow.Repository<Purchase>()
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Purchase, bool>>>()))
            .ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            new SupplierService(uow.Object).DeleteAsync(4));

        Assert.Contains("purchase history", ex.Message);
        uow.Repository<Supplier>().Verify(r => r.Delete(It.IsAny<Supplier>()), Times.Never);
    }
}
