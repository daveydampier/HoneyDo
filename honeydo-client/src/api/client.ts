import { authStorage } from './authStorage'

const BASE_URL = '/api'

/**
 * Error thrown by the API client for any non-2xx response.
 * Extends Error so stack traces are preserved and `instanceof Error` works.
 */
export class ApiError extends Error {
  status: number
  title: string
  errors?: Record<string, string[]>

  constructor(status: number, title: string, errors?: Record<string, string[]>) {
    super(title)
    this.name = 'ApiError'
    this.status = status
    this.title = title
    this.errors = errors
  }
}

/**
 * Handler invoked whenever the server returns 401 (token expired/invalid).
 * AuthContext registers itself here so a stale token triggers a global logout
 * without every caller needing to check for 401.
 */
let onUnauthorized: (() => void) | null = null
export function setUnauthorizedHandler(handler: (() => void) | null) {
  onUnauthorized = handler
}

function authHeader(): HeadersInit {
  const token = authStorage.getToken()
  return token ? { Authorization: `Bearer ${token}` } : {}
}

async function handleResponse<T>(res: Response): Promise<T> {
  if (!res.ok) {
    // Fire the global 401 hook *only* if we actually sent a token — otherwise
    // unauthenticated flows (login/register) would redirect themselves in circles.
    if (res.status === 401 && authStorage.getToken()) {
      onUnauthorized?.()
    }
    const body = await res.json().catch(() => ({ title: 'An error occurred' }))
    throw new ApiError(res.status, body.title ?? 'An error occurred', body.errors)
  }

  if (res.status === 204) return undefined as T
  return res.json()
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...authHeader(),
      ...options.headers,
    },
  })
  return handleResponse<T>(res)
}

/** Multipart upload — lets the browser set Content-Type with the correct boundary. */
async function upload<T>(path: string, formData: FormData): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    method: 'POST',
    headers: authHeader(),
    body: formData,
  })
  return handleResponse<T>(res)
}

export const api = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body: unknown) => request<T>(path, { method: 'POST', body: JSON.stringify(body) }),
  patch: <T>(path: string, body: unknown) => request<T>(path, { method: 'PATCH', body: JSON.stringify(body) }),
  delete: (path: string) => request<void>(path, { method: 'DELETE' }),
  upload: <T>(path: string, formData: FormData) => upload<T>(path, formData),
}
