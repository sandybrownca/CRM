# Business Operations CRM
## Product and Technical Specification

**Version:** 1.0  
**Project type:** AI-assisted full-stack development demonstration  
**Target audience:** Technical interviewers and engineering teams  
**Status:** Implemented through customer profiles and interaction logs

---

## 1. Product overview

Build a web-based Business Operations CRM that allows a small business to manage customer information, invoices, payments, and outstanding balances from one application.

### Primary objectives

- Demonstrate end-to-end feature ownership.
- Showcase C#/.NET, ASP.NET Core Web API, Entity Framework Core, SQL Server, React, and TypeScript.
- Implement realistic business workflows with validation, permissions, and reliable data handling.
- Demonstrate AI-assisted development using specifications, automated tests, and human code review.

### Out of scope

- Real credit card processing or storage of a full card number.
- Real customer or client data.
- Production ERP credentials or live financial integrations.
- Multi-company tenancy, jurisdiction-based tax, and complex financial reporting.

## 2. Users and permissions

- **Administrator** — customers, invoices, payments, ERP retry, user/role management
- **Operations** — customers, invoices, payments; no user/role administration

All authorization rules are enforced by the backend API.

## 3. Auth decision (implemented)

Cookie-based ASP.NET Core Identity for the React SPA:

- HttpOnly auth cookie (`bocrm.auth`)
- CSRF via `X-CSRF-TOKEN` header + antiforgery cookie
- Vite proxy / reverse proxy for same-site local development
- No tokens in `localStorage` / `sessionStorage`

## 4. Milestone status

### Done

- Solution structure, Docker Compose, CI
- Identity cookie auth + CSRF
- Customer create/search/update/deactivate APIs. The directory creates, edits, searches, and shows history. Deactivate is API-only
- Invoice drafts, issuance, void, and server-side totals including a flat tax rate. Draft edit is API-only
- Manual and simulated-card payments, with balance and activity updates in one transaction
- Dashboard of outstanding balance, open and overdue invoices, and recent activity
- Customer activity history
- Individual or company customers, billing and shipping addresses, and a job title for an individual
- Interaction logs for phone calls, email, chat transcripts, and meetings
- React login, customer directory, invoice/payment UI, dashboard, and administrator user screen
- Simulated ERP sync with idempotent retry
- Administrator user and role management
- Unit tests and integration tests (Testcontainers)
- EF Core migrations through customer profiles and interaction logs

### Next

Customer profiles and interaction logs are implemented. A demo recording is the remaining presentation step.

## 5. Payments

Staff record money received against an **issued** invoice. The server owns the balance and the tax. The client sends an amount. It does not send a new total or a tax amount.

### Tax

`Tax:Rate` in configuration is a decimal between 0 and 1. The running app uses `0.08`. Integration tests set the rate to `0` so older money fixtures stay exact.

On create or draft update, the server stores that rate on the invoice. Tax is `round(subtotal × rate, 2, away from zero)`. `TotalAmount` is subtotal plus tax. Issue recalculates from the stored rate, so a later configuration change does not rewrite an already saved draft's rate until the draft is updated. Invoices saved before the rate existed keep `TaxRate` and `TaxAmount` at zero.

### Rules

- Amount is a `decimal` greater than zero, with at most two decimal places.
- Amount cannot exceed the invoice balance due.
- A payment is `Manual` or `SimulatedCard`. A simulated card payment requires `cardBrand` and `cardLast4` (exactly four digits). The server sets `authorizationCode`. A 13- to 19-digit number in the reference or brand is rejected. The full card number is never stored.
- A partial payment leaves the invoice `Issued`. Paying the remaining balance sets it to `Paid`.
- Draft, void, and paid invoices reject new payments.
- An invoice that has received any payment cannot be voided.
- Customer outstanding balance counts only `Issued` invoices and is `TotalAmount - AmountPaid`.

### Transaction

`POST /api/v1/invoices/{id}/payments` commits three changes in one serializable database transaction:

1. Insert the `Payments` row.
2. Update the invoice `AmountPaid` and status through `Invoice.RecordPayment`.
3. Insert a `CustomerActivity` with event type `PaymentRecorded`.

If the amount is rejected, none of those rows change.

### Endpoints

| Method | Path | Notes |
|---|---|---|
| POST | `/api/v1/invoices/{id}/payments` | Body: `amount`, optional `paidAtUtc`, optional `reference`, optional `method`, and for `SimulatedCard` the `cardBrand` and `cardLast4` |
| GET | `/api/v1/payments` | `invoiceId`, `customerId`, `page`, `pageSize` |

## 6. Dashboard and history

The dashboard is a read model. It does not store its own rows. Every figure is calculated on the server from customers, invoices, and activity.

### Rules

- Outstanding balance and open-invoice count include `Issued` invoices only. Drafts, voids, and paid invoices are excluded.
- Outstanding balance is the sum of `TotalAmount - AmountPaid` on those issued invoices.
- An invoice is overdue when it is `Issued` and `DueDate` is before the current UTC date.
- The open-invoice list returns at most eight issued invoices, soonest due date first.
- Recent activity returns the eight newest `CustomerActivity` rows across all customers.
- Customer history is the same activity rows for one customer, newest first, paginated.

### Endpoints

| Method | Path | Notes |
|---|---|---|
| GET | `/api/v1/dashboard` | Active customers, outstanding, open count, overdue count, open invoices, recent activity |
| GET | `/api/v1/customers/{id}/activities` | Paginated history. `404` when the customer does not exist |

## 7. Simulated ERP

Issuing an invoice sends it to a simulated ERP. The ERP is an in-process adapter, not a live financial system. One sync row is stored per invoice.

### Rules

- Draft and void invoices are rejected.
- The first attempt creates an idempotency key. Retries send that same key.
- The simulated ERP stores one external reference per key. A retry returns the original reference and does not create a second external record.
- A failed attempt leaves the invoice issued. An administrator can retry. Operations staff cannot.
- The first success writes an `ErpSynced` customer activity. A later retry does not write another one.

### Endpoints

| Method | Path | Notes |
|---|---|---|
| GET | `/api/v1/erp-syncs` | `invoiceId`, `status`, `page`, `pageSize` |
| POST | `/api/v1/invoices/{id}/erp-sync` | Administrator only. Sync or retry |

## 8. User administration

Only an administrator can list, create, or update users. The API enforces this. Hiding the Users link is not the control.

### Rules

- A user has one role: `Administrator` or `Operations`.
- Passwords follow the Identity policy: at least 10 characters, with upper case, lower case, a digit, and a symbol.
- An administrator cannot deactivate their own account.
- The update is rejected when it would leave no active administrator.
- An inactive user cannot sign in. The auth cookie is rejected on the next request after deactivation.

### Endpoints

| Method | Path | Notes |
|---|---|---|
| GET | `/api/v1/users` | `search`, `page`, `pageSize`. Administrator only |
| POST | `/api/v1/users` | `email`, `displayName`, `password`, `role` |
| PUT | `/api/v1/users/{id}` | `displayName`, `role`, `isActive` |

## 9. Customer profiles and interaction logs

A customer is an `Individual` or a `Company`. Existing customers saved before this choice are `Company`. A job title is optional and applies only to an individual. A company request that includes a job title is rejected.

Billing and shipping are both optional physical addresses. The directory collects them. Deactivate stays on the API.

An interaction log is a staff record of one communication. It is not a system activity. Channels are `Phone`, `Email`, `Chat`, and `Meeting`. Each row stores a subject, the notes or transcript, when it happened, and the name of the person who recorded it. Logging also writes an `InteractionLogged` customer activity so the existing history still shows that a conversation was recorded. The activity stores the channel and subject, not the full transcript.

### Endpoints

| Method | Path | Notes |
|---|---|---|
| POST | `/api/v1/customers` | `kind`, optional `jobTitle` for an individual, and optional billing and shipping address fields |
| PUT | `/api/v1/customers/{id}` | Same body as create |
| GET | `/api/v1/customers/{id}/interactions` | `channel`, `page`, `pageSize`. `404` when the customer does not exist |
| POST | `/api/v1/customers/{id}/interactions` | `channel`, `subject`, `body`, optional `occurredAtUtc` |