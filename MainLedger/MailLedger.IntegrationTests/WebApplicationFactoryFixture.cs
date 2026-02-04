using DotNet.Testcontainers.Builders;
using MainLedger.API;
using MainLedger.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace MailLedger.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory for integration tests with PostgreSQL test container
/// </summary>
public class WebApplicationFactoryFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;
    
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        // Create PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("mailledger_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        // Start container
        await _postgresContainer.StartAsync();
        
        // Get connection string
        ConnectionString = _postgresContainer.GetConnectionString();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext registration
            services.RemoveAll(typeof(DbContextOptions<MailLedgerDbContext>));
            services.RemoveAll(typeof(MailLedgerDbContext));

            // Add test database context
            services.AddDbContext<MailLedgerDbContext>(options =>
            {
                options.UseNpgsql(ConnectionString);
            });

            // Build service provider and run migrations
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MailLedgerDbContext>();
            
            // Ensure database is created and migrations are applied
            dbContext.Database.Migrate();
            
            // Seed subscription plans for tests
            MainLedger.Infrastructure.Seeders.SubscriptionPlanSeeder.SeedAsync(dbContext).Wait();
        });

        builder.UseEnvironment("Testing");
    }

    public new async Task DisposeAsync()
    {
        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }
        
        await base.DisposeAsync();
    }
}
