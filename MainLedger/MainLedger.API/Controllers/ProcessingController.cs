using MainLedger.Application.Authentication.Services;
using MainLedger.Application.Processing.Commands;
using MainLedger.Application.Processing.Queries;
using MainLedger.Contracts.Processing;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MainLedger.API.Controllers;

/// <summary>
/// Controller for processing management operations.
/// </summary>
[ApiController]
[Route("api/processing")]
[Authorize]
public class ProcessingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ProcessingController> _logger;
    private readonly MainLedger.Application.Common.Interfaces.IJobManagementService _jobManagementService;

    public ProcessingController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        ILogger<ProcessingController> logger,
        MainLedger.Application.Common.Interfaces.IJobManagementService jobManagementService
    )
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
        _jobManagementService = jobManagementService;
    }

    /// <summary>
    /// Get processing status for the current user.
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<ProcessingStatusDto>> GetStatus(
        CancellationToken cancellationToken = default
    )
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            _logger.LogWarning("User ID not found in authentication context");
            return Unauthorized(new { error = "User not authenticated" });
        }

        var query = new GetProcessingStatusQuery(userId.Value);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Trigger batch classification for pending emails.
    /// </summary>
    [HttpPost("classify")]
    public async Task<ActionResult<TriggerJobResponseDto>> TriggerClassification(
        [FromQuery] int batchSize = 20,
        CancellationToken cancellationToken = default
    )
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            _logger.LogWarning("User ID not found in authentication context");
            return Unauthorized(new { error = "User not authenticated" });
        }

        try
        {
            // Create job using service (handles validation and race condition protection)
            var job = await _jobManagementService.CreateJobAsync(
                userId.Value,
                Domain.Enums.JobType.Classification,
                System.Text.Json.JsonSerializer.Serialize(new { batchSize }),
                cancellationToken
            );

            if (job == null)
            {
                return Conflict(
                    new
                    {
                        error = "A classification job is already running. Please wait for it to complete.",
                    }
                );
            }

            // Enqueue Hangfire background job
            var hangfireJobId =
                Hangfire.BackgroundJob.Enqueue<Application.BackgroundJobs.ClassificationBackgroundJob>(
                    x => x.ExecuteAsync(job.Id, userId.Value, batchSize, CancellationToken.None)
                );

            // Update job with Hangfire job ID
            job.SetHangfireJobId(hangfireJobId);
            await _jobManagementService.UpdateJobAsync(job, cancellationToken);

            _logger.LogInformation(
                "Enqueued classification job {JobId} (Hangfire: {HangfireJobId}) for user {UserId}",
                job.Id,
                hangfireJobId,
                userId
            );

            return Ok(new { jobId = job.Id, message = "Classification job enqueued successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering classification for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Trigger batch extraction for classified emails.
    /// </summary>
    [HttpPost("extract")]
    public async Task<ActionResult<TriggerJobResponseDto>> TriggerExtraction(
        [FromQuery] int batchSize = 20,
        CancellationToken cancellationToken = default
    )
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            _logger.LogWarning("User ID not found in authentication context");
            return Unauthorized(new { error = "User not authenticated" });
        }

        try
        {
            // Create job using service (handles validation and race condition protection)
            var job = await _jobManagementService.CreateJobAsync(
                userId.Value,
                Domain.Enums.JobType.Extraction,
                System.Text.Json.JsonSerializer.Serialize(new { batchSize }),
                cancellationToken
            );

            if (job == null)
            {
                return Conflict(
                    new
                    {
                        error = "An extraction job is already running. Please wait for it to complete.",
                    }
                );
            }

            // Enqueue Hangfire background job
            var hangfireJobId =
                Hangfire.BackgroundJob.Enqueue<Application.BackgroundJobs.ExtractionBackgroundJob>(
                    x => x.ExecuteAsync(job.Id, userId.Value, batchSize, CancellationToken.None)
                );

            // Update job with Hangfire job ID
            job.SetHangfireJobId(hangfireJobId);
            await _jobManagementService.UpdateJobAsync(job, cancellationToken);

            _logger.LogInformation(
                "Enqueued extraction job {JobId} (Hangfire: {HangfireJobId}) for user {UserId}",
                job.Id,
                hangfireJobId,
                userId
            );

            return Ok(new { jobId = job.Id, message = "Extraction job enqueued successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering extraction for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }
}
