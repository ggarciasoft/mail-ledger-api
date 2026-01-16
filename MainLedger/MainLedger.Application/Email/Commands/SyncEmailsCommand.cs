using MainLedger.Application.Common.Interfaces;
using MainLedger.Contracts.Email;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Services;
using MainLedger.Shared;
using MediatR;

namespace MainLedger.Application.Email.Commands;

public record SyncEmailsCommand(
    EmailProvider Provider,
    DateTime? SyncFrom = null,
    int? MaxResults = null
) : IRequest<Result<SyncResultDto>>;

public class SyncEmailsCommandHandler : IRequestHandler<SyncEmailsCommand, Result<SyncResultDto>>
{
    private readonly IEmailProviderFactory _providerFactory;
    private readonly Authentication.Services.ICurrentUserService _currentUserService;

    public SyncEmailsCommandHandler(
        IEmailProviderFactory providerFactory,
        Authentication.Services.ICurrentUserService currentUserService
    )
    {
        _providerFactory = providerFactory;
        _currentUserService = currentUserService;
    }

    public async Task<Result<SyncResultDto>> Handle(
        SyncEmailsCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var provider = _providerFactory.GetProvider(request.Provider);
            var userId = _currentUserService.GetUserId() ?? throw new InvalidOperationException("UserId cannot be null.");

            var options = new SyncOptions
            {
                SyncFrom = request.SyncFrom,
                MaxResults = request.MaxResults,
            };

            var result = await provider.SyncEmailsAsync(userId, options);

            return Result<SyncResultDto>.Success(
                new SyncResultDto
                {
                    EmailsSynced = result.EmailsSynced,
                    EmailsSkipped = result.EmailsSkipped,
                    Errors = result.Errors,
                }
            );
        }
        catch (Exception ex)
        {
            return Result<SyncResultDto>.Failure($"Failed to sync emails: {ex.Message}");
        }
    }
}
