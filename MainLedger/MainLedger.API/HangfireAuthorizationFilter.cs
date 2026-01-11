using Hangfire.Dashboard;

namespace MainLedger.API;

/// <summary>
/// Authorization filter for Hangfire dashboard in development.
/// In production, this should be replaced with proper authentication.
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // Allow all in development
        // In production, implement proper authorization
        return true;
    }
}
