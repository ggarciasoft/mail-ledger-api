using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Enums;
using MainLedger.Shared;
using MediatR;

namespace MainLedger.Application.Email.Commands;

public record DisconnectEmailProviderCommand(EmailProvider Provider) : IRequest<Result>;

public class DisconnectEmailProviderCommandHandler
    : IRequestHandler<DisconnectEmailProviderCommand, Result>
{
    private readonly IEmailProviderFactory _providerFactory;
    private readonly Authentication.Services.ICurrentUserService _currentUserService;

    public DisconnectEmailProviderCommandHandler(
        IEmailProviderFactory providerFactory,
        Authentication.Services.ICurrentUserService currentUserService
    )
    {
        _providerFactory = providerFactory;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(
        DisconnectEmailProviderCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var provider = _providerFactory.GetProvider(request.Provider);
            var userId = _currentUserService.GetUserId() ?? throw new InvalidOperationException("UserId cannot be null.");

            await provider.DisconnectAsync(userId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to disconnect provider: {ex.Message}");
        }
    }
}
