using MainLedger.Application.Common.Interfaces;
using MainLedger.Contracts.Email;
using MainLedger.Domain.Repositories;
using MainLedger.Shared;
using MediatR;

namespace MainLedger.Application.Email.Queries;

public record GetEmailConnectionsQuery() : IRequest<Result<List<EmailConnectionDto>>>;

public class GetEmailConnectionsQueryHandler
    : IRequestHandler<GetEmailConnectionsQuery, Result<List<EmailConnectionDto>>>
{
    private readonly IEmailConnectionRepository _connectionRepository;
    private readonly Authentication.Services.ICurrentUserService _currentUserService;

    public GetEmailConnectionsQueryHandler(
        IEmailConnectionRepository connectionRepository,
        Authentication.Services.ICurrentUserService currentUserService
    )
    {
        _connectionRepository = connectionRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<EmailConnectionDto>>> Handle(
        GetEmailConnectionsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var userId = _currentUserService.GetUserId() ?? throw new InvalidOperationException("UserId cannot be null.");
            var connections = await _connectionRepository.GetByUserIdAsync(userId);

            var dtos = connections
                .Select(c => new EmailConnectionDto
                {
                    Provider = c.Provider.ToString(),
                    Email = c.Email,
                    IsConnected = c.IsActive,
                    LastSyncedAt = c.LastSyncedAt,
                    ConnectedAt = c.ConnectedAt,
                })
                .ToList();

            return Result<List<EmailConnectionDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Result<List<EmailConnectionDto>>.Failure(
                $"Failed to get connections: {ex.Message}"
            );
        }
    }
}
