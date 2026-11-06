# Architecture

## Style

Modular monolith with clear boundaries:

| Project | Responsibility |
|---|---|
| Domain | Entities, enums, domain behavior |
| Application | DTOs, validators, service contracts |
| Infrastructure | EF Core, Identity, service implementations |
| Api | HTTP endpoints, auth/CSRF/CORS, health |

## Authentication

Cookie authentication via ASP.NET Core Identity.

- Auth cookie: `bocrm.auth` (HttpOnly)
- CSRF cookie: `bocrm.csrf` + request header `X-CSRF-TOKEN`
- Login and CSRF endpoints are exempt from CSRF validation
- The SPA discards the CSRF token after login. A token issued before the auth cookie exists is rejected on later mutations
- API returns 401/403 instead of redirecting to a login page

Local SPA development uses the Vite proxy so the browser treats UI and API as same-site.

## Persistence

SQL Server + EF Core migrations. Invoice subtotal, tax, total, amount paid, and payment amount use `decimal(18,2)`. `TaxRate` uses `decimal(9,6)`.

`Invoice.ApplyTaxRate` stores the configured rate and recalculates tax and total. `Invoice.RecordPayment` is the only place that changes `AmountPaid` and moves an issued invoice to `Paid`.

`PaymentService.RecordAsync` opens a serializable transaction, then saves the payment row, that balance change, and the `PaymentRecorded` customer activity together. A rejected amount never commits. `SimulatedCard` rows store brand, last four digits, and an authorization code. There is no column for a full card number.

`DashboardService` reads that same data. It does not persist a summary. Outstanding balance, open count, and overdue count are queries over issued invoices. Customer history reads `CustomerActivities` for one customer.

A customer is `Individual` or `Company`. `JobTitle` applies only to an individual. Billing and shipping addresses are columns on `Customers`. `CustomerInteractions` stores phone, email, chat, and meeting logs. Logging one also writes an `InteractionLogged` activity.

## API JSON

Status enums are serialized as strings via `JsonStringEnumConverter`, matching the SPA (`Draft`, `Issued`, `Paid`, `Void`, `Active`, `Inactive`, `Manual`, `SimulatedCard`, `Pending`, `Succeeded`, `Failed`, `Individual`, `Company`, `Phone`, `Email`, `Chat`, `Meeting`).

## ERP

`IErpClient` is the port. `SimulatedErpClient` is the in-memory adapter. It keeps one external reference per idempotency key.

`ErpSyncService` stores that key on an `ErpSyncs` row, one row per invoice. A failed call leaves the invoice issued and the row `Failed`. Retry calls the adapter with the original key. The first success writes an `ErpSynced` customer activity.

## Users

User administration uses the existing Identity users and roles. `UserAdminService` is the only writer. Cookie validation rejects a principal whose user is missing or inactive.