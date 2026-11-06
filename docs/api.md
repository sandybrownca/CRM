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
| GET | `/customers/{id}/activities` | Required | Paginated history, newest first. `404` when the customer does not exist |
| GET | `/customers/{id}/interactions` | Required | `channel`, `page`, `pageSize`. `404` when the customer does not exist |
| POST | `/customers/{id}/interactions` | Required | `channel`, `subject`, `body`, optional `occurredAtUtc` |

A customer is `Individual` or `Company`. Customers created before that choice are `Company`. `jobTitle` is optional for an individual and rejected for a company. Billing and shipping address fields are optional. An interaction is a phone call, email, chat transcript, or meeting. Saving one also writes an `InteractionLogged` activity with the channel and subject. The transcript stays on the interaction row. Enum values include `Individual`, `Company`, `Phone`, `Email`, `Chat`, and `Meeting`.

## Invoices

| Method | Path | Auth | Notes |
|---|---|---|---|
| GET | `/invoices` | Required | `customerId`, `status`, `page`, `pageSize` |
| GET | `/invoices/{id}` | Required | Details, including server-calculated totals |
| POST | `/invoices` | Required | Create a draft. Server assigns the invoice number, tax, and total |
| PUT | `/invoices/{id}` | Required | Update a draft only |
| POST | `/invoices/{id}/issue` | Required | Draft → Issued |
| POST | `/invoices/{id}/void` | Required | Rejected after any payment |

Invoice details include `subtotal`, `taxRate`, `taxAmount`, and `totalAmount`. The client does not send those figures. List items include `erpStatus` and `erpExternalReference`.

## Payments

| Method | Path | Auth | Notes |
|---|---|---|---|
| GET | `/payments` | Required | `invoiceId`, `customerId`, `page`, `pageSize` |
| POST | `/invoices/{id}/payments` | Required | Record a payment. Body: `amount`, optional `paidAtUtc`, optional `reference`, optional `method` (`Manual` or `SimulatedCard`), and for a simulated card the `cardBrand` and `cardLast4` |

The payment response includes the amount stored and the invoice balance after the server applies it (`invoiceAmountPaid`, `invoiceBalanceDue`, `invoiceStatus`). A simulated card response also includes `method`, `cardBrand`, `cardLast4`, and `authorizationCode`. Enum values are serialized as strings (`Draft`, `Issued`, `Paid`, `Void`, `Manual`, `SimulatedCard`).

Recording a payment inserts the payment, updates the invoice balance, and writes a `PaymentRecorded` customer activity in one serializable transaction.

## Dashboard

| Method | Path | Auth | Notes |
|---|---|---|---|
| GET | `/dashboard` | Required | Server-calculated summary. No request body |

The response includes `activeCustomerCount`, `outstandingBalance`, `openInvoiceCount`, `overdueInvoiceCount`, up to eight `openInvoices`, and eight `recentActivity` rows. Outstanding balance and open counts use issued invoices only. Overdue means issued and due before the current UTC date.

## ERP

| Method | Path | Auth | Notes |
|---|---|---|---|
| GET | `/erp-syncs` | Required | `invoiceId`, `status`, `page`, `pageSize` |
| POST | `/invoices/{id}/erp-sync` | Administrator | Sync or retry an issued or paid invoice |

Issuing an invoice starts the sync. The stored idempotency key is reused on retry, and the simulated ERP returns the same external reference. Draft and void invoices are rejected. Invoice list items include `erpStatus` and `erpExternalReference`.

## Users

| Method | Path | Auth | Notes |
|---|---|---|---|
| GET | `/users` | Administrator | `search`, `page`, `pageSize` |
| POST | `/users` | Administrator | `email`, `displayName`, `password`, `role` |
| PUT | `/users/{id}` | Administrator | `displayName`, `role`, `isActive` |

Operations receives 403. An administrator cannot deactivate themselves or remove the last active administrator.

Errors use Problem Details / validation problem payloads.