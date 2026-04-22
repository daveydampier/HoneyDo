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
  memberCount: number
  itemCount: number
  createdAt: string
  updatedAt: string
  closedAt: string | null
}

export interface TodoItem {
  id: string
  listId: string
  content: string
  status: { id: number; name: string }
  notes: string | null
  dueDate: string | null
  assignedTo: { id: string; displayName: string } | null
  tags: { id: string; name: string; color: string }[]
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

export interface ApiError {
  status: number
  title: string
  errors?: Record<string, string[]>
}
