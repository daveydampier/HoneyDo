/**
 * Typed test data factories.
 * Each factory returns a sensible default and accepts override fields,
 * so individual tests only specify what's relevant to them.
 */

import type { TodoList, TodoItem, Member, Profile } from '../api/types'

export function makeList(overrides: Partial<TodoList> = {}): TodoList {
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

export function makeItem(overrides: Partial<TodoItem> = {}): TodoItem {
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

export function makeMember(overrides: Partial<Member> = {}): Member {
  return {
    profileId: 'pid-alice',
    displayName: 'Alice',
    avatarUrl: null,
    role: 'Owner',
    joinedAt: '2024-01-01T00:00:00Z',
    ...overrides,
  }
}

export function makeProfile(overrides: Partial<Profile> = {}): Profile {
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
