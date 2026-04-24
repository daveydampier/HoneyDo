/**
 * Public API types — re-exported from the auto-generated schema so every
 * consumer keeps its existing import path (`import type { TodoList } from '../api/types'`).
 *
 * To regenerate after a backend change:
 *   1. Run `dotnet build` in the HoneyDo project (outputs honeydo-client/HoneyDo.json).
 *   2. Run `npm run generate:types` in honeydo-client/.
 *   3. Commit both HoneyDo.json and src/api/generated.ts.
 */

import type { components } from './generated'

type S = components['schemas']

// ---------------------------------------------------------------------------
// Direct re-exports (name and shape match exactly)
// ---------------------------------------------------------------------------

export type AuthResponse      = S['AuthResponse']
export type SendRequestResult = S['SendRequestResult']

// ---------------------------------------------------------------------------
// Renamed types (backend uses *Response / *Dto suffix, frontend drops it)
// ---------------------------------------------------------------------------

export type Tag             = S['TagDto']
export type Profile         = S['ProfileResponse']
export type FriendInfo      = S['FriendResponse']
export type ReceivedRequestInfo = S['ReceivedRequestResponse']
export type SentRequestInfo = S['SentRequestResponse']
export type AddableFriend   = S['AddableFriendResponse']
export type ActivityLogEntry = S['ActivityLogResponse']

/** FriendsResult members are structurally identical to FriendInfo / *RequestInfo. */
export type FriendsResult = S['FriendsResult']

// ---------------------------------------------------------------------------
// Enum override
// ---------------------------------------------------------------------------

/**
 * The backend serializes MemberRole as a string ("Owner" | "Contributor") via
 * JsonStringEnumConverter. The generated spec incorrectly shows it as `number`
 * because the OpenAPI schema generator doesn't inherit the global JSON converter.
 */
export type MemberRole = 'Owner' | 'Contributor'

// ---------------------------------------------------------------------------
// Types that need field-level overrides
// ---------------------------------------------------------------------------

/**
 * The .NET 10 OpenAPI generator emits integer fields as `"type": ["integer", "string"]`
 * (a JSON Schema 2020-12 multitype). openapi-typescript maps this to `number | string`.
 * The fields below are narrowed back to `number` to match the actual runtime values.
 */

export interface TodoItem extends Omit<S['TodoItemResponse'], 'status' | 'tags'> {
  /** id is int32 at runtime — narrowed from `number | string` to `number`. */
  status: { id: number; name: string }
  tags: Tag[]
}

export interface Member extends Omit<S['MemberResponse'], 'role'> {
  role: MemberRole
}

export interface TodoList extends Omit<
  S['TodoListResponse'],
  'role' | 'tags' | 'memberCount' | 'notStartedCount' | 'partialCount' | 'completeCount' | 'abandonedCount'
> {
  role: MemberRole
  tags: Tag[]
  memberCount:      number
  notStartedCount:  number
  partialCount:     number
  completeCount:    number
  abandonedCount:   number
}

// ---------------------------------------------------------------------------
// Generic wrapper — spec generates PagedResultOfTodoItemResponse (concrete)
// but we need the generic form for reuse across entity types.
// ---------------------------------------------------------------------------

export interface PagedResult<T> {
  items:           T[]
  totalCount:      number
  page:            number
  pageSize:        number
  totalPages:      number
  hasNextPage:     boolean
  hasPreviousPage: boolean
}

// ---------------------------------------------------------------------------
// Re-exported so existing `import type { ApiError } from '../api/types'` keeps working.
// The runtime class lives in ./client and extends Error.
// ---------------------------------------------------------------------------

export type { ApiError } from './client'
