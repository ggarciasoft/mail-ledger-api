using Microsoft.AspNetCore.Authorization;

namespace MainLedger.Infrastructure.Security;

/// <summary>
/// Authorization requirement that checks if the user has a specific scope.
/// </summary>
public class ScopeRequirement : IAuthorizationRequirement
{
    public string Scope { get; }

    public ScopeRequirement(string scope)
    {
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
    }
}
