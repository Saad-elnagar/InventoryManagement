using System.Net;
using System.Net.Mail;
using InventoryManagementSystem.BLL.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InventoryManagementSystem.BLL.Service;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IConfiguration configuration,
        ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var host = _configuration["Smtp:Host"];
        var portText = _configuration["Smtp:Port"];
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        var fromAddress = _configuration["Smtp:FromAddress"] ?? username;

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(portText) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(fromAddress))
        {
            throw new InvalidOperationException(
                "Smtp:Host, Smtp:Port, Smtp:Username, Smtp:Password and " +
                "Smtp:FromAddress must all be configured.");
        }

        var port = int.Parse(portText);

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = true
        };

        using var message = new MailMessage(fromAddress, to, subject, body)
        {
            IsBodyHtml = false
        };

        try
        {
            await client.SendMailAsync(message);

            _logger.LogInformation(
                "Email sent to {Recipient} via {Host} with subject '{Subject}'.",
                to, host, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient} via {Host}.", to, host);
            throw;
        }
    }
}