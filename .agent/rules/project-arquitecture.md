---
trigger: always_on
---

2️⃣ High-Level Project Structure
src/
 ├── Api/
 ├── Application/
 ├── Domain/
 ├── Infrastructure/
 ├── Integrations/
 ├── Contracts/
 └── Shared/


Each project has a single responsibility.

3️⃣ Layer-by-Layer Breakdown
🟦 1. Api (Presentation Layer)

Purpose:
Expose HTTP endpoints. Nothing else.

Responsibilities:

Controllers / Minimal APIs

Authentication & authorization

Request validation

Rate limiting

API versioning

Error handling

OpenAPI / Swagger

Must NOT:

Contain business logic

Talk directly to DB

Call external services directly

📌 Think: “Dumb edge layer”

🟨 2. Application (Use Cases)

Purpose:
Orchestrate business workflows.

Responsibilities:

Use cases (commands / queries)

Transaction boundaries

Business rules execution

Email parsing workflows

AI extraction coordination

Confirmation flows

Examples:

SyncGmailEmailsCommand

ExtractFinancialDataCommand

ConfirmTransactionCommand

ExportTransactionsQuery

📌 This is the brain of the system.

🟥 3. Domain (Core Business Logic)

Purpose:
Model the problem space.

Responsibilities:

Entities (EmailMessage, FinancialRecord)

Value Objects (Money, Currency)

Domain rules & invariants

Domain events (optional)

Must be:

Framework-agnostic

Database-agnostic

API-agnostic

📌 This should survive any rewrite.

🟩 4. Infrastructure (Persistence & Technical Details)

Purpose:
Handle technical implementations.

Responsibilities:

PostgreSQL repositories

EF Core / Dapper

Migrations

Caching

Encryption

Background jobs

Logging

Implements:

Interfaces defined in Application or Domain

📌 Swappable without breaking business logic.

🟪 5. Integrations (External Services)

Purpose:
Isolate external dependencies.

Responsibilities:

Gmail API client

AI provider (OpenAI, etc.)

OAuth token refresh

Webhooks (future)

Why separate from Infrastructure?

External APIs change frequently

Different failure modes

Easier to mock & replace

📌 Treat external APIs as hostile.

🟧 6. Contracts (Public API Models)

Purpose:
Stable interface for API consumers.

Responsibilities:

Request/response DTOs

API models

Versioned contracts

Important:

Do NOT reuse Domain entities

Breaking changes require versioning

📌 This protects your API consumers.

🟫 7. Shared (Cross-Cutting)

Purpose:
Shared utilities.

Examples:

Result types

Error codes

Pagination

Clock abstraction

Id generators

Rule:

No business logic

No infrastructure dependencies

4️⃣ What NOT To Do (Very Important)

❌ Do not:

Expose Domain entities directly

Put AI prompts in controllers

Put SQL in Application layer

Let Integrations leak into Domain

Share DTOs between UI and Domain

These mistakes kill maintainability.

5️⃣ Example Request Flow
“Sync Gmail & Extract Financial Data”
API
 → Application (SyncEmailsCommand)
   → Integrations (GmailClient)
   → Application (ExtractFinancialData)
     → Integrations (AIClient)
   → Domain (Create FinancialRecord)
   → Infrastructure (Save to DB)
 → API Response


Each layer knows only what it must.

6️⃣ Folder Example (Concrete)
Application/
 ├── Emails/
 │   ├── SyncEmailsCommand.cs
 │   ├── ExtractFinancialDataCommand.cs
 ├── Transactions/
 │   ├── ConfirmTransactionCommand.cs
 │   ├── GetTransactionsQuery.cs