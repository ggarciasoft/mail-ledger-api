using MainLedger.Application.Authentication.Services;
using MainLedger.Domain.Entities;
using MainLedger.Domain.Repositories;
using MainLedger.Domain.Services;
using MainLedger.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Authentication.Commands;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for email {Email}", request.Email);

        // Validate email format
        var email = EmailAddress.Create(request.Email);

        // Get user by email
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("Login failed: User not found for email {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: Invalid password for user {UserId}", user.Id);
            throw new UnauthorizedAccessException("Invalid email or password.");
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
            expiresAt: DateTime.UtcNow.AddDays(7));

        // Record login
        user.RecordLogin();

        // Save refresh token and update user
        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} logged in successfully", user.Id);

        // TODO: Publish UserLoggedInEvent with IP and UserAgent

        return new LoginResult(
            user.Id,
            accessToken,
            refreshTokenValue,
            ExpiresIn: 900); // 15 minutes in seconds
    }
}
