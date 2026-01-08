using MainLedger.Domain.Repositories;
using MainLedger.Domain.Services;
using MainLedger.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Authentication.Commands;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Password change attempt for user {UserId}", request.UserId);

        // Get user
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("Password change failed: User {UserId} not found", request.UserId);
            throw new KeyNotFoundException($"User {request.UserId} not found.");
        }

        // Verify old password
        if (!_passwordHasher.VerifyPassword(request.OldPassword, user.PasswordHash))
        {
            _logger.LogWarning("Password change failed: Invalid old password for user {UserId}", request.UserId);
            throw new UnauthorizedAccessException("Invalid old password.");
        }

        // Validate new password
        var newPassword = Password.Create(request.NewPassword);

        // Hash new password
        var newPasswordHash = _passwordHasher.HashPassword(newPassword.Value);

        // Change password
        user.ChangePassword(newPasswordHash);

        // Revoke all refresh tokens for security
        await _refreshTokenRepository.RevokeAllByUserIdAsync(user.Id, cancellationToken);

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password changed successfully for user {UserId}", user.Id);

        // TODO: Publish PasswordChangedEvent

        return true;
    }
}
