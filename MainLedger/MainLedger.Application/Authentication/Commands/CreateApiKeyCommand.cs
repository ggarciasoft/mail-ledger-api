using MediatR;

namespace MainLedger.Application.Authentication.Commands;

/// <summary>
/// Command to create a new API key for a user.
/// </summary>
public record CreateApiKeyCommand(
    Guid UserId,
    string Name,
    string[] Scopes,
    DateTime? ExpiresAt = null) : IRequest<CreateApiKeyResult>;

/// <summary>
/// Result of creating an API key.
/// IMPORTANT: The plain API key is only returned once and should be shown to the user.
/// </summary>
public record CreateApiKeyResult(
    Guid ApiKeyId,
    string ApiKey, // Plain text - only shown once!
    string Name,
    string[] Scopes,
    DateTime? ExpiresAt);
