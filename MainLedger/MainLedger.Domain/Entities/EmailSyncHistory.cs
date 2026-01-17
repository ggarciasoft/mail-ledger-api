using MainLedger.Domain.Common;
using MainLedger.Domain.Enums;

namespace MainLedger.Domain.Entities;

/// <summary>
/// Represents an email sync operation history record for any email provider.
/// Tracks when syncs occur, how many emails were found/processed, and success/failure status.
/// </summary>
public sealed class EmailSyncHistory : Entity
{
    public Guid UserId { get; private set; }
    public EmailProvider Provider { get; private set; }
    public DateTime SyncStartedAt { get; private set; }
    public DateTime? SyncCompletedAt { get; private set; }
    public int EmailsFound { get; private set; }
    public int EmailsProcessed { get; private set; }
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Provider-specific metadata stored as JSON (e.g., Gmail's historyId, Outlook's deltaLink)
    /// </summary>
    public string? ProviderMetadata { get; private set; }

    // Navigation property
    public User User { get; set; } = null!;

    private EmailSyncHistory(
        Guid id,
        Guid userId,
        EmailProvider provider,
        DateTime syncStartedAt,
        DateTime? syncCompletedAt,
        int emailsFound,
        int emailsProcessed,
        bool isSuccess,
        string? errorMessage,
        string? providerMetadata
    )
        : base(id)
    {
        UserId = userId;
        Provider = provider;
        SyncStartedAt = syncStartedAt;
        SyncCompletedAt = syncCompletedAt;
        EmailsFound = emailsFound;
        EmailsProcessed = emailsProcessed;
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        ProviderMetadata = providerMetadata;
    }

    /// <summary>
    /// Creates a new sync history record when a sync operation starts.
    /// </summary>
    public static EmailSyncHistory Create(
        Guid userId,
        EmailProvider provider,
        string? providerMetadata = null
    )
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        return new EmailSyncHistory(
            Guid.NewGuid(),
            userId,
            provider,
            DateTime.UtcNow,
            syncCompletedAt: null,
            emailsFound: 0,
            emailsProcessed: 0,
            isSuccess: false,
            errorMessage: null,
            providerMetadata
        );
    }

    /// <summary>
    /// Marks the sync as completed with the results.
    /// </summary>
    public void Complete(
        int emailsFound,
        int emailsProcessed,
        bool isSuccess,
        string? errorMessage = null
    )
    {
        if (emailsFound < 0)
            throw new ArgumentException("Emails found cannot be negative.", nameof(emailsFound));
        if (emailsProcessed < 0)
            throw new ArgumentException(
                "Emails processed cannot be negative.",
                nameof(emailsProcessed)
            );

        SyncCompletedAt = DateTime.UtcNow;
        EmailsFound = emailsFound;
        EmailsProcessed = emailsProcessed;
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    // For EF Core
    private EmailSyncHistory()
        : base() { }
}
