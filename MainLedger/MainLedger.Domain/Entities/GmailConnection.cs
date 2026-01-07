using MainLedger.Domain.Common;
using MainLedger.Domain.ValueObjects;

namespace MainLedger.Domain.Entities;

/// <summary>
/// Represents a Gmail connection for a user.
/// Stores OAuth credentials and sync state.
/// </summary>
public sealed class GmailConnection : Entity
{
    public Guid UserId { get; private set; }
    public EmailAddress Email { get; private set; }
    public string RefreshTokenHash { get; private set; }
    public DateTime? LastSyncedAt { get; private set; }
    public string? HistoryId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private GmailConnection(
        Guid id,
        Guid userId,
        EmailAddress email,
        string refreshTokenHash,
        DateTime createdAt) : base(id)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenHash))
        {
            throw new ArgumentException("Refresh token hash cannot be empty.", nameof(refreshTokenHash));
        }

        UserId = userId;
        Email = email ?? throw new ArgumentNullException(nameof(email));
        RefreshTokenHash = refreshTokenHash;
        CreatedAt = createdAt;
        IsActive = true;
    }

    /// <summary>
    /// Creates a new Gmail connection.
    /// </summary>
    public static GmailConnection Create(Guid userId, EmailAddress email, string refreshTokenHash)
    {
        return new GmailConnection(
            Guid.NewGuid(),
            userId,
            email,
            refreshTokenHash,
            DateTime.UtcNow);
    }

    /// <summary>
    /// Updates the last sync timestamp and history ID.
    /// </summary>
    public void UpdateLastSync(DateTime syncTime, string historyId)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Cannot sync an inactive Gmail connection.");
        }

        LastSyncedAt = syncTime;
        HistoryId = historyId;
    }

    /// <summary>
    /// Revokes the Gmail connection.
    /// </summary>
    public void Revoke()
    {
        IsActive = false;
    }

    /// <summary>
    /// Reactivates the Gmail connection with a new refresh token.
    /// </summary>
    public void Reactivate(string newRefreshTokenHash)
    {
        if (string.IsNullOrWhiteSpace(newRefreshTokenHash))
        {
            throw new ArgumentException("Refresh token hash cannot be empty.", nameof(newRefreshTokenHash));
        }

        RefreshTokenHash = newRefreshTokenHash;
        IsActive = true;
    }

    // For EF Core
    private GmailConnection() : base() { }
}
