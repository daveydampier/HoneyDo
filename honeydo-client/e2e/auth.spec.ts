/**
 * Auth E2E flows — login, register, and redirect-guard.
 */

import { test, expect } from '@playwright/test'
import { seedAuth, setupDefaultRoutes, makeList, makePagedResult, makeItem, json, waitForListsLoad, setupPageDiagnostics } from './helpers'

test.beforeEach(async ({ page }) => {
  setupPageDiagnostics(page)
})

// ---------------------------------------------------------------------------
// Login
// ---------------------------------------------------------------------------

test('login with valid credentials navigates to the lists page', async ({ page }) => {
  await setupDefaultRoutes(page)
  await page.route('/api/auth/login', (route) =>
    json(route, { token: 'test-token', profileId: 'pid-alice', displayName: 'Alice' })
  )

  await page.goto('/login')
  await page.getByRole('textbox', { name: /email/i }).fill('alice@example.com')
  await page.getByPlaceholder('Your password').fill('correct-password')

  // Set up the waiter before clicking — the login triggers a navigate('/') which
  // mounts ListsPage, which fires api.get('/lists') via use() during its render.
  const listsReady = waitForListsLoad(page)
  await page.getByRole('button', { name: /sign in/i }).click()
  await listsReady
  console.log('[auth login test] URL after ready:', page.url())
  console.log('[auth login test] Body:', (await page.locator('body').textContent())?.substring(0, 300))

  // After login, the app routes to / which renders the lists page
  await expect(page.getByRole('heading', { name: /my lists/i })).toBeVisible()
})

test('login with wrong credentials shows an error message', async ({ page }) => {
  await page.route('/api/auth/login', (route) =>
    route.fulfill({ status: 404, contentType: 'application/json', body: JSON.stringify({ title: 'Not found' }) })
  )

  await page.goto('/login')
  await page.getByRole('textbox', { name: /email/i }).fill('alice@example.com')
  await page.getByPlaceholder('Your password').fill('wrong-password')
  await page.getByRole('button', { name: /sign in/i }).click()

  await expect(page.getByText('Invalid email or password.')).toBeVisible()
})

test('unauthenticated user visiting / is redirected to /login', async ({ page }) => {
  // No seedAuth — localStorage is empty
  await page.goto('/')
  await expect(page).toHaveURL(/\/login/)
  await expect(page.getByRole('heading', { name: /sign in/i })).toBeVisible()
})

// ---------------------------------------------------------------------------
// Register
// ---------------------------------------------------------------------------

test('register with valid details navigates to the lists page', async ({ page }) => {
  await setupDefaultRoutes(page)
  await page.route('/api/auth/register', (route) =>
    json(route, { token: 'test-token', profileId: 'pid-alice', displayName: 'Alice' })
  )
  // After register the app loads lists
  await page.route('/api/lists', (route) =>
    json(route, [makeList()])
  )
  await page.route('/api/lists/*/items', (route) =>
    json(route, makePagedResult([makeItem()]))
  )

  await page.goto('/register')
  await page.getByPlaceholder('Your name').fill('Alice')
  await page.getByRole('textbox', { name: /email/i }).fill('alice@example.com')
  await page.getByPlaceholder('Min 8 characters').fill('secret123')

  // Same pattern: register triggers navigate('/') → ListsPage fetches lists.
  const listsReady = waitForListsLoad(page)
  await page.getByRole('button', { name: /create account/i }).click()
  await listsReady

  await expect(page.getByRole('heading', { name: /my lists/i })).toBeVisible()
})
