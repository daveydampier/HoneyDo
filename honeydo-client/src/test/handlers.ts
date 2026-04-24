/**
 * Default MSW request handlers — return sensible happy-path responses so
 * individual tests only need to override what they're specifically exercising.
 */

import { http, HttpResponse } from 'msw'
import { makeList, makeItem, makeProfile, makePagedResult } from './fixtures'

export const handlers = [
  // Auth
  http.post('/api/auth/login', () =>
    HttpResponse.json({ token: 'test-token', profileId: 'pid-alice', displayName: 'Alice' })
  ),
  http.post('/api/auth/register', () =>
    HttpResponse.json({ token: 'test-token', profileId: 'pid-alice', displayName: 'Alice' })
  ),

  // Profile (fetched by AppLayout on mount)
  http.get('/api/profile', () =>
    HttpResponse.json(makeProfile())
  ),

  // Lists
  http.get('/api/lists', () =>
    HttpResponse.json([makeList()])
  ),
  http.post('/api/lists', async ({ request }) => {
    const body = await request.json() as { title: string }
    return HttpResponse.json(makeList({ id: 'list-new', title: body.title }))
  }),
  http.delete('/api/lists/:listId', () =>
    new HttpResponse(null, { status: 204 })
  ),

  // List detail
  http.get('/api/lists/:listId', ({ params }) =>
    HttpResponse.json(makeList({ id: params.listId as string }))
  ),

  // Items
  http.get('/api/lists/:listId/items', () =>
    HttpResponse.json(makePagedResult([makeItem()]))
  ),
  http.post('/api/lists/:listId/items', async ({ request }) => {
    const body = await request.json() as { content: string }
    return HttpResponse.json(makeItem({ id: 'item-new', content: body.content }))
  }),
  http.patch('/api/lists/:listId/items/:itemId', async ({ request, params }) => {
    const body = await request.json() as Record<string, unknown>
    const statusId = typeof body.statusId === 'number' ? body.statusId : 1
    return HttpResponse.json(makeItem({
      id: params.itemId as string,
      status: { id: statusId, name: 'Partial' },
    }))
  }),
  http.delete('/api/lists/:listId/items/:itemId', () =>
    new HttpResponse(null, { status: 204 })
  ),

  // Tags
  http.get('/api/lists/:listId/tags', () =>
    HttpResponse.json([])
  ),
  http.get('/api/tags', () =>
    HttpResponse.json([])
  ),
]
