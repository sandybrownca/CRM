import {
  Alert,
  AppBar,
  Box,
  Button,
  CircularProgress,
  Container,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Toolbar,
  Typography,
} from '@mui/material'
import { useQuery } from '@tanstack/react-query'
import { Link as RouterLink, Navigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { getDashboard } from '../api/dashboard'

function money(value: number) {
  return `$${value.toFixed(2)}`
}

export function DashboardPage() {
  const { user, loading, logout, isAdmin } = useAuth()
  const dashboardQuery = useQuery({
    queryKey: ['dashboard'],
    queryFn: getDashboard,
    enabled: !!user,
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

  const dashboard = dashboardQuery.data

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: '#f4f7fb' }}>
      <AppBar position="static" color="transparent" elevation={0} sx={{ borderBottom: '1px solid #d7e0ea' }}>
        <Toolbar sx={{ gap: 2 }}>
          <Typography variant="h6" sx={{ fontWeight: 700, mr: 2 }}>
            Business Operations CRM
          </Typography>
          <Button component={RouterLink} to="/dashboard" color="primary" sx={{ fontWeight: 700 }}>
            Dashboard
          </Button>
          <Button component={RouterLink} to="/customers" color="inherit">
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
        <Typography variant="h4" sx={{ fontWeight: 700, mb: 0.5 }}>
          Dashboard
        </Typography>
        <Typography color="text.secondary" sx={{ mb: 3 }}>
          Outstanding balances include issued invoices only. The server calculates every total.
        </Typography>

        {dashboardQuery.isLoading && <CircularProgress />}
        {dashboardQuery.isError && <Alert severity="error">Failed to load the dashboard.</Alert>}

        {dashboard && (
          <Stack spacing={3}>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
              <SummaryCard label="Outstanding" value={money(dashboard.outstandingBalance)} />
              <SummaryCard label="Open invoices" value={String(dashboard.openInvoiceCount)} />
              <SummaryCard label="Overdue" value={String(dashboard.overdueInvoiceCount)} />
              <SummaryCard label="Active customers" value={String(dashboard.activeCustomerCount)} />
            </Stack>

            <Paper sx={{ p: 2 }}>
              <Typography variant="h6" sx={{ fontWeight: 700, mb: 1 }}>
                Open invoices
              </Typography>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Invoice</TableCell>
                    <TableCell>Customer</TableCell>
                    <TableCell>Due</TableCell>
                    <TableCell align="right">Balance</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {dashboard.openInvoices.map((invoice) => (
                    <TableRow key={invoice.id}>
                      <TableCell>{invoice.invoiceNumber}</TableCell>
                      <TableCell>{invoice.customerName}</TableCell>
                      <TableCell>{new Date(invoice.dueDate).toLocaleDateString()}</TableCell>
                      <TableCell align="right">{money(invoice.balanceDue)}</TableCell>
                    </TableRow>
                  ))}
                  {dashboard.openInvoices.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={4}>No issued invoices.</TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </Paper>

            <Paper sx={{ p: 2 }}>
              <Typography variant="h6" sx={{ fontWeight: 700, mb: 1 }}>
                Recent activity
              </Typography>
              <Stack spacing={1}>
                {dashboard.recentActivity.map((activity) => (
                  <Box key={activity.id}>
                    <Typography sx={{ fontWeight: 600 }}>
                      {activity.customerName} · {activity.eventType}
                    </Typography>
                    <Typography color="text.secondary" variant="body2">
                      {activity.description}
                    </Typography>
                  </Box>
                ))}
                {dashboard.recentActivity.length === 0 && (
                  <Typography color="text.secondary">No activity yet.</Typography>
                )}
              </Stack>
            </Paper>
          </Stack>
        )}
      </Container>
    </Box>
  )
}

function SummaryCard({ label, value }: { label: string; value: string }) {
  return (
    <Paper sx={{ p: 2, flex: 1 }}>
      <Typography color="text.secondary" variant="body2">
        {label}
      </Typography>
      <Typography variant="h5" sx={{ fontWeight: 700 }}>
        {value}
      </Typography>
    </Paper>
  )
}