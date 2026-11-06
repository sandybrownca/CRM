import { apiFetch } from './client'
import type { InvoiceStatus } from './invoices'

export type Payment = {
  id: string
  invoiceId: string
  invoiceNumber: string
  customerId: string
  customerName: string
  amount: number
  paidAtUtc: string
  reference?: string | null
  method: 'Manual' | 'SimulatedCard'
  cardBrand?: string | null
  cardLast4?: string | null
  authorizationCode?: string | null
  invoiceAmountPaid: number
  invoiceBalanceDue: number
  invoiceStatus: InvoiceStatus
  createdAtUtc: string
}

export type RecordPaymentInput = {
  amount: number
  reference?: string
  method?: 'Manual' | 'SimulatedCard'
  cardBrand?: string
  cardLast4?: string
}

export async function recordPayment(invoiceId: string, input: RecordPaymentInput): Promise<Payment> {
  return apiFetch<Payment>(`/api/v1/invoices/${invoiceId}/payments`, {
    method: 'POST',
    body: JSON.stringify({
      amount: input.amount,
      reference: input.reference ?? null,
      paidAtUtc: null,
      method: input.method ?? 'Manual',
      cardBrand: input.cardBrand ?? null,
      cardLast4: input.cardLast4 ?? null,
    }),
  })
}