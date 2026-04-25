import { Suspense, type ReactNode } from 'react'
import { Routes, Route, Navigate } from 'react-router-dom'
import { Group, Loader } from '@mantine/core'
import { useAuth } from './context/AuthContext'
import { AuthProvider } from './context/AuthContext'
import ErrorBoundary from './components/ErrorBoundary'
import AppLayout from './components/AppLayout'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import ListsPage from './pages/ListsPage'
import ListDetailPage from './pages/ListDetailPage'
import ProfilePage from './pages/ProfilePage'
import FriendsPage from './pages/FriendsPage'
import ActivityPage from './pages/ActivityPage'

/** Spinner shown by Suspense while a lazy-loaded page chunk is being fetched. */
function PageLoader() {
  return (
    <Group justify="center" pt={80}>
      <Loader size="sm" />
    </Group>
  )
}

/**
 * Wraps every page with:
 *   ErrorBoundary — catches unhandled render errors
 *   Suspense      — shows <PageLoader> while a lazy-loaded chunk is pending
 */
function PageShell({ children }: { children: ReactNode }) {
  return (
    <ErrorBoundary>
      <Suspense fallback={<PageLoader />}>
        {children}
      </Suspense>
    </ErrorBoundary>
  )
}

function PrivateRoutes() {
  const { token } = useAuth()
  return token ? <AppLayout /> : <Navigate to="/login" replace />
}

function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route element={<PrivateRoutes />}>
        <Route path="/" element={<PageShell><ListsPage /></PageShell>} />
        <Route path="/lists/:listId" element={<PageShell><ListDetailPage /></PageShell>} />
        <Route path="/lists/:listId/activity" element={<PageShell><ActivityPage /></PageShell>} />
        <Route path="/profile" element={<PageShell><ProfilePage /></PageShell>} />
        <Route path="/friends" element={<PageShell><FriendsPage /></PageShell>} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default function App() {
  return (
    <AuthProvider>
      <AppRoutes />
    </AuthProvider>
  )
}
