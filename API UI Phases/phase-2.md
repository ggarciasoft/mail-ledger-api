Phase 2: Extraction Candidates API - Implementation Walkthrough
Overview
Successfully implemented Phase 2 - Extraction Candidates API, providing the complete user confirmation workflow for extracted financial data.

Components Implemented
1. DTOs (Contracts Layer) - 4 files
ExtractionCandidateListItemDto.cs
Lightweight DTO for list views with essential fields

ExtractionCandidateDto.cs
Complete DTO with all extraction details and confidence scores

RejectExtractionRequest.cs
Request DTO for rejection with reason

UpdateExtractionRequest.cs
Request DTO for editing candidate fields

2. Repository
IExtractionCandidateRepository.cs
Repository interface with CRUD and pagination methods

ExtractionCandidateRepository.cs
Features:

Pagination with user filtering
Status filtering (Pending, Confirmed, Rejected)
Sorting by amount, merchant, date, createdAt
Includes EmailMessage for user authorization
3. Queries & Handlers
GetExtractionCandidatesQuery.cs
Query for paginated candidate lists

GetExtractionCandidatesQueryHandler.cs
Features:

Fetches email subjects for all candidates
Calculates overall confidence (average of amount, date, merchant)
Maps to list DTOs
GetExtractionCandidateByIdQuery.cs
Query for single candidate retrieval

GetExtractionCandidateByIdQueryHandler.cs
Features:

User authorization check
Returns null if not found or unauthorized
Complete candidate details
4. Commands & Handlers
ConfirmExtractionCandidateCommand.cs
Command to confirm candidate and create FinancialRecord

ConfirmExtractionCandidateCommandHandler.cs
Critical Handler - Creates FinancialRecord:

Validates user authorization
Confirms candidate (updates status)
Creates FinancialRecord from candidate data
Confirms FinancialRecord immediately
Saves both entities
Returns FinancialRecord ID
RejectExtractionCandidateCommand.cs
Command to reject candidate with reason

RejectExtractionCandidateCommandHandler.cs
Features:

User authorization
Status validation
Rejection with reason
UpdateExtractionCandidateCommand.cs
Command to update candidate fields

UpdateExtractionCandidateCommandHandler.cs
Features:

Only allows updates on Pending candidates
Updates transaction data, account info, additional details
Preserves existing values for unspecified fields
5. Controller
ExtractionCandidatesController.cs
5 endpoints for complete candidate management

API Endpoints
1. GET /api/extraction-candidates
Purpose: List extraction candidates with pagination

Query Parameters:

userId (required)
status (optional) - Pending, Confirmed, Rejected
page (default: 1)
pageSize (default: 20, max: 100)
sortBy (default: createdAt)
sortOrder (default: desc)
Example:

GET /api/extraction-candidates?userId={guid}&status=Pending
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
2. GET /api/extraction-candidates/{id}
Purpose: Get detailed candidate information

Example:

GET /api/extraction-candidates/{guid}?userId={guid}
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
  "targetAccount": null,
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
3. POST /api/extraction-candidates/{id}/confirm
Purpose: Confirm extraction and create FinancialRecord

Example:

POST /api/extraction-candidates/{guid}/confirm?userId={guid}
Response:

{
  "success": true,
  "message": "Extraction confirmed",
  "financialRecordId": "guid"
}
What Happens:

Candidate status → Confirmed
FinancialRecord created with same data
FinancialRecord status → Confirmed
Both saved to database
4. POST /api/extraction-candidates/{id}/reject
Purpose: Reject extraction with reason

Request Body:

{
  "reason": "Incorrect amount"
}
Example:

POST /api/extraction-candidates/{guid}/reject?userId={guid}
Response:

{
  "success": true,
  "message": "Extraction rejected"
}
5. PUT /api/extraction-candidates/{id}
Purpose: Edit extraction before confirming

Request Body:

{
  "amount": 155.00,
  "merchant": "Amazon.com",
  "transactionDate": "2026-01-06"
}
Example:

PUT /api/extraction-candidates/{guid}?userId={guid}
Response:

{
  "success": true,
  "message": "Extraction candidate updated"
}
Notes:

Only Pending candidates can be updated
Partial updates supported (only send changed fields)
Preserves existing values for unspecified fields
User Workflow
Typical User Journey
View Pending Candidates

GET /api/extraction-candidates?userId={guid}&status=Pending
Review Candidate Details

GET /api/extraction-candidates/{id}?userId={guid}
User Decision:

Option A: Confirm (Data is correct)

POST /api/extraction-candidates/{id}/confirm?userId={guid}
→ Creates FinancialRecord
Option B: Edit then Confirm (Data needs correction)

PUT /api/extraction-candidates/{id}?userId={guid}
→ Update fields
POST /api/extraction-candidates/{id}/confirm?userId={guid}
→ Creates FinancialRecord with corrected data
Option C: Reject (Data is wrong)

POST /api/extraction-candidates/{id}/reject?userId={guid}
→ Marks as rejected, no FinancialRecord created
Security Features
✅ User Authorization: All endpoints verify user owns the email
✅ Status Validation: Only Pending candidates can be edited/confirmed/rejected
✅ Error Handling: Proper HTTP status codes (404, 403, 400)
✅ Logging: All operations logged with user and candidate IDs

Key Design Decisions
FinancialRecord Creation
When a user confirms an extraction candidate:

A new 
FinancialRecord
 is created with the candidate's data
The FinancialRecord is immediately confirmed (status = Confirmed)
This represents the user's approval of the extracted data
FinancialRecords are immutable once confirmed
Confidence Calculation
Overall confidence = Average of:

Amount confidence
Date confidence
Merchant confidence
Transaction Type Determination
Simplified logic (can be enhanced):

Has merchant → Payment
Has source + target account → Transfer
Default → Payment
Next Steps
Remaining for Phase 2:

 Register 
IExtractionCandidateRepository
 in DI (Program.cs)
 Test all 5 endpoints via Swagger
 Verify FinancialRecord creation on confirmation
Ready for Phase 3:

Financial Records API (view confirmed transactions)
Summary
✅ 5 API endpoints for extraction candidate management
✅ Complete user workflow - view, edit, confirm, reject
✅ FinancialRecord creation on confirmation
✅ User authorization on all operations
✅ Status validation prevents invalid state changes
✅ Partial updates for editing candidates