/**
 * Test render utility.
 *
 * Wraps any component with the three providers every page needs:
 *   MantineProvider  — Mantine components throw without this
 *   MemoryRouter     — enables useNavigate / useParams / Link
 *   AuthProvider     — exposes useAuth()
 *
 * Pass `authenticated: true` (default) to pre-seed localStorage with a
 * test token so PrivateRoutes allows access. Pass `false` for auth-flow
 * tests (login, register).
 *
 * Pass `initialRoute` when the component relies on URL params, e.g.
 *   renderWithProviders(<ListDetailPage />, { initialRoute: '/lists/abc' })
 * combined with wrapping in a <Routes> that has the matching path.
 */

import { render } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { MantineProvider } from '@mantine/core'
import { AuthProvider } from '../context/AuthContext'
import { authStorage } from '../api/authStorage'

interface RenderOptions {
  /** Pre-seed localStorage with a test token. Default: true. */
  authenticated?: boolean
  /** Initial URL for MemoryRouter. Default: '/'. */
  initialRoute?: string
}

export function renderWithProviders(
  ui: React.ReactNode,
  { authenticated = true, initialRoute = '/' }: RenderOptions = {},
) {
  if (authenticated) {
    authStorage.save({ token: 'test-token', profileId: 'pid-alice', displayName: 'Alice' })
  } else {
    authStorage.clear()
  }

  return render(
    <MantineProvider>
      <MemoryRouter initialEntries={[initialRoute]}>
        <AuthProvider>
          {ui}
        </AuthProvider>
      </MemoryRouter>
    </MantineProvider>,
  )
}
