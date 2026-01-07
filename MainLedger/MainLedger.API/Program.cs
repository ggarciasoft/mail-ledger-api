using MainLedger.Domain.Repositories;
using MainLedger.Infrastructure.Persistence;
using MainLedger.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllers();

            // Configure Data Protection for token encryption
            builder.Services.AddDataProtection()
                .SetApplicationName("MailLedger");

            // Configure DbContext with PostgreSQL
            builder.Services.AddDbContext<MailLedgerDbContext>(options =>
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions => npgsqlOptions.MigrationsAssembly("MainLedger.Infrastructure")));

            // Register repositories
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IGmailConnectionRepository, GmailConnectionRepository>();
            builder.Services.AddScoped<IEmailMessageRepository, EmailMessageRepository>();
            builder.Services.AddScoped<IRuleRepository, RuleRepository>();
            builder.Services.AddScoped<IFinancialRecordRepository, FinancialRecordRepository>();
            builder.Services.AddScoped<IExtractionVersionRepository, ExtractionVersionRepository>();
            builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Register security services
            builder.Services.AddSingleton<MainLedger.Application.Common.Interfaces.ITokenEncryptionService, 
                MainLedger.Infrastructure.Security.TokenEncryptionService>();

            // Register Rules Engine
            builder.Services.AddScoped<MainLedger.Application.Common.Interfaces.IRulesEngine,
                MainLedger.Application.Services.RulesEngine>();

            // Register Gmail Integration
            builder.Services.Configure<MainLedger.Domain.Settings.GmailSettings>(
                builder.Configuration.GetSection(MainLedger.Domain.Settings.GmailSettings.SectionName));
            
            builder.Services.AddScoped<MainLedger.Application.Common.Interfaces.IGmailService, MainLedger.Integrations.Services.GmailService>();

            // Register MediatR
            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MainLedger.Application.Emails.Commands.SyncGmailEmailsCommand).Assembly));

            // Add Swagger/OpenAPI
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Seed database in development
            if (app.Environment.IsDevelopment())
            {
                using var scope = app.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<MailLedgerDbContext>();
                Infrastructure.Persistence.Seed.DatabaseSeeder.SeedAsync(context).Wait();
            }

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
