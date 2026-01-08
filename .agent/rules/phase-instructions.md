---
trigger: manual
---

\# PHASE 1 — CORE PIPELINE (Weeks 1–3)



\*\*Goal:\*\* Convert Gmail → candidate financial emails → structured data.



---



\## 1.1 System Architecture (1 day)



\### Define these components



\- Email Ingestion Service

\- Rules Engine

\- Classification Service

\- Extraction Service

\- Normalization Layer

\- Confirmation Workflow



📌 Document the flow (diagram).



---



\## 1.2 Database Schema (2 days)



\### Tables (Minimum)



\- `user`

\- `gmail\_connection`

\- `raw\_email` (encrypted, temporary)

\- `rule`

\- `financial\_event`

\- `extraction\_version`

\- `audit\_log`



\### Must-have fields



\- Confidence score

\- Status (`pending`, `confirmed`, `rejected`)

\- Source hash (deduplication)



Lock schema early.



---



\## 1.3 Gmail Ingestion (3–4 days)



\### Steps



1\. OAuth login

2\. Store refresh token encrypted

3\. Fetch emails:

&nbsp;   - Use `historyId`

&nbsp;   - Only new emails

4\. Filter by:

&nbsp;   - User rules

&nbsp;   - Sender

&nbsp;   - Keywords



\### Success Criteria



\- You process \*\*only new emails\*\*

\- No full inbox scans



---



\## 1.4 Rules Engine (2 days)



\### Implement



\- Sender rules

\- Keyword rules

\- Amount presence detection



\### Output



\- `is\_candidate = true | false`



This saves \*\*AI cost + trust\*\*.



---



\## 1.5 AI Classification \& Extraction (4–5 days)



\### Pipeline



1\. Classify email → financial or not

2\. Extract:

&nbsp;   - Amount

&nbsp;   - Currency

&nbsp;   - Direction

&nbsp;   - Merchant

&nbsp;   - source Account

&nbsp;   - Target Account

&nbsp;   - Source Bank

&nbsp;   - Target Bank

&nbsp;   - Date

&nbsp;   - Fees

&nbsp;   - Tax (ITBIS/VAT)



\### Requirements



\- Return structured JSON

\- Include confidence per field



🚫 Do not store raw LLM output.



---



\## 1.6 Normalization Layer (2 days)



\### Convert extraction → canonical model



```json

type

amount

currency

direction

merchant

source\_account

target\_account

source\_bank

target\_bank

date

tax\_amount

confidence



```



\### Deduplication



\- Hash content + amount + date