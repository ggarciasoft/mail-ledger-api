using MainLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MainLedger.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeds the database with default categories.
/// </summary>
public class CategorySeeder
{
    private readonly MailLedgerDbContext _context;
    private readonly ILogger<CategorySeeder> _logger;

    public CategorySeeder(MailLedgerDbContext context, ILogger<CategorySeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        // Check if categories already exist
        if (await _context.Categories.AnyAsync())
        {
            _logger.LogInformation("Categories already seeded, skipping");
            return;
        }

        _logger.LogInformation("Seeding default categories...");

        var categories = new[]
        {
            Category.Create("Groceries", "Food and household items"),
            Category.Create("Gasoline", "Fuel and gas stations"),
            Category.Create("Transport", "Public transport, taxis, ride-sharing"),
            Category.Create("Restaurants", "Dining out and food delivery"),
            Category.Create("Utilities", "Electricity, water, internet, phone"),
            Category.Create("Entertainment", "Movies, concerts, streaming services"),
            Category.Create("Shopping", "Clothing, electronics, general retail"),
            Category.Create("Healthcare", "Medical expenses, pharmacy, insurance"),
            Category.Create("Education", "Tuition, books, courses"),
            Category.Create("Travel", "Hotels, flights, vacation expenses"),
            Category.Create("Insurance", "Car, home, life insurance"),
            Category.Create("Subscriptions", "Monthly subscriptions and memberships"),
            Category.Create("Rent", "Housing rent or mortgage"),
            Category.Create("Fitness", "Gym, sports, wellness"),
            Category.Create("Personal Care", "Haircuts, beauty, grooming"),
            Category.Create("Gifts", "Presents and donations"),
            Category.Create("Home Improvement", "Repairs, furniture, decor"),
            Category.Create("Automotive", "Car maintenance, repairs, parking"),
            Category.Create("Pets", "Pet food, vet, supplies"),
            Category.Create("Other", "Miscellaneous expenses"),
        };

        await _context.Categories.AddRangeAsync(categories);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} default categories", categories.Length);
    }
}
