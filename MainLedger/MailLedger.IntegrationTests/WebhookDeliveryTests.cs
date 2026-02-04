using FluentAssertions;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MailLedger.IntegrationTests;

/// <summary>
/// Integration tests for webhook delivery
/// </summary>
public class WebhookDeliveryTests : IntegrationTestBase, IDisposable
{
    private readonly WireMockServer _mockServer;

    public WebhookDeliveryTests(WebApplicationFactoryFixture factory) : base(factory)
    {
        // Start WireMock server in constructor
        _mockServer = WireMockServer.Start();
    }

    public void Dispose()
    {
        _mockServer?.Stop();
        _mockServer?.Dispose();
    }

    [Fact]
    public async Task TriggerWebhook_SuccessfulDelivery_RecordsSuccess()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        
        // Setup WireMock to return 200 OK
        _mockServer.Given(Request.Create().WithPath("/webhook").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("OK"));

        var endpoint = await CreateTestWebhookEndpointAsync(
            user.Id,
            _mockServer.Url + "/webhook"
        );

        var webhookService = Factory.Services.GetRequiredService<IWebhookService>();

        var payload = new { test = "data" };

        // Act
        await webhookService.TriggerWebhooksAsync(
            user.Id,
            WebhookEventType.CandidateConfirmed,
            payload
        );

        // Wait for async delivery
        await Task.Delay(2000);

        // Assert
        var delivery = await DbContext.WebhookDeliveries
            .FirstOrDefaultAsync(d => d.WebhookEndpointId == endpoint.Id);

        delivery.Should().NotBeNull();
        delivery!.Status.Should().Be(WebhookDeliveryStatus.Success);
        delivery.AttemptCount.Should().Be(1);
        delivery.ResponseStatusCode.Should().Be(200);

        // Verify WireMock received the request
        _mockServer.LogEntries.Should().HaveCount(1);
    }

    [Fact]
    public async Task TriggerWebhook_ServerError_RecordsFailed()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        
        // Setup WireMock to always return 500 Internal Server Error
        _mockServer.Given(Request.Create().WithPath("/webhook").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        var endpoint = await CreateTestWebhookEndpointAsync(
            user.Id,
            _mockServer.Url + "/webhook"
        );

        var webhookService = Factory.Services.GetRequiredService<IWebhookService>();

        var payload = new { test = "data" };

        // Act
        await webhookService.TriggerWebhooksAsync(
            user.Id,
            WebhookEventType.CandidateConfirmed,
            payload
        );

        // Wait for retries to complete (initial + 2 retries with delays)
        // Delays are: 2s, 4s = 6s total + some buffer
        await Task.Delay(10000);

        // Assert
        var delivery = await DbContext.WebhookDeliveries
            .FirstOrDefaultAsync(d => d.WebhookEndpointId == endpoint.Id);

        delivery.Should().NotBeNull();
        delivery!.Status.Should().Be(WebhookDeliveryStatus.Failed);
        delivery.AttemptCount.Should().Be(3); // Initial attempt + 2 retries
        delivery.ResponseStatusCode.Should().Be(500);

        // Verify WireMock received 3 requests (initial + 2 retries)
        _mockServer.LogEntries.Should().HaveCount(3);
    }
}
