using MainLedger.Application.Authentication.Services;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Entities;
using MainLedger.Domain.Repositories;
using MainLedger.Domain.Services;
using MainLedger.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Authentication.Commands;

public class GoogleOAuthCommandHandler : IRequestHandler<GoogleOAuthCommand, LoginResult>
{
    private readonly IOAuthService _oauthService;
    private readonly IUserRepository _userRepository;
    private readonly IExternalLoginRepository _externalLoginRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GoogleOAuthCommandHandler> _logger;

    public GoogleOAuthCommandHandler(
        IOAuthService oauthService,
        IUserRepository userRepository,
        IExternalLoginRepository externalLoginRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork,
        ILogger<GoogleOAuthCommandHandler> logger
    )
    {
        _oauthService = oauthService;
        _userRepository = userRepository;
        _externalLoginRepository = externalLoginRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<LoginResult> Handle(GoogleOAuthCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Google OAuth authentication attempt");

        // Exchange code for user info
        var userInfo = await _oauthService.ExchangeCodeForUserInfoAsync("google", request.Code, cancellationToken);

        // Check if external login already exists
        var externalLogin = await _externalLoginRepository.GetByProviderAndUserIdAsync(
            "Google",
            userInfo.ProviderId,
            cancellationToken
        );

        User user;

        if (externalLogin != null)
        {
            // Existing SSO user - get the linked user
            user = await _userRepository.GetByIdAsync(externalLogin.UserId, cancellationToken)
                ?? throw new InvalidOperationException("User not found for external login");

            // Update last used timestamp
            externalLogin.RecordUsage();
            await _externalLoginRepository.UpdateAsync(externalLogin, cancellationToken);

            _logger.LogInformation("Existing Google SSO user {UserId} logged in", user.Id);
        }
        else
        {
            // Check if user with this email already exists
            var email = EmailAddress.Create(userInfo.Email);
            var existingUser = await _userRepository.GetByEmailAsync(email, cancellationToken);

            if (existingUser != null)
            {
                // Link Google account to existing user
                user = existingUser;
                externalLogin = ExternalLogin.Create(
                    user.Id,
                    "Google",
                    userInfo.ProviderId,
                    userInfo.Email
                );
                await _externalLoginRepository.AddAsync(externalLogin, cancellationToken);

                _logger.LogInformation("Linked Google account to existing user {UserId}", user.Id);
            }
            else
            {
                // Create new user with Google SSO
                user = User.RegisterWithSSO(
                    email,
                    userInfo.FirstName,
                    userInfo.LastName
                );

                await _userRepository.AddAsync(user, cancellationToken);

                // Create external login link
                externalLogin = ExternalLogin.Create(
                    user.Id,
                    "Google",
                    userInfo.ProviderId,
                    userInfo.Email
                );
                await _externalLoginRepository.AddAsync(externalLogin, cancellationToken);

                _logger.LogInformation("Created new user {UserId} via Google SSO", user.Id);
            }
        }

        // Check if user is active
        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: User {UserId} is not active", user.Id);
            throw new UnauthorizedAccessException("Account is deactivated.");
        }

        // Generate access token (JWT)
        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email.Value, new[] { "user" });

        // Generate refresh token
        var refreshTokenValue = _tokenGenerator.GenerateRefreshToken();
        var refreshTokenHash = _passwordHasher.HashPassword(refreshTokenValue);
        var refreshToken = RefreshToken.Create(
            user.Id,
            refreshTokenHash,
            expiresAt: DateTime.UtcNow.AddDays(7)
        );

        // Record login
        user.RecordLogin();

        // Save changes
        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} authenticated via Google OAuth successfully", user.Id);

        return new LoginResult(
            user.Id,
            accessToken,
            refreshTokenValue,
            ExpiresIn: 900 // 15 minutes in seconds
        );
    }
}
