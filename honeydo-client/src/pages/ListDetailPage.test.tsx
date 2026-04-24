import { describe, it, expect, vi } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Route, Routes } from 'react-router-dom'
import { http, HttpResponse } from 'msw'
import { server } from '../test/server'
import { renderWithProviders } from '../test/renderWithProviders'
import { makeList, makeItem, makePagedResult } from '../test/fixtures'
import ListDetailPage from './ListDetailPage'

const LIST_ID = 'list-1'

/** Render the detail page with the correct URL param wired up. */
function renderDetailPage() {
  return renderWithProviders(
    <Routes>
      <Route path="/lists/:listId" element={<ListDetailPage />} />
    </Routes>,
    { initialRoute: `/lists/${LIST_ID}` },
  )
}

describe('ListDetailPage', () => {
  it('renders the list title and task content after loading', async () => {
    server.use(
      http.get('/api/lists/:listId', () =>
        HttpResponse.json(makeList({ title: 'Weekend Chores' }))
      ),
      http.get('/api/lists/:listId/items', () =>
        HttpResponse.json(makePagedResult([makeItem({ content: 'Mow the lawn' })]))
      ),
    )

    renderDetailPage()

    await waitFor(() => expect(screen.getByText('Weekend Chores')).toBeInTheDocument())
    expect(screen.getByText('Mow the lawn')).toBeInTheDocument()
  })

  it('shows the empty state when the list has no items', async () => {
    server.use(
      http.get('/api/lists/:listId/items', () =>
        HttpResponse.json(makePagedResult([]))
      ),
    )

    renderDetailPage()

    await waitFor(() =>
      expect(screen.getByText(/no tasks yet/i)).toBeInTheDocument()
    )
  })

  it('adds a new task to the list on form submit', async () => {
    server.use(
      http.get('/api/lists/:listId/items', () =>
        HttpResponse.json(makePagedResult([]))
      ),
      http.post('/api/lists/:listId/items', async ({ request }) => {
        const body = await request.json() as { content: string }
        return HttpResponse.json(makeItem({ id: 'item-new', content: body.content }))
      }),
    )

    const user = userEvent.setup()
    renderDetailPage()

    await waitFor(() => screen.getByPlaceholderText(/new task/i))

    await user.type(screen.getByPlaceholderText(/new task/i), 'Walk the dog')
    await user.click(screen.getByRole('button', { name: /^add$/i }))

    await waitFor(() =>
      expect(screen.getByText('Walk the dog')).toBeInTheDocument()
    )
  })

  it('cycles the item status when the status button is clicked', async () => {
    server.use(
      http.get('/api/lists/:listId/items', () =>
        HttpResponse.json(makePagedResult([makeItem({ status: { id: 1, name: 'Not Started' } })]))
      ),
      http.patch('/api/lists/:listId/items/:itemId', () =>
        HttpResponse.json(makeItem({ status: { id: 2, name: 'Partial' } }))
      ),
    )

    const user = userEvent.setup()
    renderDetailPage()

    // Wait for the status button to appear and click it
    const statusBtn = await screen.findByRole('button', { name: /not started/i })
    await user.click(statusBtn)

    await waitFor(() =>
      expect(screen.getByRole('button', { name: /partial/i })).toBeInTheDocument()
    )
  })

  it('removes a task from the list when delete is clicked', async () => {
    server.use(
      http.get('/api/lists/:listId/items', () =>
        HttpResponse.json(makePagedResult([makeItem({ content: 'Task to remove' })]))
      ),
    )

    // No window.confirm needed — item delete goes straight through without a dialog.
    const user = userEvent.setup()
    renderDetailPage()

    await waitFor(() => screen.getByText('Task to remove'))

    await user.click(screen.getByRole('button', { name: /delete task/i }))

    await waitFor(() =>
      expect(screen.queryByText('Task to remove')).not.toBeInTheDocument()
    )
  })
})
