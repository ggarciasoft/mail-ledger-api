using MediatR;

namespace MainLedger.Application.Authentication.Commands;

/// <summary>
/// Command to revoke an API key.
/// </summary>
public record RevokeApiKeyCommand(Guid UserId, Guid ApiKeyId) : IRequest<bool>;
