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
- API returns 401/403 instead of redirecting to a login page

Local SPA development uses the Vite proxy so the browser treats UI and API as same-site.

## Persistence

SQL Server + EF Core migrations. Monetary fields (future invoices/payments) use `decimal(18,2)`.

## ERP (planned)

Mock adapter behind an application interface. Idempotency keys prevent duplicate external records on retry.
