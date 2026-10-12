using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Exceptions;
using InventoryManagementSystem.BLL.Service;
using InventoryManagementSystem.DAL.Entities;
using InventoryManagementSystem.Tests.TestDoubles;
using Moq;

namespace InventoryManagementSystem.Tests;

public class CustomerServiceTests
{
    [Fact]
    public async Task CreateAsync_MapsAndReturnsGeneratedId()
    {
        var uow = new UnitOfWorkMock();
        var repo = uow.Repository<Customer>();

        repo.Setup(r => r.AddAsync(It.IsAny<Customer>()))
            .Callback<Customer>(c => c.Id = 31)
            .Returns(Task.CompletedTask);

        var dto = new CustomerDTO
        {
            Name = "Ahmed",
            ContactName = "Ahmed Contact",
            Phone = "01000000000",
            Email = "ahmed@example.com",
            Address = "Cairo"
        };

        var id = await new CustomerService(uow.Object).CreateAsync(dto);

        Assert.Equal(31, id);
        repo.Verify(r => r.AddAsync(It.Is<Customer>(c =>
            c.CustomerName == "Ahmed" &&
            c.Phone == "01000000000" &&
            c.Email == "ahmed@example.com")), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_MissingCustomer_ReturnsNull()
    {
        var uow = new UnitOfWorkMock();

        uow.Repository<Customer>()
            .Setup(r => r.GetByIdAsync(404))
            .ReturnsAsync((Customer)null!);

        var result = await new CustomerService(uow.Object).GetByIdAsync(404);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_CustomerWithSalesHistory_IsRejected()
    {
        var uow = new UnitOfWorkMock();

        uow.Repository<Customer>()
            .Setup(r => r.GetByIdAsync(7))
            .ReturnsAsync(new Customer { Id = 7, CustomerName = "Ahmed" });

        uow.Repository<Sale>()
            .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Sale, bool>>>()))
            .ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => new CustomerService(uow.Object).DeleteAsync(7));

        Assert.Equal(
            "Cannot delete customer because it has sales history.",
            ex.Message);

        uow.Repository<Customer>()
            .Verify(r => r.Delete(It.IsAny<Customer>()), Times.Never);
    }
}
