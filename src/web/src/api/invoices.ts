import { apiFetch } from './client'

export type InvoiceStatus = 'Draft' | 'Issued' | 'Paid' | 'Void'

export type InvoiceLineItem = {
  id: string
  description: string
  quantity: number
  unitPrice: number
  lineTotal: number
}

export type InvoiceListItem = {
  id: string
  invoiceNumber: string
  customerId: string
  customerName: string
  issueDate: string
  dueDate: string
  status: InvoiceStatus
  totalAmount: number
  amountPaid: number
  balanceDue: number
  createdAtUtc: string
  erpStatus?: 'Pending' | 'Succeeded' | 'Failed' | null
  erpExternalReference?: string | null
}

export type InvoiceDetails = {
  id: string
  invoiceNumber: string
  customerId: string
  customerName: string
  customerEmail: string
  issueDate: string
  dueDate: string
  status: InvoiceStatus
  subtotal: number
  totalAmount: number
  amountPaid: number
  balanceDue: number
  lineItems: InvoiceLineItem[]
  createdAtUtc: string
  updatedAtUtc?: string
}

export type PagedInvoices = {
  items: InvoiceListItem[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type CreateLineItemInput = {
  description: string
  quantity: number
  unitPrice: number
}

export type CreateInvoiceInput = {
  customerId: string
  issueDate: string
  dueDate: string
  lineItems: CreateLineItemInput[]
}

export async function searchInvoices(options?: {
  customerId?: string
  status?: InvoiceStatus
  page?: number
  pageSize?: number
}): Promise<PagedInvoices> {
  const params = new URLSearchParams({
    page: String(options?.page ?? 1),
    pageSize: String(options?.pageSize ?? 20),
  })
  if (options?.customerId) {
    params.set('customerId', options.customerId)
  }
  if (options?.status) {
    params.set('status', options.status)
  }

  const query = params.toString() ? `?${params.toString()}` : ''
  return apiFetch<PagedInvoices>(`/api/v1/invoices${query}`)
}

export async function getInvoice(id: string): Promise<InvoiceDetails> {
  return apiFetch<InvoiceDetails>(`/api/v1/invoices/${id}`)
}

export async function createInvoice(input: CreateInvoiceInput): Promise<InvoiceDetails> {
  return apiFetch<InvoiceDetails>('/api/v1/invoices', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export async function issueInvoice(id: string): Promise<InvoiceDetails> {
  return apiFetch<InvoiceDetails>(`/api/v1/invoices/${id}/issue`, {
    method: 'POST',
  })
}

export async function syncInvoiceToErp(id: string): Promise<{
  id: string
  invoiceId: string
  status: 'Pending' | 'Succeeded' | 'Failed'
  idempotencyKey: string
  externalReference?: string | null
  attemptCount: number
  lastError?: string | null
}> {
  return apiFetch(`/api/v1/invoices/${id}/erp-sync`, { method: 'POST' })
}

export async function voidInvoice(id: string): Promise<InvoiceDetails> {
  return apiFetch<InvoiceDetails>(`/api/v1/invoices/${id}/void`, {
    method: 'POST',
  })
}