# Business Operations CRM

A full-stack Business Operations CRM built with **React, TypeScript, ASP.NET Core 8, and SQL Server**. The application demonstrates customer relationship management, invoicing, payment tracking, dashboard reporting, role-based access control, and simulated ERP integration.

Designed as an AI-assisted software engineering demonstration, the project emphasizes clean architecture, secure authentication, reliable financial transactions, automated testing, and maintainable full-stack development practices.

## Features

### Customer Management
- Create, search, view, and update customers.
- Support for individual and company customer profiles.
- Store optional billing and shipping addresses.
- Maintain individual customer job titles.
- Record customer interactions across phone, email, chat, and meetings.
- Track customer activity history.
- Soft-deactivate customers through the API.

### Invoice Management
- Create and update draft invoices.
- Automatically generate invoice numbers.
- Calculate tax and invoice totals on the server.
- Issue and void invoices with business-rule validation.
- Track invoice statuses: `Draft`, `Issued`, `Paid`, and `Void`.
- View outstanding balances and ERP synchronization status.

### Payment Management
- Record manual and simulated card payments.
- Support partial and full payments.
- Automatically update invoice balances and statuses.
- Maintain payment references and transaction history.
- Record payment activity against the associated customer.
- Enforce payment validation and transactional consistency.

> **Note:** Simulated card payments are for demonstration purposes only. No real payment processing or full credit card information is involved.

### Dashboard
- Active customer count.
- Total outstanding balance.
- Open invoice count.
- Overdue invoice count.
- Upcoming open invoices.
- Recent customer activity.

All dashboard figures are calculated by the backend using persisted application data.

### ERP Integration
- Simulated ERP synchronization when invoices are issued.
- Track synchronization status and external references.
- Support administrator-controlled retries.
- Use idempotency keys to prevent duplicate ERP records.
- Maintain ERP synchronization activity history.

The ERP integration uses an in-process simulated adapter rather than a live external ERP system.

### User Administration
- Create and manage application users.
- Assign `Administrator` and `Operations` roles.
- Activate and deactivate users.
- Enforce role-based access through backend authorization.
- Protect against deactivating the last active administrator or an administrator's own account.

---

## Technology Stack

| Layer | Technologies |
|---|---|
| Frontend | React, TypeScript, Vite |
| UI | Material UI |
| Data Fetching | TanStack Query |
| Forms | React Hook Form, Zod |
| Backend | .NET 8, ASP.NET Core Minimal APIs |
| Architecture | Modular Monolith, Clean Architecture principles |
| ORM | Entity Framework Core |
| Database | Microsoft SQL Server |
| Authentication | ASP.NET Core Identity |
| Security | HttpOnly Cookies, CSRF Protection, Role-Based Authorization |
| Backend Testing | xUnit, Testcontainers |
| Frontend Testing | Vitest, React Testing Library |
| DevOps | Docker, Docker Compose, GitHub Actions |

## Architecture

The application follows a modular monolith architecture with clear separation of concerns.

```text
business-operations-crm/
│
├── CLAUDE.md
├── README.md
├── docs/
│   └── specification.md
│
├── src/
│   ├── api/
│   │   ├── BusinessOperations.Domain/
│   │   ├── BusinessOperations.Application/
│   │   ├── BusinessOperations.Infrastructure/
│   │   └── BusinessOperations.Api/
│   │
│   └── web/
│       ├── src/
│       ├── package.json
│       └── vite.config.ts
│
├── tests/
│   ├── BusinessOperations.UnitTests/
│   └── BusinessOperations.IntegrationTests/
│
├── docker-compose.yml
│
└── .github/
    └── workflows/
        └── ci.yml
```

### Backend Projects

| Project | Responsibility |
|---|---|
| Domain | Business entities, enums, and domain rules |
| Application | DTOs, validation, use cases, and service contracts |
| Infrastructure | EF Core, SQL Server, Identity, persistence, and ERP adapter |
| API | HTTP endpoints, authentication, authorization, CSRF, and health checks |

The React application communicates with the backend through versioned REST API endpoints.

## Authentication and Security

Authentication is implemented using ASP.NET Core Identity with cookie-based sessions.

Security decisions include:

- HttpOnly authentication cookie (`bocrm.auth`).
- CSRF protection using an antiforgery cookie and `X-CSRF-TOKEN` request header.
- Role-based authorization enforced by backend API endpoints.
- No authentication credentials stored in `localStorage` or `sessionStorage`.
- Inactive users are prevented from authenticating.
- API authentication failures return appropriate HTTP `401` and `403` responses instead of login redirects.

For local development, Vite proxies API requests to the backend, allowing the frontend and API to operate in a same-site configuration.

## Financial Data Integrity

Financial calculations and payment processing are handled by the backend.

Key implementation decisions:

- Monetary values use C# `decimal` rather than floating-point types.
- SQL Server stores monetary values using `decimal(18,2)`.
- Tax rates use `decimal(9,6)`.
- Invoice subtotal, tax, total, and outstanding balance are calculated server-side.
- The client cannot submit authoritative invoice totals or tax amounts.
- Issued invoices cannot be silently edited.
- Payments cannot exceed the outstanding invoice balance.
- Payment creation, invoice balance updates, and customer activity records are committed within a single serializable database transaction.

This ensures that rejected payments do not leave partially updated financial records.

## ERP Synchronization

ERP integration is implemented through an adapter-based design.

The `IErpClient` interface separates ERP synchronization logic from the external system implementation.

The current implementation uses `SimulatedErpClient`, an in-memory adapter that demonstrates:

- Invoice synchronization.
- External reference generation.
- Idempotent synchronization requests.
- Failed synchronization handling.
- Administrator-controlled retries.
- Customer activity tracking after successful synchronization.

Retries reuse the original idempotency key to prevent duplicate external records.

## API Overview

All endpoints are versioned under:

```text
/api/v1
```

| Module | Main Endpoints | Description |
|---|---|---|
| Authentication | `/auth/*` | Login, logout, current user, CSRF |
| Customers | `/customers` | Customer CRUD, search, activities, interactions |
| Invoices | `/invoices` | Invoice creation, updates, issuance, voiding |
| Payments | `/payments` | Payment history and payment recording |
| Dashboard | `/dashboard` | Business summary and recent activity |
| ERP | `/erp-syncs` | Synchronization history and retry |
| Users | `/users` | Administrator user and role management |

### API Design

- DTOs are used instead of exposing EF Core entities.
- Validation errors follow Problem Details conventions.
- List endpoints support pagination and filtering.
- Enum values are serialized as strings.
- Business rules and authorization are enforced server-side.
- Swagger/OpenAPI provides interactive API documentation.

For detailed endpoint contracts and request/response behavior, see `docs/specification.md`.

## Prerequisites

Install the following tools before running the application:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 22 or later](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- Git

Docker is required for SQL Server and integration tests using Testcontainers.

## Getting Started

### 1. Clone the repository

```bash
git clone <repository-url>
cd business-operations-crm
```

### 2. Start SQL Server

```bash
docker compose up -d sqlserver
```

Wait until SQL Server is ready before starting the API.

### 3. Start the backend API

```bash
dotnet run --project src/api/BusinessOperations.Api
```

The API applies EF Core migrations and seeds demo users during startup.

Default local endpoints:

- Swagger: http://localhost:8080/swagger
- Health check: http://localhost:8080/health

The actual API port may depend on the configured launch profile.

### 4. Start the frontend

Open another terminal:

```bash
cd src/web
npm install
npm run dev
```

The Vite development server proxies `/api` requests to the backend.

Open the local Vite URL displayed in the terminal.

## Demo Accounts

The local development environment includes the following seeded accounts:

| Role | Email | Password |
|---|---|---|
| Administrator | `admin@local.dev` | `ChangeMe!Admin123` |
| Operations | `ops@local.dev` | `ChangeMe!Ops123` |

**Security:** These credentials are intended exclusively for local demonstrations. Change them through application configuration or environment variables before using a shared environment.

## Running with Docker Compose

To build and start the complete application:

```bash
docker compose up --build
```

Default application endpoints:

| Service | URL |
|---|---|
| Web Application | http://localhost:8081 |
| Backend API | http://localhost:8080 |
| Swagger | http://localhost:8080/swagger |

## Running Tests

### Backend Unit Tests

```bash
dotnet test tests/BusinessOperations.UnitTests
```

### Backend Integration Tests

```bash
dotnet test tests/BusinessOperations.IntegrationTests
```

Integration tests use Testcontainers to provision SQL Server instances. Docker must be running.

### Frontend Tests

```bash
cd src/web
npm install
npm test
```

The test suite covers backend business rules, API integration, persistence behavior, and frontend components.

## Configuration

Application configuration is managed through the .NET configuration system.

Important configuration areas include:

- SQL Server connection string.
- Authentication and cookie settings.
- Identity password policies.
- Tax rate (`Tax:Rate`).
- ERP simulation settings.
- Application environment and logging.

Do not commit production credentials, secrets, or real customer information to the repository.

## Current Implementation Status

The core CRM functionality is implemented.

| Feature | Status |
|---|---|
| Modular backend architecture | Complete |
| SQL Server and EF Core persistence | Complete |
| Identity cookie authentication | Complete |
| CSRF protection | Complete |
| Role-based authorization | Complete |
| Customer management | Complete |
| Individual and company profiles | Complete |
| Customer interaction logs | Complete |
| Customer activity history | Complete |
| Invoice management | Complete |
| Tax and financial calculations | Complete |
| Manual and simulated card payments | Complete |
| Dashboard and reporting | Complete |
| Simulated ERP synchronization | Complete |
| Idempotent ERP retry | Complete |
| Administrator user management | Complete |
| Backend unit and integration tests | Implemented |
| Frontend tests | Implemented |
| Docker Compose | Complete |
| GitHub Actions CI | Configured |

The remaining presentation milestone is a recorded end-to-end application demonstration.

## AI-Assisted Development Workflow

This project demonstrates a specification-driven AI-assisted development workflow.

Development is guided by:

- `CLAUDE.md` — repository-specific AI coding instructions.
- `docs/specification.md` — product requirements, business rules, API contracts, and technical decisions.

Features are developed as small vertical slices:

1. Define requirements and acceptance criteria.
2. Identify affected domain entities, API endpoints, and UI components.
3. Write or update automated tests.
4. Implement the smallest complete feature.
5. Review generated code for correctness, security, and maintainability.
6. Run relevant tests.
7. Update documentation.

AI-generated changes are reviewed by a human before being accepted. Automated implementation does not replace architectural judgment, testing, or code review.

## Project Goals

This project was built to demonstrate practical full-stack engineering skills, including:

- Building REST APIs with ASP.NET Core.
- Designing modular and maintainable application architecture.
- Developing modern React and TypeScript applications.
- Implementing secure cookie-based authentication.
- Enforcing business rules and role-based permissions.
- Managing financial data with transactional consistency.
- Integrating external systems through adapter patterns.
- Writing unit and integration tests.
- Containerizing applications with Docker.
- Automating validation through CI pipelines.
- Using AI-assisted development with specifications and human code review.

## License

Add a license file to the repository if you intend to distribute this project under an open-source license.