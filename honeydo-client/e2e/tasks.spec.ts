/**
 * List detail / task management E2E flows.
 */

import { test, expect } from '@playwright/test'
import { seedAuth, setupDefaultRoutes, makeList, makeItem, makePagedResult, json, noContent } from './helpers'

const LIST_ID = 'list-1'

// Regex helpers matching the same patterns used in helpers.ts
const itemsUrl = (listId: string) => new RegExp(`/api/lists/${listId}/items(\\?.*)?$`)
const itemUrl = (listId: string, itemId: string) =>
  new RegExp(`/api/lists/${listId}/items/${itemId}(\\?.*)?$`)
const listUrl = (listId: string) => new RegExp(`/api/lists/${listId}(\\?.*)?$`)

test.beforeEach(async ({ page }) => {
  await seedAuth(page)
  await setupDefaultRoutes(page)
})

test('list detail page shows the list title and tasks', async ({ page }) => {
  await page.route(listUrl(LIST_ID), (route) =>
    json(route, makeList({ title: 'Weekend Chores' }))
  )
  await page.route(itemsUrl(LIST_ID), (route) =>
    json(route, makePagedResult([makeItem({ content: 'Mow the lawn' })]))
  )

  await page.goto(`/lists/${LIST_ID}`, { waitUntil: 'networkidle' })

  await expect(page.getByText('Weekend Chores')).toBeVisible()
  await expect(page.getByText('Mow the lawn')).toBeVisible()
})

test('empty state is shown when the list has no tasks', async ({ page }) => {
  await page.route(itemsUrl(LIST_ID), (route) =>
    json(route, makePagedResult([]))
  )

  await page.goto(`/lists/${LIST_ID}`, { waitUntil: 'networkidle' })

  await expect(page.getByText(/no tasks yet/i)).toBeVisible()
})

test('adding a task appends it to the list', async ({ page }) => {
  await page.route(itemsUrl(LIST_ID), async (route) => {
    if (route.request().method() === 'GET') return json(route, makePagedResult([]))
    const body = await route.request().postDataJSON() as { content: string }
    return json(route, makeItem({ id: 'item-new', content: body.content }))
  })

  await page.goto(`/lists/${LIST_ID}`, { waitUntil: 'networkidle' })
  await expect(page.getByPlaceholder(/new task/i)).toBeVisible()

  await page.getByPlaceholder(/new task/i).fill('Walk the dog')
  await page.getByRole('button', { name: /^add$/i }).click()

  await expect(page.getByText('Walk the dog')).toBeVisible()
})

test('clicking the status button cycles the task status', async ({ page }) => {
  await page.route(itemsUrl(LIST_ID), (route) =>
    json(route, makePagedResult([makeItem({ status: { id: 1, name: 'Not Started' } })]))
  )
  await page.route(itemUrl(LIST_ID, 'item-1'), (route) =>
    json(route, makeItem({ status: { id: 2, name: 'Partial' } }))
  )

  await page.goto(`/lists/${LIST_ID}`, { waitUntil: 'networkidle' })

  const statusBtn = page.getByRole('button', { name: /not started/i })
  await expect(statusBtn).toBeVisible()
  await statusBtn.click()

  await expect(page.getByRole('button', { name: /partial/i })).toBeVisible()
})

test('deleting a task removes it from the list', async ({ page }) => {
  await page.route(itemsUrl(LIST_ID), (route) =>
    json(route, makePagedResult([makeItem({ content: 'Task to remove' })]))
  )
  await page.route(itemUrl(LIST_ID, 'item-1'), (route) =>
    noContent(route)
  )

  await page.goto(`/lists/${LIST_ID}`, { waitUntil: 'networkidle' })
  await expect(page.getByText('Task to remove')).toBeVisible()

  await page.getByRole('button', { name: /delete task/i }).click()

  await expect(page.getByText('Task to remove')).not.toBeVisible()
})
