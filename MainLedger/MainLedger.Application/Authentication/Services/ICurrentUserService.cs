namespace MainLedger.Application.Authentication.Services;

/// <summary>
/// Service for accessing the current authenticated user's information.
/// Implementation should be provided by Infrastructure layer.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's ID.
    /// </summary>
    /// <returns>User ID if authenticated, null otherwise</returns>
    Guid? GetUserId();

    /// <summary>
    /// Gets the current user's email.
    /// </summary>
    /// <returns>User email if authenticated, null otherwise</returns>
    string? GetEmail();

    /// <summary>
    /// Checks if the current user has a specific scope.
    /// </summary>
    /// <param name="scope">Scope to check</param>
    /// <returns>True if user has the scope, false otherwise</returns>
    bool HasScope(string scope);

    /// <summary>
    /// Checks if the current user is authenticated.
    /// </summary>
    /// <returns>True if authenticated, false otherwise</returns>
    bool IsAuthenticated();
}
