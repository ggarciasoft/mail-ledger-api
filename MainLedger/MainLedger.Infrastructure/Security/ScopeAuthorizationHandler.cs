using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace MainLedger.Infrastructure.Security;

/// <summary>
/// Authorization handler that validates scope requirements.
/// Checks if the authenticated user has the required scope claim.
/// </summary>
public class ScopeAuthorizationHandler : AuthorizationHandler<ScopeRequirement>
{
    private readonly ILogger<ScopeAuthorizationHandler> _logger;

    public ScopeAuthorizationHandler(ILogger<ScopeAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ScopeRequirement requirement
    )
    {
        _logger.LogInformation(
            "ScopeAuthorizationHandler invoked for scope: {Scope}",
            requirement.Scope
        );

        // Check if user is authenticated
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("User not authenticated");
            return Task.CompletedTask;
        }

        // Check if this is API key authentication
        var authType = context.User.FindFirst("authType")?.Value;
        _logger.LogInformation("Auth type: {AuthType}", authType ?? "null");

        // Only enforce scopes for API key authentication
        // JWT users (logged in via UI) have full access
        if (authType != "ApiKey")
        {
            _logger.LogInformation("JWT user - granting access without scope check");
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Check if user has the required scope
        var scopes = context.User.FindAll("scope").Select(c => c.Value).ToList();
        _logger.LogInformation("User has scopes: {Scopes}", string.Join(", ", scopes));

        // Check for exact scope match
        if (scopes.Contains(requirement.Scope, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Scope match found - granting access");
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Check for admin:all scope
        if (scopes.Contains("admin:all", StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Admin scope found - granting access");
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Requirement not met
        _logger.LogWarning(
            "Scope requirement not met. Required: {Required}, Has: {Has}",
            requirement.Scope,
            string.Join(", ", scopes)
        );
        return Task.CompletedTask;
    }
}
