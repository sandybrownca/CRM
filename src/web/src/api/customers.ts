import { apiFetch } from './client'

export type CustomerStatus = 'Active' | 'Inactive'
export type CustomerKind = 'Individual' | 'Company'
export type InteractionChannel = 'Phone' | 'Email' | 'Chat' | 'Meeting'

export type CustomerListItem = {
  id: string
  name: string
  email: string
  status: CustomerStatus
  outstandingBalance: number
  openInvoiceCount: number
  kind: CustomerKind
  jobTitle?: string | null
  billingCity?: string | null
}

export type CustomerDetails = CustomerListItem & {
  phone?: string | null
  billingAddressLine1?: string | null
  billingAddressLine2?: string | null
  billingState?: string | null
  billingPostalCode?: string | null
  billingCountry?: string | null
  shippingAddressLine1?: string | null
  shippingAddressLine2?: string | null
  shippingCity?: string | null
  shippingState?: string | null
  shippingPostalCode?: string | null
  shippingCountry?: string | null
}

export type CustomerActivity = {
  id: string
  customerId: string
  customerName: string
  eventType: string
  description: string
  relatedRecordId?: string | null
  occurredAtUtc: string
}

export type CustomerInteraction = {
  id: string
  customerId: string
  channel: InteractionChannel
  subject: string
  body: string
  occurredAtUtc: string
  recordedByName: string
}

export type PagedActivities = {
  items: CustomerActivity[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type PagedInteractions = {
  items: CustomerInteraction[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type PagedCustomers = {
  items: CustomerListItem[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type CustomerInput = {
  name: string
  email: string
  phone?: string
  kind: CustomerKind
  jobTitle?: string
  billingAddressLine1?: string
  billingAddressLine2?: string
  billingCity?: string
  billingState?: string
  billingPostalCode?: string
  billingCountry?: string
  shippingAddressLine1?: string
  shippingAddressLine2?: string
  shippingCity?: string
  shippingState?: string
  shippingPostalCode?: string
  shippingCountry?: string
}

export type LogInteractionInput = {
  channel: InteractionChannel
  subject: string
  body: string
  occurredAtUtc?: string
}

export async function searchCustomers(
  search: string,
  page = 1,
  pageSize = 20,
): Promise<PagedCustomers> {
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
  })
  if (search.trim()) {
    params.set('search', search.trim())
  }
  return apiFetch<PagedCustomers>(`/api/v1/customers?${params}`)
}

export async function getCustomer(id: string): Promise<CustomerDetails> {
  return apiFetch<CustomerDetails>(`/api/v1/customers/${id}`)
}

export async function searchCustomerActivities(customerId: string): Promise<PagedActivities> {
  return apiFetch<PagedActivities>(`/api/v1/customers/${customerId}/activities?page=1&pageSize=20`)
}

export async function searchCustomerInteractions(customerId: string): Promise<PagedInteractions> {
  return apiFetch<PagedInteractions>(`/api/v1/customers/${customerId}/interactions?page=1&pageSize=20`)
}

export async function createCustomer(input: CustomerInput): Promise<CustomerDetails> {
  return apiFetch('/api/v1/customers', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export async function updateCustomer(id: string, input: CustomerInput): Promise<CustomerDetails> {
  return apiFetch(`/api/v1/customers/${id}`, {
    method: 'PUT',
    body: JSON.stringify(input),
  })
}

export async function logInteraction(
  customerId: string,
  input: LogInteractionInput,
): Promise<CustomerInteraction> {
  return apiFetch(`/api/v1/customers/${customerId}/interactions`, {
    method: 'POST',
    body: JSON.stringify(input),
  })
}