# Business Operations CRM

AI-assisted full-stack demonstration CRM for managing customers, invoices, payments, and simulated ERP synchronization.

## Stack

- **Frontend:** React, TypeScript, Vite, TanStack Query, React Hook Form, Zod, Material UI
- **Backend:** .NET 8, ASP.NET Core minimal APIs, EF Core, SQL Server
- **Auth:** ASP.NET Core Identity with **HttpOnly cookie** sessions + CSRF (`X-CSRF-TOKEN`)
- **Tests:** xUnit + Testcontainers (SQL Server), Vitest / React Testing Library (frontend)

## Repository layout

```text
business-operations-crm/
├── CLAUDE.md
├── README.md
├── docs/
├── src/api/          # Domain, Application, Infrastructure, Api
├── src/web/          # React SPA
├── tests/
├── docker-compose.yml
└── .github/workflows/ci.yml
```

## Prerequisites

- .NET 8 SDK
- Node.js 22+
- Docker Desktop (SQL Server + optional full stack)

## Quick start (local)

### 1. Start SQL Server

```bash
docker compose up -d sqlserver
```

### 2. Run the API

```bash
dotnet run --project src/api/BusinessOperations.Api
```

The API applies EF migrations and seeds demo users on startup.

- Swagger: `http://localhost:8080/swagger` (or the port in `launchSettings.json`)
- Health: `/health`

### 3. Run the web app

```bash
cd src/web
npm install
npm run dev
```

Vite proxies `/api` to the API so cookie auth stays same-site.

### Demo users

| Role | Email | Password |
|---|---|---|
| Administrator | `admin@local.dev` | `ChangeMe!Admin123` |
| Operations | `ops@local.dev` | `ChangeMe!Ops123` |

Change these via configuration / environment variables. Do not use them outside local demos.

## Docker Compose (full stack)

```bash
docker compose up --build
```

- Web: `http://localhost:8081`
- API: `http://localhost:8080`

## Tests

```bash
dotnet test tests/BusinessOperations.UnitTests
dotnet test tests/BusinessOperations.IntegrationTests

cd src/web
npm test
```

Integration tests start SQL Server via Testcontainers (Docker required).

## Auth design

- Session credential is a Secure/HttpOnly/`SameSite=Lax` cookie (`bocrm.auth`).
- Mutating requests require `X-CSRF-TOKEN` (from `GET /api/v1/auth/csrf`).
- Role checks are enforced by the API (`Administrator`, `Operations`).
- Credentials are never stored in `localStorage` / `sessionStorage`.

## Current milestone

Authenticated **customer directory** with create/search/deactivate APIs, SQL Server persistence, cookie auth, CSRF, unit + integration tests, Docker, and CI.

Next: invoices → payments → ERP sync → dashboard polish.

## Agent workflow

See `CLAUDE.md` and `docs/specification.md`. Features are implemented as small vertical slices with tests first.
