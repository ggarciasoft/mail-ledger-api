using MainLedger.Domain.Common;

namespace MainLedger.Domain.Entities;

/// <summary>
/// Represents a version of the AI extraction model and prompt.
/// Used for tracking which model/prompt version extracted each financial record.
/// </summary>
public sealed class ExtractionVersion : Entity
{
    public string Version { get; private set; }
    public string ModelName { get; private set; }
    public string PromptHash { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? DeprecatedAt { get; private set; }

    private ExtractionVersion(
        Guid id,
        string version,
        string modelName,
        string promptHash,
        DateTime createdAt) : base(id)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version cannot be empty.", nameof(version));
        if (string.IsNullOrWhiteSpace(modelName))
            throw new ArgumentException("Model name cannot be empty.", nameof(modelName));
        if (string.IsNullOrWhiteSpace(promptHash))
            throw new ArgumentException("Prompt hash cannot be empty.", nameof(promptHash));

        Version = version;
        ModelName = modelName;
        PromptHash = promptHash;
        IsActive = true;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Creates a new extraction version.
    /// </summary>
    public static ExtractionVersion Create(string version, string modelName, string promptHash)
    {
        return new ExtractionVersion(
            Guid.NewGuid(),
            version,
            modelName,
            promptHash,
            DateTime.UtcNow);
    }

    /// <summary>
    /// Deprecates this extraction version.
    /// </summary>
    public void Deprecate()
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Extraction version is already deprecated.");
        }

        IsActive = false;
        DeprecatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reactivates this extraction version.
    /// </summary>
    public void Reactivate()
    {
        if (IsActive)
        {
            throw new InvalidOperationException("Extraction version is already active.");
        }

        IsActive = true;
        DeprecatedAt = null;
    }

    // For EF Core
    private ExtractionVersion() : base() { }
}
