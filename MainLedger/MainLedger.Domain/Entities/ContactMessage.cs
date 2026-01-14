namespace MainLedger.Domain.Entities;

public class ContactMessage
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public bool IsRead { get; private set; }

    private ContactMessage() { } // EF Core

    public static ContactMessage Create(
        string firstName,
        string lastName,
        string email,
        string subject,
        string message
    )
    {
        return new ContactMessage
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Subject = subject,
            Message = message,
            CreatedAt = DateTime.UtcNow,
            IsRead = false,
        };
    }

    public void MarkAsRead()
    {
        IsRead = true;
    }
}
