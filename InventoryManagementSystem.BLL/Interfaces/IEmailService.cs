namespace InventoryManagementSystem.BLL.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}