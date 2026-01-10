---
trigger: always_on
---

MailLedger Backend API - Context Documentation
Last Updated: 2026-01-10

Project Overview
MailLedger is a financial data extraction system that processes emails to extract, classify, and normalize financial transactions. The backend is built as a modular monolith using .NET 8 with Clean Architecture principles.

Technology Stack
Core Framework
.NET 8 (ASP.NET Core Web API)
C# 12
PostgreSQL (Primary database)
Entity Framework Core (ORM)
Key Libraries
MediatR - CQRS pattern implementation
JWT Bearer Authentication - Secure API authentication
log4net - Logging framework
Npgsql - PostgreSQL data provider
Data Protection API - Token encryption
External Integrations
Gmail API - Email ingestion (read-only OAuth)
OpenAI API - Classification and extraction services
Project Structure
The solution follows Clean Architecture with clear separation of concerns:

MainLedger/
├── MainLedger.API/              # Presentation Layer
│   ├── Controllers/             # 10 API controllers
│   ├── Middleware/              # HTTP logging middleware
│   └── Program.cs               # Application entry point
│
├── MainLedger.Application/      # Use Cases & Business Logic
│   ├── Authentication/          # Auth commands & queries
│   ├── Dashboard/               # Dashboard statistics
│   ├── Emails/                  # Email management
│   ├── ExtractionCandidates/    # Candidate processing
│   ├── FinancialRecords/        # Financial record queries
│   ├── Gmail/                   # Gmail integration commands
│   ├── Processing/              # Batch processing jobs
│   ├── Common/                  # Shared interfaces & behaviors
│   └── Services/                # Rules engine & normalization
│
├── MainLedger.Domain/           # Core Business Logic
│   ├── Entities/                # 12 domain entities
│   ├── ValueObjects/            # 9 value objects
│   ├── Enums/                   # 7 enumerations
│   ├── Events/                  # 6 domain events
│   ├── Repositories/            # 13 repository interfaces
│   ├── Services/                # Domain services (hashing, tokens)
│   └── Settings/                # Configuration models
│
├── MainLedger.Infrastructure/   # Technical Implementation
│   ├── Persistence/             # EF Core DbContext & repositories
│   ├── Security/                # JWT, password hashing, encryption
│   └── Migrations/              # Database migrations
│
├── MainLedger.Integrations/     # External Services
│   └── Services/                # Gmail & OpenAI clients
│
├── MainLedger.Contracts/        # API DTOs
│   ├── Authentication/          # Auth request/response models
│   ├── Dashboard/               # Dashboard DTOs
│   ├── Emails/                  # Email DTOs
│   ├── ExtractionCandidates/    # Candidate DTOs
│   ├── FinancialRecords/        # Financial record DTOs
│   ├── Gmail/                   # Gmail sync DTOs
│   ├── Processing/              # Processing job DTOs
│   └── Common/                  # Shared DTOs (pagination, etc.)
│
└── MainLedger.Shared/           # Cross-Cutting Utilities
    └── Result types, error codes, helpers
API Endpoints
Authentication (/api/authentication)
POST /register - User registration
POST /login - User login (returns JWT + refresh token)
POST /refresh-token - Refresh access token
POST /logout - Invalidate refresh token
POST /forgot-password - Request password reset
POST /reset-password - Reset password with token
POST /verify-email - Verify email address
POST /resend-verification - Resend verification email
Dashboard (/api/dashboard)
GET /stats - Get dashboard statistics (period-based)
Emails (/api/emails)
GET / - Get paginated emails with filters
GET /{id} - Get email by ID
Extraction Candidates (/api/extraction-candidates)
GET / - Get paginated candidates with filters
GET /{id} - Get candidate details
POST /{id}/confirm - Confirm candidate → create financial record
POST /{id}/reject - Reject candidate
PUT /{id} - Update candidate before confirmation
POST /bulk-confirm - Bulk confirm multiple candidates
POST /bulk-reject - Bulk reject multiple candidates
Financial Records (/api/financial-records)
GET / - Get paginated financial records with filters
GET /{id} - Get financial record details
Gmail (/api/gmail)
GET /auth-url - Get OAuth authorization URL
POST /callback - Handle OAuth callback
POST /sync - Trigger email sync
GET /connection-status - Check Gmail connection status
Processing (/api/processing)
POST /classify - Trigger classification job
POST /extract - Trigger extraction job
Rules (/api/rules)
GET / - Get all rules
GET /{id} - Get rule by ID
POST / - Create new rule
PUT /{id} - Update rule
DELETE /{id} - Delete rule
PATCH /{id}/toggle - Enable/disable rule
API Keys (/api/api-keys)
GET / - Get user's API keys
POST / - Create new API key
DELETE /{id} - Revoke API key
Users (/api/users)
GET /me - Get current user profile
Core Domain Entities
User - System users with authentication
EmailMessage - Ingested emails (temporary storage)
GmailConnection - OAuth tokens for Gmail access
Rule - User-defined filtering rules
ExtractionCandidate - AI-extracted financial data (pending confirmation)
FinancialRecord - Confirmed financial transactions (immutable)
ExtractionVersion - Versioned extraction prompts
AuditLog - System audit trail
ApiKey - API authentication keys
EmailVerificationToken - Email verification tokens
PasswordResetToken - Password reset tokens
RefreshToken - JWT refresh tokens
Key Enumerations
EmailClassification - Financial, NonFinancial, Unknown
TransactionType - Payment, Transfer, Authorization, Refund, Deposit, Withdrawal
TransactionDirection - In, Out
CandidateStatus - Pending, Confirmed, Rejected
RuleAction - Ignore, Classify, Extract, FlagForReview
RuleConditionType - SenderEquals, SubjectContains, BodyContains, DateRange
Processing Pipeline
The system follows a multi-stage pipeline:

1. Email Ingestion (Gmail API)
   ↓
2. Rules Engine (Filter & Route)
   ↓
3. Classification Service (OpenAI)
   ↓
4. Extraction Service (OpenAI)
   ↓
5. Normalization Layer
   ↓
6. User Confirmation (ExtractionCandidate)
   ↓
7. Financial Record (Immutable)
Authentication & Security
JWT-based authentication with refresh tokens
Password hashing using PBKDF2
Token encryption using Data Protection API
OAuth 2.0 for Gmail integration
CORS configured for frontend origins (localhost:3000, localhost:5173)
API key authentication for external integrations
Database
Provider: PostgreSQL
ORM: Entity Framework Core
Migrations: Code-first approach
Connection: Npgsql with dynamic JSON support
Seeding: Development data seeder
Configuration
Key configuration sections in 
appsettings.json
:

ConnectionStrings:DefaultConnection - PostgreSQL connection
Jwt - JWT settings (issuer, audience, secret key)
OpenAI - OpenAI API configuration
Gmail - Gmail OAuth credentials
Logging - log4net configuration
Development Workflow
Database Migrations:

dotnet ef migrations add MigrationName --project MainLedger.Infrastructure --startup-project MainLedger.API
dotnet ef database update --project MainLedger.Infrastructure --startup-project MainLedger.API
Run API:

dotnet run --project MainLedger.API
Swagger UI: Available at /swagger in development mode

Design Principles
Clean Architecture - Dependency inversion, domain-centric
CQRS - Commands and queries separated via MediatR
Repository Pattern - Data access abstraction
Unit of Work - Transaction management
Immutability - Financial records are immutable once confirmed
Audit Trail - All critical operations logged
Fail-Safe - Classification defaults to "Unknown" on errors
Recent Updates
Bulk Operations: Added bulk confirm/reject endpoints for extraction candidates
Processing Management: Implemented batch processing triggers for classification and extraction
API Keys: Added API key management for external integrations
Email Verification: Implemented email verification flow
Password Reset: Added password reset functionality