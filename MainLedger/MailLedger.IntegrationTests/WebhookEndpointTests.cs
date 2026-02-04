using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MainLedger.Contracts.Webhooks;
using MainLedger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MailLedger.IntegrationTests;

/// <summary>
/// Integration tests for webhook endpoint CRUD operations
/// </summary>
public class WebhookEndpointTests : IntegrationTestBase
{
    public WebhookEndpointTests(WebApplicationFactoryFixture factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateWebhook_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var client = CreateAuthenticatedClient(user.Id, user.Email.Value);

        var request = new CreateWebhookEndpointRequest(
            Url: "https://example.com/webhook",
            Events: new List<string> { "CandidateConfirmed" }
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/webhooks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<WebhookEndpointDto>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.Url.Should().Be(request.Url);
        result.Events.Should().BeEquivalentTo(request.Events);
        result.IsActive.Should().BeTrue();
        result.SecretKey.Should().NotBeNullOrEmpty(); // Only returned on creation

        // Verify in database
        var endpoint = await DbContext.WebhookEndpoints.FindAsync(result.Id);
        endpoint.Should().NotBeNull();
        endpoint!.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetWebhooks_ReturnsUserWebhooks()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var client = CreateAuthenticatedClient(user.Id, user.Email.Value);

        // Create 2 webhooks for this user
        await CreateTestWebhookEndpointAsync(user.Id, "https://example.com/webhook1");
        await CreateTestWebhookEndpointAsync(user.Id, "https://example.com/webhook2");

        // Act
        var response = await client.GetAsync("/api/webhooks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<WebhookEndpointDto>>();
        result.Should().NotBeNull();
        result!.Count.Should().Be(2);
        result.Should().AllSatisfy(e => e.SecretKey.Should().BeNull()); // Secret not returned in list
    }

    [Fact]
    public async Task UpdateWebhook_ValidRequest_UpdatesEndpoint()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var client = CreateAuthenticatedClient(user.Id, user.Email.Value);

        var endpoint = await CreateTestWebhookEndpointAsync(
            user.Id,
            "https://example.com/webhook",
            new List<WebhookEventType> { WebhookEventType.CandidateConfirmed }
        );

        var updateRequest = new UpdateWebhookEndpointRequest(
            Url: "https://example.com/updated-webhook",
            Events: new List<string> { "CandidateBulkConfirmed" }
        );

        // Act
        var response = await client.PutAsJsonAsync($"/api/webhooks/{endpoint.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Clear change tracker to ensure fresh query from database
        DbContext.ChangeTracker.Clear();

        // Verify changes persisted
        var updated = await DbContext.WebhookEndpoints.FindAsync(endpoint.Id);
        updated.Should().NotBeNull();
        updated!.Url.Should().Be(updateRequest.Url);
        updated.Events.Should().Contain(WebhookEventType.CandidateBulkConfirmed);
    }

    [Fact]
    public async Task DeleteWebhook_ValidRequest_DeletesEndpoint()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var client = CreateAuthenticatedClient(user.Id, user.Email.Value);

        var endpoint = await CreateTestWebhookEndpointAsync(user.Id);

        // Act
        var response = await client.DeleteAsync($"/api/webhooks/{endpoint.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Clear change tracker to ensure fresh query from database
        DbContext.ChangeTracker.Clear();

        // Verify deleted from database
        var deleted = await DbContext.WebhookEndpoints.FindAsync(endpoint.Id);
        deleted.Should().BeNull();
    }
}
