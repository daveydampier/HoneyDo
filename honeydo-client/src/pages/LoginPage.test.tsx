import { describe, it, expect } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Route, Routes } from 'react-router-dom'
import { http, HttpResponse } from 'msw'
import { axe } from 'jest-axe'
import { server } from '../test/server'
import { renderWithProviders } from '../test/renderWithProviders'
import LoginPage from './LoginPage'

// Render login page on /login with a stub home page to verify navigation.
function renderLogin() {
  return renderWithProviders(
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/" element={<div>Home</div>} />
    </Routes>,
    { authenticated: false, initialRoute: '/login' },
  )
}

// Helpers for finding Mantine inputs.
// - Email uses getByRole('textbox') — type="email" has implicit ARIA role "textbox".
// - Password has no implicit ARIA role (type="password"), so use the placeholder.
// - Using getByRole name matching relies on the ARIA accessible name which excludes
//   the aria-hidden required-star span, so the regex matches cleanly.
const emailInput = () => screen.getByRole('textbox', { name: /email/i })
const passwordInput = () => screen.getByPlaceholderText('Your password')

describe('LoginPage', () => {
  it('renders the email input, password input, and submit button', () => {
    renderLogin()
    expect(emailInput()).toBeInTheDocument()
    expect(passwordInput()).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument()
  })

  it('navigates to / after a successful login', async () => {
    const user = userEvent.setup()
    renderLogin()

    await user.type(emailInput(), 'alice@example.com')
    await user.type(passwordInput(), 'correct-password')
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    // Home stub appears after navigation
    await waitFor(() => expect(screen.getByText('Home')).toBeInTheDocument())
  })

  it('shows "Invalid email or password" when the server returns 404', async () => {
    server.use(
      http.post('/api/auth/login', () =>
        HttpResponse.json({ title: 'Not found' }, { status: 404 })
      ),
    )

    const user = userEvent.setup()
    renderLogin()

    await user.type(emailInput(), 'alice@example.com')
    await user.type(passwordInput(), 'wrong-password')
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() =>
      expect(screen.getByText('Invalid email or password.')).toBeInTheDocument()
    )
  })

  it('shows the API error title for non-404 failures', async () => {
    server.use(
      http.post('/api/auth/login', () =>
        HttpResponse.json({ title: 'Service unavailable' }, { status: 503 })
      ),
    )

    const user = userEvent.setup()
    renderLogin()

    await user.type(emailInput(), 'alice@example.com')
    await user.type(passwordInput(), 'any')
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() =>
      expect(screen.getByText('Service unavailable')).toBeInTheDocument()
    )
  })

  it('has no axe violations on initial render', async () => {
    const { container } = renderLogin()
    expect(await axe(container)).toHaveNoViolations()
  })
})
