/**
 * Lists page E2E flows — view, create, delete, search.
 */

import { test, expect } from '@playwright/test'
import { seedAuth, setupDefaultRoutes, makeList, json, noContent } from './helpers'

test.beforeEach(async ({ page }) => {
  await seedAuth(page)
  await setupDefaultRoutes(page)
})

test('lists page shows list titles returned by the API', async ({ page }) => {
  await page.route('/api/lists', (route) =>
    json(route, [
      makeList({ id: 'l1', title: 'Groceries' }),
      makeList({ id: 'l2', title: 'Home Repairs', role: 'Contributor', ownerName: 'Bob' }),
    ])
  )

  await page.goto('/', { waitUntil: 'networkidle' })

  await expect(page.getByText('Groceries')).toBeVisible()
  await expect(page.getByText('Home Repairs')).toBeVisible()
})

test('empty state is shown when there are no lists', async ({ page }) => {
  await page.route('/api/lists', (route) => json(route, []))

  await page.goto('/', { waitUntil: 'networkidle' })

  await expect(page.getByText(/no active lists/i)).toBeVisible()
})

test('creating a new list adds it to the page', async ({ page }) => {
  await page.route('/api/lists', async (route) => {
    if (route.request().method() === 'GET') return json(route, [])
    const body = await route.request().postDataJSON() as { title: string }
    return json(route, makeList({ id: 'list-new', title: body.title }))
  })

  await page.goto('/', { waitUntil: 'networkidle' })
  await expect(page.getByText(/no active lists/i)).toBeVisible()

  await page.getByPlaceholder(/new list title/i).fill('Weekend Chores')
  await page.getByRole('button', { name: /^create$/i }).click()

  await expect(page.getByText('Weekend Chores')).toBeVisible()
})

test('deleting a list removes it from the page', async ({ page }) => {
  await page.route('/api/lists', (route) =>
    json(route, [makeList({ id: 'l1', title: 'Doomed List', role: 'Owner' })])
  )
  await page.route('/api/lists/l1', (route) => noContent(route))

  await page.goto('/', { waitUntil: 'networkidle' })
  await expect(page.getByText('Doomed List')).toBeVisible()

  page.once('dialog', (dialog) => dialog.accept())
  await page.getByRole('button', { name: /delete/i }).click()

  await expect(page.getByText('Doomed List')).not.toBeVisible()
})

test('search box filters the list by title', async ({ page }) => {
  await page.route('/api/lists', (route) =>
    json(route, [
      makeList({ id: 'l1', title: 'Groceries' }),
      makeList({ id: 'l2', title: 'Home Repairs' }),
    ])
  )

  await page.goto('/', { waitUntil: 'networkidle' })
  await expect(page.getByText('Groceries')).toBeVisible()
  await expect(page.getByText('Home Repairs')).toBeVisible()

  await page.getByPlaceholder(/search lists/i).fill('groc')

  await expect(page.getByText('Groceries')).toBeVisible()
  await expect(page.getByText('Home Repairs')).not.toBeVisible()
})
