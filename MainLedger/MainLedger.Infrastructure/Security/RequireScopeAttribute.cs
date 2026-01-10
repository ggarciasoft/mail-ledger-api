using Microsoft.AspNetCore.Authorization;

namespace MainLedger.Infrastructure.Security;

/// <summary>
/// Authorization attribute that requires a specific scope.
/// Only enforced for API key authentication, not JWT.
/// </summary>
public class RequireScopeAttribute : AuthorizeAttribute
{
    public RequireScopeAttribute(string scope)
    {
        Policy = $"RequireScope:{scope}";
    }
}
