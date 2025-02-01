using System.Net;
using System.Net.Mail;

namespace TestPlatform2.Services;

public interface IEmailService
{
    Task SendEmailAsync(string email, string subject, string message);
}

// SmtpEmailService.cs
public class SmtpEmailService : IEmailService
{
    private readonly SmtpClient _smtpClient;
    private readonly string _fromEmail;

    public SmtpEmailService(IConfiguration config)
    {
        // Load SMTP settings from appsettings.json
        var smtpHost = config["Smtp:Host"];
        var smtpPort = int.Parse(config["Smtp:Port"]);
        var smtpUsername = config["Smtp:Username"];
        var smtpPassword = config["Smtp:Password"];
        _fromEmail = config["Smtp:FromEmail"];

        _smtpClient = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUsername, smtpPassword),
            EnableSsl = true
        };
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        var mailMessage = new MailMessage
        {
            From = new MailAddress(_fromEmail),
            Subject = subject,
            Body = message,
            IsBodyHtml = true
        };
        mailMessage.To.Add(email);

        await _smtpClient.SendMailAsync(mailMessage);
    }
}