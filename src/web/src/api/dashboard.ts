import { apiFetch } from './client'
import type { CustomerActivity } from './customers'
import type { InvoiceListItem } from './invoices'

export type Dashboard = {
  activeCustomerCount: number
  outstandingBalance: number
  openInvoiceCount: number
  overdueInvoiceCount: number
  openInvoices: InvoiceListItem[]
  recentActivity: CustomerActivity[]
}

export async function getDashboard(): Promise<Dashboard> {
  return apiFetch<Dashboard>('/api/v1/dashboard')
}