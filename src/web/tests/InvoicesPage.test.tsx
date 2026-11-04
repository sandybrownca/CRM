import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { describe, expect, it, vi } from 'vitest'
import { InvoicesPage } from '../src/pages/InvoicesPage'
import { AuthProvider } from '../src/auth/AuthContext'

vi.mock('../src/api/auth', () => ({
  me: vi.fn().mockResolvedValue({
    id: '1',
    email: 'admin@local.dev',
    displayName: 'Admin User',
    roles: ['Administrator'],
  }),
  login: vi.fn(),
  logout: vi.fn(),
}))

vi.mock('../src/api/invoices', () => ({
  searchInvoices: vi.fn().mockResolvedValue({
    items: [
      {
        id: 'inv-1',
        invoiceNumber: 'INV-202611-0001',
        customerId: 'cust-1',
        customerName: 'Acme Corp',
        issueDate: '2026-11-03T00:00:00Z',
        dueDate: '2026-11-17T00:00:00Z',
        status: 'Draft',
        totalAmount: 1500,
        amountPaid: 0,
        balanceDue: 1500,
        createdAtUtc: '2026-11-03T00:00:00Z',
      },
    ],
    page: 1,
    pageSize: 20,
    totalCount: 1,
    totalPages: 1,
  }),
  createInvoice: vi.fn(),
  issueInvoice: vi.fn(),
  voidInvoice: vi.fn(),
}))

vi.mock('../src/api/customers', () => ({
  searchCustomers: vi.fn().mockResolvedValue({
    items: [],
    page: 1,
    pageSize: 20,
    totalCount: 0,
    totalPages: 0,
  }),
}))

describe('InvoicesPage', () => {
  it('renders invoices list and action buttons', async () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    })

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <AuthProvider>
            <InvoicesPage />
          </AuthProvider>
        </MemoryRouter>
      </QueryClientProvider>,
    )

    expect(await screen.findByRole('heading', { name: /^Invoices$/i })).toBeInTheDocument()
    expect(await screen.findByText('INV-202611-0001')).toBeInTheDocument()
    expect(screen.getByText('Acme Corp')).toBeInTheDocument()
    expect(screen.getAllByText('$1500.00')).toHaveLength(2)
    expect(screen.getByRole('button', { name: /New invoice/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Issue/i })).toBeInTheDocument()
  })
})
