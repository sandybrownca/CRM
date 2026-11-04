# API overview

Base path: `/api/v1`

## Auth

| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `/auth/login` | Anonymous | Sets auth cookie |
| POST | `/auth/logout` | Required | Clears auth cookie; CSRF required |
| GET | `/auth/me` | Required | Current user + roles |
| GET | `/auth/csrf` | Anonymous | Issues CSRF cookie + token |

## Customers

| Method | Path | Auth | Notes |
|---|---|---|---|
| GET | `/customers` | Required | `search`, `page`, `pageSize` |
| GET | `/customers/{id}` | Required | Details |
| POST | `/customers` | Required | Create |
| PUT | `/customers/{id}` | Required | Update |
| POST | `/customers/{id}/deactivate` | Required | Soft deactivate |

Errors use Problem Details / validation problem payloads.
