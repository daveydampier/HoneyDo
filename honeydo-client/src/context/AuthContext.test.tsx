import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, act } from '@testing-library/react'
import { AuthProvider, useAuth } from './AuthContext'
import { authStorage } from '../api/authStorage'

// ---------------------------------------------------------------------------
// Mock the api module so tests never hit real network calls
// ---------------------------------------------------------------------------

vi.mock('../api/client', () => ({
  api: {
    post: vi.fn(),
  },
  setUnauthorizedHandler: vi.fn(),
  ApiError: class ApiError extends Error {
    constructor(public status: number, public title: string) { super(title) }
  },
}))

import { api } from '../api/client'
const mockPost = api.post as ReturnType<typeof vi.fn>

// ---------------------------------------------------------------------------
// Helper components
// ---------------------------------------------------------------------------

function AuthDisplay() {
  const { token, profileId, displayName, isLoading } = useAuth()
  return (
    <>
      <span data-testid="token">{token ?? 'null'}</span>
      <span data-testid="profileId">{profileId ?? 'null'}</span>
      <span data-testid="displayName">{displayName ?? 'null'}</span>
      <span data-testid="isLoading">{isLoading ? 'true' : 'false'}</span>
    </>
  )
}

// Swallows the login rejection so the test runner doesn't see unhandled rejections.
// Individual tests assert on state, not on thrown errors from the click handler.
function LoginButton() {
  const { login } = useAuth()
  return <button onClick={() => void login('a@b.com', 'pass').catch(() => {})}>Login</button>
}

function LogoutButton() {
  const { logout } = useAuth()
  return <button onClick={logout}>Logout</button>
}

function renderWithAuth(children: React.ReactNode) {
  return render(<AuthProvider>{children}</AuthProvider>)
}

function get(testId: string) {
  return screen.getByTestId(testId).textContent
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('AuthContext', () => {
  beforeEach(() => {
    localStorage.clear()
    mockPost.mockReset()
  })

  // -------------------------------------------------------------------------
  describe('initial state', () => {
    it('reads persisted values from localStorage on mount', () => {
      authStorage.save({ token: 'persisted-tok', profileId: 'pid', displayName: 'Alice' })
      renderWithAuth(<AuthDisplay />)
      expect(get('token')).toBe('persisted-tok')
      expect(get('profileId')).toBe('pid')
      expect(get('displayName')).toBe('Alice')
    })

    it('starts with nulls when localStorage is empty', () => {
      renderWithAuth(<AuthDisplay />)
      expect(get('token')).toBe('null')
      expect(get('profileId')).toBe('null')
    })

    it('isLoading is false on initial render', () => {
      renderWithAuth(<AuthDisplay />)
      expect(get('isLoading')).toBe('false')
    })
  })

  // -------------------------------------------------------------------------
  describe('login', () => {
    it('persists auth response to state and localStorage', async () => {
      mockPost.mockResolvedValue({ token: 'tok', profileId: 'pid', displayName: 'Bob' })
      renderWithAuth(<><AuthDisplay /><LoginButton /></>)

      await act(async () => {
        screen.getByRole('button', { name: 'Login' }).click()
      })

      expect(get('token')).toBe('tok')
      expect(get('displayName')).toBe('Bob')
      expect(authStorage.getToken()).toBe('tok')
    })

    it('sets isLoading to false after login completes', async () => {
      mockPost.mockResolvedValue({ token: 'tok', profileId: 'pid', displayName: 'Bob' })
      renderWithAuth(<><AuthDisplay /><LoginButton /></>)

      await act(async () => {
        screen.getByRole('button', { name: 'Login' }).click()
      })

      expect(get('isLoading')).toBe('false')
    })

    it('sets isLoading to false even when login throws', async () => {
      mockPost.mockRejectedValue(new Error('Network error'))
      renderWithAuth(<><AuthDisplay /><LoginButton /></>)

      await act(async () => {
        screen.getByRole('button', { name: 'Login' }).click()
      })

      expect(get('isLoading')).toBe('false')
    })
  })

  // -------------------------------------------------------------------------
  describe('logout', () => {
    it('clears token from state and localStorage', async () => {
      authStorage.save({ token: 'tok', profileId: 'pid', displayName: 'Alice' })
      renderWithAuth(<><AuthDisplay /><LogoutButton /></>)

      await act(async () => {
        screen.getByRole('button', { name: 'Logout' }).click()
      })

      expect(get('token')).toBe('null')
      expect(get('profileId')).toBe('null')
      expect(get('displayName')).toBe('null')
      expect(authStorage.getToken()).toBeNull()
    })
  })
})
