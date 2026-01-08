---
trigger: always_on
---

React UI Implementation Plan - MailLedger

Overview

Build a modern React UI for MailLedger that consumes the 17 API endpoints we've created. The UI will provide a complete workflow from email sync to confirmed financial records.



Technology Stack

Core

React 18 with TypeScript

Vite for build tooling

React Router for navigation

State Management

TanStack Query (React Query) for server state

Zustand for client state (auth, UI preferences)

UI Components

shadcn/ui (Radix UI + Tailwind CSS)

Recharts for data visualization

Lucide React for icons

Forms \& Validation

React Hook Form for form management

Zod for schema validation

Phase 1: Dashboard \& Overview

Purpose

Main landing page showing key metrics and recent activity.



Components

DashboardPage

Overview cards (total emails, pending items, confirmed records)

Spending trends chart (line/bar chart)

Top merchants chart (pie/bar chart)

Recent activity feed

Quick action buttons

API Integration

GET /api/dashboard/overview

GET /api/dashboard/spending-trends

GET /api/dashboard/top-merchants

Features

✅ Real-time metrics

✅ Interactive charts (Recharts)

✅ Period selector (week/month/year)

✅ Quick navigation to pending items

Phase 2: Email Management

Purpose

View and manage synced emails from Gmail.



Components

EmailsPage

Email list with filters (status, financial/non-financial)

Search by subject/sender

Pagination

Status badges (Pending, Classified, Extracted, Failed)

EmailDetailModal

Full email content

Processing status

Classification results

Link to extraction candidate (if exists)

API Integration

GET /api/emails

GET /api/emails/{id}

GET /api/emails/statistics

Features

✅ Advanced filtering

✅ Sort by date, status

✅ Email preview

✅ Status indicators

✅ Error details for failed emails

Phase 3: Extraction Candidates Workflow

Purpose

Review, edit, and confirm AI-extracted financial data.



Components

ExtractionCandidatesPage

Candidate list with confidence scores

Filter by status (Pending, Confirmed, Rejected)

Sort by date, amount, confidence

Batch actions (future)

CandidateReviewModal

Side-by-side view (email + extracted data)

Editable fields (amount, merchant, date, etc.)

Confidence indicators per field

Action buttons (Confirm, Reject, Edit)

ConfirmDialog

Final confirmation before creating financial record

Summary of data to be confirmed

RejectDialog

Rejection reason input

Reason templates (incorrect amount, wrong merchant, etc.)

API Integration

GET /api/extraction-candidates

GET /api/extraction-candidates/{id}

POST /api/extraction-candidates/{id}/confirm

POST /api/extraction-candidates/{id}/reject

PUT /api/extraction-candidates/{id}

Features

✅ Inline editing

✅ Confidence visualization

✅ Email preview alongside data

✅ Validation before confirm

✅ Undo reject (future)

Phase 4: Financial Records

Purpose

View confirmed financial transactions (the source of truth).



Components

FinancialRecordsPage

Transaction list with advanced filters

Date range picker

Amount range filter

Merchant search

Bank filter

Export to CSV (future)

RecordDetailModal

Complete transaction details

Link to source email

Confidence score

Extraction version

Timestamps (created, confirmed)

RecordStatisticsPanel

Total records

Total amount

Average amount

Breakdown by type

Breakdown by bank

Monthly trend chart

API Integration

GET /api/financial-records

GET /api/financial-records/{id}

GET /api/financial-records/statistics

Features

✅ Advanced filtering (8 parameters)

✅ Sorting (amount, merchant, type, date)

✅ Statistics dashboard

✅ Monthly trends visualization

✅ Read-only (immutable data)

Phase 5: Processing Management

Purpose

Trigger and monitor batch AI processing jobs.



Components

ProcessingPage

Status cards (pending classification, pending extraction)

Trigger buttons (Classify, Extract)

Batch size selector

Job history (future)

Progress indicators

TriggerJobDialog

Batch size input

Confirmation

Progress bar during execution

Results summary (succeeded/failed counts)

API Integration

GET /api/processing/status

POST /api/processing/classify

POST /api/processing/extract

Features

✅ One-click job triggers

✅ Configurable batch size

✅ Real-time progress (polling)

✅ Success/failure reporting

✅ Auto-refresh after job completion

Phase 6: Settings \& Configuration

Purpose

Manage user preferences and Gmail connection.



Components

SettingsPage

Gmail connection status

Last sync timestamp

Sync button

User preferences (future)

Rules management (future)

GmailSyncDialog

Sync progress

Emails synced count

Error handling

API Integration

POST /api/gmail/sync (existing)

GET /api/processing/status (for sync status)

Features

✅ Manual sync trigger

✅ Connection status

✅ Sync history

✅ OAuth re-authentication (future)

Shared Components

Layout

AppLayout - Main layout with sidebar navigation

Sidebar - Navigation menu

Header - User menu, notifications

Breadcrumbs - Page navigation

Data Display

DataTable - Reusable table with sorting, filtering, pagination

StatCard - Metric display card

StatusBadge - Status indicator

ConfidenceMeter - Visual confidence indicator

EmptyState - No data placeholder

Forms

FormField - Reusable form field wrapper

DateRangePicker - Date range selector

AmountInput - Currency input

SearchInput - Search with debounce

Charts

TrendChart - Line/area chart for trends

PieChart - Merchant distribution

BarChart - Category breakdown

Routing Structure

/                          → Dashboard

/emails                    → Email list

/emails/:id                → Email detail (modal)

/extraction-candidates     → Candidate list

/extraction-candidates/:id → Candidate review (modal)

/financial-records         → Records list

/financial-records/:id     → Record detail (modal)

/processing                → Processing management

/settings                  → Settings \& sync

State Management Strategy

Server State (React Query)

All API data

Automatic caching

Background refetching

Optimistic updates

Client State (Zustand)

User authentication

UI preferences (theme, sidebar state)

Filter states (persist across navigation)

API Client Setup

Base Configuration

const apiClient = axios.create({

&nbsp; baseURL: import.meta.env.VITE\_API\_URL,

&nbsp; headers: {

&nbsp;   'Content-Type': 'application/json'

&nbsp; }

});

// Add auth token interceptor

apiClient.interceptors.request.use((config) => {

&nbsp; const token = getAuthToken();

&nbsp; if (token) {

&nbsp;   config.headers.Authorization = `Bearer ${token}`;

&nbsp; }

&nbsp; return config;

});

React Query Hooks

// Example hooks

useEmails(filters)

useEmail(id)

useExtractionCandidates(filters)

useConfirmCandidate()

useFinancialRecords(filters)

useDashboardOverview()

useProcessingStatus()

useTriggerClassification()

Design System

Colors

Primary: Blue (trust, finance)

Success: Green (confirmed)

Warning: Yellow (pending review)

Danger: Red (rejected, failed)

Info: Purple (classified)

Typography

Font: Inter (clean, modern)

Headings: Bold, larger sizes

Body: Regular weight

Mono: Fira Code (for IDs, amounts)

Spacing

Consistent 4px grid

Card padding: 24px

Section gaps: 32px

Implementation Order

Week 1: Foundation

Project setup (Vite + React + TypeScript)

Install dependencies (shadcn/ui, React Query, etc.)

API client setup

Authentication flow

Layout components (Sidebar, Header)

Week 2: Dashboard \& Email Management

Dashboard page with charts

Email list page

Email detail modal

Statistics panels

Week 3: Extraction Workflow

Extraction candidates list

Candidate review modal

Edit functionality

Confirm/Reject dialogs

Week 4: Financial Records

Financial records list

Record detail modal

Advanced filtering

Statistics dashboard

Week 5: Processing \& Settings

Processing management page

Job trigger dialogs

Settings page

Gmail sync integration

Week 6: Polish \& Testing

Error handling

Loading states

Empty states

Responsive design

E2E testing

Key Features Summary

✅ 17 API endpoints fully integrated

✅ Modern React with TypeScript

✅ Real-time updates via React Query

✅ Interactive charts for data visualization

✅ Complete workflow from email to confirmed record

✅ Advanced filtering on all list views

✅ Inline editing for extraction candidates

✅ Batch processing controls

✅ Responsive design for all screen sizes

✅ Accessible (WCAG 2.1 AA)



Next Steps

Create React project with Vite

Set up shadcn/ui and Tailwind

Implement authentication (OAuth flow)

Build layout components

Start with Dashboard (Phase 1)

Iterate through phases 2-6

The UI will provide a seamless experience for users to manage their email-derived financial data!

