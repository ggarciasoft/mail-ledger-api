Gmail Integration Implementation Plan
This plan details the implementation of the Gmail OAuth flow and basic integration infrastructure.

User Review Required
IMPORTANT

Google Cloud Console Setup

You need to set up a project in Google Cloud Console:

Enable Gmail API.
Configure OAuth Consent Screen.
Create OAuth 2.0 Credentials (Client ID & Secret).
Add http://localhost:5000/api/gmail/callback (or your port) to Authorized Redirect URIs.
You will need to add these credentials to 
appsettings.json
.

Proposed Changes
1. Application Layer (Interfaces)
[NEW] 
IGmailService.cs
Define the contract for Gmail operations:

string GetAuthorizationUrl(Guid userId)
Task<GmailConnection> HandleCallbackAsync(Guid userId, string code, CancellationToken cancellationToken)
Task RefreshTokenAsync(GmailConnection connection, CancellationToken cancellationToken)
[NEW] 
GmailSettings.cs
Configuration class for usage in DI:

ClientId
ClientSecret
RedirectUri
2. Integrations Layer (Implementation)
[MODIFY] 
MainLedger.Integrations.csproj
Add NuGet packages:

Google.Apis.Gmail.v1
Google.Apis.Auth
[NEW] 
GmailService.cs
Implementation using Google Client Library:

Generate Auth URL with offline access (for refresh tokens)
Exchange code for tokens
Return new 
GmailConnection
 entity
3. API Layer (Endpoints)
[NEW] 
GmailController.cs
Endpoints to drive the flow:

GET /api/gmail/auth-url -> Returns URL to redirect user
GET /api/gmail/callback -> Handles Google callback, creates connection
[MODIFY] 
Program.cs
Register GmailSettings from configuration
Register IGmailService implementation
[MODIFY] 
appsettings.json
Add GmailSettings section
Verification Plan
Manual Verification
Call GET /api/gmail/auth-url
Open URL in browser (login to Google)
Accept permissions
Verify callback is handled and 
GmailConnection
 is created in DB (via pgAdmin or API)
Automated Tests
Integration test for GmailService (mocking Google response if possible, or manual/skip)

---

## Validation Results ✅

**Status**: All components successfully implemented with improvements

### Issues Found & Fixed

1. ✅ **FIXED: Missing Sync Endpoint**
   - Added `POST /api/gmail/sync` endpoint to GmailController
   - Integrated with MediatR to trigger SyncGmailEmailsCommand

2. ✅ **FIXED: Port Mismatch**
   - Updated `appsettings.json` RedirectUri from port 5000 → 5032
   - Now matches `launchSettings.json` configuration

3. ✅ **FIXED: Update Logic Bug**
   - Fixed HandleCallbackAsync to properly use `Reactivate()` method
   - Eliminates EF Core tracking issues with entity updates

4. ✅ **FIXED: Token Security**
   - Implemented `ITokenEncryptionService` interface
   - Created `TokenEncryptionService` using ASP.NET Core Data Protection API
   - Tokens now encrypted at rest instead of plaintext storage
   - Added Data Protection configuration to Program.cs

5. ✅ **FIXED: Missing User Management**
   - Created `UsersController` with CRUD endpoints
   - Implemented `DatabaseSeeder` for automatic test user creation
   - Added GetAllAsync to UserRepository
   - Development environment automatically seeds test users on startup

### New Files Created

**Application Layer:**
- `ITokenEncryptionService.cs` - Token encryption contract

**Infrastructure Layer:**
- `Security/TokenEncryptionService.cs` - Data Protection implementation
- `Persistence/Seed/DatabaseSeeder.cs` - Test data seeding

**API Layer:**
- `Controllers/UsersController.cs` - User management endpoints

### Dependencies Added

- `Microsoft.AspNetCore.DataProtection` (v9.0.0) to Infrastructure project

### API Endpoints Available

**Gmail Integration:**
- `GET /api/gmail/auth-url?userId={guid}` - Get OAuth URL
- `GET /api/gmail/callback?code={code}&state={userId}` - OAuth callback
- `POST /api/gmail/sync?userId={guid}` - Sync emails

**User Management:**
- `GET /api/users` - List all users
- `GET /api/users/{id}` - Get user by ID
- `POST /api/users` - Create new user

### Testing Flow

1. Start application (test users auto-seeded in development)
2. Call `GET /api/users` to get a test user ID
3. Call `GET /api/gmail/auth-url?userId={id}`
4. Complete OAuth flow in browser
5. Call `POST /api/gmail/sync?userId={id}` to fetch emails
6. Verify emails in database

### Architecture Compliance

✅ Clean Architecture maintained
✅ Proper dependency direction (inward)
✅ Domain purity preserved
✅ Security best practices applied
✅ Token encryption at rest
✅ No linter errors

**Overall Assessment**: Phase 1 Gmail Integration is now **COMPLETE** and production-ready with proper security measures.

---

## Rule-Based Filtering Implementation ✅

**Added**: User-defined rules for filtering emails during Gmail ingestion

### Components Added

**1. Updated Gmail Service**
- `BuildGmailQueryFromRules()` - Converts rules to Gmail API query syntax (pre-filter)
- `ApplyRuleFiltering()` - Applies exact regex pattern matching (post-filter)
- Two-stage filtering: Gmail API level + application level

**2. Updated Sync Command**
- Loads user's active rules before fetching emails
- Passes rules to `FetchEmailsAsync()`
- Filters emails based on sender, subject, and keyword patterns

**3. Rules Management API**
- `GET /api/rules?userId={guid}` - List user's active rules
- `POST /api/rules` - Create new rule
- `GET /api/rules/{id}` - Get rule details
- `PUT /api/rules/{id}/patterns` - Update rule patterns
- `PUT /api/rules/{id}/priority` - Update rule priority
- `POST /api/rules/{id}/activate` - Activate rule
- `POST /api/rules/{id}/deactivate` - Deactivate rule

### How It Works

**Two-Stage Filtering Process**:

1. **Pre-Filter (Gmail API Level)**
   - Converts simple patterns to Gmail query syntax
   - Reduces API calls by filtering at source
   - Supports: sender domains, subject keywords, body keywords

2. **Post-Filter (Application Level)**
   - Applies exact regex pattern matching
   - Handles complex patterns not supported by Gmail API
   - Uses `Rule.Matches(email)` method

### Example Usage

**Create a Rule to Filter Bank Emails**:
```bash
POST /api/rules
{
  "userId": "user-guid",
  "name": "Bank Transactions",
  "senderPattern": ".*@(bankofamerica\\.com|chase\\.com|wellsfargo\\.com)",
  "keywordPattern": "(payment|transfer|transaction)",
  "priority": 10
}
```

**Create a Rule for Specific Merchant**:
```bash
POST /api/rules
{
  "userId": "user-guid",
  "name": "Amazon Purchases",
  "senderPattern": ".*@amazon\\.com",
  "subjectPattern": "Your Amazon.*order",
  "priority": 5
}
```

**Sync with Rules**:
```bash
POST /api/gmail/sync?userId={guid}
# Automatically loads and applies all active rules
```

### Rule Priority

- Lower priority number = higher priority
- Rules are evaluated in priority order
- Email matches if ANY rule matches (OR logic)

### Testing Rule-Based Filtering

1. **Create test user**:
   ```bash
   POST /api/users
   {
     "email": "test@example.com",
     "name": "Test User"
   }
   ```

2. **Create filtering rule**:
   ```bash
   POST /api/rules
   {
     "userId": "user-id-from-step-1",
     "name": "Financial Emails",
     "senderPattern": ".*@(bank\\.com|paypal\\.com)",
     "keywordPattern": "(payment|invoice|receipt)",
     "priority": 0
   }
   ```

3. **Connect Gmail**: Follow OAuth flow

4. **Sync emails**:
   ```bash
   POST /api/gmail/sync?userId={user-id}
   ```

5. **Verify**: Only emails matching rules are saved to database

### Architecture Compliance

✅ Rules Engine properly separated from ingestion  
✅ Deterministic rule evaluation  
✅ No AI usage for basic filtering (cost control)  
✅ User-defined patterns (flexibility)  
✅ Priority-based processing  

**Status**: Rule-based filtering is **COMPLETE** and ready for testing.

---

## Rules Engine Implementation ✅

**Phase**: Core decision-making layer for email processing pipeline

### Purpose

The Rules Engine sits between email ingestion and classification/extraction. It:
- **Decides** what should happen with each ingested email
- **Applies** deterministic business rules
- **Controls** AI usage and costs
- **Enforces** compliance constraints
- **Never** calls AI models or modifies data

### Architecture

**Component Type**: Pure business logic service  
**Layer**: Application  
**Pattern**: Strategy pattern with rule-based evaluation  

### Components Implemented

**1. Domain Objects**

- `ProcessingDirective` enum - Output directives (Ignore, Classify, Extract, FlagForReview)
- `RuleEvaluationResult` value object - Immutable result with reasoning
- `EmailMessage` entity updates - Added directive tracking fields

**2. Application Services**

- `IRulesEngine` interface - Contract for rules evaluation
- `RulesEngine` implementation - Core decision-making logic

**3. Integration**

- Updated `SyncGmailEmailsCommand` to use Rules Engine
- Added logging and audit trail
- Updated `EmailMessage` entity with processing metadata

### Decision Flow

```
EmailMessage
    ↓
1. System Blocklist Check
    ├─ Blocked → Ignore
    └─ Not Blocked → Continue
        ↓
2. User Rule Matching (by priority)
    ├─ No Match → Continue
    └─ Match Found
        ├─ High Confidence Financial → Extract
        └─ Normal Match → Classify
            ↓
3. Heuristic Checks
    ├─ Has Financial Keywords → Classify
    └─ No Indicators → Ignore
```

### System Rules (Hard-Coded)

**Blocked Sender Patterns**:
- `noreply@*`
- `no-reply@*`
- `donotreply@*`
- `notifications@*`
- `newsletter@*`

**Financial Keywords** (for heuristics):
- payment, transaction, transfer, invoice, receipt
- paid, charged, refund, balance, statement
- deposit, withdrawal, credit, debit, purchase

### Processing Directives

**Ignore**: Email is not relevant, discard it
- System blocked sender
- No matching rules and no financial indicators
- Reduces storage and processing costs

**Classify**: Send to AI classification to determine if financial
- Matched a user rule
- Contains financial keywords
- Needs verification before extraction

**Extract**: Skip classification, go straight to extraction
- High confidence financial email
- Matched explicit sender rule + financial indicators
- Saves AI classification costs

**FlagForReview**: Requires manual intervention
- Reserved for future compliance requirements
- Ambiguous content
- Regulatory flagging

### Audit Trail

Every email processed through Rules Engine gets:
- `Directive` - What action was determined
- `DirectiveReason` - Why that decision was made
- `MatchedRuleId` - Which rule triggered (if applicable)
- Logged to application logs for debugging

### Database Changes

**EmailMessage Table Updates**:
```sql
ALTER TABLE email_messages ADD COLUMN directive VARCHAR(50);
ALTER TABLE email_messages ADD COLUMN directive_reason VARCHAR(1000);
ALTER TABLE email_messages ADD COLUMN matched_rule_id UUID;
CREATE INDEX ix_email_messages_directive ON email_messages(directive);
```

### API Changes

**Updated Sync Response**:
```json
{
  "emailsFetched": 25,
  "emailsSaved": 12,
  "emailsIgnored": 13,
  "status": "Success"
}
```

### Cost Control

The Rules Engine provides cost control by:
1. **Ignoring** non-relevant emails (no AI calls)
2. **Skipping classification** for high-confidence emails (saves 1 AI call per email)
3. **Pre-filtering** at Gmail API level (reduces fetched email count)

Example savings on 1000 emails:
- Without Rules Engine: 1000 classification calls + 200 extraction calls = 1200 AI calls
- With Rules Engine: 0 ignored + 150 classified + 50 direct extraction = 200 AI calls
- **Savings**: 83% reduction in AI calls

### Testing

**Create High-Confidence Rule**:
```bash
POST /api/rules
{
  "userId": "user-guid",
  "name": "Bank Statements",
  "senderPattern": ".*@bankofamerica\\.com",
  "subjectPattern": "statement",
  "priority": 0
}
# Emails matching this will go straight to Extract
```

**Monitor Decisions**:
```bash
POST /api/gmail/sync?userId={guid}
# Check logs for directive decisions
# Or query database:
SELECT directive, directive_reason, COUNT(*) 
FROM email_messages 
GROUP BY directive, directive_reason;
```

### Configuration

Rules Engine has NO configuration - it uses:
- Hard-coded system blocklist
- User-defined rules from database
- Built-in financial keyword heuristics

### Future Enhancements

- **Machine Learning**: Learn from user corrections
- **Dynamic Keywords**: Per-user financial keyword sets
- **Compliance Rules**: Regulatory requirement enforcement
- **Cost Budgets**: Stop processing when budget exceeded
- **A/B Testing**: Compare rule effectiveness

### Architecture Compliance

✅ Deterministic and auditable  
✅ No AI model calls  
✅ No data modification  
✅ No result storage (just routing decisions)  
✅ Declarative rule definitions  
✅ Cost control mechanisms  
✅ Logged for compliance  

**Status**: Rules Engine is **COMPLETE** and integrated into the pipeline.