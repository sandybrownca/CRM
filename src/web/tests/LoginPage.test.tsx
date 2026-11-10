
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { LoginPage } from '../src/pages/LoginPage'
import { useAuth } from '../src/auth/AuthContext'

const mockNavigate = vi.fn()
const mockLogin = vi.fn()

const mockAuth = vi.hoisted(() => ({
  login: vi.fn(),
  user: null as null | { id: string; email: string },
  loading: false,
}))

vi.mock('../src/auth/AuthContext', () => ({
  useAuth: () => mockAuth,
  AuthProvider: ({ children }: { children: React.ReactNode }) => children,
}))

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>(
    'react-router-dom',
  )

  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

function LocationDisplay() {
  const location = useLocation()
  return <div data-testid="current-location">{location.pathname}</div>
}

function renderLoginPage() {
  return render(
    <MemoryRouter initialEntries={['/login']}>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/dashboard" element={<div>Dashboard Page</div>} />
      </Routes>
      <LocationDisplay />
    </MemoryRouter>,
  )
}

describe('LoginPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()

    mockAuth.login = vi.fn()
    mockAuth.user = null
    mockAuth.loading = false
  })

  it('renders the sign-in form', () => {
    renderLoginPage()

    expect(
      screen.getByText(/Business Operations CRM/i),
    ).toBeInTheDocument()

    expect(
      screen.getByText(/Sign in with a demo account/i),
    ).toBeInTheDocument()

    expect(screen.getByLabelText(/Email/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/Password/i)).toBeInTheDocument()

    expect(
      screen.getByRole('button', { name: /Sign in/i }),
    ).toBeInTheDocument()
  })

  it('renders the default demo credentials', () => {
    renderLoginPage()

    expect(screen.getByLabelText(/Email/i)).toHaveValue(
      'admin@local.dev',
    )

    expect(screen.getByLabelText(/Password/i)).toHaveValue(
      'ChangeMe!Admin123',
    )
  })

  it('successfully logs in and navigates to the dashboard', async () => {
    const user = userEvent.setup()
    mockAuth.login.mockResolvedValue(undefined)

    renderLoginPage()

    await user.click(screen.getByRole('button', { name: /Sign in/i }))

    await waitFor(() => {
      expect(mockAuth.login).toHaveBeenCalledWith(
        'admin@local.dev',
        'ChangeMe!Admin123',
      )
    })

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/dashboard')
    })
  })

  it('displays an error when login fails', async () => {
    const user = userEvent.setup()

    mockAuth.login.mockRejectedValue(new Error('Invalid credentials'))

    renderLoginPage()

    await user.click(screen.getByRole('button', { name: /Sign in/i }))

    expect(
      await screen.findByText('Invalid email or password.'),
    ).toBeInTheDocument()

    expect(mockNavigate).not.toHaveBeenCalled()
  })

  it('does not navigate when login fails', async () => {
    const user = userEvent.setup()

    mockAuth.login.mockRejectedValue(new Error('Unauthorized'))

    renderLoginPage()

    await user.click(screen.getByRole('button', { name: /Sign in/i }))

    await screen.findByText('Invalid email or password.')

    expect(mockNavigate).not.toHaveBeenCalled()
  })

  it('shows validation error for an invalid email', async () => {
    const user = userEvent.setup()

    renderLoginPage()

    const emailInput = screen.getByLabelText(/Email/i)

    await user.clear(emailInput)
    await user.type(emailInput, 'invalid-email')

    await user.click(screen.getByRole('button', { name: /Sign in/i }))

    expect(
      await screen.findByText(/valid email/i),
    ).toBeInTheDocument()

    expect(mockAuth.login).not.toHaveBeenCalled()
  })

  it('shows validation error when email is empty', async () => {
    const user = userEvent.setup()

    renderLoginPage()

    await user.clear(screen.getByLabelText(/Email/i))

    await user.click(screen.getByRole('button', { name: /Sign in/i }))

    expect(
      await screen.findByText(/valid email/i),
    ).toBeInTheDocument()

    expect(mockAuth.login).not.toHaveBeenCalled()
  })

  it('shows validation error when password is empty', async () => {
    const user = userEvent.setup()

    renderLoginPage()

    await user.clear(screen.getByLabelText(/Password/i))

    await user.click(screen.getByRole('button', { name: /Sign in/i }))

    expect(
      await screen.findByText('Password is required'),
    ).toBeInTheDocument()

    expect(mockAuth.login).not.toHaveBeenCalled()
  })

  it('does not submit when both fields are invalid', async () => {
    const user = userEvent.setup()

    renderLoginPage()

    await user.clear(screen.getByLabelText(/Email/i))
    await user.clear(screen.getByLabelText(/Password/i))

    await user.click(screen.getByRole('button', { name: /Sign in/i }))

    expect(
      await screen.findByText('Password is required'),
    ).toBeInTheDocument()

    expect(mockAuth.login).not.toHaveBeenCalled()
    expect(mockNavigate).not.toHaveBeenCalled()
  })

  it('redirects authenticated users to the dashboard', () => {
    mockAuth.user = {
      id: '1',
      email: 'admin@local.dev',
    }

    renderLoginPage()

    expect(screen.queryByLabelText(/Email/i)).not.toBeInTheDocument()

    expect(
      screen.getByTestId('current-location'),
    ).toHaveTextContent('/dashboard')
  })

  it('does not redirect while authentication is loading', () => {
    mockAuth.loading = true

    mockAuth.user = {
      id: '1',
      email: 'admin@local.dev',
    }

    renderLoginPage()

    expect(screen.getByLabelText(/Email/i)).toBeInTheDocument()

    expect(
      screen.getByTestId('current-location'),
    ).toHaveTextContent('/login')
  })

  it('disables the submit button while login is processing', async () => {
    const user = userEvent.setup()

    let resolveLogin!: () => void

    mockAuth.login.mockImplementation(
      () =>
        new Promise<void>((resolve) => {
          resolveLogin = resolve
        }),
    )

    renderLoginPage()

    const submitButton = screen.getByRole('button', {
      name: /Sign in/i,
    })

    await user.click(submitButton)

    await waitFor(() => {
      expect(submitButton).toBeDisabled()
    })

    resolveLogin()

    await waitFor(() => {
      expect(submitButton).not.toBeDisabled()
    })
  })
})
