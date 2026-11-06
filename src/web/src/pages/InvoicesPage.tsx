import {
  Alert,
  AppBar,
  Box,
  Button,
  Chip,
  CircularProgress,
  Container,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Toolbar,
  Typography,
} from '@mui/material'
import DeleteIcon from '@mui/icons-material/Delete'
import AddIcon from '@mui/icons-material/Add'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Link as RouterLink, Navigate, useSearchParams } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { searchCustomers } from '../api/customers'
import {
  createInvoice,
  issueInvoice,
  searchInvoices,
  syncInvoiceToErp,
  voidInvoice,
  type CreateLineItemInput,
  type InvoiceListItem,
  type InvoiceStatus,
} from '../api/invoices'
import { recordPayment } from '../api/payments'

export function InvoicesPage() {
  const { user, loading, logout, isAdmin } = useAuth()
  const [searchParams, setSearchParams] = useSearchParams()
  const customerIdFilter = searchParams.get('customerId') || undefined
  const [statusFilter, setStatusFilter] = useState<InvoiceStatus | ''>('')
  const [open, setOpen] = useState(false)
  const [errorMsg, setErrorMsg] = useState<string | null>(null)
  const [paymentTarget, setPaymentTarget] = useState<InvoiceListItem | null>(null)
  const [paymentAmount, setPaymentAmount] = useState('')
  const [paymentReference, setPaymentReference] = useState('')
  const [paymentMethod, setPaymentMethod] = useState<'Manual' | 'SimulatedCard'>('Manual')
  const [cardBrand, setCardBrand] = useState('Visa')
  const [cardLast4, setCardLast4] = useState('')
  const [paymentError, setPaymentError] = useState<string | null>(null)
  const [paymentNotice, setPaymentNotice] = useState<string | null>(null)
  const queryClient = useQueryClient()

  // New Invoice Form State
  const today = new Date().toISOString().split('T')[0]
  const in14Days = new Date(Date.now() + 14 * 24 * 60 * 60 * 1000).toISOString().split('T')[0]

  const [selectedCustomerId, setSelectedCustomerId] = useState<string>('')
  const [issueDate, setIssueDate] = useState<string>(today)
  const [dueDate, setDueDate] = useState<string>(in14Days)
  const [lineItems, setLineItems] = useState<CreateLineItemInput[]>([
    { description: '', quantity: 1, unitPrice: 0 },
  ])

  const invoicesQuery = useQuery({
    queryKey: ['invoices', customerIdFilter, statusFilter],
    queryFn: () =>
      searchInvoices({
        customerId: customerIdFilter,
        status: statusFilter ? statusFilter : undefined,
      }),
    enabled: !!user,
  })

  const customersQuery = useQuery({
    queryKey: ['customers-list-lookup'],
    queryFn: () => searchCustomers('', 1, 100),
    enabled: !!user && open,
  })

  const createMutation = useMutation({
    mutationFn: createInvoice,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['invoices'] })
      await queryClient.invalidateQueries({ queryKey: ['customers'] })
      setOpen(false)
      resetForm()
    },
    onError: (err: Error) => {
      setErrorMsg(err.message || 'Failed to create invoice.')
    },
  })

  const issueMutation = useMutation({
    mutationFn: issueInvoice,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['invoices'] })
      await queryClient.invalidateQueries({ queryKey: ['customers'] })
    },
  })

  const voidMutation = useMutation({
    mutationFn: voidInvoice,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['invoices'] })
      await queryClient.invalidateQueries({ queryKey: ['customers'] })
    },
  })

  const erpMutation = useMutation({
    mutationFn: syncInvoiceToErp,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['invoices'] })
      await queryClient.invalidateQueries({ queryKey: ['dashboard'] })
    },
  })

  const paymentMutation = useMutation({
    mutationFn: (input: {
      invoiceId: string
      amount: number
      reference?: string
      method: 'Manual' | 'SimulatedCard'
      cardBrand?: string
      cardLast4?: string
    }) => recordPayment(input.invoiceId, input),
    onSuccess: async (payment) => {
      await queryClient.invalidateQueries({ queryKey: ['invoices'] })
      await queryClient.invalidateQueries({ queryKey: ['customers'] })
      await queryClient.invalidateQueries({ queryKey: ['dashboard'] })
      setPaymentTarget(null)
      setPaymentNotice(
        payment.authorizationCode
          ? `Simulated authorization ${payment.authorizationCode} for ${payment.cardBrand} ending ${payment.cardLast4}. The card number was not stored.`
          : null,
      )
    },
    onError: (err: Error) => {
      setPaymentError(err.message || 'Failed to record payment.')
    },
  })

  const resetForm = () => {
    setSelectedCustomerId('')
    setIssueDate(today)
    setDueDate(in14Days)
    setLineItems([{ description: '', quantity: 1, unitPrice: 0 }])
    setErrorMsg(null)
  }

  const handleAddLineItem = () => {
    setLineItems([...lineItems, { description: '', quantity: 1, unitPrice: 0 }])
  }

  const handleRemoveLineItem = (index: number) => {
    setLineItems(lineItems.filter((_, i) => i !== index))
  }

  const handleLineItemChange = (
    index: number,
    field: keyof CreateLineItemInput,
    value: string | number,
  ) => {
    const updated = [...lineItems]
    if (field === 'quantity' || field === 'unitPrice') {
      updated[index] = { ...updated[index], [field]: Number(value) }
    } else {
      updated[index] = { ...updated[index], description: String(value) }
    }
    setLineItems(updated)
  }

  const calculatedTotal = lineItems.reduce(
    (sum, item) => sum + (Number(item.quantity) || 0) * (Number(item.unitPrice) || 0),
    0,
  )

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setErrorMsg(null)

    if (!selectedCustomerId) {
      setErrorMsg('Please select a customer.')
      return
    }

    if (lineItems.length === 0 || lineItems.some((li) => !li.description.trim())) {
      setErrorMsg('All line items must have a description.')
      return
    }

    if (calculatedTotal <= 0) {
      setErrorMsg('Invoice total must be greater than $0.00.')
      return
    }

    await createMutation.mutateAsync({
      customerId: selectedCustomerId,
      issueDate,
      dueDate,
      lineItems,
    })
  }

  if (loading) {
    return (
      <Box sx={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <CircularProgress />
      </Box>
    )
  }

  if (!user) {
    return <Navigate to="/login" replace />
  }

  const getStatusColor = (status: InvoiceStatus) => {
    switch (status) {
      case 'Draft':
        return 'default'
      case 'Issued':
        return 'primary'
      case 'Paid':
        return 'success'
      case 'Void':
        return 'error'
    }
  }

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: '#f4f7fb' }}>
      <AppBar position="static" color="transparent" elevation={0} sx={{ borderBottom: '1px solid #d7e0ea' }}>
        <Toolbar sx={{ gap: 2 }}>
          <Typography variant="h6" sx={{ fontWeight: 700, mr: 2 }}>
            Business Operations CRM
          </Typography>
          <Button component={RouterLink} to="/dashboard" color="inherit">
            Dashboard
          </Button>
          <Button component={RouterLink} to="/customers" color="inherit">
            Customers
          </Button>
          <Button component={RouterLink} to="/invoices" color="primary" sx={{ fontWeight: 700 }}>
            Invoices
          </Button>
          {isAdmin && (
            <Button component={RouterLink} to="/users" color="inherit">
              Users
            </Button>
          )}
          <Box sx={{ flexGrow: 1 }} />
          <Typography variant="body2">{user.displayName}</Typography>
          <Button onClick={() => void logout()}>Sign out</Button>
        </Toolbar>
      </AppBar>

      <Container sx={{ py: 4 }}>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={2}
          sx={{ mb: 3, justifyContent: 'space-between', alignItems: { sm: 'center' } }}
        >
          <Box>
            <Typography variant="h4" sx={{ fontWeight: 700 }}>
              Invoices
            </Typography>
            <Typography color="text.secondary">
              Create and issue invoices, then record payments against the balance due.
            </Typography>
          </Box>
          <Button
            variant="contained"
            onClick={() => {
              resetForm()
              setOpen(true)
            }}
            sx={{ alignSelf: 'center', flexShrink: 0 }}
          >
            New invoice
          </Button>
        </Stack>

        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 3 }}>
          <FormControl sx={{ minWidth: 180, bgcolor: 'white' }} size="small">
            <InputLabel>Status</InputLabel>
            <Select
              value={statusFilter}
              label="Status"
              onChange={(e) => setStatusFilter(e.target.value as InvoiceStatus | '')}
            >
              <MenuItem value="">All Statuses</MenuItem>
              <MenuItem value="Draft">Draft</MenuItem>
              <MenuItem value="Issued">Issued</MenuItem>
              <MenuItem value="Paid">Paid</MenuItem>
              <MenuItem value="Void">Void</MenuItem>
            </Select>
          </FormControl>

          {customerIdFilter && (
            <Button
              variant="outlined"
              size="small"
              onClick={() => {
                searchParams.delete('customerId')
                setSearchParams(searchParams)
              }}
            >
              Clear Customer Filter
            </Button>
          )}
        </Stack>

        {paymentNotice && (
          <Alert severity="success" sx={{ mb: 2 }} onClose={() => setPaymentNotice(null)}>
            {paymentNotice}
          </Alert>
        )}
        {invoicesQuery.isLoading && <CircularProgress />}
        {invoicesQuery.isError && <Alert severity="error">Failed to load invoices.</Alert>}

        {invoicesQuery.data && (
          <Box sx={{ bgcolor: 'white', borderRadius: 2, overflow: 'auto', border: '1px solid #e1e7ec' }}>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell sx={{ fontWeight: 700 }}>Invoice #</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>Customer</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>Issue Date</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>Due Date</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>Status</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>ERP</TableCell>
                  <TableCell align="right" sx={{ fontWeight: 700 }}>
                    Total
                  </TableCell>
                  <TableCell align="right" sx={{ fontWeight: 700 }}>
                    Paid
                  </TableCell>
                  <TableCell align="right" sx={{ fontWeight: 700 }}>
                    Balance Due
                  </TableCell>
                  <TableCell align="center" sx={{ fontWeight: 700 }}>
                    Actions
                  </TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {invoicesQuery.data.items.map((inv) => (
                  <TableRow key={inv.id} hover>
                    <TableCell sx={{ fontWeight: 600 }}>{inv.invoiceNumber}</TableCell>
                    <TableCell>{inv.customerName}</TableCell>
                    <TableCell>{new Date(inv.issueDate).toLocaleDateString()}</TableCell>
                    <TableCell>{new Date(inv.dueDate).toLocaleDateString()}</TableCell>
                    <TableCell>
                      <Chip
                        label={inv.status}
                        size="small"
                        color={getStatusColor(inv.status)}
                        variant={inv.status === 'Draft' ? 'outlined' : 'filled'}
                      />
                    </TableCell>
                    <TableCell>
                      {inv.erpStatus === 'Succeeded' && inv.erpExternalReference
                        ? inv.erpExternalReference
                        : inv.erpStatus === 'Failed'
                          ? 'Failed'
                          : inv.erpStatus === 'Pending'
                            ? 'Pending'
                            : '—'}
                    </TableCell>
                    <TableCell align="right">${inv.totalAmount.toFixed(2)}</TableCell>
                    <TableCell align="right">${inv.amountPaid.toFixed(2)}</TableCell>
                    <TableCell align="right" sx={{ fontWeight: 600 }}>
                      ${inv.balanceDue.toFixed(2)}
                    </TableCell>
                    <TableCell align="center">
                      <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end', flexWrap: 'nowrap' }}>
                        {inv.status === 'Draft' && (
                          <Button
                            size="small"
                            variant="outlined"
                            onClick={() => issueMutation.mutate(inv.id)}
                            disabled={issueMutation.isPending}
                            sx={{ whiteSpace: 'nowrap' }}
                          >
                            Issue
                          </Button>
                        )}
                        {inv.status === 'Issued' && inv.balanceDue > 0 && (
                          <Button
                            size="small"
                            variant="outlined"
                            onClick={() => {
                              setPaymentTarget(inv)
                              setPaymentAmount(inv.balanceDue.toFixed(2))
                              setPaymentReference('')
                              setPaymentMethod('Manual')
                              setCardBrand('Visa')
                              setCardLast4('')
                              setPaymentError(null)
                            }}
                            sx={{ whiteSpace: 'nowrap' }}
                          >
                            Record payment
                          </Button>
                        )}
                        {isAdmin && (inv.status === 'Issued' || inv.status === 'Paid') && (
                          <Button
                            size="small"
                            variant="outlined"
                            onClick={() => erpMutation.mutate(inv.id)}
                            disabled={erpMutation.isPending}
                            sx={{ whiteSpace: 'nowrap' }}
                          >
                            Sync to ERP
                          </Button>
                        )}
                        {(inv.status === 'Draft' || inv.status === 'Issued') && inv.amountPaid === 0 && (
                          <Button
                            size="small"
                            variant="outlined"
                            color="error"
                            onClick={() => voidMutation.mutate(inv.id)}
                            disabled={voidMutation.isPending}
                            sx={{ whiteSpace: 'nowrap' }}
                          >
                            Void
                          </Button>
                        )}
                      </Stack>
                    </TableCell>
                  </TableRow>
                ))}
                {invoicesQuery.data.items.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={10} align="center" sx={{ py: 3 }}>
                      <Typography color="text.secondary">No invoices found.</Typography>
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </Box>
        )}
      </Container>

      {/* New Invoice Dialog */}
      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="md">
        <DialogTitle sx={{ fontWeight: 700 }}>Create New Invoice</DialogTitle>
        <Box component="form" onSubmit={handleSubmit}>
          <DialogContent>
            <Stack spacing={3} sx={{ mt: 1 }}>
              {errorMsg && <Alert severity="error">{errorMsg}</Alert>}

              <FormControl fullWidth required>
                <InputLabel>Customer</InputLabel>
                <Select
                  value={selectedCustomerId}
                  label="Customer"
                  onChange={(e) => setSelectedCustomerId(e.target.value)}
                >
                  {customersQuery.data?.items.map((cust) => (
                    <MenuItem key={cust.id} value={cust.id}>
                      {cust.name} ({cust.email})
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>

              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                <TextField
                  label="Issue Date"
                  type="date"
                  value={issueDate}
                  onChange={(e) => setIssueDate(e.target.value)}
                  fullWidth
                  required
                  slotProps={{ inputLabel: { shrink: true } }}
                />
                <TextField
                  label="Due Date"
                  type="date"
                  value={dueDate}
                  onChange={(e) => setDueDate(e.target.value)}
                  fullWidth
                  required
                  slotProps={{ inputLabel: { shrink: true } }}
                />
              </Stack>

              <Box>
                <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                  <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                    Line Items
                  </Typography>
                  <Button size="small" startIcon={<AddIcon />} onClick={handleAddLineItem}>
                    Add Item
                  </Button>
                </Stack>

                <Stack spacing={2}>
                  {lineItems.map((item, index) => {
                    const lineTotal = (Number(item.quantity) || 0) * (Number(item.unitPrice) || 0)
                    return (
                      <Stack
                        key={index}
                        direction={{ xs: 'column', sm: 'row' }}
                        spacing={1.5}
                        sx={{ alignItems: 'center' }}
                      >
                        <TextField
                          label="Description"
                          value={item.description}
                          onChange={(e) => handleLineItemChange(index, 'description', e.target.value)}
                          fullWidth
                          size="small"
                          required
                        />
                        <TextField
                          label="Qty"
                          type="number"
                          value={item.quantity}
                          onChange={(e) => handleLineItemChange(index, 'quantity', e.target.value)}
                          sx={{ width: { sm: 100 } }}
                          size="small"
                          slotProps={{ htmlInput: { min: 1, step: 1 } }}
                          required
                        />
                        <TextField
                          label="Unit Price ($)"
                          type="number"
                          value={item.unitPrice}
                          onChange={(e) => handleLineItemChange(index, 'unitPrice', e.target.value)}
                          sx={{ width: { sm: 140 } }}
                          size="small"
                          slotProps={{ htmlInput: { min: 0, step: '0.01' } }}
                          required
                        />
                        <Typography sx={{ width: 100, textAlign: 'right', fontWeight: 600 }}>
                          ${lineTotal.toFixed(2)}
                        </Typography>
                        <IconButton
                          size="small"
                          color="error"
                          onClick={() => handleRemoveLineItem(index)}
                          disabled={lineItems.length === 1}
                        >
                          <DeleteIcon fontSize="small" />
                        </IconButton>
                      </Stack>
                    )
                  })}
                </Stack>
              </Box>

              <Box sx={{ display: 'flex', justifyContent: 'flex-end', pt: 1, borderTop: '1px solid #eee' }}>
                <Box sx={{ textAlign: 'right' }}>
                  <Typography variant="h6" sx={{ fontWeight: 700 }}>
                    Subtotal: ${calculatedTotal.toFixed(2)}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    The server adds 8% tax when the draft is saved. The saved total includes that tax.
                  </Typography>
                </Box>
              </Box>
            </Stack>
          </DialogContent>
          <DialogActions sx={{ px: 3, pb: 2 }}>
            <Button onClick={() => setOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={createMutation.isPending}>
              Create Draft Invoice
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      <Dialog open={paymentTarget !== null} onClose={() => setPaymentTarget(null)} fullWidth maxWidth="sm">
        <DialogTitle sx={{ fontWeight: 700 }}>Record payment</DialogTitle>
        <Box
          component="form"
          onSubmit={(event) => {
            event.preventDefault()
            if (!paymentTarget) {
              return
            }

            setPaymentError(null)
            const trimmed = paymentAmount.trim()
            if (!/^\d+(\.\d{1,2})?$/.test(trimmed)) {
              setPaymentError('Enter an amount greater than zero with at most two decimal places.')
              return
            }

            const amount = Number(trimmed)
            if (amount <= 0) {
              setPaymentError('Enter an amount greater than zero with at most two decimal places.')
              return
            }

            const amountCents = Math.round(amount * 100)
            const balanceCents = Math.round(paymentTarget.balanceDue * 100)
            if (amountCents > balanceCents) {
              setPaymentError('Amount cannot exceed the balance due.')
              return
            }

            if (paymentMethod === 'SimulatedCard' && !/^\d{4}$/.test(cardLast4)) {
              setPaymentError('Enter only the last four digits. Do not enter the full card number.')
              return
            }

            paymentMutation.mutate({
              invoiceId: paymentTarget.id,
              amount,
              reference: paymentReference.trim() || undefined,
              method: paymentMethod,
              cardBrand: paymentMethod === 'SimulatedCard' ? cardBrand.trim() : undefined,
              cardLast4: paymentMethod === 'SimulatedCard' ? cardLast4 : undefined,
            })
          }}
        >
          <DialogContent>
            <Stack spacing={2} sx={{ mt: 1 }}>
              {paymentError && <Alert severity="error">{paymentError}</Alert>}
              <Typography>
                {paymentTarget?.invoiceNumber} · {paymentTarget?.customerName}
              </Typography>
              <Typography color="text.secondary">
                Balance due ${paymentTarget ? paymentTarget.balanceDue.toFixed(2) : '0.00'}. The server
                applies this amount to the invoice. A partial payment leaves the invoice issued.
              </Typography>
              <TextField
                label="Amount"
                type="number"
                value={paymentAmount}
                onChange={(e) => setPaymentAmount(e.target.value)}
                fullWidth
                required
                slotProps={{ htmlInput: { min: 0.01, step: '0.01' } }}
              />
              <TextField
                select
                label="Method"
                value={paymentMethod}
                onChange={(e) => setPaymentMethod(e.target.value as 'Manual' | 'SimulatedCard')}
                fullWidth
              >
                <MenuItem value="Manual">Recorded payment</MenuItem>
                <MenuItem value="SimulatedCard">Simulated card</MenuItem>
              </TextField>
              {paymentMethod === 'SimulatedCard' && (
                <>
                  <TextField
                    label="Card brand"
                    value={cardBrand}
                    onChange={(e) => setCardBrand(e.target.value)}
                    fullWidth
                    required
                  />
                  <TextField
                    label="Last four digits"
                    value={cardLast4}
                    onChange={(e) => setCardLast4(e.target.value)}
                    fullWidth
                    required
                    placeholder="4242"
                    slotProps={{ htmlInput: { maxLength: 4, inputMode: 'numeric' } }}
                  />
                </>
              )}
              <TextField
                label="Reference"
                value={paymentReference}
                onChange={(e) => setPaymentReference(e.target.value)}
                fullWidth
                placeholder="Check number or transfer id"
              />
            </Stack>
          </DialogContent>
          <DialogActions sx={{ px: 3, pb: 2 }}>
            <Button onClick={() => setPaymentTarget(null)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={paymentMutation.isPending}>
              Record payment
            </Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Box>
  )
}