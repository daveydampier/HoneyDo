/**
 * Shared helpers for Playwright E2E tests.
 *
 * API calls are mocked with page.route() — no real backend required.
 * Auth state is injected via localStorage before each page navigation.
 */

import type { Page, Route } from '@playwright/test'

// ---------------------------------------------------------------------------
// Auth helpers
// ---------------------------------------------------------------------------

export const ALICE = {
  token: 'test-token',
  profileId: 'pid-alice',
  displayName: 'Alice',
}

/**
 * Seed localStorage with a valid auth session before the page loads.
 * Call this before page.goto() so the app reads the token on boot.
 */
export async function seedAuth(page: Page, user = ALICE) {
  await page.addInitScript((u) => {
    localStorage.setItem('token', u.token)
    localStorage.setItem('profileId', u.profileId)
    localStorage.setItem('displayName', u.displayName)
  }, user)
}

// ---------------------------------------------------------------------------
// Fixture factories (mirror src/test/fixtures.ts)
// ---------------------------------------------------------------------------

export function makeList(overrides: Record<string, unknown> = {}) {
  return {
    id: 'list-1',
    title: 'Groceries',
    role: 'Owner',
    ownerName: 'Alice',
    contributorNames: [],
    memberCount: 1,
    notStartedCount: 2,
    partialCount: 0,
    completeCount: 1,
    abandonedCount: 0,
    createdAt: '2024-01-01T00:00:00Z',
    updatedAt: '2024-01-01T00:00:00Z',
    closedAt: null,
    tags: [],
    ...overrides,
  }
}

export function makeItem(overrides: Record<string, unknown> = {}) {
  return {
    id: 'item-1',
    listId: 'list-1',
    content: 'Buy milk',
    status: { id: 1, name: 'Not Started' },
    notes: null,
    dueDate: null,
    assignedTo: null,
    tags: [],
    isStarred: false,
    createdAt: '2024-01-01T00:00:00Z',
    updatedAt: '2024-01-01T00:00:00Z',
    ...overrides,
  }
}

export function makeProfile(overrides: Record<string, unknown> = {}) {
  return {
    id: 'pid-alice',
    email: 'alice@example.com',
    displayName: 'Alice',
    phoneNumber: null,
    avatarUrl: null,
    createdAt: '2024-01-01T00:00:00Z',
    ...overrides,
  }
}

export function makePagedResult<T>(items: T[]) {
  return {
    items,
    totalCount: items.length,
    page: 1,
    pageSize: 20,
    totalPages: 1,
    hasNextPage: false,
    hasPreviousPage: false,
  }
}

// ---------------------------------------------------------------------------
// Route helpers
// ---------------------------------------------------------------------------

/** Respond with JSON and status 200. */
export function json(route: Route, body: unknown, status = 200) {
  return route.fulfill({
    status,
    contentType: 'application/json',
    body: JSON.stringify(body),
  })
}

/** Respond with 204 No Content. */
export function noContent(route: Route) {
  return route.fulfill({ status: 204 })
}

// Regex patterns — using regex guarantees query strings and deep paths match correctly.
// Playwright route strings treat '*' as a single-segment wildcard (no '/'), so nested
// paths like /api/lists/:id/items would not be matched by '/api/lists/*'.

/** /api/lists  (no trailing slash) */
const LISTS_URL = /\/api\/lists(\?.*)?$/

/** /api/lists/:listId  (exactly one more segment, no /items) */
const LIST_URL = /\/api\/lists\/[^/]+(\?.*)?$/

/** /api/lists/:listId/items  (with optional query string) */
const ITEMS_URL = /\/api\/lists\/[^/]+\/items(\?.*)?$/

/** /api/lists/:listId/items/:itemId */
const ITEM_URL = /\/api\/lists\/[^/]+\/items\/[^/?]+(\?.*)?$/

/** /api/lists/:listId/tags */
const LIST_TAGS_URL = /\/api\/lists\/[^/]+\/tags(\?.*)?$/

// ---------------------------------------------------------------------------
// Data-load waiters
// ---------------------------------------------------------------------------
// With React 19 use() + Suspense, pages fire their API calls during the initial
// render rather than in useEffect. To avoid races between the fetch resolving
// and toBeVisible() polling, create a waitForResponse promise BEFORE calling
// page.goto() and await it afterwards. This guarantees the mock has responded
// (and therefore React has re-rendered with data) before any DOM assertion runs.

/**
 * Returns a promise that resolves once the /api/lists GET response lands.
 * Must be created BEFORE the navigation that triggers the request.
 * Logs the response status so failures are visible in CI output.
 */
export async function waitForListsLoad(page: Page) {
  const response = await page.waitForResponse(
    r => /\/api\/lists(\?.*)?$/.test(r.url()) && r.request().method() === 'GET',
  )
  if (!response.ok()) {
    console.error(`[waitForListsLoad] Non-OK response: ${response.status()} ${response.url()}`)
  }
  return response
}

/**
 * Returns a promise that resolves once a /api/lists/:id/items GET response lands.
 * Must be created BEFORE the navigation that triggers the request.
 * Logs the response status so failures are visible in CI output.
 */
export async function waitForItemsLoad(page: Page) {
  const response = await page.waitForResponse(
    r => /\/api\/lists\/[^/]+\/items(\?.*)?$/.test(r.url()) && r.request().method() === 'GET',
  )
  if (!response.ok()) {
    console.error(`[waitForItemsLoad] Non-OK response: ${response.status()} ${response.url()}`)
  }
  return response
}

/**
 * Attach console-error and uncaught-error listeners to surface React/JS errors
 * in the Playwright test output.  Call once per test (or in beforeEach).
 */
export function setupPageDiagnostics(page: Page) {
  page.on('console', (msg) => {
    if (msg.type() === 'error') {
      console.error('[browser console error]', msg.text())
    }
  })
  page.on('pageerror', (err) => {
    console.error('[browser uncaught error]', err.message)
  })
}

/**
 * Register all the happy-path API routes that every authenticated test needs.
 * Individual tests can override specific routes with additional page.route()
 * calls registered BEFORE page.goto() — Playwright uses the most-recently-
 * registered matching handler first.
 */
export async function setupDefaultRoutes(page: Page) {
  // Profile — fetched by AppLayout on mount
  await page.route('/api/profile', (route) => json(route, makeProfile()))

  // Specific item operations (PATCH / DELETE) — must be registered before the
  // broader ITEMS_URL pattern so the more-specific rule wins.
  await page.route(ITEM_URL, async (route) => {
    const method = route.request().method()
    if (method === 'PATCH') {
      const body = await route.request().postDataJSON() as Record<string, unknown>
      const statusId = typeof body.statusId === 'number' ? body.statusId : 2
      return json(route, makeItem({ status: { id: statusId, name: 'Partial' } }))
    }
    if (method === 'DELETE') return noContent(route)
    return route.continue()
  })

  // Items collection (GET / POST)
  await page.route(ITEMS_URL, async (route) => {
    const method = route.request().method()
    if (method === 'GET') return json(route, makePagedResult([makeItem()]))
    if (method === 'POST') {
      const body = await route.request().postDataJSON() as { content: string }
      return json(route, makeItem({ id: 'item-new', content: body.content }))
    }
    return route.continue()
  })

  // List tags
  await page.route(LIST_TAGS_URL, (route) => json(route, []))

  // Single list (GET / DELETE)
  await page.route(LIST_URL, async (route) => {
    const method = route.request().method()
    if (method === 'GET') {
      // Extract list id from the path
      const listId = new URL(route.request().url()).pathname.split('/').at(-1)
      return json(route, makeList({ id: listId }))
    }
    if (method === 'DELETE') return noContent(route)
    return route.continue()
  })

  // Lists collection (GET / POST)
  await page.route(LISTS_URL, async (route) => {
    const method = route.request().method()
    if (method === 'GET') return json(route, [makeList()])
    if (method === 'POST') {
      const body = await route.request().postDataJSON() as { title: string }
      return json(route, makeList({ id: 'list-new', title: body.title }))
    }
    return route.continue()
  })

  // Global tags
  await page.route('/api/tags', (route) => json(route, []))
}
