using MainLedger.Domain.Enums;

namespace MainLedger.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default
    );
    Task QueueEmailAsync(
        string to,
        EmailType type,
        Dictionary<string, string> placeholders,
        CancellationToken cancellationToken = default
    );
}
