# Business Operations CRM
## Product and Technical Specification

**Version:** 1.0  
**Project type:** AI-assisted full-stack development demonstration  
**Target audience:** Technical interviewers and engineering teams  
**Status:** Initial implementation specification (milestone 1 in progress)

---

## 1. Product overview

Build a web-based Business Operations CRM that allows a small business to manage customer information, invoices, payments, and outstanding balances from one application.

### Primary objectives

- Demonstrate end-to-end feature ownership.
- Showcase C#/.NET, ASP.NET Core Web API, Entity Framework Core, SQL Server, React, and TypeScript.
- Implement realistic business workflows with validation, permissions, and reliable data handling.
- Demonstrate AI-assisted development using specifications, automated tests, and human code review.

### Out of scope

- Real credit card processing or storage of payment credentials.
- Real customer or client data.
- Production ERP credentials or live financial integrations.
- Multi-company tenancy, advanced accounting, tax calculation, and complex financial reporting.

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

### Done (milestone 1 scaffold)

- Solution structure, Docker Compose, CI
- Identity cookie auth + CSRF
- Customer create/search/update/deactivate APIs
- React login + customer directory
- Unit tests + integration test project (Testcontainers)
- EF Core initial migration

### Next

1. Invoice creation, issuance, calculations
2. Payment recording and transaction safety
3. Dashboard + richer customer/invoice UI
4. Simulated ERP integration and retry
5. Authorization hardening and demo recording
