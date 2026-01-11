using MainLedger.Domain.Entities;

namespace MainLedger.Application.Common.Interfaces;

/// <summary>
/// Service for sending real-time job notifications via SignalR.
/// </summary>
public interface IJobNotificationService
{
    /// <summary>
    /// Notifies clients that a job has been updated.
    /// </summary>
    Task NotifyJobUpdated(Guid userId, ProcessingJob job);

    /// <summary>
    /// Notifies clients that a job has completed successfully.
    /// </summary>
    Task NotifyJobCompleted(Guid userId, ProcessingJob job);

    /// <summary>
    /// Notifies clients that a job has failed.
    /// </summary>
    Task NotifyJobFailed(Guid userId, ProcessingJob job);
}
