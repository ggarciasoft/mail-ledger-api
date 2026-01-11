using MainLedger.Domain.Common;

namespace MainLedger.Domain.Entities;

/// <summary>
/// Represents a Gmail sync operation history record.
/// Tracks when syncs occur, how many emails were found/processed, and success/failure status.
/// </summary>
public sealed class GmailSyncHistory : Entity
{
    public Guid UserId { get; private set; }
    public DateTime SyncStartedAt { get; private set; }
    public DateTime? SyncCompletedAt { get; private set; }
    public int EmailsFound { get; private set; }
    public int EmailsProcessed { get; private set; }
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? LastHistoryId { get; private set; }

    private GmailSyncHistory(
        Guid id,
        Guid userId,
        DateTime syncStartedAt,
        DateTime? syncCompletedAt,
        int emailsFound,
        int emailsProcessed,
        bool isSuccess,
        string? errorMessage,
        string? lastHistoryId
    )
        : base(id)
    {
        UserId = userId;
        SyncStartedAt = syncStartedAt;
        SyncCompletedAt = syncCompletedAt;
        EmailsFound = emailsFound;
        EmailsProcessed = emailsProcessed;
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        LastHistoryId = lastHistoryId;
    }

    /// <summary>
    /// Creates a new sync history record when a sync operation starts.
    /// </summary>
    public static GmailSyncHistory Create(Guid userId, string? lastHistoryId = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        return new GmailSyncHistory(
            Guid.NewGuid(),
            userId,
            DateTime.UtcNow,
            syncCompletedAt: null,
            emailsFound: 0,
            emailsProcessed: 0,
            isSuccess: false,
            errorMessage: null,
            lastHistoryId
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
    private GmailSyncHistory()
        : base() { }
}
