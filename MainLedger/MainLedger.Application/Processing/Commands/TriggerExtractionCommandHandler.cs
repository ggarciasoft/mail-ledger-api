using MainLedger.Application.Emails.Commands;
using MainLedger.Contracts.Processing;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Processing.Commands;

public class TriggerExtractionCommandHandler : IRequestHandler<TriggerExtractionCommand, TriggerJobResponseDto>
{
    private readonly IMediator _mediator;
    private readonly ILogger<TriggerExtractionCommandHandler> _logger;

    public TriggerExtractionCommandHandler(
        IMediator mediator,
        ILogger<TriggerExtractionCommandHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<TriggerJobResponseDto> Handle(TriggerExtractionCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Triggering batch extraction for user {UserId} with batch size {BatchSize}",
            request.UserId, request.BatchSize);

        try
        {
            // Trigger the existing batch extraction command
            var batchCommand = new BatchExtractFinancialDataCommand(request.UserId, request.BatchSize);
            var result = await _mediator.Send(batchCommand, cancellationToken);

            return new TriggerJobResponseDto
            {
                Success = true,
                Message = $"Extraction completed: {result.EmailsExtracted} succeeded, {result.EmailsFailed} failed",
                ProcessedCount = result.EmailsProcessed,
                SucceededCount = result.EmailsExtracted,
                FailedCount = result.EmailsFailed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering extraction for user {UserId}", request.UserId);
            return new TriggerJobResponseDto
            {
                Success = false,
                Message = $"Extraction failed: {ex.Message}",
                ProcessedCount = 0,
                SucceededCount = 0,
                FailedCount = 0
            };
        }
    }
}
