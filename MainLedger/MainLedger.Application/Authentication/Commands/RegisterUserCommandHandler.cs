using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MainLedger.Domain.Services;
using MainLedger.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Authentication.Commands;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Guid>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationTokenRepository _tokenRepository;
    private readonly IUserSubscriptionRepository _userSubscriptionRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IEmailVerificationTokenRepository tokenRepository,
        IUserSubscriptionRepository userSubscriptionRepository,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<RegisterUserCommandHandler> logger
    )
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _userSubscriptionRepository = userSubscriptionRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registering new user with email {Email}", request.Email);

        // Validate email format
        var email = EmailAddress.Create(request.Email);

        // Check if user already exists
        var existingUser = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (existingUser != null)
        {
            throw new InvalidOperationException($"User with email {request.Email} already exists.");
        }

        // Validate password strength
        var password = Password.Create(request.Password);

        // Hash password
        var passwordHash = _passwordHasher.HashPassword(password.Value);

        // Create user
        var user = User.Register(email, passwordHash, request.FirstName, request.LastName);

        // Generate email verification token
        var verificationToken = _tokenGenerator.GenerateEmailVerificationToken();
        var tokenHash = _passwordHasher.HashPassword(verificationToken);
        var emailVerificationToken = EmailVerificationToken.Create(user.Id, tokenHash);

        // Save user and token
        await _userRepository.AddAsync(user, cancellationToken);
        await _tokenRepository.AddAsync(emailVerificationToken, cancellationToken);

        // Create Free tier subscription for new user
        var userSubscription = new UserSubscription(user.Id, SubscriptionPlan.FreePlanId);
        await _userSubscriptionRepository.AddAsync(userSubscription, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User {UserId} registered successfully with email {Email}",
            user.Id,
            request.Email
        );

        // Send email verification email (not welcome email yet)
        var verificationLink =
            $"http://localhost:5173/verify-email?token={verificationToken}&email={Uri.EscapeDataString(request.Email)}";
        await _emailService.QueueEmailAsync(
            request.Email,
            EmailType.EmailVerification,
            new Dictionary<string, string>
            {
                { "Name", request.FirstName },
                { "VerificationLink", verificationLink },
            },
            cancellationToken
        );

        _logger.LogInformation("Verification email queued for user {UserId}", user.Id);

        // TODO: Publish UserRegisteredEvent

        return user.Id;
    }
}
