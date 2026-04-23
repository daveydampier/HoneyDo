import { useState, useEffect, type FormEvent } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import type { TodoItem, Tag, PagedResult, TodoList, Member, AddableFriend, ApiError } from '../api/types'
import { useAuth } from '../context/AuthContext'
import { getTagTextColor } from '../utils/tags'
import {
  Container, Group, Title, Text, Anchor, Button, Badge,
  Paper, Stack, Alert, Loader, Textarea, TextInput,
  UnstyledButton,
} from '@mantine/core'
import {
  IconAlertCircle, IconSortAscending, IconSortDescending,
  IconActivity, IconUsers,
} from '@tabler/icons-react'

const STATUS_LABELS: Record<number, string> = { 1: 'Not Started', 2: 'Partial', 3: 'Complete', 4: 'Abandoned' }
const STATUS_COLORS: Record<number, string> = { 1: 'gray', 2: 'yellow', 3: 'green', 4: 'red' }
const NOTES_MAX = 256

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })
}

function isDatePast(dateStr: string): boolean {
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  const d = new Date(dateStr + 'T00:00:00')
  return d < today
}

function isOverdue(dueDate: string | null | undefined, statusId: number): boolean {
  if (!dueDate) return false
  if (statusId === 3 || statusId === 4) return false
  return isDatePast(dueDate)
}

export default function ListDetailPage() {
  const { listId } = useParams<{ listId: string }>()
  const { profileId } = useAuth()
  const navigate = useNavigate()

  const [list, setList] = useState<TodoList | null>(null)
  const [items, setItems] = useState<TodoItem[]>([])
  const [loadingItems, setLoadingItems] = useState(true)
  const [content, setContent] = useState('')
  const [dueDate, setDueDate] = useState('')
  const [createError, setCreateError] = useState<string | null>(null)

  const [sortBy, setSortBy] = useState<'DueDate' | 'CreatedAt'>('DueDate')
  const [ascending, setAscending] = useState(true)

  const [editingId, setEditingId] = useState<string | null>(null)
  const [editContent, setEditContent] = useState('')
  const [editNotes, setEditNotes] = useState('')
  const [editError, setEditError] = useState<string | null>(null)

  const [myTags, setMyTags] = useState<Tag[]>([])
  const [editTagIds, setEditTagIds] = useState<Set<string>>(new Set())
  const [togglingTagId, setTogglingTagId] = useState<string | null>(null)

  const [members, setMembers] = useState<Member[]>([])
  const [addableFriends, setAddableFriends] = useState<AddableFriend[]>([])
  const [showMembers, setShowMembers] = useState(false)
  const [addingFriend, setAddingFriend] = useState<string | null>(null)
  const [memberError, setMemberError] = useState<string | null>(null)

  // General action error (status cycle, delete, close) — shown above the items list
  const [actionError, setActionError] = useState<string | null>(null)

  const isOwner = list?.role === 'Owner'
  const isClosed = !!list?.closedAt
  const canClose = isOwner && !isClosed && items.length > 0 && items.every(i => i.status.id !== 1)

  useEffect(() => {
    if (!listId) return
    api.get<TodoList>(`/lists/${listId}`).then(setList).catch(() => {})
  }, [listId])

  useEffect(() => {
    if (!listId) return
    setLoadingItems(true)
    api.get<PagedResult<TodoItem>>(`/lists/${listId}/items?sortBy=${sortBy}&ascending=${ascending}`)
      .then(res => setItems(res.items))
      .catch(() => {})
      .finally(() => setLoadingItems(false))
  }, [listId, sortBy, ascending])

  useEffect(() => {
    if (!listId) return
    api.get<Tag[]>(`/lists/${listId}/tags`).then(setMyTags).catch(() => {})
  }, [listId])

  useEffect(() => {
    if (!showMembers || !listId) return
    loadMembers()
  }, [showMembers, listId])

  async function loadMembers() {
    if (!listId) return
    try {
      const [membersRes, friendsRes] = await Promise.all([
        api.get<Member[]>(`/lists/${listId}/members`),
        api.get<AddableFriend[]>(`/lists/${listId}/addable-friends`),
      ])
      setMembers(membersRes)
      setAddableFriends(friendsRes)
    } catch { /* ignore */ }
  }

  async function handleAddFriend(friendProfileId: string) {
    if (!listId) return
    setAddingFriend(friendProfileId)
    setMemberError(null)
    try {
      const newMember = await api.post<Member>(`/lists/${listId}/members/${friendProfileId}`, {})
      setMembers(prev => [...prev, newMember])
      setAddableFriends(prev => prev.filter(f => f.profileId !== friendProfileId))
    } catch (err) {
      const apiErr = err as ApiError
      setMemberError(apiErr.errors?.ProfileId?.[0] ?? apiErr.title)
    } finally {
      setAddingFriend(null)
    }
  }

  async function handleRemoveMember(memberProfileId: string, displayName: string) {
    if (!listId) return
    if (!confirm(`Remove ${displayName} from this list?`)) return
    setMemberError(null)
    try {
      await api.delete(`/lists/${listId}/members/${memberProfileId}`)
      setMembers(prev => prev.filter(m => m.profileId !== memberProfileId))
      const friendsRes = await api.get<AddableFriend[]>(`/lists/${listId}/addable-friends`)
      setAddableFriends(friendsRes)
    } catch {
      setMemberError('Failed to remove member. Please try again.')
    }
  }

  async function handleClose() {
    if (!listId) return
    if (!confirm('Close this list? It will become read-only.')) return
    setActionError(null)
    try {
      const updated = await api.post<TodoList>(`/lists/${listId}/close`, {})
      setList(updated)
    } catch {
      setActionError('Failed to close the list. Make sure all tasks are marked Partial, Complete, or Abandoned.')
    }
  }

  async function handleCreate(e: FormEvent) {
    e.preventDefault()
    setCreateError(null)
    if (dueDate && isDatePast(dueDate)) {
      if (!confirm('The due date you entered is in the past. Add this task anyway?')) return
    }
    try {
      const item = await api.post<TodoItem>(`/lists/${listId}/items`, {
        content,
        dueDate: dueDate || null,
      })
      setItems(prev => [item, ...prev])
      setContent('')
      setDueDate('')
    } catch (err) {
      const apiErr = err as ApiError
      setCreateError(apiErr.errors?.Content?.[0] ?? apiErr.title)
    }
  }

  async function handleStatusCycle(item: TodoItem) {
    const next = item.status.id === 4 ? 1 : item.status.id + 1
    setActionError(null)
    try {
      const updated = await api.patch<TodoItem>(`/lists/${listId}/items/${item.id}`, { statusId: next })
      setItems(prev => prev.map(i => i.id === updated.id ? updated : i))
    } catch {
      setActionError('Failed to update status. Please try again.')
    }
  }

  function startEdit(item: TodoItem) {
    setEditingId(item.id)
    setEditContent(item.content)
    setEditNotes(item.notes ?? '')
    setEditTagIds(new Set(item.tags.map(t => t.id)))
    setEditError(null)
  }

  function cancelEdit() {
    setEditingId(null)
    setEditTagIds(new Set())
    setEditError(null)
  }

  async function handleTagToggle(tagId: string) {
    if (!listId || !editingId) return
    const isApplied = editTagIds.has(tagId)
    setTogglingTagId(tagId)
    try {
      if (isApplied) {
        await api.delete(`/lists/${listId}/items/${editingId}/tags/${tagId}`)
        setEditTagIds(prev => { const s = new Set(prev); s.delete(tagId); return s })
        setItems(prev => prev.map(i =>
          i.id === editingId ? { ...i, tags: i.tags.filter(t => t.id !== tagId) } : i
        ))
      } else {
        await api.post(`/lists/${listId}/items/${editingId}/tags/${tagId}`, {})
        setEditTagIds(prev => new Set([...prev, tagId]))
        const tag = myTags.find(t => t.id === tagId)
        if (tag) {
          setItems(prev => prev.map(i =>
            i.id === editingId ? { ...i, tags: [...i.tags, tag] } : i
          ))
        }
      }
    } catch { /* leave state unchanged */ }
    finally { setTogglingTagId(null) }
  }

  async function handleEditSave(id: string) {
    setEditError(null)
    try {
      const updated = await api.patch<TodoItem>(`/lists/${listId}/items/${id}`, {
        content: editContent,
        notes: editNotes,
      })
      setItems(prev => prev.map(i => i.id === updated.id ? updated : i))
      setEditingId(null)
    } catch (err) {
      const apiErr = err as ApiError
      setEditError(apiErr.errors?.Content?.[0] ?? apiErr.errors?.Notes?.[0] ?? apiErr.title)
    }
  }

  async function handleDelete(id: string) {
    setActionError(null)
    try {
      await api.delete(`/lists/${listId}/items/${id}`)
      setItems(prev => prev.filter(i => i.id !== id))
    } catch {
      setActionError('Failed to delete item. Please try again.')
    }
  }

  return (
    <Container size="md" pt="xl">
      <Anchor component={Link} to="/" size="sm" c="dimmed">← Back to lists</Anchor>

      {/* Page header */}
      <Group justify="space-between" mt="sm" mb="md" wrap="nowrap" align="flex-start">
        <Group gap="sm" align="center">
          <Title order={1}>{list?.title ?? 'Tasks'}</Title>
          {isClosed && <Badge color="green" variant="light">Closed</Badge>}
        </Group>
        <Group gap="xs" wrap="nowrap">
          {isOwner && !isClosed && (
            <Button
              size="xs"
              color="green"
              disabled={!canClose}
              title={!canClose ? 'All tasks must be Partial, Complete, or Abandoned to close this list' : 'Close this list'}
              onClick={handleClose}
            >
              Close List
            </Button>
          )}
          <Button
            size="xs"
            variant="outline"
            leftSection={<IconActivity size={13} />}
            onClick={() => navigate(`/lists/${listId}/activity`)}
          >
            Activity
          </Button>
          <Button
            size="xs"
            variant={showMembers ? 'filled' : 'outline'}
            leftSection={<IconUsers size={13} />}
            onClick={() => setShowMembers(v => !v)}
          >
            {showMembers ? 'Hide members' : `Members (${list?.memberCount ?? '…'})`}
          </Button>
        </Group>
      </Group>

      {/* Closed banner */}
      {isClosed && (
        <Alert color="green" variant="light" mb="md">
          ✅ This list was closed on {formatDate(list!.closedAt!)} and is now read-only.
        </Alert>
      )}

      {/* Members panel */}
      {showMembers && (
        <Paper p="md" radius="md" withBorder mb="xl">
          <Title order={4} mb="sm">Members</Title>

          {memberError && (
            <Alert color="red" variant="light" icon={<IconAlertCircle size={14} />} mb="sm">
              {memberError}
            </Alert>
          )}

          <Stack gap="xs" mb="md">
            {members.map(m => (
              <Group key={m.profileId} gap="sm">
                <Text size="sm" fw={600} flex={1}>{m.displayName}</Text>
                <Badge
                  size="xs"
                  color={m.role === 'Owner' ? 'blue' : 'gray'}
                  variant="filled"
                >
                  {m.role}
                </Badge>
                {isOwner && m.role !== 'Owner' && m.profileId !== profileId && (
                  <Button
                    variant="subtle"
                    color="red"
                    size="xs"
                    onClick={() => handleRemoveMember(m.profileId, m.displayName)}
                  >
                    Remove
                  </Button>
                )}
              </Group>
            ))}
          </Stack>

          {isOwner && (
            <>
              <Text size="sm" fw={500} c="dimmed" mb="xs">Add a Friend as Collaborator</Text>
              {addableFriends.length === 0 ? (
                <Text size="sm" c="dimmed">
                  All your friends are already on this list, or you have no friends to add yet.
                </Text>
              ) : (
                <Stack gap="xs">
                  {addableFriends.map(f => (
                    <Group key={f.profileId} gap="sm">
                      <Text size="sm" fw={500} flex={1}>{f.displayName}</Text>
                      <Text size="xs" c="dimmed">{f.email}</Text>
                      <Button
                        size="xs"
                        loading={addingFriend === f.profileId}
                        onClick={() => handleAddFriend(f.profileId)}
                      >
                        Add
                      </Button>
                    </Group>
                  ))}
                </Stack>
              )}
            </>
          )}
        </Paper>
      )}

      {/* Action error (status cycle / delete / close) */}
      {actionError && (
        <Alert
          color="red"
          variant="light"
          icon={<IconAlertCircle size={14} />}
          withCloseButton
          onClose={() => setActionError(null)}
          mb="sm"
        >
          {actionError}
        </Alert>
      )}

      {/* Sort toolbar */}
      <Group gap="xs" mb="sm">
        <Text size="sm" c="dimmed">Sort by</Text>
        <Button.Group>
          <Button
            size="xs"
            variant={sortBy === 'DueDate' ? 'filled' : 'default'}
            onClick={() => setSortBy('DueDate')}
          >
            Due Date
          </Button>
          <Button
            size="xs"
            variant={sortBy === 'CreatedAt' ? 'filled' : 'default'}
            onClick={() => setSortBy('CreatedAt')}
          >
            Created Date
          </Button>
        </Button.Group>
        <Button
          size="xs"
          variant="default"
          ml="auto"
          leftSection={ascending ? <IconSortAscending size={13} /> : <IconSortDescending size={13} />}
          onClick={() => setAscending(v => !v)}
        >
          {ascending ? 'Asc' : 'Desc'}
        </Button>
      </Group>

      {/* New item form */}
      {!isClosed && (
        <form onSubmit={handleCreate}>
          <Stack gap="xs" mb="md">
            <Group gap="sm">
              <TextInput
                flex={1}
                placeholder="New task…"
                value={content}
                onChange={e => setContent(e.target.value)}
                required
              />
              <input
                type="date"
                value={dueDate}
                onChange={e => setDueDate(e.target.value)}
                style={{
                  fontSize: 13,
                  padding: '6px 8px',
                  borderRadius: 6,
                  border: '1px solid var(--mantine-color-default-border)',
                  background: 'var(--mantine-color-default)',
                  color: 'var(--mantine-color-text)',
                  colorScheme: 'inherit',
                }}
              />
              <Button type="submit">Add</Button>
            </Group>
            {createError && (
              <Alert color="red" variant="light" icon={<IconAlertCircle size={14} />} py="xs">
                {createError}
              </Alert>
            )}
          </Stack>
        </form>
      )}

      {/* Items list */}
      {loadingItems ? (
        <Group justify="center" mt="xl"><Loader size="sm" /></Group>
      ) : (
        <Stack gap="sm">
          {items.length === 0 && (
            <Text size="sm" c="dimmed">No tasks yet. Add one above.</Text>
          )}

          {/* Column headers */}
          {items.length > 0 && (
            <Group gap={12} px="sm">
              <Text size="xs" fw={600} c="dimmed" tt="uppercase" style={{ width: 110, flexShrink: 0, letterSpacing: '0.05em' }}>Status</Text>
              <Text size="xs" fw={600} c="dimmed" tt="uppercase" style={{ flex: 1, letterSpacing: '0.05em' }}>Task</Text>
              <Text size="xs" fw={600} c="dimmed" tt="uppercase" style={{ width: 90, flexShrink: 0, letterSpacing: '0.05em' }}>Created</Text>
              <Text size="xs" fw={600} c="dimmed" tt="uppercase" style={{ width: 90, flexShrink: 0, letterSpacing: '0.05em' }}>Due Date</Text>
              <div style={{ width: 90, flexShrink: 0 }} />
            </Group>
          )}

          {items.map(item => (
            <Paper key={item.id} p="sm" radius="md" withBorder>
              {editingId === item.id ? (
                /* ── Edit mode ── */
                <Stack gap="sm">
                  <TextInput
                    value={editContent}
                    onChange={e => setEditContent(e.target.value)}
                    autoFocus
                    placeholder="Task content"
                  />

                  <div style={{ position: 'relative' }}>
                    <Textarea
                      value={editNotes}
                      onChange={e => setEditNotes(e.target.value)}
                      placeholder="Add a note… (optional)"
                      maxLength={NOTES_MAX}
                      minRows={3}
                      autosize
                    />
                    <Text
                      size="xs"
                      c={editNotes.length > NOTES_MAX - 20 ? 'red' : 'dimmed'}
                      style={{ position: 'absolute', bottom: 6, right: 8, pointerEvents: 'none' }}
                    >
                      {editNotes.length}/{NOTES_MAX}
                    </Text>
                  </div>

                  {/* Tag picker */}
                  {myTags.length > 0 && (
                    <Stack gap={4}>
                      <Text size="xs" c="dimmed">Tags</Text>
                      <Group gap="xs">
                        {myTags.map(tag => {
                          const applied = editTagIds.has(tag.id)
                          return (
                            <UnstyledButton
                              key={tag.id}
                              onClick={() => handleTagToggle(tag.id)}
                              disabled={togglingTagId === tag.id}
                              style={{
                                background: applied ? tag.color : `${tag.color}22`,
                                color: applied ? getTagTextColor(tag.color) : tag.color,
                                border: `1px solid ${tag.color}`,
                                borderRadius: 10,
                                padding: '3px 10px',
                                fontSize: 12,
                                fontWeight: 500,
                                opacity: togglingTagId === tag.id ? 0.6 : 1,
                                cursor: togglingTagId === tag.id ? 'not-allowed' : 'pointer',
                              }}
                            >
                              {tag.name}
                            </UnstyledButton>
                          )
                        })}
                      </Group>
                    </Stack>
                  )}

                  {editError && (
                    <Alert color="red" variant="light" icon={<IconAlertCircle size={14} />} py="xs">
                      {editError}
                    </Alert>
                  )}

                  <Group gap="xs">
                    <Button size="xs" onClick={() => handleEditSave(item.id)}>Save</Button>
                    <Button size="xs" variant="default" onClick={cancelEdit}>Cancel</Button>
                  </Group>
                </Stack>
              ) : (
                /* ── View mode ── */
                <Stack gap="xs">
                  <Group gap={12} wrap="nowrap">
                    {/* Status */}
                    {isClosed ? (
                      <Badge
                        color={STATUS_COLORS[item.status.id]}
                        variant="light"
                        style={{ width: 110, flexShrink: 0 }}
                      >
                        {STATUS_LABELS[item.status.id]}
                      </Badge>
                    ) : (
                      <Button
                        size="xs"
                        variant="outline"
                        color={STATUS_COLORS[item.status.id]}
                        style={{ width: 110, flexShrink: 0 }}
                        onClick={() => handleStatusCycle(item)}
                        title="Click to cycle status"
                      >
                        {STATUS_LABELS[item.status.id]}
                      </Button>
                    )}

                    {/* Content */}
                    <Text
                      size="sm"
                      flex={1}
                      td={item.status.id === 3 ? 'line-through' : undefined}
                      c={item.status.id === 4 ? 'dimmed' : undefined}
                    >
                      {item.content}
                    </Text>

                    {/* Created */}
                    <Text size="xs" c="dimmed" style={{ width: 90, flexShrink: 0 }}>
                      {formatDate(item.createdAt)}
                    </Text>

                    {/* Due date */}
                    <Text
                      size="xs"
                      fw={isOverdue(item.dueDate, item.status.id) ? 600 : 400}
                      c={isOverdue(item.dueDate, item.status.id) ? 'red' : 'dimmed'}
                      style={{ width: 90, flexShrink: 0 }}
                    >
                      {item.dueDate
                        ? <>{item.dueDate}{isOverdue(item.dueDate, item.status.id) && ' ⚠️'}</>
                        : <Text span c="dimmed" style={{ opacity: 0.4 }}>—</Text>
                      }
                    </Text>

                    {/* Actions */}
                    <Group gap="xs" style={{ width: 90, flexShrink: 0 }} justify="flex-end">
                      {!isClosed && (
                        <>
                          <Button variant="subtle" size="xs" onClick={() => startEdit(item)}>Edit</Button>
                          <Button variant="subtle" color="red" size="xs" onClick={() => handleDelete(item.id)}>Delete</Button>
                        </>
                      )}
                    </Group>
                  </Group>

                  {/* Tag pills */}
                  {item.tags.length > 0 && (
                    <Group gap={4} pl={122}>
                      {item.tags.map(tag => (
                        <Badge
                          key={tag.id}
                          size="xs"
                          style={{ background: tag.color, color: getTagTextColor(tag.color) }}
                          variant="filled"
                        >
                          {tag.name}
                        </Badge>
                      ))}
                    </Group>
                  )}

                  {/* Notes */}
                  {item.notes ? (
                    <Text size="sm" c="dimmed" fs="italic" pl={2} style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
                      {item.notes}
                    </Text>
                  ) : !isClosed ? (
                    <UnstyledButton onClick={() => startEdit(item)} style={{ paddingLeft: 2 }}>
                      <Text size="xs" c="dimmed" style={{ opacity: 0.5 }}>+ Add note</Text>
                    </UnstyledButton>
                  ) : null}
                </Stack>
              )}
            </Paper>
          ))}
        </Stack>
      )}
    </Container>
  )
}
