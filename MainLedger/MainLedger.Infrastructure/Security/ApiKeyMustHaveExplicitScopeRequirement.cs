using Microsoft.AspNetCore.Authorization;

namespace MainLedger.Infrastructure.Security;

/// <summary>
/// Authorization requirement that blocks API key access to endpoints without explicit scope requirements.
/// JWT users are allowed full access.
/// </summary>
public class ApiKeyMustHaveExplicitScopeRequirement : IAuthorizationRequirement { }
