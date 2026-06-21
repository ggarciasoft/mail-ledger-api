# MailLedger API

The .NET 9 backend for **MailLedger** — a service that turns email into structured financial data. It connects to a user's Gmail or Outlook mailbox, syncs messages, uses OpenAI to classify which emails are financial and extract transaction details, and lets users review and confirm those into an immutable financial ledger.

## What it does

```
Connect mailbox (Gmail/Outlook OAuth)
        │
        ▼
Sync emails ──► Rules engine ──► AI classification ──► AI extraction
                                                            │
                                                            ▼
                                            Extraction candidates (pending review)
                                                            │
                                                  user confirms / rejects
                                                            ▼
                                              Financial records (immutable ledger)
                                                            │
                                          dashboards · CSV export · webhooks
```

Additional capabilities: JWT and API-key authentication, Google/Microsoft SSO, Stripe subscriptions with tiered usage limits, scheduled workflow automation, outbound webhooks, real-time job updates over SignalR, and SMTP notifications.

## Tech stack

- **.NET 9** / ASP.NET Core Web API
- **Entity Framework Core 9** with **PostgreSQL** (Npgsql)
- **Hangfire** (PostgreSQL storage) for background jobs
- **MediatR** for application commands/queries
- **SignalR** for real-time job notifications
- **OpenAI** SDK for classification & extraction
- **Google.Apis.Gmail** + **Microsoft.Graph** for email providers
- **Stripe.net** for subscriptions
- **log4net** for logging
- **JWT bearer** + BCrypt password hashing + ASP.NET Data Protection for token encryption

## Solution structure

Clean/Onion architecture: `API → Application (MediatR) → Domain ← Infrastructure / Integrations`.

| Project | Description |
|---------|-------------|
| `MainLedger.API` | ASP.NET Core host: controllers, middleware, SignalR hub, Swagger, Hangfire dashboard, `Program.cs`. |
| `MainLedger.Application` | Application layer: MediatR handlers, background jobs, workflow/subscription/rules/normalization services. |
| `MainLedger.Domain` | Domain model: entities, enums, value objects, repository interfaces, settings, domain events. |
| `MainLedger.Infrastructure` | EF Core persistence & repositories, JWT/API-key security, OAuth, Stripe, webhooks, SMTP, seeders. |
| `MainLedger.Integrations` | External adapters: Gmail API, Outlook/Microsoft Graph, OpenAI. |
| `MainLedger.Contracts` | Request/response DTOs shared between API and Application. |
| `MainLedger.Shared` | Shared utilities (e.g. the `Result`/`Result<T>` pattern). |
| `MailLedger.IntegrationTests` | xUnit integration tests using Testcontainers PostgreSQL. |
| `MailLedger.PlaygroundProject` | Standalone dummy webhook receiver for manual webhook testing. |

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- A running **PostgreSQL** instance
- **Docker** (only required to run the integration tests via Testcontainers)
- Optional credentials for full functionality: Google (Gmail/SSO), Microsoft (Outlook/SSO), OpenAI, Stripe, SMTP

## Configuration

Configuration lives in `MainLedger.API/appsettings.json`. **Do not commit secrets** — use [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) (the API project has a `UserSecretsId`) or environment variables for local development.

Key configuration sections:

| Section | Keys |
|---------|------|
| `ConnectionStrings` | `DefaultConnection` (PostgreSQL; also used by Hangfire) |
| `Jwt` | `SecretKey`, `Issuer`, `Audience`, `ExpirationMinutes` |
| `Gmail` | `ClientId`, `ClientSecret`, `RedirectUri` |
| `Outlook` | `ClientId`, `ClientSecret`, `TenantId`, `RedirectUri`, `Scopes` |
| `OpenAI` | `ApiKey`, `Model`, `MaxTokens`, `Temperature`, `MaxBodyLength`, `UseSimulation` |
| `EmailSettings` | `SmtpServer`, `Port`, `SenderName`, `SenderEmail`, `Username`, `Password` |
| `Stripe` | `SecretKey`, `PublishableKey`, `WebhookSecret`, `CheckoutSuccessUrl`, `CheckoutCancelUrl`, `PlanPriceIds` |
| `OAuthSSO` | `Google` and `Microsoft` (each with `ClientId`, `ClientSecret`, `RedirectUri`, etc.) |
| `FrontendUrl` | Base URL of the MailLedger UI (used for redirects) |

> Set `OpenAI:UseSimulation` to `true` to run the AI pipeline with mock data and no OpenAI calls — handy for local development.

## Database setup

Apply EF Core migrations before first run:

```bash
cd mail-ledger-api/MainLedger
dotnet ef database update --project MainLedger.Infrastructure --startup-project MainLedger.API
```

In the `Development` environment, the app also seeds default subscription plans and categories on startup.

## Running the API

```bash
cd mail-ledger-api/MainLedger/MainLedger.API
dotnet restore
dotnet run
```

Launch profiles (`Properties/launchSettings.json`):

| Profile | URLs |
|---------|------|
| `https` (default) | `https://localhost:7166`, `http://localhost:5032` |
| `http` | `http://localhost:5032` |

When `ASPNETCORE_ENVIRONMENT=Development`:

- **Swagger UI** is served at the API root
- **Hangfire dashboard** is available at `/hangfire`
- The database is seeded and recurring jobs are scheduled

CORS allows the frontend at `http://localhost:3000` and `http://localhost:5173` (Vite).

## API overview

Controllers live in `MainLedger.API/Controllers/`. Primary route groups:

| Route prefix | Purpose |
|--------------|---------|
| `/api/auth` | Register, login, refresh, email verification, password reset, Google/Microsoft SSO |
| `/api/email`, `/api/gmail` | Email provider OAuth, connections, sync (Gmail/Outlook) |
| `/api/emails` | List/inspect/delete synced emails + statistics |
| `/api/processing` | Trigger classification & extraction |
| `/api/extraction-candidates` | Review, confirm, reject, edit AI-extracted candidates |
| `/api/financial-records` | List/inspect confirmed records, statistics, CSV export |
| `/api/rules` | CRUD and activation of email filtering rules |
| `/api/categories` | Category lookup |
| `/api/dashboard` | Overview, spending trends, top merchants |
| `/api/jobs` | Background job status & history |
| `/api/workflow` | Manual vs scheduled automation configuration |
| `/api/webhooks` | Outbound webhook endpoints & delivery history |
| `/api/api-keys` | Programmatic API key management |
| `/api/users` | Current user profile & notification preferences |
| `/api/subscriptions` | Plans, usage, upgrade/cancel, checkout, Stripe webhook |
| `/api/contact` | Public contact form |

Real-time job updates are pushed over the SignalR hub at `/api/hubs/jobs`.

## Authentication

- **JWT bearer** for interactive user sessions (issued via login/refresh/OAuth)
- **API keys** (custom `ApiKey` scheme) for programmatic access, with scope-based policies such as `read:transactions`, `write:transactions`, `read:rules`, etc.
- **Google/Microsoft SSO** for user login, plus separate Gmail/Outlook OAuth for mailbox access
- Passwords hashed with BCrypt; stored OAuth tokens encrypted via ASP.NET Data Protection

## Background jobs

Powered by Hangfire (5 workers, PostgreSQL storage). Jobs include email sync, AI classification, AI extraction, a sequential pipeline (sync → classify → extract), outbound email sending, webhook retries, and log cleanup. User-specific recurring schedules are managed dynamically by the workflow service based on each user's `WorkflowConfiguration`.

## Testing

Integration tests use xUnit, FluentAssertions, and Testcontainers (spins up `postgres:16-alpine`), so **Docker must be running**.

```bash
cd mail-ledger-api/MainLedger
dotnet test MailLedger.IntegrationTests/MailLedger.IntegrationTests.csproj
```

## Related projects

- **`mail-ledger-ui`** — the React frontend that consumes this API.
- **`azure/`** — ARM templates and parameters for deploying the frontend and backend to Azure.
