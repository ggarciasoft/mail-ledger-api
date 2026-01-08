using MainLedger.Domain.Entities;
using MainLedger.Domain.Repositories;
using MainLedger.Domain.Services;
using MainLedger.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Authentication.Commands;

public class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RequestPasswordResetCommandHandler> _logger;

    public RequestPasswordResetCommandHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository tokenRepository,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        IUnitOfWork unitOfWork,
        ILogger<RequestPasswordResetCommandHandler> logger)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Password reset requested for email {Email}", request.Email);

        // Always return true for security (don't reveal if email exists)
        try
        {
            var email = EmailAddress.Create(request.Email);
            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

            if (user != null && user.IsActive)
            {
                // Generate password reset token
                var resetToken = _tokenGenerator.GeneratePasswordResetToken();
                var tokenHash = _passwordHasher.HashPassword(resetToken);
                var passwordResetToken = PasswordResetToken.Create(user.Id, tokenHash);

                // Save token
                await _tokenRepository.AddAsync(passwordResetToken, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Password reset token created for user {UserId}", user.Id);

                // TODO: Send password reset email with token
            }
            else
            {
                _logger.LogWarning("Password reset requested for non-existent or inactive user: {Email}", request.Email);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing password reset request for {Email}", request.Email);
        }

        // Always return true to prevent email enumeration
        return true;
    }
}
