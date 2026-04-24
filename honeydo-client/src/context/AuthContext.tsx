import { createContext, useContext, useState, useCallback, useEffect } from 'react'
import { api, setUnauthorizedHandler } from '../api/client'
import type { AuthResponse } from '../api/types'

interface AuthContextValue {
  token: string | null
  profileId: string | null
  displayName: string | null
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string, displayName: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [token, setToken] = useState<string | null>(() => localStorage.getItem('token'))
  const [profileId, setProfileId] = useState<string | null>(() => localStorage.getItem('profileId'))
  const [displayName, setDisplayName] = useState<string | null>(() => localStorage.getItem('displayName'))

  const persist = useCallback((res: AuthResponse) => {
    localStorage.setItem('token', res.token)
    localStorage.setItem('profileId', res.profileId)
    localStorage.setItem('displayName', res.displayName)
    setToken(res.token)
    setProfileId(res.profileId)
    setDisplayName(res.displayName)
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    const res = await api.post<AuthResponse>('/auth/login', { email, password })
    persist(res)
  }, [persist])

  const register = useCallback(async (email: string, password: string, displayName: string) => {
    const res = await api.post<AuthResponse>('/auth/register', { email, password, displayName })
    persist(res)
  }, [persist])

  const logout = useCallback(() => {
    localStorage.removeItem('token')
    localStorage.removeItem('profileId')
    localStorage.removeItem('displayName')
    setToken(null)
    setProfileId(null)
    setDisplayName(null)
  }, [])

  // On any 401 response the api client fires this hook. Clearing the token
  // causes PrivateRoutes to redirect to /login on the next render.
  useEffect(() => {
    setUnauthorizedHandler(logout)
    return () => setUnauthorizedHandler(null)
  }, [logout])

  return (
    <AuthContext.Provider value={{ token, profileId, displayName, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
