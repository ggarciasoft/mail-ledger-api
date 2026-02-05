using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.BackgroundJobs;

/// <summary>
/// Recurring job to mark webhook deliveries stuck in Pending status as Failed
/// Runs every 5 minutes to find deliveries that have been pending for more than 10 minutes
/// </summary>
public class RecurringWebhookRetryJob
{
    private readonly IWebhookDeliveryRepository _webhookDeliveryRepository;
    private readonly ILogger<RecurringWebhookRetryJob> _logger;

    public RecurringWebhookRetryJob(
        IWebhookDeliveryRepository webhookDeliveryRepository,
        ILogger<RecurringWebhookRetryJob> logger)
    {
        _webhookDeliveryRepository = webhookDeliveryRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting webhook retry job to clean up stuck pending deliveries...");

        try
        {
            // Find deliveries stuck in Pending status for more than 10 minutes
            var cutoffTime = DateTime.UtcNow.AddMinutes(-10);
            var stuckDeliveries = await _webhookDeliveryRepository.GetStuckPendingDeliveriesAsync(cutoffTime);

            if (!stuckDeliveries.Any())
            {
                _logger.LogDebug("No stuck pending webhook deliveries found.");
                return;
            }

            _logger.LogWarning("Found {Count} stuck pending webhook deliveries. Marking as failed.", stuckDeliveries.Count);

            foreach (var delivery in stuckDeliveries)
            {
                try
                {
                    delivery.Status = WebhookDeliveryStatus.Failed;
                    delivery.ErrorMessage = "Delivery stuck in pending status (likely due to API restart)";
                    
                    await _webhookDeliveryRepository.UpdateAsync(delivery);

                    _logger.LogWarning(
                        "Marked stuck delivery {DeliveryId} as failed (was pending since {CreatedAt})",
                        delivery.Id, delivery.CreatedAt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error marking stuck webhook delivery {DeliveryId} as failed",
                        delivery.Id);
                }
            }

            _logger.LogInformation("Webhook retry job completed. Marked {Count} stuck deliveries as failed.", stuckDeliveries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in webhook retry job");
            throw;
        }
    }
}
