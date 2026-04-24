import { describe, it, expect, vi } from 'vitest'
import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { axe } from 'jest-axe'
import { server } from '../test/server'
import { renderWithProviders } from '../test/renderWithProviders'
import { makeList } from '../test/fixtures'
import ListsPage from './ListsPage'

function renderListsPage() {
  return renderWithProviders(<ListsPage />)
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

    renderListsPage()

    await waitFor(() => expect(screen.getByText('Groceries')).toBeInTheDocument())
    expect(screen.getByText('Home Repairs')).toBeInTheDocument()
  })

  it('shows the empty state when there are no active lists', async () => {
    server.use(
      http.get('/api/lists', () => HttpResponse.json([]))
    )

    renderListsPage()

    await waitFor(() =>
      expect(screen.getByText(/no active lists/i)).toBeInTheDocument()
    )
  })

  it('adds a newly created list to the top of the active list', async () => {
    server.use(
      http.get('/api/lists', () => HttpResponse.json([makeList({ id: 'l1', title: 'Existing' })]))
    )

    const user = userEvent.setup()
    renderListsPage()

    await waitFor(() => screen.getByText('Existing'))

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
    renderListsPage()

    await waitFor(() => screen.getByText('Doomed List'))

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
    renderListsPage()

    await waitFor(() => screen.getByText('Groceries'))

    await user.type(screen.getByPlaceholderText(/search lists/i), 'groc')

    expect(screen.getByText('Groceries')).toBeInTheDocument()
    expect(screen.queryByText('Home Repairs')).not.toBeInTheDocument()
  })

  it('shows an error alert when the API call fails', async () => {
    server.use(
      http.get('/api/lists', () =>
        HttpResponse.json({ title: 'Server error' }, { status: 500 })
      ),
    )

    renderListsPage()

    await waitFor(() =>
      expect(screen.getByText('Failed to load lists.')).toBeInTheDocument()
    )
  })

  it('has no axe violations once lists are loaded', async () => {
    const { container } = renderListsPage()
    // Wait for the page to finish loading before running axe (default fixture title)
    await waitFor(() => screen.getByText('Groceries'))
    expect(await axe(container)).toHaveNoViolations()
  })
})
