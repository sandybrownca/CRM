import { Alert, Box, Button, Paper, Stack, TextField, Typography } from '@mui/material'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

const schema = z.object({
  email: z.string().email(),
  password: z.string().min(1, 'Password is required'),
})

type FormValues = z.infer<typeof schema>

export function LoginPage() {
  const { login, user, loading } = useAuth()
  const navigate = useNavigate()
  const [error, setError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      email: 'admin@local.dev',
      password: 'ChangeMe!Admin123',
    },
  })

  if (!loading && user) {
    return <Navigate to="/customers" replace />
  }

  const onSubmit = handleSubmit(async (values) => {
    setError(null)
    try {
      await login(values.email, values.password)
      navigate('/customers')
    } catch {
      setError('Invalid email or password.')
    }
  })

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: 'linear-gradient(160deg, #0f2744 0%, #1f4e79 45%, #d9e6f2 100%)',
      }}
    >
      <Paper sx={{ p: 4, width: 420, maxWidth: '92vw' }} elevation={4}>
        <Stack spacing={2} component="form" onSubmit={onSubmit}>
          <Typography variant="h4" sx={{ fontWeight: 700 }}>
            Business Operations CRM
          </Typography>
          <Typography color="text.secondary">
            Sign in with a demo account to manage customers.
          </Typography>
          {error && <Alert severity="error">{error}</Alert>}
          <TextField
            label="Email"
            {...register('email')}
            error={!!errors.email}
            helperText={errors.email?.message}
            fullWidth
          />
          <TextField
            label="Password"
            type="password"
            {...register('password')}
            error={!!errors.password}
            helperText={errors.password?.message}
            fullWidth
          />
          <Button type="submit" variant="contained" size="large" disabled={isSubmitting}>
            Sign in
          </Button>
        </Stack>
      </Paper>
    </Box>
  )
}
