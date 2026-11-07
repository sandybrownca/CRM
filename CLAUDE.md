# Business Operations CRM — AI Agent Instructions

## 1. Project Context

This repository contains a full-stack Business Operations CRM built with:

- React and TypeScript
- .NET 8 and ASP.NET Core Minimal APIs
- Entity Framework Core
- Microsoft SQL Server
- ASP.NET Core Identity
- xUnit, Testcontainers, Vitest, and React Testing Library

The application follows a modular monolith architecture.

**Before implementing any feature, read `docs/specification.md`.**

The specification is the source of truth for product behavior, business rules, API contracts, and technical constraints.

## 2. Engineering Principles

- Follow the existing architecture and coding conventions.
- Do not invent libraries, APIs, database fields, or business rules.
- Do not modify tests simply to make an implementation pass.
- Avoid unrelated refactoring and unnecessary dependencies.
- Implement features as small, complete vertical slices.
- Keep domain logic out of API endpoint handlers.
- Use dependency injection and existing service abstractions.
- Prefer explicit, maintainable implementations over unnecessary abstractions.
- Document important design decisions and unresolved limitations.

## 3. Architecture

Maintain the existing project boundaries:

| Project | Responsibility |
|---|---|
| `BusinessOperations.Domain` | Entities, enums, and domain behavior |
| `BusinessOperations.Application` | DTOs, validation, use cases, and interfaces |
| `BusinessOperations.Infrastructure` | EF Core, Identity, persistence, and service implementations |
| `BusinessOperations.Api` | HTTP endpoints, authentication, authorization, and API configuration |
| `src/web` | React and TypeScript SPA |

Do not introduce dependencies that violate the existing architectural boundaries.

## 4. Authentication and Authorization

Use ASP.NET Core Identity with cookie-based authentication.

- Authentication cookie: `bocrm.auth`.
- Cookie credentials must remain HttpOnly.
- Never store authentication credentials in `localStorage` or `sessionStorage`.
- Protect state-changing requests with CSRF antiforgery tokens.
- Use the `X-CSRF-TOKEN` request header.
- Enforce authorization through backend API policies or endpoint requirements.
- Supported roles are `Administrator` and `Operations`.
- Never rely on frontend visibility or disabled controls as an authorization mechanism.
- Return appropriate `401` and `403` responses rather than login redirects.

## 5. Financial and Business Rules

Financial calculations must remain server-authoritative.

- Use C# `decimal` for all monetary values.
- Never use floating-point arithmetic for financial calculations.
- Store monetary values using the existing SQL decimal precision.
- Calculate invoice subtotal, tax, total, and outstanding balances on the server.
- Never trust client-submitted invoice totals or tax amounts.
- Do not silently modify issued invoices.
- Validate payment amounts against the outstanding balance.
- Payments must be greater than zero and have no more than two decimal places.
- Payment creation, invoice balance updates, and customer activity records must execute within one serializable database transaction.
- Rejected payments must not leave partial database changes.
- Never store full credit card numbers.

Preserve existing domain methods such as `Invoice.ApplyTaxRate` and `Invoice.RecordPayment` rather than duplicating their business logic elsewhere.

## 6. Customer and Activity Management

- Support `Individual` and `Company` customer types.
- Job titles apply only to individual customers.
- Billing and shipping addresses are optional.
- Customer interactions support `Phone`, `Email`, `Chat`, and `Meeting`.
- Logging an interaction must also create an `InteractionLogged` customer activity.
- Keep interaction transcripts in interaction records, not duplicated into customer activity descriptions.
- Customer activity history must be ordered newest first.

## 7. ERP Integration

- Integrate through the existing `IErpClient` abstraction.
- Keep the simulated ERP adapter separate from synchronization business logic.
- Use one synchronization record per invoice.
- Preserve the original idempotency key across retries.
- Do not generate duplicate external references for retry requests.
- Only administrators can manually retry ERP synchronization.
- Successful synchronization must create an `ErpSynced` customer activity once.

Do not introduce real ERP credentials or production integrations without explicit requirements.

## 8. API Conventions

- API prefix: `/api/v1`.
- Use DTOs rather than exposing EF Core entities.
- Follow existing endpoint and service patterns.
- Use Problem Details for API errors.
- Use validation problem responses for invalid requests.
- Support pagination and filtering on list endpoints.
- Serialize enum values as strings.
- Maintain Swagger/OpenAPI documentation.
- Enforce authorization and validation on the backend.

## 9. Database and Persistence

- Use EF Core for database access.
- Follow existing entity configurations and migration conventions.
- Create migrations when the database schema changes.
- Preserve existing data and backward compatibility where practical.
- Avoid unnecessary database round trips.
- Do not store derived dashboard summaries when they can be calculated reliably from persisted data.
- Ensure transactional operations remain atomic.

## 10. Required Feature Workflow

Follow this workflow for every feature or significant bug fix:

1. Read the relevant sections of `docs/specification.md`.
2. Understand the existing implementation and affected components.
3. Define expected behavior and acceptance criteria.
4. Identify impacted domain entities, services, endpoints, database changes, and UI components.
5. Write or update tests.
6. Implement the smallest complete solution.
7. Review the changes for correctness, security, and maintainability.
8. Run the relevant automated tests.
9. Update documentation when behavior or API contracts change.
10. Summarize the implementation, test results, and remaining limitations.

## 11. Testing Requirements

Every feature must include appropriate tests.

### Backend

- Domain and business-rule unit tests.
- Application service tests.
- API and persistence integration tests where appropriate.
- SQL Server integration tests using Testcontainers.

### Frontend

- Component tests using Vitest and React Testing Library.
- Tests for validation and user interactions.
- Tests for loading, success, and error states where applicable.

Do not skip relevant tests or claim tests passed without executing them.

## 12. Security and Data Handling

- Never include secrets, passwords, access tokens, or real customer information in prompts, commits, logs, or test fixtures.
- Do not introduce insecure authentication or authorization shortcuts.
- Do not log sensitive payment information.
- Never persist full payment card numbers.
- Review changes for common security issues before considering them complete.

## 13. Git and Delivery Guidelines

- Keep changes focused and logically scoped.
- Use descriptive commit messages.
- Do not commit generated files or local secrets unless explicitly required.
- Do not merge or deploy changes automatically.
- Do not make destructive database or infrastructure changes without explicit approval.
- Human review is required before accepting AI-generated changes.

## 14. Completion Report

When a task is complete, provide:

- Summary of implemented changes.
- Important technical decisions.
- Tests executed and their results.
- Database migrations added, if applicable.
- Known limitations or follow-up work.

A task is not complete simply because the code has been generated. It is complete when the implementation has been reviewed, relevant tests have been executed, and documentation has been updated.