using MainLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MainLedger.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for creating DbContext instances during migrations.
/// This allows EF Core tools to create the DbContext without running the application.
/// Reads connection string from appsettings.json in the API project.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MailLedgerDbContext>
{
    public MailLedgerDbContext CreateDbContext(string[] args)
    {
        // Build configuration to read from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../MainLedger.API"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<MailLedgerDbContext>();

        // Read connection string from appsettings.json
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsqlOptions => npgsqlOptions.MigrationsAssembly("MainLedger.Infrastructure"));

        return new MailLedgerDbContext(optionsBuilder.Options);
    }
}
