using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace MainLedger.Infrastructure.Security;

/// <summary>
/// Authorization handler that blocks API keys from accessing endpoints without explicit scope requirements.
/// This ensures API keys can only access endpoints that have [RequireScope] attributes.
/// JWT users are not affected and have full access.
/// </summary>
public class ApiKeyMustHaveExplicitScopeHandler
    : AuthorizationHandler<ApiKeyMustHaveExplicitScopeRequirement>
{
    private readonly ILogger<ApiKeyMustHaveExplicitScopeHandler> _logger;

    public ApiKeyMustHaveExplicitScopeHandler(ILogger<ApiKeyMustHaveExplicitScopeHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ApiKeyMustHaveExplicitScopeRequirement requirement
    )
    {
        // Check if user is authenticated
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("User not authenticated");
            return Task.CompletedTask;
        }

        // Check if this is API key authentication
        var authType = context.User.FindFirst("authType")?.Value;

        // JWT users have full access - no restrictions
        if (authType != "ApiKey")
        {
            _logger.LogDebug("JWT user - allowing access");
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // For API keys, check if there's at least one ScopeRequirement in the context
        // If there is, it means the endpoint has [RequireScope] attribute
        var hasScopeRequirement = context.PendingRequirements.Any(r => r is ScopeRequirement);

        if (hasScopeRequirement)
        {
            // Endpoint has explicit scope requirement - allow the ScopeAuthorizationHandler to decide
            _logger.LogDebug(
                "API key accessing endpoint with scope requirement - delegating to ScopeAuthorizationHandler"
            );
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // API key trying to access endpoint without scope requirement - block it
        _logger.LogWarning(
            "API key blocked from accessing endpoint without explicit scope requirement"
        );
        // Don't call context.Succeed() - this will fail the authorization
        return Task.CompletedTask;
    }
}
