using MainLedger.Application.Common.Interfaces;

namespace MainLedger.Infrastructure.Services;

/// <summary>
/// Service for encoding/decoding OAuth state parameters with user context
/// State format: {userId}:{randomGuid}
/// </summary>
public class OAuthStateService : IOAuthStateService
{
    public string GenerateState(Guid userId)
    {
        var randomPart = Guid.NewGuid().ToString();
        return $"{userId}:{randomPart}";
    }

    public Guid? ParseUserId(string state)
    {
        if (string.IsNullOrWhiteSpace(state))
            return null;

        var parts = state.Split(':', 2);
        if (parts.Length != 2)
            return null;

        if (Guid.TryParse(parts[0], out var userId))
            return userId;

        return null;
    }
}
