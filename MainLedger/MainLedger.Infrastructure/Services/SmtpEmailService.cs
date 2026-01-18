using System.Net;
using System.Net.Mail;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MainLedger.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IEmailNotificationRepository _emailNotificationRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IEmailNotificationRepository emailNotificationRepository,
        IConfiguration configuration,
        ILogger<SmtpEmailService> logger
    )
    {
        _emailNotificationRepository = emailNotificationRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var host = _configuration["EmailSettings:SmtpServer"] ?? "localhost";
            var port = int.Parse(_configuration["EmailSettings:Port"] ?? "1025");
            var senderEmail =
                _configuration["EmailSettings:SenderEmail"] ?? "no-reply@mailledger.com";
            var senderName = _configuration["EmailSettings:SenderName"] ?? "MailLedger";
            var username = _configuration["EmailSettings:Username"];
            var password = _configuration["EmailSettings:Password"];

            using var client = new SmtpClient(host, port);
            client.EnableSsl = port != 1025 && port != 25; // Simple check, adjust as needed

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                client.Credentials = new NetworkCredential(username, password);
            }

            var message = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };
            message.To.Add(to);

            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Email sent successfully to {Recipient}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", to);
            throw; // Re-throw to handle in background job
        }
    }

    public async Task QueueEmailAsync(
        string to,
        EmailType type,
        Dictionary<string, string> placeholders,
        CancellationToken cancellationToken = default
    )
    {
        // Simple template resolution (in a real app, this would be more robust)
        var subject = GetSubjectForType(type);
        var bodyTemplate = GetBodyTemplateForType(type);

        var body = ReplacePlaceholders(bodyTemplate, placeholders);

        var notification = EmailNotification.Create(to, subject, body, type);
        await _emailNotificationRepository.AddAsync(notification, cancellationToken);

        _logger.LogInformation("Queued email {Type} for {Recipient}", type, to);
    }

    private string GetSubjectForType(EmailType type)
    {
        return type switch
        {
            EmailType.UserWelcome => "Welcome to MailLedger!",
            EmailType.PasswordReset => "Reset Your Password",
            EmailType.AccountAlert => "Important Account Alert",
            EmailType.MonthlyReport => "Your Monthly Financial Report",
            _ => "Notification from MailLedger",
        };
    }

    private string GetBodyTemplateForType(EmailType type)
    {
        var templateFileName = type switch
        {
            EmailType.UserWelcome => "WelcomeEmail.html",
            EmailType.PasswordReset => "PasswordResetEmail.html",
            EmailType.AccountAlert => "AccountAlertEmail.html",
            EmailType.MonthlyReport => "MonthlyReportEmail.html",
            _ => null,
        };

        if (templateFileName == null)
        {
            return "<p>{{Message}}</p>";
        }

        var templatePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "EmailTemplates",
            templateFileName
        );

        if (!File.Exists(templatePath))
        {
            _logger.LogWarning("Email template not found: {TemplatePath}", templatePath);
            return "<p>{{Message}}</p>";
        }

        return File.ReadAllText(templatePath);
    }

    private string ReplacePlaceholders(string template, Dictionary<string, string> placeholders)
    {
        foreach (var kvp in placeholders)
        {
            template = template.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
        }
        return template;
    }
}
