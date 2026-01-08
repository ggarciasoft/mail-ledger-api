using MediatR;

namespace MainLedger.Application.Authentication.Queries;

/// <summary>
/// Query to list all API keys for a user.
/// </summary>
public record ListApiKeysQuery(Guid UserId) : IRequest<List<ApiKeyDto>>;

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
