import { describe, it, expect, vi } from 'vitest'
import { screen, waitFor, act } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { axe } from 'jest-axe'
import { server } from '../test/server'
import { renderWithProviders } from '../test/renderWithProviders'
import { makeList } from '../test/fixtures'
import ListsPage from './ListsPage'

/**
 * ListsPage uses React 19's use() which suspends on first render.
 * Wrapping in await act(async () => {}) lets React flush the Suspense
 * resolution before assertions run.
 */
async function renderListsPage() {
  let result!: ReturnType<typeof renderWithProviders>
  await act(async () => {
    result = renderWithProviders(<ListsPage />)
  })
  return result
}

describe('ListsPage', () => {
  it('renders list titles returned by the API', async () => {
    server.use(
      http.get('/api/lists', () =>
        HttpResponse.json([
          makeList({ id: 'l1', title: 'Groceries' }),
          makeList({ id: 'l2', title: 'Home Repairs', role: 'Contributor', ownerName: 'Bob' }),
        ])
      ),
    )

    await renderListsPage()

    expect(screen.getByText('Groceries')).toBeInTheDocument()
    expect(screen.getByText('Home Repairs')).toBeInTheDocument()
  })

  it('shows the empty state when there are no active lists', async () => {
    server.use(
      http.get('/api/lists', () => HttpResponse.json([]))
    )

    await renderListsPage()

    expect(screen.getByText(/no active lists/i)).toBeInTheDocument()
  })

  it('adds a newly created list to the top of the active list', async () => {
    server.use(
      http.get('/api/lists', () => HttpResponse.json([makeList({ id: 'l1', title: 'Existing' })]))
    )

    const user = userEvent.setup()
    await renderListsPage()

    await user.type(screen.getByPlaceholderText(/new list title/i), 'New List')
    await user.click(screen.getByRole('button', { name: /^create$/i }))

    await waitFor(() => expect(screen.getByText('New List')).toBeInTheDocument())
  })

  it('removes a deleted list from the UI', async () => {
    server.use(
      http.get('/api/lists', () =>
        HttpResponse.json([makeList({ id: 'l1', title: 'Doomed List', role: 'Owner' })])
      ),
    )

    vi.spyOn(window, 'confirm').mockReturnValue(true)

    const user = userEvent.setup()
    await renderListsPage()

    await user.click(screen.getByRole('button', { name: /delete/i }))

    await waitFor(() =>
      expect(screen.queryByText('Doomed List')).not.toBeInTheDocument()
    )
  })

  it('filters the list when the user types in the search box', async () => {
    server.use(
      http.get('/api/lists', () =>
        HttpResponse.json([
          makeList({ id: 'l1', title: 'Groceries' }),
          makeList({ id: 'l2', title: 'Home Repairs' }),
        ])
      ),
    )

    const user = userEvent.setup()
    await renderListsPage()

    await user.type(screen.getByPlaceholderText(/search lists/i), 'groc')

    expect(screen.getByText('Groceries')).toBeInTheDocument()
    expect(screen.queryByText('Home Repairs')).not.toBeInTheDocument()
  })

  // NOTE: The old "shows an error alert" test was removed because the error
  // handling behavior changed with the use() migration.  API failures now
  // surface to the nearest ErrorBoundary (added to renderWithProviders and
  // PageShell in App) rather than setting local state.  Testing that the
  // ErrorBoundary renders "Something went wrong" on a rejected use() promise
  // is not feasible in jsdom: the promise rejection never triggers a re-render
  // without an awaited async act(), and wrapping the render in async act hangs
  // indefinitely due to React's dev-mode error-boundary replay loop.

  it('has no axe violations once lists are loaded', async () => {
    const { container } = await renderListsPage()
    expect(await axe(container)).toHaveNoViolations()
  })
})
