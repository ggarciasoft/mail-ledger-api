using MainLedger.Application.Emails.Commands;
using MainLedger.Contracts.Processing;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Processing.Commands;

public class TriggerClassificationCommandHandler : IRequestHandler<TriggerClassificationCommand, TriggerJobResponseDto>
{
    private readonly IMediator _mediator;
    private readonly ILogger<TriggerClassificationCommandHandler> _logger;

    public TriggerClassificationCommandHandler(
        IMediator mediator,
        ILogger<TriggerClassificationCommandHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<TriggerJobResponseDto> Handle(TriggerClassificationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Triggering batch classification for user {UserId} with batch size {BatchSize}",
            request.UserId, request.BatchSize);

        try
        {
            // Trigger the existing batch classification command
            var batchCommand = new BatchClassifyEmailsCommand(request.UserId, request.BatchSize);
            var result = await _mediator.Send(batchCommand, cancellationToken);

            return new TriggerJobResponseDto
            {
                Success = true,
                Message = $"Classification completed: {result.EmailsClassified} succeeded, {result.EmailsFailed} failed",
                ProcessedCount = result.EmailsProcessed,
                SucceededCount = result.EmailsClassified,
                FailedCount = result.EmailsFailed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering classification for user {UserId}", request.UserId);
            return new TriggerJobResponseDto
            {
                Success = false,
                Message = $"Classification failed: {ex.Message}",
                ProcessedCount = 0,
                SucceededCount = 0,
                FailedCount = 0
            };
        }
    }
}
