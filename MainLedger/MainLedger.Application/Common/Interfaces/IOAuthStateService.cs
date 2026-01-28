namespace MainLedger.Application.Common.Interfaces;

/// <summary>
/// Service for encoding/decoding OAuth state parameters with user context
/// </summary>
public interface IOAuthStateService
{
    /// <summary>
    /// Generate a state parameter that includes the user ID
    /// </summary>
    /// <param name="userId">User ID to encode in state</param>
    /// <returns>Encoded state string</returns>
    string GenerateState(Guid userId);

    /// <summary>
    /// Parse a state parameter to extract the user ID
    /// </summary>
    /// <param name="state">Encoded state string</param>
    /// <returns>User ID if valid, null otherwise</returns>
    Guid? ParseUserId(string state);
}
