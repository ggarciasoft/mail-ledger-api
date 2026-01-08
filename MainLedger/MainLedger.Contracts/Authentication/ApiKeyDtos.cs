namespace MainLedger.Contracts.Authentication;

/// <summary>
/// Request to create an API key.
/// </summary>
public record CreateApiKeyRequest(
    string Name,
    string[] Scopes,
    DateTime? ExpiresAt = null);

/// <summary>
/// Response after creating an API key.
/// IMPORTANT: The API key is only shown once!
/// </summary>
public record CreateApiKeyResponse(
    Guid Id,
    string ApiKey,
    string Name,
    string[] Scopes,
    DateTime? ExpiresAt,
    string Message);

/// <summary>
/// DTO for API key information (masked).
/// </summary>
public record ApiKeyDto(
    Guid Id,
    string Name,
    string MaskedKey,
    string[] Scopes,
    bool IsActive,
    DateTime? ExpiresAt,
    DateTime? LastUsedAt,
    DateTime CreatedAt);
