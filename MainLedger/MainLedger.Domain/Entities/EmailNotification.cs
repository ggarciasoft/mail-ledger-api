using MainLedger.Domain.Enums;

namespace MainLedger.Domain.Entities;

public class EmailNotification
{
    public Guid Id { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public EmailType Type { get; set; }
    public EmailStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int RetryCount { get; set; }

    public EmailNotification()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        Status = EmailStatus.Pending;
        RetryCount = 0;
    }

    public static EmailNotification Create(
        string recipient,
        string subject,
        string body,
        EmailType type
    )
    {
        return new EmailNotification
        {
            Recipient = recipient,
            Subject = subject,
            Body = body,
            Type = type,
        };
    }

    public void MarkAsSent()
    {
        Status = EmailStatus.Sent;
        SentAt = DateTime.UtcNow;
        ErrorMessage = null;
    }

    public void MarkAsFailed(string error)
    {
        Status = EmailStatus.Failed;
        ErrorMessage = error;
        RetryCount++;
    }
}
