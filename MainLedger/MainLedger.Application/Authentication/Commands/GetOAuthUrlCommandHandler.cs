using MainLedger.Application.Common.Interfaces;
using MediatR;

namespace MainLedger.Application.Authentication.Commands;

public class GetOAuthUrlCommandHandler : IRequestHandler<GetOAuthUrlCommand, string>
{
    private readonly IOAuthService _oauthService;

    public GetOAuthUrlCommandHandler(IOAuthService oauthService)
    {
        _oauthService = oauthService ?? throw new ArgumentNullException(nameof(oauthService));
    }

    public async Task<string> Handle(GetOAuthUrlCommand request, CancellationToken cancellationToken)
    {
        return await _oauthService.GetAuthorizationUrlAsync(request.Provider, request.State);
    }
}
