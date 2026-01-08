namespace MainLedger.Contracts.Authentication;

/// <summary>
/// Request to refresh access token.
/// </summary>
public record RefreshTokenRequest(string RefreshToken);

/// <summary>
/// Request to verify email.
/// </summary>
public record VerifyEmailRequest(string Token);

/// <summary>
/// Request to reset password.
/// </summary>
public record RequestPasswordResetRequest(string Email);

/// <summary>
/// Request to reset password with token.
/// </summary>
public record ResetPasswordRequest(string Token, string NewPassword);

/// <summary>
/// Request to change password.
/// </summary>
public record ChangePasswordRequest(string OldPassword, string NewPassword);
