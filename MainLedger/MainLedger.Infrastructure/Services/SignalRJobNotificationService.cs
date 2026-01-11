using MainLedger.Application.Common.Interfaces;
using MainLedger.Contracts.Jobs;
using MainLedger.Domain.Entities;
using Microsoft.AspNetCore.SignalR;

namespace MainLedger.Infrastructure.Services;

/// <summary>
/// SignalR implementation of job notification service.
/// Uses dynamic hub context to avoid circular dependency with API project.
/// </summary>
public class SignalRJobNotificationService : IJobNotificationService
{
    private readonly IHubContext<Hub> _hubContext;

    public SignalRJobNotificationService(IHubContext<Hub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyJobUpdated(Guid userId, ProcessingJob job)
    {
        var dto = MapToDto(job);
        await _hubContext.Clients.Group($"user_{userId}").SendAsync("JobUpdated", dto);
    }

    public async Task NotifyJobCompleted(Guid userId, ProcessingJob job)
    {
        var dto = MapToDto(job);
        await _hubContext.Clients.Group($"user_{userId}").SendAsync("JobCompleted", dto);
    }

    public async Task NotifyJobFailed(Guid userId, ProcessingJob job)
    {
        var dto = MapToDto(job);
        await _hubContext.Clients.Group($"user_{userId}").SendAsync("JobFailed", dto);
    }

    private JobDto MapToDto(ProcessingJob job)
    {
        return new JobDto
        {
            JobId = job.Id.ToString(),
            UserId = job.UserId.ToString(),
            JobType = job.JobType.ToString(),
            Status = job.Status.ToString(),
            Progress = job.Progress,
            TotalItems = job.TotalItems,
            ProcessedItems = job.ProcessedItems,
            SuccessCount = job.SuccessCount,
            FailureCount = job.FailureCount,
            ErrorMessage = job.ErrorMessage,
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
        };
    }
}
