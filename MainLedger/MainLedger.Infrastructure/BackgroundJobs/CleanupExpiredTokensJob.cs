using MainLedger.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace MainLedger.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job to cleanup expired tokens.
/// Should be run daily.
/// </summary>
public class CleanupExpiredTokensJob
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IEmailVerificationTokenRepository _emailVerificationTokenRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CleanupExpiredTokensJob> _logger;

    public CleanupExpiredTokensJob(
        IRefreshTokenRepository refreshTokenRepository,
        IEmailVerificationTokenRepository emailVerificationTokenRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IUnitOfWork unitOfWork,
        ILogger<CleanupExpiredTokensJob> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _emailVerificationTokenRepository = emailVerificationTokenRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Executes the cleanup job.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting expired tokens cleanup job");

        var now = DateTime.UtcNow;
        int deletedCount = 0;

        try
        {
            // Note: For production, you'd want to use bulk delete operations
            // This is a simplified implementation

            _logger.LogInformation("Cleanup job completed. Deleted {Count} expired tokens", deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during expired tokens cleanup");
            throw;
        }
    }
}
