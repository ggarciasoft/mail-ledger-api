using MainLedger.Application.Common.Interfaces;
using MainLedger.Contracts.Email;
using MainLedger.Domain.Enums;
using MainLedger.Shared;
using MediatR;

namespace MainLedger.Application.Email.Commands;

public record ConnectEmailProviderCommand(EmailProvider Provider, string Code, string State)
    : IRequest<Result<EmailConnectionDto>>;

public class ConnectEmailProviderCommandHandler
    : IRequestHandler<ConnectEmailProviderCommand, Result<EmailConnectionDto>>
{
    private readonly IEmailProviderFactory _providerFactory;
    private readonly IOAuthStateService _oauthStateService;

    public ConnectEmailProviderCommandHandler(
        IEmailProviderFactory providerFactory,
        IOAuthStateService oauthStateService
    )
    {
        _providerFactory = providerFactory;
        _oauthStateService = oauthStateService;
    }

    public async Task<Result<EmailConnectionDto>> Handle(
        ConnectEmailProviderCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Extract user ID from state parameter
            var userId = _oauthStateService.ParseUserId(request.State);
            if (userId == null)
            {
                return Result<EmailConnectionDto>.Failure("Invalid OAuth state parameter");
            }

            var provider = _providerFactory.GetProvider(request.Provider);
            var result = await provider.HandleOAuthCallbackAsync(request.Code, request.State, userId.Value);

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
