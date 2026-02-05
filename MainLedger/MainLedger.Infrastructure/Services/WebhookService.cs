using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MainLedger.Infrastructure.Services;

public class WebhookService : IWebhookService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookService> _logger;

    private static readonly int[] RetryDelaysMs = { 2000, 4000, 8000 }; // 2s, 4s, 8s

    public WebhookService(
        IServiceScopeFactory serviceScopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }


    public async Task TriggerWebhooksAsync(
        Guid userId, 
        WebhookEventType eventType, 
        object payload, 
        CancellationToken cancellationToken = default)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var webhookEndpointRepository = scope.ServiceProvider.GetRequiredService<IWebhookEndpointRepository>();
        var webhookDeliveryRepository = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryRepository>();

        // Get active webhook endpoints for this user and event type
        var endpoints = await webhookEndpointRepository.GetActiveByUserIdAndEventTypeAsync(
            userId, 
            eventType, 
            cancellationToken);

        if (!endpoints.Any())
        {
            _logger.LogDebug("No active webhook endpoints found for user {UserId} and event {EventType}", 
                userId, eventType);
            return;
        }

        // Serialize payload once
        var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Trigger each endpoint (fire and forget with background task)
        foreach (var endpoint in endpoints)
        {
            // Create delivery record
            var delivery = new WebhookDelivery
            {
                Id = Guid.NewGuid(),
                WebhookEndpointId = endpoint.Id,
                EventType = eventType,
                Payload = payloadJson,
                Status = WebhookDeliveryStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await webhookDeliveryRepository.AddAsync(delivery, cancellationToken);

            // Trigger delivery in background with new scope (pass IDs, not entities)
            var deliveryId = delivery.Id;
            var endpointId = endpoint.Id;
            
            _ = Task.Run(async () =>
            {
                try
                {
                    await DeliverWebhookAsync(endpointId, deliveryId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Unhandled exception in webhook delivery background task for delivery {DeliveryId}", 
                        deliveryId);
                }
            });
        }
    }


    private async Task DeliverWebhookAsync(Guid endpointId, Guid deliveryId)
    {
        _logger.LogInformation("DeliverWebhookAsync started for endpoint {EndpointId}, delivery {DeliveryId}", 
            endpointId, deliveryId);

        // Create new scope for background task
        using var scope = _serviceScopeFactory.CreateScope();
        var webhookEndpointRepository = scope.ServiceProvider.GetRequiredService<IWebhookEndpointRepository>();
        var webhookDeliveryRepository = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryRepository>();

        _logger.LogDebug("Loading endpoint and delivery from database...");

        // Load endpoint and delivery
        var endpoint = await webhookEndpointRepository.GetByIdAsync(endpointId);
        var delivery = await webhookDeliveryRepository.GetByIdAsync(deliveryId);

        if (endpoint == null || delivery == null)
        {
            _logger.LogError("Webhook endpoint or delivery not found (Endpoint: {EndpointId}, Delivery: {DeliveryId})", 
                endpointId, deliveryId);
            return;
        }


        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                delivery.AttemptCount++;
                delivery.LastAttemptAt = DateTime.UtcNow;

                // Generate signature
                var signature = GenerateSignature(delivery.Payload, endpoint.SecretKey);
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

                // Create request
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Url)
                {
                    Content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json")
                };

                // Add webhook headers
                request.Headers.Add("X-Webhook-Signature", signature);
                request.Headers.Add("X-Webhook-Timestamp", timestamp);
                request.Headers.Add("X-Webhook-Event", delivery.EventType.ToString());
                request.Headers.Add("X-Webhook-Delivery-Id", delivery.Id.ToString());

                // Send request
                var response = await httpClient.SendAsync(request);

                delivery.ResponseStatusCode = (int)response.StatusCode;
                delivery.ResponseBody = await response.Content.ReadAsStringAsync();

                // Truncate response body to 1000 chars
                if (delivery.ResponseBody?.Length > 1000)
                {
                    delivery.ResponseBody = delivery.ResponseBody.Substring(0, 1000);
                }

                if (response.IsSuccessStatusCode)
                {
                    // Success!
                    delivery.Status = WebhookDeliveryStatus.Success;
                    await webhookDeliveryRepository.UpdateAsync(delivery);

                    // Update endpoint's last triggered timestamp
                    endpoint.LastTriggeredAt = DateTime.UtcNow;
                    await webhookEndpointRepository.UpdateAsync(endpoint);

                    _logger.LogInformation(
                        "Webhook delivered successfully to {Url} for event {EventType} (Delivery ID: {DeliveryId})",
                        endpoint.Url, delivery.EventType, delivery.Id);
                    
                    return; // Success, exit retry loop
                }
                else
                {
                    delivery.ErrorMessage = $"HTTP {delivery.ResponseStatusCode}: {response.ReasonPhrase}";
                    _logger.LogWarning(
                        "Webhook delivery failed with status {StatusCode} to {Url} (Attempt {Attempt}/3, Delivery ID: {DeliveryId})",
                        delivery.ResponseStatusCode, endpoint.Url, attempt + 1, delivery.Id);
                }
            }
            catch (Exception ex)
            {
                delivery.ErrorMessage = ex.Message;
                _logger.LogError(ex,
                    "Webhook delivery exception to {Url} (Attempt {Attempt}/3, Delivery ID: {DeliveryId})",
                    endpoint.Url, attempt + 1, delivery.Id);
            }

            // Save attempt
            await webhookDeliveryRepository.UpdateAsync(delivery);

            // Retry with exponential backoff (if not last attempt)
            if (attempt < 2)
            {
                await Task.Delay(RetryDelaysMs[attempt]);
            }
        }

        // All retries failed
        delivery.Status = WebhookDeliveryStatus.Failed;
        await webhookDeliveryRepository.UpdateAsync(delivery);

        _logger.LogError(
            "Webhook delivery failed after 3 attempts to {Url} for event {EventType} (Delivery ID: {DeliveryId})",
            endpoint.Url, delivery.EventType, delivery.Id);
    }

    public string GenerateSignature(string payload, string secretKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public bool ValidateSignature(string payload, string signature, string secretKey)
    {
        var expectedSignature = GenerateSignature(payload, secretKey);
        return string.Equals(signature, expectedSignature, StringComparison.OrdinalIgnoreCase);
    }
}

