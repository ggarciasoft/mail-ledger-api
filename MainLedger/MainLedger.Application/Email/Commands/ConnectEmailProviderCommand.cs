using MainLedger.Application.Common.Interfaces;
using MainLedger.Contracts.Email;
using MainLedger.Domain.Enums;
using MainLedger.Shared;
using MediatR;

namespace MainLedger.Application.Email.Commands;

public record ConnectEmailProviderCommand(EmailProvider Provider, string Code)
    : IRequest<Result<EmailConnectionDto>>;

public class ConnectEmailProviderCommandHandler
    : IRequestHandler<ConnectEmailProviderCommand, Result<EmailConnectionDto>>
{
    private readonly IEmailProviderFactory _providerFactory;
    private readonly Authentication.Services.ICurrentUserService _currentUserService;

    public ConnectEmailProviderCommandHandler(
        IEmailProviderFactory providerFactory,
        Authentication.Services.ICurrentUserService currentUserService
    )
    {
        _providerFactory = providerFactory;
        _currentUserService = currentUserService;
    }

    public async Task<Result<EmailConnectionDto>> Handle(
        ConnectEmailProviderCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var provider = _providerFactory.GetProvider(request.Provider);
            var userId = _currentUserService.GetUserId() ?? throw new InvalidOperationException("UserId cannot be null.");

            var result = await provider.HandleOAuthCallbackAsync(request.Code, userId);

            if (!result.Success)
            {
                return Result<EmailConnectionDto>.Failure(
                    result.ErrorMessage ?? "Connection failed"
                );
            }

            return Result<EmailConnectionDto>.Success(
                new EmailConnectionDto
                {
                    Provider = request.Provider.ToString(),
                    Email = result.Email,
                    IsConnected = true,
                    ConnectedAt = DateTime.UtcNow,
                }
            );
        }
        catch (Exception ex)
        {
            return Result<EmailConnectionDto>.Failure($"Failed to connect provider: {ex.Message}");
        }
    }
}
