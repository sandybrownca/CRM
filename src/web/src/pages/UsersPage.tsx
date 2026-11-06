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
  MenuItem,
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
import { useState } from 'react'
import { Link as RouterLink, Navigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { createUser, searchUsers, updateUser, type AppRole, type UserAccount } from '../api/users'

function problemMessage(error: Error) {
  try {
    const body = JSON.parse(error.message) as { errors?: Record<string, string[]>; title?: string }
    const first = body.errors ? Object.values(body.errors).flat()[0] : undefined
    return first || body.title || 'Could not save the user.'
  } catch {
    return 'Could not save the user.'
  }
}

export function UsersPage() {
  const { user, loading, logout, isAdmin } = useAuth()
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [open, setOpen] = useState(false)
  const [email, setEmail] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [password, setPassword] = useState('')
  const [role, setRole] = useState<AppRole>('Operations')
  const [formError, setFormError] = useState<string | null>(null)

  const usersQuery = useQuery({
    queryKey: ['users', search],
    queryFn: () => searchUsers(search),
    enabled: !!user && isAdmin,
  })

  const createMutation = useMutation({
    mutationFn: createUser,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['users'] })
      setOpen(false)
      setEmail('')
      setDisplayName('')
      setPassword('')
      setRole('Operations')
      setFormError(null)
    },
    onError: (error: Error) => setFormError(problemMessage(error)),
  })

  const updateMutation = useMutation({
    mutationFn: (input: { account: UserAccount; role: AppRole; isActive: boolean }) =>
      updateUser(input.account.id, {
        displayName: input.account.displayName,
        role: input.role,
        isActive: input.isActive,
      }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['users'] })
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

  if (!isAdmin) {
    return <Navigate to="/dashboard" replace />
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
          <Button component={RouterLink} to="/invoices" color="inherit">
            Invoices
          </Button>
          <Button component={RouterLink} to="/users" color="primary" sx={{ fontWeight: 700 }}>
            Users
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
          sx={{ mb: 3, justifyContent: 'space-between', alignItems: { sm: 'center' } }}
        >
          <Box>
            <Typography variant="h4" sx={{ fontWeight: 700 }}>
              Users
            </Typography>
            <Typography color="text.secondary">
              Administrators manage who can sign in and which role they have.
            </Typography>
          </Box>
          <Button variant="contained" onClick={() => setOpen(true)} sx={{ alignSelf: 'center', flexShrink: 0 }}>
            New user
          </Button>
        </Stack>

        <TextField
          label="Search users"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          size="small"
          sx={{ mb: 2, bgcolor: 'white', minWidth: 280 }}
        />

        {usersQuery.isLoading && <CircularProgress />}
        {usersQuery.isError && <Alert severity="error">Failed to load users.</Alert>}
        {updateMutation.isError && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {problemMessage(updateMutation.error)}
          </Alert>
        )}

        {usersQuery.data && (
          <Box sx={{ bgcolor: 'white', borderRadius: 2, overflow: 'auto', border: '1px solid #e1e7ec' }}>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Name</TableCell>
                  <TableCell>Email</TableCell>
                  <TableCell>Role</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell />
                </TableRow>
              </TableHead>
              <TableBody>
                {usersQuery.data.items.map((account) => (
                  <TableRow key={account.id}>
                    <TableCell>{account.displayName}</TableCell>
                    <TableCell>{account.email}</TableCell>
                    <TableCell>
                      <TextField
                        select
                        size="small"
                        label="Role"
                        value={account.roles[0] ?? 'Operations'}
                        onChange={(event) =>
                          updateMutation.mutate({
                            account,
                            role: event.target.value as AppRole,
                            isActive: account.isActive,
                          })
                        }
                        sx={{ minWidth: 160 }}
                      >
                        <MenuItem value="Operations">Operations</MenuItem>
                        <MenuItem value="Administrator">Administrator</MenuItem>
                      </TextField>
                    </TableCell>
                    <TableCell>{account.isActive ? 'Active' : 'Inactive'}</TableCell>
                    <TableCell align="right">
                      <Button
                        size="small"
                        onClick={() =>
                          updateMutation.mutate({
                            account,
                            role: account.roles[0] ?? 'Operations',
                            isActive: !account.isActive,
                          })
                        }
                        disabled={updateMutation.isPending || account.id === user.id}
                      >
                        {account.isActive ? 'Deactivate' : 'Activate'}
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
                {usersQuery.data.items.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={5}>No users found.</TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </Box>
        )}
      </Container>

      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>Create user</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {formError && <Alert severity="error">{formError}</Alert>}
            <TextField label="Name" value={displayName} onChange={(event) => setDisplayName(event.target.value)} fullWidth />
            <TextField label="Email" value={email} onChange={(event) => setEmail(event.target.value)} fullWidth />
            <TextField
              label="Password"
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              helperText="At least 10 characters, with upper and lower case, a number, and a symbol."
              fullWidth
            />
            <TextField select label="Role" value={role} onChange={(event) => setRole(event.target.value as AppRole)} fullWidth>
              <MenuItem value="Operations">Operations</MenuItem>
              <MenuItem value="Administrator">Administrator</MenuItem>
            </TextField>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            disabled={createMutation.isPending}
            onClick={() => createMutation.mutate({ email, displayName, password, role })}
          >
            Create
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}