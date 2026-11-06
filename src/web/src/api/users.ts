import { apiFetch } from './client'

export type AppRole = 'Administrator' | 'Operations'

export type UserAccount = {
  id: string
  email: string
  displayName: string
  isActive: boolean
  roles: AppRole[]
}

export type PagedUsers = {
  items: UserAccount[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export async function searchUsers(search: string): Promise<PagedUsers> {
  const params = new URLSearchParams({
    page: '1',
    pageSize: '20',
  })
  if (search) {
    params.set('search', search)
  }
  return apiFetch<PagedUsers>(`/api/v1/users?${params}`)
}

export async function createUser(input: {
  email: string
  displayName: string
  password: string
  role: AppRole
}): Promise<UserAccount> {
  return apiFetch<UserAccount>('/api/v1/users', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export async function updateUser(
  id: string,
  input: { displayName: string; role: AppRole; isActive: boolean },
): Promise<UserAccount> {
  return apiFetch<UserAccount>(`/api/v1/users/${id}`, {
    method: 'PUT',
    body: JSON.stringify(input),
  })
}