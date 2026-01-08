using MainLedger.Application.Authentication.Services;
using MainLedger.Domain.Entities;
using MainLedger.Domain.Repositories;
using MainLedger.Domain.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Authentication.Commands;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<LoginResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refresh token request");

        // Find all refresh tokens and verify against the provided token
        // Note: In production, you might want to add an index or better lookup mechanism
        var tokenHash = _passwordHasher.HashPassword(request.RefreshToken);
        
        // For now, we'll need to get all tokens and verify (not ideal for production)
        // TODO: Implement a better token lookup mechanism
        var allTokens = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        
        if (allTokens == null || !allTokens.IsValid())
        {
            _logger.LogWarning("Refresh token is invalid or expired");
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        // Get user
        var user = await _userRepository.GetByIdAsync(allTokens.UserId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("User {UserId} not found or inactive", allTokens.UserId);
            throw new UnauthorizedAccessException("User not found or inactive.");
        }

        // Revoke old refresh token
        allTokens.Revoke();

        // Generate new access token
        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email.Value, new[] { "user" });

        // Generate new refresh token
        var newRefreshTokenValue = _tokenGenerator.GenerateRefreshToken();
        var newRefreshTokenHash = _passwordHasher.HashPassword(newRefreshTokenValue);
        var newRefreshToken = RefreshToken.Create(
            user.Id,
            newRefreshTokenHash,
            expiresAt: DateTime.UtcNow.AddDays(7));

        // Save new refresh token
        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Refresh token renewed for user {UserId}", user.Id);

        return new LoginResult(
            user.Id,
            accessToken,
            newRefreshTokenValue,
            ExpiresIn: 900); // 15 minutes in seconds
    }
}
