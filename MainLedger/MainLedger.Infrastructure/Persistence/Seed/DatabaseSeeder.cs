using MainLedger.Domain.Entities;
using MainLedger.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds initial data for development and testing.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Seeds test users for development.
    /// </summary>
    public static async Task SeedAsync(MailLedgerDbContext context)
    {
        // Check if any users exist
        if (await context.Users.AnyAsync())
        {
            return; // Database already seeded
        }

        // Create test users
        var testUser1 = User.Create(EmailAddress.Create("test@mailledger.com"));

        var testUser2 = User.Create(EmailAddress.Create("demo@mailledger.com"));

        context.Users.AddRange(testUser1, testUser2);
        await context.SaveChangesAsync();
    }
}

