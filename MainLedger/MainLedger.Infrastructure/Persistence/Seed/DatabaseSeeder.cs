using MainLedger.Domain.Entities;
using MainLedger.Domain.ValueObjects;
using MainLedger.Domain.Services;
using Microsoft.EntityFrameworkCore;
using MainLedger.Infrastructure.Security;

namespace MainLedger.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds initial data for development and testing.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Default permission scopes for the system.
    /// </summary>
    public static class Scopes
    {
        public const string ReadTransactions = "read:transactions";
        public const string WriteTransactions = "write:transactions";
        public const string ReadRules = "read:rules";
        public const string WriteRules = "write:rules";
        public const string ReadEmails = "read:emails";
        public const string WriteEmails = "write:emails";
        public const string ReadUsers = "read:users";
        public const string WriteUsers = "write:users";
        public const string AdminAll = "admin:all";

        public static string[] AllScopes => new[]
        {
            ReadTransactions,
            WriteTransactions,
            ReadRules,
            WriteRules,
            ReadEmails,
            WriteEmails,
            ReadUsers,
            WriteUsers,
            AdminAll
        };
    }

    /// <summary>
    /// Seeds initial users and authentication data for development.
    /// </summary>
    public static async Task SeedAsync(MailLedgerDbContext context)
    {
        // Check if any users exist
        if (await context.Users.AnyAsync())
        {
            return; // Database already seeded
        }

        var passwordHasher = new PasswordHasher();

        // Create default admin user
        // Default credentials: admin@mailledger.com / Admin123!
        var adminUser = User.Register(
            EmailAddress.Create("admin@mailledger.com"),
            passwordHasher.HashPassword("Admin123!"),
            "Admin",
            "User"
        );
        adminUser.VerifyEmail(); // Auto-verify admin email

        // Create test user for development
        // Default credentials: test@mailledger.com / Test123!
        var testUser = User.Register(
            EmailAddress.Create("test@mailledger.com"),
            passwordHasher.HashPassword("Test123!"),
            "Test",
            "User"
        );

        // Create demo user for development
        // Default credentials: demo@mailledger.com / Demo123!
        var demoUser = User.Register(
            EmailAddress.Create("demo@mailledger.com"),
            passwordHasher.HashPassword("Demo123!"),
            "Demo",
            "User"
        );

        context.Users.AddRange(adminUser, testUser, demoUser);
        await context.SaveChangesAsync();

        // Create default API key for admin user with all scopes
        var adminApiKey = ApiKey.Create(
            adminUser.Id,
            passwordHasher.HashPassword("mlk_admin_default_key_" + Guid.NewGuid().ToString("N")),
            "Admin Default Key",
            Scopes.AllScopes,
            expiresAt: null // Never expires
        );

        // Create limited API key for test user
        var testApiKey = ApiKey.Create(
            testUser.Id,
            passwordHasher.HashPassword("mlk_test_default_key_" + Guid.NewGuid().ToString("N")),
            "Test Default Key",
            new[] { Scopes.ReadTransactions, Scopes.ReadRules },
            expiresAt: DateTime.UtcNow.AddYears(1)
        );

        context.ApiKeys.AddRange(adminApiKey, testApiKey);
        await context.SaveChangesAsync();
    }
}

