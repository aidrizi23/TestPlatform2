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
    private readonly bool _isConfigured;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _logger = logger;
        
        // Load SMTP settings from appsettings.json
        var smtpHost = config["Smtp:Host"];
        var smtpPortString = config["Smtp:Port"];
        var smtpUsername = config["Smtp:Username"];
        var smtpPassword = config["Smtp:Password"];
        _fromEmail = config["Smtp:FromEmail"];

        _logger.LogInformation("Initializing SMTP Email Service...");
        _logger.LogInformation("SMTP Host: {Host}", smtpHost);
        _logger.LogInformation("SMTP Port: {Port}", smtpPortString);
        _logger.LogInformation("SMTP Username: {Username}", smtpUsername);
        _logger.LogInformation("SMTP FromEmail: {FromEmail}", _fromEmail);

        // Validate configuration
        if (string.IsNullOrEmpty(smtpHost) || 
            string.IsNullOrEmpty(smtpPortString) || 
            string.IsNullOrEmpty(smtpUsername) || 
            string.IsNullOrEmpty(smtpPassword) || 
            string.IsNullOrEmpty(_fromEmail))
        {
            _logger.LogError("SMTP configuration is incomplete. Missing values: Host={Host}, Port={Port}, Username={Username}, Password={HasPassword}, FromEmail={FromEmail}", 
                smtpHost, smtpPortString, smtpUsername, !string.IsNullOrEmpty(smtpPassword), _fromEmail);
            _isConfigured = false;
            return;
        }

        if (!int.TryParse(smtpPortString, out int smtpPort))
        {
            _logger.LogError("Invalid SMTP port: {Port}", smtpPortString);
            _isConfigured = false;
            return;
        }

        try
        {
            _logger.LogInformation("Creating SMTP client - Host: {Host}, Port: {Port}, Username: {Username}", 
                smtpHost, smtpPort, smtpUsername);

            _smtpClient = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Timeout = 30000 // 30 seconds timeout
            };
            
            _isConfigured = true;
            _logger.LogInformation("SMTP client configured successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize SMTP client");
            _isConfigured = false;
        }
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        if (!_isConfigured)
        {
            _logger.LogError("SMTP is not properly configured. Cannot send email to {Email}", email);
            throw new InvalidOperationException("SMTP service is not properly configured. Please check your email settings.");
        }

        if (string.IsNullOrEmpty(email))
        {
            _logger.LogError("Cannot send email: recipient email is null or empty");
            throw new ArgumentException("Recipient email cannot be null or empty", nameof(email));
        }

        if (string.IsNullOrEmpty(subject))
        {
            _logger.LogWarning("Email subject is empty for recipient {Email}", email);
        }

        try
        {
            _logger.LogInformation("Attempting to send email to {Email} with subject: '{Subject}'", email, subject);
            _logger.LogDebug("Email body length: {BodyLength} characters", message?.Length ?? 0);

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail, "TestPlatform"),
                Subject = subject ?? "No Subject",
                Body = message ?? "",
                IsBodyHtml = true,
                Priority = MailPriority.Normal
            };

            // Validate email address format
            if (!IsValidEmail(email))
            {
                _logger.LogError("Invalid email address format: {Email}", email);
                throw new ArgumentException($"Invalid email address format: {email}", nameof(email));
            }

            mailMessage.To.Add(email);

            _logger.LogInformation("Sending email via SMTP...");
            await _smtpClient.SendMailAsync(mailMessage);
            
            _logger.LogInformation("Successfully sent email to {Email} with subject: '{Subject}'", email, subject);
        }
        catch (SmtpException smtpEx)
        {
            _logger.LogError(smtpEx, "SMTP error sending email to {Email}. StatusCode: {StatusCode}, Message: {Message}", 
                email, smtpEx.StatusCode, smtpEx.Message);
            
            // Provide more specific error messages based on SMTP status code
            var errorMessage = smtpEx.StatusCode switch
            {
                SmtpStatusCode.MailboxBusy => "Email server is busy. Please try again later.",
                SmtpStatusCode.ClientNotPermitted => "Email client is not permitted to send. Check authentication.",
                SmtpStatusCode.TransactionFailed => "Email transaction failed. Check recipient address.",
                SmtpStatusCode.GeneralFailure => "General email failure. Check SMTP configuration.",
                _ => $"Failed to send email via SMTP: {smtpEx.Message}"
            };
            
            throw new InvalidOperationException(errorMessage, smtpEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email to {Email}. Error type: {ErrorType}, Message: {Message}", 
                email, ex.GetType().Name, ex.Message);
            throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
        }
    }

    public async Task<bool> TestEmailConfigurationAsync()
    {
        if (!_isConfigured)
        {
            _logger.LogWarning("Cannot test email configuration - SMTP is not configured");
            return false;
        }

        try
        {
            _logger.LogInformation("Testing email configuration by sending test email to {FromEmail}", _fromEmail);
            
            var testEmailBody = "<h2>Email Configuration Test</h2>" +
                               "<p>If you receive this email, your SMTP configuration is working correctly!</p>" +
                               $"<p>Test sent at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>";
            
            await SendEmailAsync(_fromEmail, "TestPlatform - Email Configuration Test", testEmailBody);
            
            _logger.LogInformation("Email configuration test successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email configuration test failed");
            return false;
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        try
        {
            _smtpClient?.Dispose();
            _logger.LogInformation("SMTP client disposed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing SMTP client");
        }
    }
}