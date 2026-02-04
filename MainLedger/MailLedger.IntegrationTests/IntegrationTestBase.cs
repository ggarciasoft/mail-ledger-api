using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.ValueObjects;
using MainLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace MailLedger.IntegrationTests;

/// <summary>
/// Base class for integration tests providing common utilities
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<WebApplicationFactoryFixture>, IAsyncLifetime
{
    protected readonly WebApplicationFactoryFixture Factory;
    protected readonly HttpClient Client;
    protected MailLedgerDbContext DbContext = null!;

    protected IntegrationTestBase(WebApplicationFactoryFixture factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Create a new scope for this test
        var scope = Factory.Services.CreateScope();
        DbContext = scope.ServiceProvider.GetRequiredService<MailLedgerDbContext>();
        
        // Clean database before each test
        await CleanDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        // Clean up after test
        await CleanDatabaseAsync();
    }

    /// <summary>
    /// Clean all test data from database
    /// </summary>
    protected async Task CleanDatabaseAsync()
    {
        // Delete in correct order to respect foreign keys
        await DbContext.WebhookDeliveries.ExecuteDeleteAsync();
        await DbContext.WebhookEndpoints.ExecuteDeleteAsync();
        await DbContext.FinancialRecords.ExecuteDeleteAsync();
        await DbContext.ExtractionCandidates.ExecuteDeleteAsync();
        await DbContext.EmailMessages.ExecuteDeleteAsync();
        await DbContext.Rules.ExecuteDeleteAsync();
        await DbContext.EmailConnections.ExecuteDeleteAsync();
        await DbContext.ApiKeys.ExecuteDeleteAsync();
        await DbContext.RefreshTokens.ExecuteDeleteAsync();
        await DbContext.UserSubscriptions.ExecuteDeleteAsync();
        await DbContext.Users.ExecuteDeleteAsync();
    }

    /// <summary>
    /// Create a test user with Pro subscription plan
    /// </summary>
    protected async Task<User> CreateTestUserAsync(
        string email = "test@example.com",
        string password = "Test123!")
    {
        // Get Pro subscription plan
        var plan = await DbContext.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Name == "Pro");

        if (plan == null)
        {
            throw new InvalidOperationException("Pro subscription plan not found. Ensure database is seeded.");
        }

        // Create user using factory method
        var user = User.Register(
            EmailAddress.Create(email),
            BCrypt.Net.BCrypt.HashPassword(password),
            "Test",
            "User"
        );

        // Activate and verify user for testing
        user.Activate();
        user.VerifyEmail();

        // Create subscription using public constructor
        var subscription = new UserSubscription(user.Id, plan.Id);

        DbContext.Users.Add(user);
        DbContext.UserSubscriptions.Add(subscription);
        await DbContext.SaveChangesAsync();

        return user;
    }

    /// <summary>
    /// Generate JWT token for test user
    /// </summary>
    protected string GenerateJwtToken(Guid userId, string email = "test@example.com")
    {
        // Use the same secret key as in appsettings.json
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-secret-key-min-32-characters-long"));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: "MailLedger", // Must match appsettings.json Jwt:Issuer
            audience: "MailLedgerAPI", // Must match appsettings.json Jwt:Audience
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Create authenticated HTTP client for test user
    /// </summary>
    protected HttpClient CreateAuthenticatedClient(Guid userId, string email = "test@example.com")
    {
        var client = Factory.CreateClient();
        var token = GenerateJwtToken(userId, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Create a test webhook endpoint
    /// </summary>
    protected async Task<WebhookEndpoint> CreateTestWebhookEndpointAsync(
        Guid userId,
        string url = "https://example.com/webhook",
        List<WebhookEventType>? events = null,
        bool isActive = true)
    {
        events ??= new List<WebhookEventType> { WebhookEventType.CandidateConfirmed };

        var endpoint = new WebhookEndpoint
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Url = url,
            SecretKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            Events = events,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.WebhookEndpoints.Add(endpoint);
        await DbContext.SaveChangesAsync();

        return endpoint;
    }
}
