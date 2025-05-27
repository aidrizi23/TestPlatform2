// Services/EmailService.cs - Enhanced with better error handling and logging
using System.Net;
using System.Net.Mail;

namespace TestPlatform2.Services;

public interface IEmailService
{
    Task SendEmailAsync(string email, string subject, string message);
    Task<bool> TestEmailConfigurationAsync();
}

public class SmtpEmailService : IEmailService
{
    private readonly SmtpClient _smtpClient;
    private readonly string _fromEmail;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _logger = logger;
        
        // Load SMTP settings from appsettings.json
        var smtpHost = config["Smtp:Host"];
        var smtpPortString = config["Smtp:Port"];
        var smtpUsername = config["Smtp:Username"];
        var smtpPassword = config["Smtp:Password"];
        _fromEmail = config["Smtp:FromEmail"];

        // Validate configuration
        if (string.IsNullOrEmpty(smtpHost) || 
            string.IsNullOrEmpty(smtpPortString) || 
            string.IsNullOrEmpty(smtpUsername) || 
            string.IsNullOrEmpty(smtpPassword) || 
            string.IsNullOrEmpty(_fromEmail))
        {
            _logger.LogError("SMTP configuration is incomplete. Please check appsettings.json");
            throw new InvalidOperationException("SMTP configuration is missing required values");
        }

        if (!int.TryParse(smtpPortString, out int smtpPort))
        {
            _logger.LogError("Invalid SMTP port: {Port}", smtpPortString);
            throw new InvalidOperationException($"Invalid SMTP port: {smtpPortString}");
        }

        _logger.LogInformation("Initializing SMTP client - Host: {Host}, Port: {Port}, Username: {Username}", 
            smtpHost, smtpPort, smtpUsername);

        _smtpClient = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUsername, smtpPassword),
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        try
        {
            _logger.LogInformation("Attempting to send email to {Email} with subject: {Subject}", email, subject);

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);

            await _smtpClient.SendMailAsync(mailMessage);
            
            _logger.LogInformation("Successfully sent email to {Email}", email);
        }
        catch (SmtpException smtpEx)
        {
            _logger.LogError(smtpEx, "SMTP error sending email to {Email}. StatusCode: {StatusCode}", 
                email, smtpEx.StatusCode);
            throw new InvalidOperationException($"Failed to send email via SMTP: {smtpEx.Message}", smtpEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email to {Email}", email);
            throw;
        }
    }

    public async Task<bool> TestEmailConfigurationAsync()
    {
        try
        {
            _logger.LogInformation("Testing email configuration by sending test email to {FromEmail}", _fromEmail);
            
            await SendEmailAsync(_fromEmail, "Test Email Configuration", 
                "<h2>Email Configuration Test</h2><p>If you receive this email, your SMTP configuration is working correctly!</p>");
            
            _logger.LogInformation("Email configuration test successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email configuration test failed");
            return false;
        }
    }

    public void Dispose()
    {
        _smtpClient?.Dispose();
    }
}