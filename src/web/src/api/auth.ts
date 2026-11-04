import { apiFetch, clearCsrfToken } from './client'

export type AuthUser = {
  id: string
  email: string
  displayName: string
  roles: string[]
}

export async function login(email: string, password: string): Promise<AuthUser> {
  clearCsrfToken()
  return apiFetch<AuthUser>('/api/v1/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  })
}

export async function logout(): Promise<void> {
  await apiFetch<void>('/api/v1/auth/logout', { method: 'POST' })
  clearCsrfToken()
}

export async function me(): Promise<AuthUser> {
  return apiFetch<AuthUser>('/api/v1/auth/me')
}
