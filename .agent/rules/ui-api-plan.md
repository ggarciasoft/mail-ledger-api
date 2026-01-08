---
trigger: manual
---

UI-Facing API Layer - Implementation Plan
Overview
Build a comprehensive read-only API layer for the UI to display user data, processing status, and financial information. This follows the CQRS pattern with query-focused endpoints optimized for UI consumption.

Phase 1: Email Management API
Purpose
Allow users to view their synced emails, processing status, and classification results.

Endpoints
GET /api/emails
Query Parameters:

userId (required)
status (optional): Pending, Classified, Extracted, Failed
isFinancial (optional): true/false
page (default: 1)
pageSize (default: 20)
sortBy (default: receivedAt)
sortOrder (default: desc)
Response:

{
  "items": [
    {
      "id": "guid",
      "subject": "Your payment confirmation",
      "from": "payments@example.com",
      "receivedAt": "2026-01-07T12:00:00Z",
      "processingStatus": "Extracted",
      "isFinancial": true,
      "category": "Payment",
      "classificationConfidence": 0.95,
      "hasExtractionCandidate": true
    }
  ],
  "totalCount": 150,
  "page": 1,
  "pageSize": 20,
  "totalPages": 8
}
GET /api/emails/{id}
Response:

{
  "id": "guid",
  "messageId": "gmail-message-id",
  "subject": "Your payment confirmation",
  "from": "payments@example.com",
  "receivedAt": "2026-01-07T12:00:00Z",
  "bodyText": "Full email body...",
  "processingStatus": "Extracted",
  "processingError": null,
  "directive": "Extract",
  "directiveReason": "Matched financial rule",
  "isFinancial": true,
  "category": "Payment",
  "classificationConfidence": 0.95,
  "classifiedAt": "2026-01-07T12:01:00Z",
  "createdAt": "2026-01-07T12:00:30Z"
}
GET /api/emails/statistics
Query Parameters:

userId (required)
Response:

{
  "totalEmails": 500,
  "pending": 50,
  "classified": 30,
  "extracted": 400,
  "failed": 20,
  "financialEmails": 350,
  "nonFinancialEmails": 130,
  "lastSyncAt": "2026-01-07T12:00:00Z"
}
Phase 2: Extraction Candidates API
Purpose
Display extracted financial data awaiting user confirmation.

Endpoints
GET /api/extraction-candidates
Query Parameters:

userId (required)
status (optional): Pending, Confirmed, Rejected
page (default: 1)
pageSize (default: 20)
sortBy (default: createdAt)
sortOrder (default: desc)
Response:

{
  "items": [
    {
      "id": "guid",
      "emailId": "guid",
      "emailSubject": "Payment confirmation",
      "amount": 150.00,
      "currency": "USD",
      "merchant": "Amazon",
      "transactionDate": "2026-01-07",
      "sourceAccount": "***1234",
      "sourceBank": "BHD",
      "status": "Pending",
      "confidence": 0.92,
      "createdAt": "2026-01-07T12:00:00Z"
    }
  ],
  "totalCount": 45,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3
}
GET /api/extraction-candidates/{id}
Response:

{
  "id": "guid",
  "emailId": "guid",
  "emailSubject": "Payment confirmation",
  "amount": 150.00,
  "currency": "USD",
  "merchant": "Amazon",
  "transactionDate": "2026-01-07",
  "sourceAccount": "***1234",
  "targetAccount": "***5678",
  "sourceBank": "BHD",
  "targetBank": null,
  "fees": 2.50,
  "tax": 15.00,
  "referenceId": "TXN-123456",
  "amountConfidence": 0.95,
  "dateConfidence": 0.90,
  "merchantConfidence": 0.88,
  "status": "Pending",
  "createdAt": "2026-01-07T12:00:00Z",
  "confirmedAt": null,
  "rejectionReason": null
}
POST /api/extraction-candidates/{id}/confirm
Purpose: User confirms the extraction is correct

Response:

{
  "success": true,
  "message": "Extraction confirmed",
  "financialRecordId": "guid"
}
POST /api/extraction-candidates/{id}/reject
Request Body:

{
  "reason": "Incorrect amount"
}
Response:

{
  "success": true,
  "message": "Extraction rejected"
}
PUT /api/extraction-candidates/{id}
Purpose: User edits extraction before confirming

Request Body:

{
  "amount": 155.00,
  "merchant": "Amazon.com",
  "transactionDate": "2026-01-06"
}
Phase 3: Financial Records API
Purpose
Display confirmed financial transactions (the final, trusted data).

Endpoints
GET /api/financial-records
Query Parameters:

userId (required)
startDate (optional)
endDate (optional)
minAmount (optional)
maxAmount (optional)
merchant (optional): search term
currency (optional)
sourceBank (optional)
page (default: 1)
pageSize (default: 20)
Response:

{
  "items": [
    {
      "id": "guid",
      "type": "Payment",
      "amount": 150.00,
      "currency": "USD",
      "merchant": "Amazon",
      "transactionDate": "2026-01-07",
      "sourceAccount": "***1234",
      "sourceBank": "BHD",
      "confirmedAt": "2026-01-07T13:00:00Z"
    }
  ],
  "totalCount": 320,
  "totalAmount": 45000.00,
  "page": 1,
  "pageSize": 20,
  "totalPages": 16
}
GET /api/financial-records/{id}
Full details of a confirmed financial record

GET /api/financial-records/statistics
Query Parameters:

userId (required)
startDate (optional)
endDate (optional)
Response:

{
  "totalRecords": 320,
  "totalAmount": 45000.00,
  "currency": "USD",
  "averageAmount": 140.62,
  "byCategory": {
    "Payment": 200,
    "Transfer": 80,
    "Refund": 40
  },
  "byBank": {
    "BHD": 150,
    "Popular": 100,
    "Banreservas": 70
  },
  "monthlyTrend": [
    { "month": "2025-12", "count": 85, "total": 12000.00 },
    { "month": "2026-01", "count": 95, "total": 13500.00 }
  ]
}
Phase 4: Dashboard & Analytics API
Purpose
Provide aggregated data for dashboard widgets and charts.

Endpoints
GET /api/dashboard/overview
Query Parameters:

userId (required)
Response:

{
  "totalEmails": 500,
  "pendingClassification": 50,
  "pendingExtraction": 30,
  "pendingConfirmation": 45,
  "confirmedRecords": 320,
  "failedProcessing": 20,
  "lastSyncAt": "2026-01-07T12:00:00Z",
  "recentActivity": [
    {
      "type": "EmailSynced",
      "count": 25,
      "timestamp": "2026-01-07T12:00:00Z"
    },
    {
      "type": "ExtractionConfirmed",
      "count": 5,
      "timestamp": "2026-01-07T11:30:00Z"
    }
  ]
}
GET /api/dashboard/spending-trends
Query Parameters:

userId (required)
period (optional): week, month, year
groupBy (optional): day, week, month
Response:

{
  "period": "month",
  "data": [
    {
      "date": "2026-01-01",
      "totalSpent": 1500.00,
      "transactionCount": 12
    },
    {
      "date": "2026-01-02",
      "totalSpent": 850.00,
      "transactionCount": 8
    }
  ],
  "totalSpent": 13500.00,
  "averageDaily": 450.00
}
GET /api/dashboard/top-merchants
Query Parameters:

userId (required)
limit (default: 10)
startDate (optional)
endDate (optional)
Response:

{
  "merchants": [
    {
      "name": "Amazon",
      "totalSpent": 3500.00,
      "transactionCount": 25,
      "percentage": 25.9
    },
    {
      "name": "Uber",
      "totalSpent": 1200.00,
      "transactionCount": 45,
      "percentage": 8.9
    }
  ]
}
Phase 5: Processing Management API
Purpose
Allow users to trigger and monitor batch processing jobs.

Endpoints
GET /api/processing/status
Query Parameters:

userId (required)
Response:

{
  "pendingClassification": 50,
  "pendingExtraction": 30,
  "canClassify": true,
  "canExtract": true,
  "lastClassificationJob": {
    "startedAt": "2026-01-07T11:00:00Z",
    "completedAt": "2026-01-07T11:00:30Z",
    "processed": 20,
    "succeeded": 18,
    "failed": 2
  },
  "lastExtractionJob": {
    "startedAt": "2026-01-07T11:05:00Z",
    "completedAt": "2026-01-07T11:06:00Z",
    "processed": 15,
    "succeeded": 14,
    "failed": 1
  }
}
POST /api/processing/classify
Trigger batch classification

POST /api/processing/extract
Trigger batch extraction

Phase 6: User Settings & Rules API
Purpose
Manage user preferences and filtering rules.

Endpoints
GET /api/rules
List user's email filtering rules

POST /api/rules
Create new rule

PUT /api/rules/{id}
Update existing rule

DELETE /api/rules/{id}
Delete rule

GET /api/settings
Get user preferences

PUT /api/settings
Update user preferences

Implementation Architecture
CQRS Pattern
Commands (already implemented):

SyncGmailEmailsCommand
BatchClassifyEmailsCommand
BatchExtractFinancialDataCommand
ConfirmExtractionCommand (to create)
RejectExtractionCommand (to create)
Queries (to create):

GetEmailsQuery
GetEmailByIdQuery
GetEmailStatisticsQuery
GetExtractionCandidatesQuery
GetExtractionCandidateByIdQuery
GetFinancialRecordsQuery
GetFinancialRecordByIdQuery
GetDashboardOverviewQuery
GetSpendingTrendsQuery
GetTopMerchantsQuery
DTOs (Data Transfer Objects)
Create response models in MainLedger.Contracts:

EmailDto
EmailListDto
ExtractionCandidateDto
FinancialRecordDto
DashboardOverviewDto
StatisticsDto
PaginatedResponse<T>
Controllers
EmailsController - Email viewing
ExtractionCandidatesController - Candidate management
FinancialRecordsController - Confirmed records
DashboardController - Analytics & overview
ProcessingController - Job management
RulesController - Rule management (already exists)
Database Optimizations
Indexes to Add
-- Email queries
CREATE INDEX ix_emails_user_status ON email_messages(user_id, processing_status);
CREATE INDEX ix_emails_user_financial ON email_messages(user_id, is_financial);
CREATE INDEX ix_emails_received_at ON email_messages(received_at DESC);
-- Extraction candidates
CREATE INDEX ix_candidates_user_status ON extraction_candidates(user_id, status);
CREATE INDEX ix_candidates_created_at ON extraction_candidates(created_at DESC);
-- Financial records
CREATE INDEX ix_records_user_date ON financial_records(user_id, transaction_date DESC);
CREATE INDEX ix_records_merchant ON financial_records(merchant);
CREATE INDEX ix_records_amount ON financial_records(amount);
Security Considerations
Authentication: All endpoints require authenticated user
Authorization: Users can only access their own data
Rate Limiting: Prevent API abuse
Input Validation: Validate all query parameters
Pagination Limits: Max pageSize = 100
Next Steps
Phase 1: Implement Email Management API (highest priority for UI)
Phase 2: Implement Extraction Candidates API (user confirmation flow)
Phase 3: Implement Financial Records API (final data display)
Phase 4: Implement Dashboard API (analytics)
Phase 5: Implement Processing Management API (job control)
Phase 6: Enhance Rules API (user preferences)
Each phase builds on the previous, allowing incremental UI development.