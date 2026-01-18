using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MainLedger.Domain.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Authentication.Commands;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationTokenRepository _tokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VerifyEmailCommandHandler> _logger;

    public VerifyEmailCommandHandler(
        IUserRepository userRepository,
        IEmailVerificationTokenRepository tokenRepository,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        ILogger<VerifyEmailCommandHandler> logger
    )
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email verification attempt");

        // Hash the token to find it
        var tokenHash = _passwordHasher.HashPassword(request.Token);
        var verificationToken = await _tokenRepository.GetByTokenHashAsync(
            tokenHash,
            cancellationToken
        );

        if (verificationToken == null || !verificationToken.IsValid())
        {
            _logger.LogWarning("Email verification failed: Invalid or expired token");
            return false;
        }

        // Get user
        var user = await _userRepository.GetByIdAsync(verificationToken.UserId, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning(
                "Email verification failed: User {UserId} not found",
                verificationToken.UserId
            );
            return false;
        }

        // Verify email and activate user
        user.VerifyEmail();
        user.Activate();

        // Mark token as used
        verificationToken.MarkAsUsed();

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Email verified and user activated for user {UserId}", user.Id);

        // Send welcome email now that user is verified and active
        await _emailService.QueueEmailAsync(
            user.Email.Value,
            EmailType.UserWelcome,
            new Dictionary<string, string> { { "Name", user.FirstName } },
            cancellationToken
        );

        _logger.LogInformation("Welcome email queued for user {UserId}", user.Id);

        // TODO: Publish EmailVerifiedEvent

        return true;
    }
}
