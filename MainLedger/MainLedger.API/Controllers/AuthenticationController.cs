using MainLedger.Application.Authentication.Commands;
using MainLedger.Contracts.Authentication;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MainLedger.API.Controllers;

/// <summary>
/// Controller for authentication operations.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthenticationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthenticationController> _logger;

    public AuthenticationController(IMediator mediator, ILogger<AuthenticationController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user.
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new RegisterUserCommand(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName);

            var userId = await _mediator.Send(command, cancellationToken);

            return Ok(new RegisterResponse(
                userId,
                request.Email,
                "Registration successful. Please check your email to verify your account."));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Registration failed for {Email}", request.Email);
            return BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid registration data for {Email}", request.Email);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", request.Email);
            return StatusCode(500, new { error = "An error occurred during registration" });
        }
    }

    /// <summary>
    /// Login with email and password.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var command = new LoginCommand(
                request.Email,
                request.Password,
                ipAddress,
                userAgent);

            var result = await _mediator.Send(command, cancellationToken);

            return Ok(new LoginResponse(
                result.UserId,
                result.AccessToken,
                result.RefreshToken,
                result.ExpiresIn));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Login failed for {Email}", request.Email);
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", request.Email);
            return StatusCode(500, new { error = "An error occurred during login" });
        }
    }

    /// <summary>
    /// Refresh access token using refresh token.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new RefreshTokenCommand(request.RefreshToken);
            var result = await _mediator.Send(command, cancellationToken);

            return Ok(new LoginResponse(
                result.UserId,
                result.AccessToken,
                result.RefreshToken,
                result.ExpiresIn));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Token refresh failed");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { error = "An error occurred during token refresh" });
        }
    }

    /// <summary>
    /// Verify email address.
    /// </summary>
    [HttpPost("verify-email")]
    public async Task<ActionResult> VerifyEmail(
        [FromBody] VerifyEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new VerifyEmailCommand(request.Token);
            var result = await _mediator.Send(command, cancellationToken);

            if (result)
            {
                return Ok(new { message = "Email verified successfully" });
            }

            return BadRequest(new { error = "Invalid or expired verification token" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email verification");
            return StatusCode(500, new { error = "An error occurred during email verification" });
        }
    }

    /// <summary>
    /// Request password reset.
    /// </summary>
    [HttpPost("request-password-reset")]
    public async Task<ActionResult> RequestPasswordReset(
        [FromBody] RequestPasswordResetRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new RequestPasswordResetCommand(request.Email);
            await _mediator.Send(command, cancellationToken);

            // Always return success to prevent email enumeration
            return Ok(new { message = "If the email exists, a password reset link has been sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset request");
            return StatusCode(500, new { error = "An error occurred" });
        }
    }

    /// <summary>
    /// Reset password using reset token.
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new ResetPasswordCommand(request.Token, request.NewPassword);
            var result = await _mediator.Send(command, cancellationToken);

            if (result)
            {
                return Ok(new { message = "Password reset successfully" });
            }

            return BadRequest(new { error = "Invalid or expired reset token" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid password during reset");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset");
            return StatusCode(500, new { error = "An error occurred during password reset" });
        }
    }

    /// <summary>
    /// Change password (requires authentication).
    /// </summary>
    [HttpPost("change-password")]
    // [Authorize] // TODO: Add authorization
    public async Task<ActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Get userId from authenticated user
            var userId = Guid.Empty; // Placeholder

            var command = new ChangePasswordCommand(userId, request.OldPassword, request.NewPassword);
            var result = await _mediator.Send(command, cancellationToken);

            if (result)
            {
                return Ok(new { message = "Password changed successfully" });
            }

            return BadRequest(new { error = "Failed to change password" });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Invalid old password");
            return Unauthorized(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid new password");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password change");
            return StatusCode(500, new { error = "An error occurred during password change" });
        }
    }

    /// <summary>
    /// Get Google OAuth authorization URL.
    /// </summary>
    [HttpGet("google/url")]
    public async Task<ActionResult<OAuthUrlResponse>> GetGoogleAuthUrl(
        [FromQuery] string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var state = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            var command = new GetOAuthUrlCommand("google", state);
            var url = await _mediator.Send(command, cancellationToken);

            return Ok(new OAuthUrlResponse(url, state));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Google OAuth URL");
            return StatusCode(500, new { error = "An error occurred" });
        }
    }

    /// <summary>
    /// Handle Google OAuth callback.
    /// </summary>
    [HttpPost("google/callback")]
    public async Task<ActionResult<LoginResponse>> GoogleCallback(
        [FromBody] OAuthCallbackRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var command = new GoogleOAuthCommand(request.Code, ipAddress, userAgent);
            var result = await _mediator.Send(command, cancellationToken);

            return Ok(new LoginResponse(
                result.UserId,
                result.AccessToken,
                result.RefreshToken,
                result.ExpiresIn));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Google OAuth authentication failed");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google OAuth callback");
            return StatusCode(500, new { error = "An error occurred during authentication" });
        }
    }

    /// <summary>
    /// Get Microsoft OAuth authorization URL.
    /// </summary>
    [HttpGet("microsoft/url")]
    public async Task<ActionResult<OAuthUrlResponse>> GetMicrosoftAuthUrl(
        [FromQuery] string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var state = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            var command = new GetOAuthUrlCommand("microsoft", state);
            var url = await _mediator.Send(command, cancellationToken);

            return Ok(new OAuthUrlResponse(url, state));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Microsoft OAuth URL");
            return StatusCode(500, new { error = "An error occurred" });
        }
    }

    /// <summary>
    /// Handle Microsoft OAuth callback.
    /// </summary>
    [HttpPost("microsoft/callback")]
    public async Task<ActionResult<LoginResponse>> MicrosoftCallback(
        [FromBody] OAuthCallbackRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var command = new MicrosoftOAuthCommand(request.Code, ipAddress, userAgent);
            var result = await _mediator.Send(command, cancellationToken);

            return Ok(new LoginResponse(
                result.UserId,
                result.AccessToken,
                result.RefreshToken,
                result.ExpiresIn));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Microsoft OAuth authentication failed");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Microsoft OAuth callback");
            return StatusCode(500, new { error = "An error occurred during authentication" });
        }
    }
}
