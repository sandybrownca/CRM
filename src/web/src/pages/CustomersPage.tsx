import {
  Alert,
  AppBar,
  Box,
  Button,
  CircularProgress,
  Container,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
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
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Controller, useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { Link as RouterLink, Navigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import {
  createCustomer,
  getCustomer,
  logInteraction,
  searchCustomerActivities,
  searchCustomerInteractions,
  searchCustomers,
  updateCustomer,
  type CustomerDetails,
  type CustomerInput,
  type CustomerKind,
  type InteractionChannel,
} from '../api/customers'

const schema = z.object({
  kind: z.enum(['Individual', 'Company']),
  name: z.string().min(1, 'Name is required'),
  email: z.string().email(),
  phone: z.string().optional(),
  jobTitle: z.string().max(100).optional(),
  billingAddressLine1: z.string().optional(),
  billingAddressLine2: z.string().optional(),
  billingCity: z.string().optional(),
  billingState: z.string().optional(),
  billingPostalCode: z.string().optional(),
  billingCountry: z.string().optional(),
  shippingAddressLine1: z.string().optional(),
  shippingAddressLine2: z.string().optional(),
  shippingCity: z.string().optional(),
  shippingState: z.string().optional(),
  shippingPostalCode: z.string().optional(),
  shippingCountry: z.string().optional(),
})

type FormValues = z.infer<typeof schema>

const emptyForm: FormValues = {
  kind: 'Company',
  name: '',
  email: '',
  phone: '',
  jobTitle: '',
  billingAddressLine1: '',
  billingAddressLine2: '',
  billingCity: '',
  billingState: '',
  billingPostalCode: '',
  billingCountry: '',
  shippingAddressLine1: '',
  shippingAddressLine2: '',
  shippingCity: '',
  shippingState: '',
  shippingPostalCode: '',
  shippingCountry: '',
}

const bodyLabel: Record<InteractionChannel, string> = {
  Phone: 'Call notes',
  Email: 'Email',
  Chat: 'Transcript',
  Meeting: 'Meeting notes',
}

function problemMessage(error: Error, fallback: string) {
  try {
    const body = JSON.parse(error.message) as { errors?: Record<string, string[]>; title?: string }
    const first = body.errors ? Object.values(body.errors).flat()[0] : undefined
    return first || body.title || fallback
  } catch {
    return fallback
  }
}

function blank(value?: string | null) {
  const trimmed = value?.trim()
  return trimmed ? trimmed : undefined
}

function toInput(values: FormValues): CustomerInput {
  return {
    kind: values.kind,
    name: values.name.trim(),
    email: values.email.trim(),
    phone: blank(values.phone),
    jobTitle: values.kind === 'Individual' ? blank(values.jobTitle) : undefined,
    billingAddressLine1: blank(values.billingAddressLine1),
    billingAddressLine2: blank(values.billingAddressLine2),
    billingCity: blank(values.billingCity),
    billingState: blank(values.billingState),
    billingPostalCode: blank(values.billingPostalCode),
    billingCountry: blank(values.billingCountry),
    shippingAddressLine1: blank(values.shippingAddressLine1),
    shippingAddressLine2: blank(values.shippingAddressLine2),
    shippingCity: blank(values.shippingCity),
    shippingState: blank(values.shippingState),
    shippingPostalCode: blank(values.shippingPostalCode),
    shippingCountry: blank(values.shippingCountry),
  }
}

function toForm(customer: CustomerDetails): FormValues {
  return {
    kind: customer.kind,
    name: customer.name,
    email: customer.email,
    phone: customer.phone ?? '',
    jobTitle: customer.jobTitle ?? '',
    billingAddressLine1: customer.billingAddressLine1 ?? '',
    billingAddressLine2: customer.billingAddressLine2 ?? '',
    billingCity: customer.billingCity ?? '',
    billingState: customer.billingState ?? '',
    billingPostalCode: customer.billingPostalCode ?? '',
    billingCountry: customer.billingCountry ?? '',
    shippingAddressLine1: customer.shippingAddressLine1 ?? '',
    shippingAddressLine2: customer.shippingAddressLine2 ?? '',
    shippingCity: customer.shippingCity ?? '',
    shippingState: customer.shippingState ?? '',
    shippingPostalCode: customer.shippingPostalCode ?? '',
    shippingCountry: customer.shippingCountry ?? '',
  }
}

function AddressFields({
  title,
  prefix,
  register,
}: {
  title: string
  prefix: 'billing' | 'shipping'
  register: ReturnType<typeof useForm<FormValues>>['register']
}) {
  return (
    <Stack spacing={2}>
      <Typography sx={{ fontWeight: 700 }}>{title}</Typography>
      <TextField label="Address line 1" {...register(`${prefix}AddressLine1`)} fullWidth />
      <TextField label="Address line 2" {...register(`${prefix}AddressLine2`)} fullWidth />
      <TextField label="City" {...register(`${prefix}City`)} fullWidth />
      <TextField label="State" {...register(`${prefix}State`)} fullWidth />
      <TextField label="Postal code" {...register(`${prefix}PostalCode`)} fullWidth />
      <TextField label="Country" {...register(`${prefix}Country`)} fullWidth />
    </Stack>
  )
}

export function CustomersPage() {
  const { user, loading, logout, isAdmin } = useAuth()
  const [search, setSearch] = useState('')
  const [open, setOpen] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [pageError, setPageError] = useState<string | null>(null)
  const [historyCustomer, setHistoryCustomer] = useState<{ id: string; name: string } | null>(null)
  const [logCustomer, setLogCustomer] = useState<{ id: string; name: string } | null>(null)
  const [channel, setChannel] = useState<InteractionChannel>('Phone')
  const [subject, setSubject] = useState('')
  const [body, setBody] = useState('')
  const [occurredAt, setOccurredAt] = useState('')
  const [logError, setLogError] = useState<string | null>(null)
  const queryClient = useQueryClient()

  const customersQuery = useQuery({
    queryKey: ['customers', search],
    queryFn: () => searchCustomers(search),
    enabled: !!user,
  })

  const activitiesQuery = useQuery({
    queryKey: ['customer-activities', historyCustomer?.id],
    queryFn: () => searchCustomerActivities(historyCustomer!.id),
    enabled: !!user && !!historyCustomer,
  })

  const interactionsQuery = useQuery({
    queryKey: ['customer-interactions', logCustomer?.id],
    queryFn: () => searchCustomerInteractions(logCustomer!.id),
    enabled: !!user && !!logCustomer,
  })

  const {
    register,
    handleSubmit,
    reset,
    control,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: emptyForm,
  })

  const kind = watch('kind')

  const saveMutation = useMutation({
    mutationFn: async (values: FormValues) => {
      const input = toInput(values)
      if (editingId) {
        return updateCustomer(editingId, input)
      }
      return createCustomer(input)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['customers'] })
      setOpen(false)
      setEditingId(null)
      reset(emptyForm)
    },
  })

  const logMutation = useMutation({
    mutationFn: async () => {
      if (!logCustomer) {
        throw new Error('Choose a customer first.')
      }
      return logInteraction(logCustomer.id, {
        channel,
        subject: subject.trim(),
        body: body.trim(),
        occurredAtUtc: occurredAt ? new Date(occurredAt).toISOString() : undefined,
      })
    },
    onSuccess: async () => {
      setSubject('')
      setBody('')
      setOccurredAt('')
      setLogError(null)
      await queryClient.invalidateQueries({ queryKey: ['customer-interactions', logCustomer?.id] })
      await queryClient.invalidateQueries({ queryKey: ['customer-activities', logCustomer?.id] })
    },
  })

  function openCreate() {
    setEditingId(null)
    setPageError(null)
    reset(emptyForm)
    setOpen(true)
  }

  async function openEdit(id: string) {
    setPageError(null)
    try {
      const customer = await getCustomer(id)
      setEditingId(customer.id)
      reset(toForm(customer))
      setOpen(true)
    } catch (error) {
      setPageError(problemMessage(error as Error, 'Could not load that customer.'))
    }
  }

  function openLog(customer: { id: string; name: string }) {
    setLogCustomer(customer)
    setChannel('Phone')
    setSubject('')
    setBody('')
    setOccurredAt('')
    setLogError(null)
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
          <Button component={RouterLink} to="/customers" color="primary" sx={{ fontWeight: 700 }}>
            Customers
          </Button>
          <Button component={RouterLink} to="/invoices" color="inherit">
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
              Customers
            </Typography>
            <Typography color="text.secondary">
              People and companies, with addresses and a log of calls, email, chats, and meetings.
            </Typography>
          </Box>
          <Button variant="contained" onClick={openCreate} sx={{ alignSelf: 'center', flexShrink: 0 }}>
            New customer
          </Button>
        </Stack>

        <TextField
          label="Search customers"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          fullWidth
          sx={{ mb: 2, bgcolor: 'white' }}
        />

        {pageError && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setPageError(null)}>
            {pageError}
          </Alert>
        )}
        {customersQuery.isLoading && <CircularProgress />}
        {customersQuery.isError && <Alert severity="error">Failed to load customers.</Alert>}

        {customersQuery.data && (
          <Box sx={{ bgcolor: 'white', borderRadius: 2, overflow: 'auto' }}>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Name</TableCell>
                  <TableCell>Type</TableCell>
                  <TableCell>Job title</TableCell>
                  <TableCell>Email</TableCell>
                  <TableCell>City</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="right">Outstanding</TableCell>
                  <TableCell align="right">Open invoices</TableCell>
                  <TableCell />
                </TableRow>
              </TableHead>
              <TableBody>
                {customersQuery.data.items.map((customer) => (
                  <TableRow key={customer.id} hover>
                    <TableCell>{customer.name}</TableCell>
                    <TableCell>{customer.kind}</TableCell>
                    <TableCell>{customer.jobTitle || '—'}</TableCell>
                    <TableCell>{customer.email}</TableCell>
                    <TableCell>{customer.billingCity || '—'}</TableCell>
                    <TableCell>{customer.status}</TableCell>
                    <TableCell align="right">${customer.outstandingBalance.toFixed(2)}</TableCell>
                    <TableCell align="right">
                      {customer.openInvoiceCount > 0 ? (
                        <Button
                          component={RouterLink}
                          to={`/invoices?customerId=${customer.id}`}
                          size="small"
                          sx={{ minWidth: 'auto', fontWeight: 600 }}
                        >
                          {customer.openInvoiceCount} open
                        </Button>
                      ) : (
                        0
                      )}
                    </TableCell>
                    <TableCell align="right">
                      <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
                        <Button size="small" variant="outlined" onClick={() => void openEdit(customer.id)} sx={{ whiteSpace: 'nowrap' }}>
                          Edit
                        </Button>
                        <Button size="small" variant="outlined" onClick={() => openLog(customer)} sx={{ whiteSpace: 'nowrap' }}>
                          Log
                        </Button>
                        <Button
                          size="small"
                          variant="outlined"
                          onClick={() => setHistoryCustomer({ id: customer.id, name: customer.name })}
                          sx={{ whiteSpace: 'nowrap' }}
                        >
                          History
                        </Button>
                      </Stack>
                    </TableCell>
                  </TableRow>
                ))}
                {customersQuery.data.items.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={9}>
                      <Typography color="text.secondary">No customers found.</Typography>
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </Box>
        )}
      </Container>

      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="md">
        <DialogTitle>{editingId ? 'Edit customer' : 'Create customer'}</DialogTitle>
        <Box
          component="form"
          onSubmit={handleSubmit(async (values) => {
            await saveMutation.mutateAsync(values)
          })}
        >
          <DialogContent>
            <Stack spacing={2} sx={{ mt: 1 }}>
              {saveMutation.isError && (
                <Alert severity="error">
                  {problemMessage(saveMutation.error as Error, 'Could not save the customer.')}
                </Alert>
              )}
              <Controller
                name="kind"
                control={control}
                render={({ field }) => (
                  <FormControl fullWidth>
                    <InputLabel id="customer-kind-label">Type</InputLabel>
                    <Select
                      labelId="customer-kind-label"
                      label="Type"
                      value={field.value}
                      onChange={(event) => field.onChange(event.target.value as CustomerKind)}
                    >
                      <MenuItem value="Individual">Individual</MenuItem>
                      <MenuItem value="Company">Company</MenuItem>
                    </Select>
                  </FormControl>
                )}
              />
              <TextField
                label={kind === 'Individual' ? 'Full name' : 'Company name'}
                {...register('name')}
                error={!!errors.name}
                helperText={errors.name?.message}
                fullWidth
              />
              {kind === 'Individual' && (
                <TextField
                  label="Job title"
                  {...register('jobTitle')}
                  error={!!errors.jobTitle}
                  helperText={errors.jobTitle?.message || 'Optional. Applies to an individual.'}
                  fullWidth
                />
              )}
              <TextField
                label="Email"
                {...register('email')}
                error={!!errors.email}
                helperText={errors.email?.message}
                fullWidth
              />
              <TextField label="Phone" {...register('phone')} fullWidth />
              <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 3 }}>
                <AddressFields title="Billing address" prefix="billing" register={register} />
                <AddressFields title="Shipping address" prefix="shipping" register={register} />
              </Box>
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={isSubmitting || saveMutation.isPending}>
              {editingId ? 'Save' : 'Create'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      <Dialog open={!!logCustomer} onClose={() => setLogCustomer(null)} fullWidth maxWidth="sm">
        <DialogTitle>Interaction log · {logCustomer?.name}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {interactionsQuery.isLoading && <CircularProgress size={24} />}
            {interactionsQuery.isError && <Alert severity="error">Could not load interaction logs.</Alert>}
            {logError && <Alert severity="error">{logError}</Alert>}
            {interactionsQuery.data?.items.map((entry) => (
              <Box key={entry.id} sx={{ borderBottom: '1px solid #e1e7ec', pb: 1 }}>
                <Typography sx={{ fontWeight: 600 }}>
                  {entry.channel} · {entry.subject}
                </Typography>
                <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
                  {entry.body}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {new Date(entry.occurredAtUtc).toLocaleString()} · {entry.recordedByName}
                </Typography>
              </Box>
            ))}
            {interactionsQuery.data && interactionsQuery.data.items.length === 0 && (
              <Typography color="text.secondary">No calls, emails, chats, or meetings yet.</Typography>
            )}
            <FormControl fullWidth>
              <InputLabel id="interaction-channel-label">Channel</InputLabel>
              <Select
                labelId="interaction-channel-label"
                label="Channel"
                value={channel}
                onChange={(event) => setChannel(event.target.value as InteractionChannel)}
              >
                <MenuItem value="Phone">Phone call</MenuItem>
                <MenuItem value="Email">Email</MenuItem>
                <MenuItem value="Chat">Chat</MenuItem>
                <MenuItem value="Meeting">Meeting</MenuItem>
              </Select>
            </FormControl>
            <TextField label="Subject" value={subject} onChange={(event) => setSubject(event.target.value)} fullWidth />
            <TextField
              label={bodyLabel[channel]}
              value={body}
              onChange={(event) => setBody(event.target.value)}
              fullWidth
              multiline
              minRows={3}
            />
            <TextField
              label="When"
              type="datetime-local"
              value={occurredAt}
              onChange={(event) => setOccurredAt(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
              helperText="Leave blank to use the current time."
              fullWidth
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setLogCustomer(null)}>Close</Button>
          <Button
            variant="contained"
            disabled={logMutation.isPending || !subject.trim() || !body.trim()}
            onClick={() => {
              setLogError(null)
              logMutation.mutate(undefined, {
                onError: (error) => setLogError(problemMessage(error as Error, 'Could not save the interaction.')),
              })
            }}
          >
            Add to log
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={!!historyCustomer} onClose={() => setHistoryCustomer(null)} fullWidth maxWidth="sm">
        <DialogTitle>History · {historyCustomer?.name}</DialogTitle>
        <DialogContent>
          {activitiesQuery.isLoading && <CircularProgress size={24} />}
          {activitiesQuery.isError && <Alert severity="error">Could not load history.</Alert>}
          {activitiesQuery.data && (
            <Stack spacing={1.5} sx={{ mt: 1 }}>
              {activitiesQuery.data.items.map((activity) => (
                <Box key={activity.id}>
                  <Typography sx={{ fontWeight: 600 }}>{activity.eventType}</Typography>
                  <Typography variant="body2">{activity.description}</Typography>
                  <Typography variant="caption" color="text.secondary">
                    {new Date(activity.occurredAtUtc).toLocaleString()}
                  </Typography>
                </Box>
              ))}
              {activitiesQuery.data.items.length === 0 && (
                <Typography color="text.secondary">No activity yet.</Typography>
              )}
            </Stack>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setHistoryCustomer(null)}>Close</Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}