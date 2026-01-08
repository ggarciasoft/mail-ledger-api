using MainLedger.Application.Authentication.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace MainLedger.Infrastructure.Security;

/// <summary>
/// Current user service implementation using HttpContext.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("userId") 
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }

        return null;
    }

    public string? GetEmail()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("email")?.Value;
    }

    public bool HasScope(string scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
            return false;

        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            return false;

        // Check for admin:all scope
        if (user.HasClaim("scope", "admin:all"))
            return true;

        // Check for specific scope
        return user.HasClaim("scope", scope);
    }

    public bool IsAuthenticated()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    }
}
