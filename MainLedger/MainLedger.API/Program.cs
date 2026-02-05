using Hangfire;
using Hangfire.PostgreSql;
using MainLedger.Domain.Repositories;
using MainLedger.Infrastructure.Persistence;
using MainLedger.Infrastructure.Persistence.Repositories;
using MainLedger.Infrastructure.Persistence.Seeders;
using MainLedger.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllers();

            // Configure CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(
                    "AllowFrontend",
                    policy =>
                    {
                        policy
                            .WithOrigins(
                                "http://localhost:3000", // React default
                                "http://localhost:5173", // Vite default
                                "https://localhost:3000",
                                "https://localhost:5173"
                            )
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials();
                    }
                );
            });

            // Configure Data Protection for token encryption
            builder.Services.AddDataProtection().SetApplicationName("MailLedger");
            // Configure log4net
            var logRepository = log4net.LogManager.GetRepository(
                System.Reflection.Assembly.GetEntryAssembly()
            );
            log4net.Config.XmlConfigurator.Configure(
                logRepository,
                new System.IO.FileInfo("log4net.config")
            );

            // Configure DbContext with PostgreSQL
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.EnableDynamicJson();
            var dataSource = dataSourceBuilder.Build();

            builder.Services.AddDbContext<MailLedgerDbContext>(options =>
                options.UseNpgsql(
                    dataSource,
                    npgsqlOptions => npgsqlOptions.MigrationsAssembly("MainLedger.Infrastructure")
                )
            );

            // Configure Hangfire with PostgreSQL storage
            builder.Services.AddHangfire(configuration =>
                configuration
                    .SetDataCompatibilityLevel(Hangfire.CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UsePostgreSqlStorage(options =>
                    {
                        options.UseNpgsqlConnection(connectionString);
                    })
            );

            // Add Hangfire server
            builder.Services.AddHangfireServer(options =>
            {
                options.WorkerCount = 5; // Number of concurrent jobs
            });

            // Register repositories
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IEmailMessageRepository, EmailMessageRepository>();
            builder.Services.AddScoped<IRuleRepository, RuleRepository>();
            builder.Services.AddScoped<IFinancialRecordRepository, FinancialRecordRepository>();
            builder.Services.AddScoped<IExtractionVersionRepository, ExtractionVersionRepository>();
            builder.Services.AddScoped<
                IExtractionCandidateRepository,
                ExtractionCandidateRepository
            >();
            builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            builder.Services.AddScoped<IEmailSyncHistoryRepository, EmailSyncHistoryRepository>();
            builder.Services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
            builder.Services.AddScoped<
                IEmailVerificationTokenRepository,
                EmailVerificationTokenRepository
            >();
            builder.Services.AddScoped<
                IPasswordResetTokenRepository,
                PasswordResetTokenRepository
            >();
            builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            builder.Services.AddScoped<IProcessingJobRepository, ProcessingJobRepository>();
            builder.Services.AddScoped<IContactMessageRepository, ContactMessageRepository>();
            builder.Services.AddScoped<IEmailConnectionRepository, EmailConnectionRepository>();
            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<IWebhookEndpointRepository, WebhookEndpointRepository>();
            builder.Services.AddScoped<IWebhookDeliveryRepository, WebhookDeliveryRepository>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Register HTTP Context Accessor (required for CurrentUserService)
            builder.Services.AddHttpContextAccessor();

            // Register MemoryCache for PKCE state storage
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<
                MainLedger.Application.Common.Interfaces.IPkceStateStore,
                MainLedger.Infrastructure.Services.InMemoryPkceStateStore
            >();

            // Register OAuth state service
            builder.Services.AddSingleton<
                MainLedger.Application.Common.Interfaces.IOAuthStateService,
                MainLedger.Infrastructure.Services.OAuthStateService
            >();

            // Register security services
            builder.Services.AddSingleton<
                MainLedger.Application.Common.Interfaces.ITokenEncryptionService,
                MainLedger.Infrastructure.Security.TokenEncryptionService
            >();

            // Register authentication services
            builder.Services.AddSingleton<
                MainLedger.Domain.Services.IPasswordHasher,
                MainLedger.Infrastructure.Security.PasswordHasher
            >();

            builder.Services.AddSingleton<
                MainLedger.Domain.Services.ITokenGenerator,
                MainLedger.Infrastructure.Security.TokenGenerator
            >();

            builder.Services.AddScoped<
                MainLedger.Application.Authentication.Services.IJwtTokenService,
                MainLedger.Infrastructure.Security.JwtTokenService
            >();

            builder.Services.AddScoped<
                MainLedger.Application.Authentication.Services.ICurrentUserService,
                MainLedger.Infrastructure.Security.CurrentUserService
            >();

            // Register Rules Engine
            builder.Services.AddScoped<
                MainLedger.Application.Common.Interfaces.IRulesEngine,
                MainLedger.Application.Services.RulesEngine
            >();

            // Register Classification Service
            builder.Services.Configure<MainLedger.Domain.Settings.OpenAISettings>(
                builder.Configuration.GetSection(
                    MainLedger.Domain.Settings.OpenAISettings.SectionName
                )
            );

            builder.Services.AddScoped<
                MainLedger.Application.Common.Interfaces.IClassificationService,
                MainLedger.Integrations.Services.OpenAIClassificationService
            >();

            // Register Extraction Service
            builder.Services.AddScoped<
                MainLedger.Application.Common.Interfaces.IExtractionService,
                MainLedger.Integrations.Services.OpenAIExtractionService
            >();

            // Register Normalization Service
            builder.Services.AddScoped<
                MainLedger.Application.Common.Interfaces.INormalizationService,
                MainLedger.Application.Services.NormalizationService
            >();

            // Register Job Management Service
            builder.Services.AddScoped<
                MainLedger.Application.Common.Interfaces.IJobManagementService,
                MainLedger.Application.Services.JobManagementService
            >();

            // Register Background Jobs
            builder.Services.AddScoped<MainLedger.Application.BackgroundJobs.EmailSyncBackgroundJob>();
            builder.Services.AddScoped<MainLedger.Application.BackgroundJobs.ClassificationBackgroundJob>();
            builder.Services.AddScoped<MainLedger.Application.BackgroundJobs.ExtractionBackgroundJob>();

            // Register Workflow Service
            builder.Services.AddScoped<
                MainLedger.Application.Common.Interfaces.IWorkflowService,
                MainLedger.Application.Services.WorkflowService
            >();

            // Register Recurring Jobs
            builder.Services.AddScoped<MainLedger.Application.BackgroundJobs.RecurringEmailSyncJob>();
            builder.Services.AddScoped<MainLedger.Application.BackgroundJobs.RecurringClassificationJob>();
            builder.Services.AddScoped<MainLedger.Application.BackgroundJobs.RecurringExtractionJob>();
            builder.Services.AddScoped<MainLedger.Application.BackgroundJobs.SequentialPipelineJob>();

            // Register Workflow Configuration Repository
            builder.Services.AddScoped<
                MainLedger.Domain.Repositories.IWorkflowConfigurationRepository,
                MainLedger.Infrastructure.Persistence.Repositories.WorkflowConfigurationRepository
            >();

            // Register Subscription Repositories
            builder.Services.AddScoped<
                MainLedger.Domain.Repositories.ISubscriptionPlanRepository,
                MainLedger.Infrastructure.Persistence.Repositories.SubscriptionPlanRepository
            >();
            builder.Services.AddScoped<
                MainLedger.Domain.Repositories.IUserSubscriptionRepository,
                MainLedger.Infrastructure.Persistence.Repositories.UserSubscriptionRepository
            >();

            // Register Subscription Service
            builder.Services.AddScoped<
                MainLedger.Application.Common.Interfaces.ISubscriptionService,
                MainLedger.Application.Services.SubscriptionService
            >();

            // Register Subscription Repositories
            builder.Services.AddScoped<
                MainLedger.Domain.Repositories.ISubscriptionPlanRepository,
                MainLedger.Infrastructure.Persistence.Repositories.SubscriptionPlanRepository
            >();
            builder.Services.AddScoped<
                MainLedger.Domain.Repositories.IUserSubscriptionRepository,
                MainLedger.Infrastructure.Persistence.Repositories.UserSubscriptionRepository
            >();

            // Register Subscription Service
            builder.Services.AddScoped<
                MainLedger.Application.Common.Interfaces.ISubscriptionService,
                MainLedger.Application.Services.SubscriptionService
            >();

            // Register SignalR
            builder.Services.AddSignalR();

            // Register Job Notification Service with JobHub context
            builder.Services.AddScoped<MainLedger.Application.Common.Interfaces.IJobNotificationService>(
                sp =>
                {
                    var hubContext = sp.GetRequiredService<
                        IHubContext<MainLedger.API.Hubs.JobHub>
                    >();
                    // Cast to IHubContext<Hub> to avoid circular dependency
                    return new MainLedger.Infrastructure.Services.SignalRJobNotificationService(
                        (IHubContext<Hub>)(object)hubContext
                    );
                }
            );

            // Register Gmail Integration
            builder.Services.Configure<MainLedger.Domain.Settings.GmailSettings>(
                builder.Configuration.GetSection(
                    MainLedger.Domain.Settings.GmailSettings.SectionName
                )
            );

            builder.Services.AddScoped<
                MainLedger.Application.Common.Interfaces.IGmailService,
                MainLedger.Integrations.Services.GmailService
            >();

            // Register Outlook Integration
            builder.Services.Configure<MainLedger.Domain.Settings.OutlookSettings>(
                builder.Configuration.GetSection("Outlook")
            );

            builder.Services.AddSingleton(sp =>
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MainLedger.Domain.Settings.OutlookSettings>>().Value
            );

            // Register Email Providers
            builder.Services.AddScoped<
                MainLedger.Domain.Services.IEmailProvider,
                MainLedger.Integrations.Services.GmailEmailProvider
            >();

            builder.Services.AddScoped<
                MainLedger.Domain.Services.IEmailProvider,
                MainLedger.Integrations.Services.OutlookEmailProvider
            >();

            // Register Email Provider Factory
            builder.Services.AddScoped<
                MainLedger.Application.Common.Interfaces.IEmailProviderFactory,
                MainLedger.Application.Services.EmailProviderFactory
            >();

            // Register Email Notification Services
            builder.Services.AddScoped<
                MainLedger.Domain.Repositories.IEmailNotificationRepository,
                MainLedger.Infrastructure.Persistence.Repositories.EmailNotificationRepository
            >();
            builder.Services.AddScoped<
                MainLedger.Application.Common.Interfaces.IEmailService,
                MainLedger.Infrastructure.Services.SmtpEmailService
            >();
            builder.Services.AddScoped<MainLedger.Application.BackgroundJobs.EmailSendingBackgroundJob>();

            // Register Webhook Service
            builder.Services.AddHttpClient(); // Required for WebhookService
            builder.Services.AddScoped<
                MainLedger.Application.Common.Interfaces.IWebhookService,
                MainLedger.Infrastructure.Services.WebhookService
            >();

            builder.Services.AddScoped<
                Domain.Services.IPasswordHasher,
                MainLedger.Infrastructure.Security.PasswordHasher
            >();

            // Register MediatR
            builder.Services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(
                    typeof(MainLedger.Application.Emails.Commands.SyncGmailEmailsCommand).Assembly
                )
            );

            // Configure JWT Authentication
            builder
                .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    var jwtSettings = builder.Configuration.GetSection("Jwt");
                    var secretKey =
                        jwtSettings["SecretKey"]
                        ?? throw new InvalidOperationException("JWT SecretKey not configured");

                    options.TokenValidationParameters =
                        new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = jwtSettings["Issuer"],
                            ValidAudience = jwtSettings["Audience"],
                            IssuerSigningKey =
                                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                                    System.Text.Encoding.UTF8.GetBytes(secretKey)
                                ),
                            ClockSkew = TimeSpan.Zero,
                        };
                })
                .AddScheme<
                    AuthenticationSchemeOptions,
                    MainLedger.Infrastructure.Security.ApiKeyAuthenticationHandler
                >("ApiKey", options => { });

            // Configure Authorization to support both JWT and API Key authentication
            builder.Services.AddScoped<
                Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
                MainLedger.Infrastructure.Security.ScopeAuthorizationHandler
            >();

            builder.Services.AddScoped<
                Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
                MainLedger.Infrastructure.Security.ApiKeyMustHaveExplicitScopeHandler
            >();

            builder.Services.AddAuthorization(options =>
            {
                options.DefaultPolicy =
                    new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, "ApiKey")
                        .RequireAuthenticatedUser()
                        .AddRequirements(
                            new MainLedger.Infrastructure.Security.ApiKeyMustHaveExplicitScopeRequirement()
                        )
                        .Build();

                // Add scope-based policies for API key authorization
                var allowedScopes = new[]
                {
                    "read:transactions",
                    "write:transactions",
                    "read:rules",
                    "write:rules",
                    "read:users",
                    "write:users",
                };

                foreach (var scope in allowedScopes)
                {
                    options.AddPolicy(
                        $"RequireScope:{scope}",
                        policy =>
                            policy
                                .AddAuthenticationSchemes(
                                    JwtBearerDefaults.AuthenticationScheme,
                                    "ApiKey"
                                )
                                .RequireAuthenticatedUser()
                                .Requirements.Add(
                                    new MainLedger.Infrastructure.Security.ScopeRequirement(scope)
                                )
                    );
                }
            });

            // Add Swagger/OpenAPI
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add log4net logging
            builder.Logging.ClearProviders();
            builder.Logging.AddLog4Net("log4net.config");

            var app = builder.Build();

            // Seed database in development
            if (app.Environment.IsDevelopment())
            {
                using var scope = app.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<MailLedgerDbContext>();
                Infrastructure.Persistence.Seed.DatabaseSeeder.SeedAsync(context).Wait();
                Infrastructure.Seeders.SubscriptionPlanSeeder.SeedAsync(context).Wait();
                app.UseSwagger();
                app.UseSwaggerUI();

                // Hangfire Dashboard
                app.UseHangfireDashboard(
                    "/hangfire",
                    new Hangfire.DashboardOptions
                    {
                        Authorization = new[] { new HangfireAuthorizationFilter() },
                    }
                );

                // Configure recurring jobs
                Hangfire.RecurringJob.AddOrUpdate<MainLedger.Application.BackgroundJobs.EmailSendingBackgroundJob>(
                    "process-email-queue",
                    job => job.ExecuteAsync(CancellationToken.None),
                    Hangfire.Cron.Minutely
                );

                // Webhook retry job - runs every 5 minutes to clean up stuck pending deliveries
                Hangfire.RecurringJob.AddOrUpdate<MainLedger.Application.BackgroundJobs.RecurringWebhookRetryJob>(
                    "webhook-retry-cleanup",
                    job => job.ExecuteAsync(),
                    "*/5 * * * *" // Every 5 minutes
                );
            }

            app.UseHttpsRedirection();

            // HTTP Request/Response Logging (configurable via appsettings.json)
            app.UseMiddleware<MainLedger.API.Middleware.HttpLoggingMiddleware>();

            // Enable CORS
            app.UseCors("AllowFrontend");

            // Enable Authentication & Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // Map SignalR Hub
            app.MapHub<MainLedger.API.Hubs.JobHub>("/api/hubs/jobs");

            // Seed default categories
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<MailLedgerDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<CategorySeeder>>();
                var categorySeeder = new CategorySeeder(context, logger);
                await categorySeeder.SeedAsync();
            }

            app.Run();
        }
    }
}
