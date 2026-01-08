using MainLedger.Domain.Repositories;
using MainLedger.Domain.Services;
using MainLedger.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Authentication.Commands;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository tokenRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Password reset attempt");

        // Validate new password
        var password = Password.Create(request.NewPassword);

        // Hash the token to find it
        var tokenHash = _passwordHasher.HashPassword(request.Token);
        var resetToken = await _tokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (resetToken == null || !resetToken.IsValid())
        {
            _logger.LogWarning("Password reset failed: Invalid or expired token");
            return false;
        }

        // Get user
        var user = await _userRepository.GetByIdAsync(resetToken.UserId, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("Password reset failed: User {UserId} not found", resetToken.UserId);
            return false;
        }

        // Hash new password
        var newPasswordHash = _passwordHasher.HashPassword(password.Value);

        // Reset password
        user.ResetPassword(newPasswordHash);

        // Mark token as used
        resetToken.MarkAsUsed();

        // Revoke all refresh tokens for security
        await _refreshTokenRepository.RevokeAllByUserIdAsync(user.Id, cancellationToken);

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset successfully for user {UserId}", user.Id);

        // TODO: Publish PasswordChangedEvent

        return true;
    }
}
