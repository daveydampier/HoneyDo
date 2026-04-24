import { createContext, useContext, useState, useCallback, useEffect } from 'react'
import { api, setUnauthorizedHandler } from '../api/client'
import { authStorage } from '../api/authStorage'
import type { AuthResponse } from '../api/types'

interface AuthContextValue {
  token:       string | null
  profileId:   string | null
  displayName: string | null
  isLoading:   boolean
  login:    (email: string, password: string) => Promise<void>
  register: (email: string, password: string, displayName: string) => Promise<void>
  logout:   () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const initial = authStorage.load()
  const [token,       setToken]       = useState<string | null>(initial.token)
  const [profileId,   setProfileId]   = useState<string | null>(initial.profileId)
  const [displayName, setDisplayName] = useState<string | null>(initial.displayName)
  const [isLoading,   setIsLoading]   = useState(false)

  const persist = useCallback((res: AuthResponse) => {
    authStorage.save(res)
    setToken(res.token)
    setProfileId(res.profileId)
    setDisplayName(res.displayName)
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    setIsLoading(true)
    try {
      const res = await api.post<AuthResponse>('/auth/login', { email, password })
      persist(res)
    } finally {
      setIsLoading(false)
    }
  }, [persist])

  const register = useCallback(async (email: string, password: string, displayName: string) => {
    setIsLoading(true)
    try {
      const res = await api.post<AuthResponse>('/auth/register', { email, password, displayName })
      persist(res)
    } finally {
      setIsLoading(false)
    }
  }, [persist])

  const logout = useCallback(() => {
    authStorage.clear()
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
    <AuthContext.Provider value={{ token, profileId, displayName, isLoading, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
