export interface AuthResponse {
  token: string
  profileId: string
  displayName: string
}

export interface TodoList {
  id: string
  title: string
  role: 'Owner' | 'Contributor'
  ownerName: string
  contributorNames: string[]
  memberCount: number
  notStartedCount: number
  partialCount: number
  completeCount: number
  abandonedCount: number
  createdAt: string
  updatedAt: string
  closedAt: string | null
  tags: Tag[]
}

export interface Tag {
  id: string
  name: string
  color: string
}

export interface TodoItem {
  id: string
  listId: string
  content: string
  status: { id: number; name: string }
  notes: string | null
  dueDate: string | null
  assignedTo: { id: string; displayName: string } | null
  tags: Tag[]
  isStarred: boolean
  createdAt: string
  updatedAt: string
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

export interface Member {
  profileId: string
  displayName: string
  avatarUrl: string | null
  role: 'Owner' | 'Contributor'
  joinedAt: string
}

export interface Profile {
  id: string
  email: string
  displayName: string
  phoneNumber: string | null
  avatarUrl: string | null
  createdAt: string
}

export interface FriendInfo {
  profileId: string
  displayName: string
  email: string
  avatarUrl: string | null
}

export interface ReceivedRequestInfo {
  requesterId: string
  displayName: string
  email: string
  avatarUrl: string | null
  createdAt: string
}

export interface SentRequestInfo {
  addresseeId: string
  displayName: string
  email: string
  avatarUrl: string | null
  createdAt: string
}

export interface FriendsResult {
  friends: FriendInfo[]
  pendingReceived: ReceivedRequestInfo[]
  pendingSent: SentRequestInfo[]
}

export interface SendRequestResult {
  invitationSent: boolean
}

export interface AddableFriend {
  profileId: string
  displayName: string
  email: string
  avatarUrl: string | null
}

export interface ActivityLogEntry {
  id: string
  actionType: string
  actorName: string
  detail: string | null
  timestamp: string
}

// Re-exported so existing `import type { ApiError } from '../api/types'` keeps working.
// The runtime class lives in ./client and extends Error.
export type { ApiError } from './client'
