using MainLedger.Domain.Common;
using MainLedger.Domain.ValueObjects;
using System.Text.RegularExpressions;

namespace MainLedger.Domain.Entities;

/// <summary>
/// Represents a user-defined rule for filtering and routing emails.
/// </summary>
public sealed class Rule : Entity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; }
    public string? SenderPattern { get; private set; }
    public string? SubjectPattern { get; private set; }
    public string? KeywordPattern { get; private set; }
    public string? LabelPattern { get; private set; }
    public bool IsActive { get; private set; }
    public int Priority { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Rule(
        Guid id,
        Guid userId,
        string name,
        string? senderPattern,
        string? subjectPattern,
        string? keywordPattern,
        string? labelPattern,
        int priority,
        DateTime createdAt) : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Rule name cannot be empty.", nameof(name));

        if (priority < 0)
            throw new ArgumentException("Priority cannot be negative.", nameof(priority));

        UserId = userId;
        Name = name;
        SenderPattern = senderPattern;
        SubjectPattern = subjectPattern;
        KeywordPattern = keywordPattern;
        LabelPattern = labelPattern;
        Priority = priority;
        IsActive = true;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Creates a new rule.
    /// </summary>
    public static Rule Create(
        Guid userId,
        string name,
        string? senderPattern = null,
        string? subjectPattern = null,
        string? keywordPattern = null,
        string? labelPattern = null,
        int priority = 0)
    {
        return new Rule(
            Guid.NewGuid(),
            userId,
            name,
            senderPattern,
            subjectPattern,
            keywordPattern,
            labelPattern,
            priority,
            DateTime.UtcNow);
    }

    /// <summary>
    /// Evaluates if this rule matches the given email.
    /// </summary>
    public bool Matches(EmailMessage email)
    {
        if (!IsActive) return false;

        bool senderMatch = string.IsNullOrWhiteSpace(SenderPattern) ||
            Regex.IsMatch(email.From.Value, SenderPattern, RegexOptions.IgnoreCase);

        bool subjectMatch = string.IsNullOrWhiteSpace(SubjectPattern) ||
            Regex.IsMatch(email.Subject, SubjectPattern, RegexOptions.IgnoreCase);

        bool keywordMatch = string.IsNullOrWhiteSpace(KeywordPattern) ||
            Regex.IsMatch(email.BodyText, KeywordPattern, RegexOptions.IgnoreCase);

        return senderMatch && subjectMatch && keywordMatch;
    }

    /// <summary>
    /// Activates the rule.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the rule.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the rule patterns.
    /// </summary>
    public void UpdatePatterns(
        string? senderPattern = null,
        string? subjectPattern = null,
        string? keywordPattern = null,
        string? labelPattern = null)
    {
        SenderPattern = senderPattern;
        SubjectPattern = subjectPattern;
        KeywordPattern = keywordPattern;
        LabelPattern = labelPattern;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the rule name.
    /// </summary>
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Rule name cannot be empty.", nameof(name));

        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the rule priority.
    /// </summary>
    public void UpdatePriority(int priority)
    {
        if (priority < 0)
            throw new ArgumentException("Priority cannot be negative.", nameof(priority));

        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    // For EF Core
    private Rule() : base() { }
}
