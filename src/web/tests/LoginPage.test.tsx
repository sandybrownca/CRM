import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { LoginPage } from '../src/pages/LoginPage'
import { AuthProvider } from '../src/auth/AuthContext'

vi.mock('../src/api/auth', () => ({
  me: vi.fn().mockRejectedValue(new Error('unauthenticated')),
  login: vi.fn(),
  logout: vi.fn(),
}))

describe('LoginPage', () => {
  it('renders sign-in form', async () => {
    render(
      <MemoryRouter>
        <AuthProvider>
          <LoginPage />
        </AuthProvider>
      </MemoryRouter>,
    )

    expect(await screen.findByText(/Business Operations CRM/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/Email/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Sign in/i })).toBeInTheDocument()
  })
})
