namespace InventoryManagementSystem.BLL.Exceptions;

public sealed class BusinessRuleException : Exception
{
    public BusinessRuleException(string message)
        : base(message)
    {
    }
}
