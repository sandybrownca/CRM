import { apiFetch } from './client'

export type CustomerStatus = 'Active' | 'Inactive'

export type CustomerListItem = {
  id: string
  name: string
  email: string
  status: CustomerStatus
  outstandingBalance: number
  openInvoiceCount: number
}

export type PagedCustomers = {
  items: CustomerListItem[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type CreateCustomerInput = {
  name: string
  email: string
  phone?: string
  billingAddressLine1?: string
  billingCity?: string
  billingState?: string
  billingPostalCode?: string
  billingCountry?: string
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

export async function createCustomer(
  input: CreateCustomerInput,
): Promise<CustomerListItem> {
  return apiFetch('/api/v1/customers', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}
