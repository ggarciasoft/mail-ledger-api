---
trigger: always_on
---

Authentication & Authorization Implementation Phases
Overview
This document outlines the phased implementation of user registration, login, and access control for the Mail Ledger API. The implementation follows clean architecture principles and integrates with the existing modular monolith structure.

PHASE 1 — Foundation & Database Schema (2-3 days)
1.1 Database Schema Design (1 day)
Core Tables
user

id (uuid, PK)
email (varchar, unique, indexed)
password_hash (varchar)
first_name (varchar)
last_name (varchar)
is_email_verified (boolean, default: false)
is_active (boolean, default: true)
created_at (timestamp)
updated_at (timestamp)
last_login_at (timestamp, nullable)
api_key

id (uuid, PK)
user_id (uuid, FK → user.id)
key_hash (varchar, unique, indexed)
name (varchar) — user-friendly name
scopes (jsonb) — array of permissions
is_active (boolean, default: true)
expires_at (timestamp, nullable)
last_used_at (timestamp, nullable)
created_at (timestamp)
refresh_token

id (uuid, PK)
user_id (uuid, FK → user.id)
token_hash (varchar, unique, indexed)
expires_at (timestamp)
is_revoked (boolean, default: false)
created_at (timestamp)
revoked_at (timestamp, nullable)
email_verification_token

id (uuid, PK)
user_id (uuid, FK → user.id)
token_hash (varchar, unique)
expires_at (timestamp)
used_at (timestamp, nullable)
created_at (timestamp)
password_reset_token

id (uuid, PK)
user_id (uuid, FK → user.id)
token_hash (varchar, unique)
expires_at (timestamp)
used_at (timestamp, nullable)
created_at (timestamp)
audit_log (extend existing)

Add fields for authentication events:
event_type (login, logout, registration, password_reset, etc.)
ip_address (varchar)
user_agent (text)
Indexes
user.email (unique)
api_key.key_hash (unique)
api_key.user_id (for lookups)
refresh_token.token_hash (unique)
refresh_token.user_id (for user token management)
1.2 EF Core Entities & Configuration (1 day)
Create entities in Domain layer:

User (aggregate root)
ApiKey (entity)
RefreshToken (entity)
EmailVerificationToken (value object)
PasswordResetToken (value object)
Create EF configurations in Infrastructure:

UserConfiguration
ApiKeyConfiguration
RefreshTokenConfiguration
etc.
1.3 Migration & Seed Data (0.5 day)
Create initial migration
Seed default admin user (for testing)
Seed default scopes/permissions
PHASE 2 — Domain Layer (2-3 days)
2.1 Value Objects (1 day)
Email

Validation logic
Immutable
Equality by value
Password

Hashing logic (BCrypt/Argon2)
Strength validation
Never store plain text
ApiKeyValue

Generation logic (cryptographically secure)
Prefix support (e.g., mlk_live_...)
Masking for display
Scope

Enumeration or string-based
Examples: read:transactions, write:rules, admin:all
2.2 Domain Entities (1 day)
User (Aggregate Root)

Methods:
Register(email, password)
VerifyEmail(token)
ChangePassword(oldPassword, newPassword)
ResetPassword(newPassword)
UpdateProfile(firstName, lastName)
RecordLogin()
Deactivate()
ApiKey (Entity)

Methods:
Create(userId, name, scopes, expiresAt)
Revoke()
RecordUsage()
HasScope(scope)
RefreshToken (Entity)

Methods:
Create(userId, expiresAt)
Revoke()
IsValid()
2.3 Domain Events (0.5 day)
UserRegisteredEvent
EmailVerifiedEvent
PasswordChangedEvent
UserLoggedInEvent
ApiKeyCreatedEvent
ApiKeyRevokedEvent
2.4 Domain Services (0.5 day)
IPasswordHasher (interface in Domain, implementation in Infrastructure)

HashPassword(password)
VerifyPassword(password, hash)
ITokenGenerator (interface in Domain, implementation in Infrastructure)

GenerateEmailVerificationToken()
GeneratePasswordResetToken()
GenerateApiKey()
PHASE 3 — Application Layer (3-4 days)
3.1 Authentication Commands (2 days)
RegisterUserCommand

Input: email, password, firstName, lastName
Output: userId
Logic:
Validate email uniqueness
Hash password
Create user
Generate email verification token
Publish UserRegisteredEvent
LoginCommand

Input: email, password
Output: accessToken, refreshToken
Logic:
Validate credentials
Generate JWT access token
Generate refresh token
Record login
Publish UserLoggedInEvent
RefreshTokenCommand

Input: refreshToken
Output: new accessToken, new refreshToken
Logic:
Validate refresh token
Revoke old token
Generate new tokens
VerifyEmailCommand

Input: token
Output: success/failure
Logic:
Validate token
Mark email as verified
Publish EmailVerifiedEvent
RequestPasswordResetCommand

Input: email
Output: success (always, for security)
Logic:
Find user by email
Generate reset token
Send reset email (future)
ResetPasswordCommand

Input: token, newPassword
Output: success/failure
Logic:
Validate token
Hash new password
Update user
Revoke all refresh tokens
ChangePasswordCommand

Input: userId, oldPassword, newPassword
Output: success/failure
Logic:
Verify old password
Hash new password
Update user
Revoke all refresh tokens
3.2 API Key Commands (1 day)
CreateApiKeyCommand

Input: userId, name, scopes, expiresAt
Output: apiKey (plain text, shown only once)
Logic:
Generate secure key
Hash and store
Return plain key
RevokeApiKeyCommand

Input: userId, apiKeyId
Output: success/failure
ListApiKeysQuery

Input: userId
Output: list of API keys (masked)
3.3 User Queries (0.5 day)
GetUserByIdQuery GetUserByEmailQuery GetCurrentUserQuery

3.4 Services & Interfaces (0.5 day)
IJwtTokenService (interface in Application)

GenerateAccessToken(userId, email, scopes)
ValidateToken(token)
ICurrentUserService (interface in Application)

GetUserId()
GetEmail()
HasScope(scope)
PHASE 4 — Infrastructure Layer (2-3 days)
4.1 Repositories (1 day)
UserRepository

GetByIdAsync(userId)
GetByEmailAsync(email)
AddAsync(user)
UpdateAsync(user)
ApiKeyRepository

GetByKeyHashAsync(keyHash)
GetByUserIdAsync(userId)
AddAsync(apiKey)
UpdateAsync(apiKey)
RefreshTokenRepository

GetByTokenHashAsync(tokenHash)
GetByUserIdAsync(userId)
AddAsync(refreshToken)
RevokeAllByUserIdAsync(userId)
4.2 Security Implementations (1.5 days)
PasswordHasher (using BCrypt or Argon2) TokenGenerator (cryptographically secure random) JwtTokenService

Configure JWT settings (issuer, audience, secret, expiration)
Sign tokens
Validate tokens
4.3 Background Jobs (0.5 day)
CleanupExpiredTokensJob

Run daily
Delete expired refresh tokens
Delete used verification tokens
PHASE 5 — API Layer (2-3 days)
5.1 Contracts (DTOs) (0.5 day)
Request DTOs:

RegisterRequest
LoginRequest
RefreshTokenRequest
VerifyEmailRequest
RequestPasswordResetRequest
ResetPasswordRequest
ChangePasswordRequest
CreateApiKeyRequest
Response DTOs:

AuthenticationResponse (accessToken, refreshToken, expiresIn)
UserResponse
ApiKeyResponse (masked key)
ApiKeyCreatedResponse (plain key, shown once)
5.2 Controllers (1 day)
AuthenticationController

POST /api/v1/auth/register
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/verify-email
POST /api/v1/auth/request-password-reset
POST /api/v1/auth/reset-password
POST /api/v1/auth/change-password
POST /api/v1/auth/logout
ApiKeysController (requires authentication)

POST /api/v1/api-keys
GET /api/v1/api-keys
DELETE /api/v1/api-keys/{id}
UsersController (requires authentication)

GET /api/v1/users/me
PUT /api/v1/users/me
5.3 Middleware & Filters (1 day)
JwtAuthenticationMiddleware

Extract JWT from Authorization header
Validate token
Set user context
ApiKeyAuthenticationMiddleware

Extract API key from header (e.g., X-API-Key)
Validate key
Set user context with scopes
ScopeAuthorizationFilter

Check if user has required scope
Return 403 if unauthorized
5.4 Configuration (0.5 day)
Program.cs updates:

Register authentication services
Configure JWT bearer authentication
Configure API key authentication
Add authorization policies
Register middleware
PHASE 6 — Security & Validation (1-2 days)
6.1 Rate Limiting (0.5 day)
Implement rate limiting for:
Login attempts (prevent brute force)
Registration (prevent spam)
Password reset requests
6.2 Input Validation (0.5 day)
FluentValidation for all commands
Email format validation
Password strength requirements
Sanitize inputs
6.3 Security Headers (0.5 day)
CORS configuration
HTTPS enforcement
Security headers middleware
PHASE 7 — Testing & Verification (2-3 days)
7.1 Unit Tests (1.5 days)
Domain entity tests
Command handler tests
Query handler tests
Service tests
7.2 Integration Tests (1 day)
Authentication flow tests
API key flow tests
Authorization tests
Repository tests
7.3 Manual Testing (0.5 day)
Postman/Swagger testing
Security testing
Edge case validation
PHASE 8 — Documentation & Polish (1 day)
8.1 API Documentation
Update Swagger/OpenAPI specs
Add authentication examples
Document error responses
8.2 Developer Guide
How to register
How to obtain API keys
How to use JWT vs API keys
Scope documentation
Implementation Order Summary
Week 1: Phases 1-2 (Foundation + Domain)
Week 2: Phases 3-4 (Application + Infrastructure)
Week 3: Phases 5-6 (API + Security)
Week 4: Phases 7-8 (Testing + Documentation)
Key Design Decisions
Authentication Strategy
Two authentication methods:

JWT (for UI/SPA)

Short-lived access tokens (15 min)
Long-lived refresh tokens (7 days)
Stored in httpOnly cookies (recommended) or localStorage
API Keys (for integrations)

Long-lived or no expiration
Scope-based permissions
Can be revoked anytime
Authorization Strategy
Scope-based permissions:

read:transactions — Read financial records
write:rules — Create/update rules
read:emails — View email metadata
admin:all — Full access
Password Security
Hashing: BCrypt or Argon2id
Minimum requirements:
8+ characters
Mix of uppercase, lowercase, numbers
Special characters recommended
Token Security
Access tokens: 15 minutes expiration
Refresh tokens: 7 days, stored hashed
Email verification: 24 hours expiration
Password reset: 1 hour expiration
Security Checklist
 Passwords are hashed (never stored plain)
 Tokens are cryptographically secure
 API keys are hashed in database
 Rate limiting on sensitive endpoints
 HTTPS enforced
 CORS properly configured
 SQL injection prevented (parameterized queries)
 XSS prevented (input sanitization)
 CSRF tokens (if using cookies)
 Audit logging for all auth events
 Email verification required
 Account lockout after failed attempts
 Secure password reset flow
Future Enhancements (Post-MVP)
 Two-factor authentication (2FA)
 OAuth2 providers (Google, GitHub)
 Session management UI
 IP whitelisting for API keys
 Webhook signing with API keys
 Role-based access control (RBAC)
 Multi-tenancy support
 SSO/SAML support
