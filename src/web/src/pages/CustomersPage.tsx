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
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { Link as RouterLink, Navigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { createCustomer, searchCustomers } from '../api/customers'

const schema = z.object({
  name: z.string().min(1, 'Name is required'),
  email: z.string().email(),
  phone: z.string().optional(),
})

type FormValues = z.infer<typeof schema>

export function CustomersPage() {
  const { user, loading, logout } = useAuth()
  const [search, setSearch] = useState('')
  const [open, setOpen] = useState(false)
  const queryClient = useQueryClient()

  const customersQuery = useQuery({
    queryKey: ['customers', search],
    queryFn: () => searchCustomers(search),
    enabled: !!user,
  })

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
  })

  const createMutation = useMutation({
    mutationFn: createCustomer,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['customers'] })
      setOpen(false)
      reset()
    },
  })

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
          <Button component={RouterLink} to="/customers" color="primary" sx={{ fontWeight: 700 }}>
            Customers
          </Button>
          <Button component={RouterLink} to="/invoices" color="inherit">
            Invoices
          </Button>
          <Box sx={{ flexGrow: 1 }} />
          <Typography variant="body2">{user.displayName}</Typography>
          <Button onClick={() => void logout()}>Sign out</Button>
        </Toolbar>
      </AppBar>

      <Container sx={{ py: 4 }}>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={2}
          sx={{ mb: 3, justifyContent: 'space-between' }}
        >
          <Box>
            <Typography variant="h4" sx={{ fontWeight: 700 }}>
              Customers
            </Typography>
            <Typography color="text.secondary">Search, create, and review customer records.</Typography>
          </Box>
          <Button variant="contained" onClick={() => setOpen(true)}>
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

        {customersQuery.isLoading && <CircularProgress />}
        {customersQuery.isError && <Alert severity="error">Failed to load customers.</Alert>}

        {customersQuery.data && (
          <Box sx={{ bgcolor: 'white', borderRadius: 2, overflow: 'auto' }}>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Name</TableCell>
                  <TableCell>Email</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="right">Outstanding</TableCell>
                  <TableCell align="right">Open invoices</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {customersQuery.data.items.map((customer) => (
                  <TableRow key={customer.id} hover>
                    <TableCell>{customer.name}</TableCell>
                    <TableCell>{customer.email}</TableCell>
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
                  </TableRow>
                ))}
                {customersQuery.data.items.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={5}>
                      <Typography color="text.secondary">No customers found.</Typography>
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </Box>
        )}
      </Container>

      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>Create customer</DialogTitle>
        <Box
          component="form"
          onSubmit={handleSubmit(async (values) => {
            await createMutation.mutateAsync(values)
          })}
        >
          <DialogContent>
            <Stack spacing={2} sx={{ mt: 1 }}>
              {createMutation.isError && (
                <Alert severity="error">Could not create customer. Check validation and try again.</Alert>
              )}
              <TextField
                label="Name"
                {...register('name')}
                error={!!errors.name}
                helperText={errors.name?.message}
                fullWidth
              />
              <TextField
                label="Email"
                {...register('email')}
                error={!!errors.email}
                helperText={errors.email?.message}
                fullWidth
              />
              <TextField label="Phone" {...register('phone')} fullWidth />
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={isSubmitting || createMutation.isPending}>
              Create
            </Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Box>
  )
}
