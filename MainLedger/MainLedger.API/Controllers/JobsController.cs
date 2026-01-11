using MainLedger.Application.Authentication.Services;
using MainLedger.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MainLedger.API.Controllers;

/// <summary>
/// Controller for managing background processing jobs.
/// </summary>
[ApiController]
[Route("api/jobs")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly IProcessingJobRepository _jobRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<JobsController> _logger;

    public JobsController(
        IProcessingJobRepository jobRepository,
        ICurrentUserService currentUserService,
        ILogger<JobsController> logger
    )
    {
        _jobRepository = jobRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get all jobs for the current user.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetJobs(
        [FromQuery] Domain.Enums.JobType? jobType = null,
        CancellationToken cancellationToken = default
    )
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        try
        {
            var jobs = await _jobRepository.GetRecentJobsAsync(userId.Value, 50, cancellationToken);

            // Filter by job type if specified
            if (jobType.HasValue)
            {
                jobs = jobs.Where(j => j.JobType == jobType.Value).ToList();
            }

            return Ok(
                jobs.Select(j => new
                {
                    id = j.Id,
                    jobType = j.JobType.ToString(),
                    status = j.Status.ToString(),
                    progress = j.Progress,
                    totalItems = j.TotalItems,
                    processedItems = j.ProcessedItems,
                    successCount = j.SuccessCount,
                    failureCount = j.FailureCount,
                    errorMessage = j.ErrorMessage,
                    startedAt = j.StartedAt,
                    completedAt = j.CompletedAt,
                    createdAt = j.CreatedAt,
                })
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting jobs for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific job by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetJob(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        try
        {
            var job = await _jobRepository.GetByIdAsync(id, cancellationToken);

            if (job == null)
            {
                return NotFound(new { error = "Job not found" });
            }

            // Verify job belongs to current user
            if (job.UserId != userId.Value)
            {
                return Forbid();
            }

            return Ok(
                new
                {
                    id = job.Id,
                    jobType = job.JobType.ToString(),
                    status = job.Status.ToString(),
                    progress = job.Progress,
                    totalItems = job.TotalItems,
                    processedItems = job.ProcessedItems,
                    successCount = job.SuccessCount,
                    failureCount = job.FailureCount,
                    errorMessage = job.ErrorMessage,
                    startedAt = job.StartedAt,
                    completedAt = job.CompletedAt,
                    createdAt = job.CreatedAt,
                    metadata = job.Metadata,
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job {JobId} for user {UserId}", id, userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get active jobs for the current user.
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveJobs(
        [FromQuery] Domain.Enums.JobType? jobType = null,
        CancellationToken cancellationToken = default
    )
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        try
        {
            var jobs = await _jobRepository.GetActiveJobsForUserAsync(
                userId.Value,
                jobType,
                cancellationToken
            );

            return Ok(
                jobs.Select(j => new
                {
                    id = j.Id,
                    jobType = j.JobType.ToString(),
                    status = j.Status.ToString(),
                    progress = j.Progress,
                    totalItems = j.TotalItems,
                    processedItems = j.ProcessedItems,
                    successCount = j.SuccessCount,
                    failureCount = j.FailureCount,
                    startedAt = j.StartedAt,
                    createdAt = j.CreatedAt,
                })
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active jobs for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cancel a pending or running job.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelJob(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var userId = _currentUserService.GetUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        try
        {
            var job = await _jobRepository.GetByIdAsync(id, cancellationToken);

            if (job == null)
            {
                return NotFound(new { error = "Job not found" });
            }

            // Verify job belongs to current user
            if (job.UserId != userId.Value)
            {
                return Forbid();
            }

            // Cancel the job
            job.Cancel();
            _jobRepository.Update(job);

            // Note: We would also need to cancel the Hangfire job here
            // Hangfire.BackgroundJob.Delete(job.HangfireJobId);

            _logger.LogInformation("Cancelled job {JobId} for user {UserId}", id, userId);

            return Ok(new { message = "Job cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling job {JobId} for user {UserId}", id, userId);
            return BadRequest(new { error = ex.Message });
        }
    }
}
