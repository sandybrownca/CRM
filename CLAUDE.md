# Business Operations CRM — Agent Instructions

Read `docs/specification.md` before implementing any feature. That document is the source of truth for product behavior, API contracts, and technical constraints.

## Conventions

- Follow the existing architecture and coding conventions. Do not invent libraries, APIs, or database fields.
- Do not modify tests merely to make an implementation pass.
- Do not introduce unrelated refactoring or unnecessary dependencies.
- Never include secrets or real client data in prompts, commits, or logs.
- Use small, clearly scoped implementation tasks.
- Include tests with every feature.
- Review generated changes for correctness, security, and maintainability.
- Run the relevant tests before declaring a task complete.
- Explain important design decisions and any unresolved limitations.
- Do not merge or deploy changes automatically without human review.

## Architecture

Modular monolith:

- `BusinessOperations.Domain` — entities, enums, domain rules
- `BusinessOperations.Application` — use cases, DTOs, interfaces, validation
- `BusinessOperations.Infrastructure` — EF Core, Identity, ERP adapter, persistence
- `BusinessOperations.Api` — HTTP endpoints, auth configuration, Problem Details
- `src/web` — React + TypeScript SPA

## Authentication

Use ASP.NET Core Identity with **cookie-based authentication** for the React SPA.

- Credentials live in a Secure, HttpOnly, SameSite cookie — never in `localStorage` or `sessionStorage`.
- Protect state-changing requests with antiforgery (CSRF) tokens.
- Enforce role-based authorization (`Administrator`, `Operations`) on the API. Hiding UI controls is not sufficient.
- Prefer same-site hosting (reverse proxy or Vite proxy) so cookies work without cross-origin complexity.

## Money and financial rules

- Use `decimal` for all monetary values. Never use floating-point arithmetic.
- The server calculates invoice totals and outstanding balances. Never trust client-submitted totals.
- Payment create + balance update + activity history must run in a database transaction.
- Issued invoices must not be silently edited; use issue / void / correction workflows.

## Required workflow for each feature

1. Define the feature and acceptance criteria.
2. Identify the affected API endpoints, database entities, and UI components.
3. Write the tests.
4. Implement the smallest complete solution.
5. Review the changes and run the tests.
6. Update documentation and report the outcome.

## API

- Prefix: `/api/v1`
- DTOs only — do not expose EF entities directly
- Problem Details for errors
- Pagination and filtering on list endpoints
- OpenAPI/Swagger documentation
