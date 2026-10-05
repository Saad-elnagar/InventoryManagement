using InventoryManagementSystem.DAL.Entities;
using InventoryManagementSystem.DAL.Repository;
using Moq;

namespace InventoryManagementSystem.Tests.TestDoubles;

public sealed class UnitOfWorkMock
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Dictionary<Type, object> _repositories = new();

    public IUnitOfWork Object => _unitOfWork.Object;

    public UnitOfWorkMock()
    {
        _unitOfWork
            .Setup(x => x.BeginTransactionAsync())
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.CommitTransactionAsync())
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.RollbackTransactionAsync())
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);
    }

    public Mock<IGenaricRepository<T>> Repository<T>()
        where T : BaseEntity
    {
        if (_repositories.TryGetValue(typeof(T), out var existing))
            return (Mock<IGenaricRepository<T>>)existing;

        var mock = new Mock<IGenaricRepository<T>>();
        _repositories[typeof(T)] = mock;

        _unitOfWork
            .Setup(x => x.GenaricRepository<T>())
            .Returns(mock.Object);

        return mock;
    }

    public void VerifyTransactionStarted(Times times) =>
        _unitOfWork.Verify(x => x.BeginTransactionAsync(), times);

    public void VerifyCommitted(Times times) =>
        _unitOfWork.Verify(x => x.CommitTransactionAsync(), times);

    public void VerifyRolledBack(Times times) =>
        _unitOfWork.Verify(x => x.RollbackTransactionAsync(), times);
}
