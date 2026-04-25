import { describe, it, expect, vi } from 'vitest'
import { screen, waitFor, act } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Route, Routes } from 'react-router-dom'
import { http, HttpResponse } from 'msw'
import { axe } from 'jest-axe'
import { server } from '../test/server'
import { renderWithProviders } from '../test/renderWithProviders'
import { makeList, makeItem, makePagedResult } from '../test/fixtures'
import ListDetailPage from './ListDetailPage'

const LIST_ID = 'list-1'

/**
 * ListDetailPage uses React 19's use() which suspends on first render.
 * Wrapping in await act(async () => {}) lets React flush the Suspense
 * resolution before assertions run.
 */
async function renderDetailPage() {
  let result!: ReturnType<typeof renderWithProviders>
  await act(async () => {
    result = renderWithProviders(
      <Routes>
        <Route path="/lists/:listId" element={<ListDetailPage />} />
      </Routes>,
      { initialRoute: `/lists/${LIST_ID}` },
    )
  })
  return result
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

    await renderDetailPage()

    expect(screen.getByText('Weekend Chores')).toBeInTheDocument()
    expect(screen.getByText('Mow the lawn')).toBeInTheDocument()
  })

  it('shows the empty state when the list has no items', async () => {
    server.use(
      http.get('/api/lists/:listId/items', () =>
        HttpResponse.json(makePagedResult([]))
      ),
    )

    await renderDetailPage()

    expect(screen.getByText(/no tasks yet/i)).toBeInTheDocument()
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
    await renderDetailPage()

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
    await renderDetailPage()

    await user.click(screen.getByRole('button', { name: /not started/i }))

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
    await renderDetailPage()

    await user.click(screen.getByRole('button', { name: /delete task/i }))

    await waitFor(() =>
      expect(screen.queryByText('Task to remove')).not.toBeInTheDocument()
    )
  })

  it('has no axe violations once the list is loaded', async () => {
    server.use(
      http.get('/api/lists/:listId', () =>
        HttpResponse.json(makeList({ title: 'Accessibility Test List' }))
      ),
      http.get('/api/lists/:listId/items', () =>
        HttpResponse.json(makePagedResult([makeItem({ content: 'Check the ramp' })]))
      ),
    )

    const { container } = await renderDetailPage()
    expect(screen.getByText('Accessibility Test List')).toBeInTheDocument()
    expect(screen.getByText('Check the ramp')).toBeInTheDocument()
    expect(await axe(container)).toHaveNoViolations()
  })
})
