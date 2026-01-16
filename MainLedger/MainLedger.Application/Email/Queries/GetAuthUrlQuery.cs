using MainLedger.Application.Authentication.Services;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Contracts.Email;
using MainLedger.Domain.Enums;
using MainLedger.Shared;
using MediatR;

namespace MainLedger.Application.Email.Queries;

public record GetAuthUrlQuery(EmailProvider Provider) : IRequest<Result<GetAuthUrlResponse>>;

public class GetAuthUrlQueryHandler : IRequestHandler<GetAuthUrlQuery, Result<GetAuthUrlResponse>>
{
    private readonly IEmailProviderFactory _providerFactory;
    private readonly ICurrentUserService _currentUserService;

    public GetAuthUrlQueryHandler(
        IEmailProviderFactory providerFactory,
        Authentication.Services.ICurrentUserService currentUserService
    )
    {
        _providerFactory = providerFactory;
        _currentUserService = currentUserService;
    }

    public async Task<Result<GetAuthUrlResponse>> Handle(
        GetAuthUrlQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var provider = _providerFactory.GetProvider(request.Provider);
            var userId = _currentUserService.GetUserId() ?? throw new InvalidOperationException("UserId cannot be null.");

            var result = await provider.GetAuthorizationUrlAsync(userId);

            return Result<GetAuthUrlResponse>.Success(
                new GetAuthUrlResponse
                {
                    AuthorizationUrl = result.AuthorizationUrl,
                    State = result.State,
                }
            );
        }
        catch (Exception ex)
        {
            return Result<GetAuthUrlResponse>.Failure(
                $"Failed to get authorization URL: {ex.Message}"
            );
        }
    }
}
