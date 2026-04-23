import { useState, useEffect, useMemo, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import type { TodoList, Tag, ApiError } from '../api/types'
import {
  Container, Group, Title, Text, TextInput, Button,
  Paper, Stack, Anchor, Loader, Alert, Badge,
} from '@mantine/core'
import { IconSearch, IconAlertCircle, IconTag } from '@tabler/icons-react'
import { getTagTextColor } from '../utils/tags'

export default function ListsPage() {
  const [lists, setLists] = useState<TodoList[]>([])
  const [newTitle, setNewTitle] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [selectedTagIds, setSelectedTagIds] = useState<Set<string>>(new Set())

  const activeLists = lists.filter(l => !l.closedAt)
  const closedLists = [...lists.filter(l => !!l.closedAt)]
    .sort((a, b) => new Date(b.closedAt!).getTime() - new Date(a.closedAt!).getTime())

  // Union of all distinct tags across all active lists — for the filter bar
  const availableTags = useMemo<Tag[]>(() => {
    const seen = new Map<string, Tag>()
    for (const list of activeLists) {
      for (const tag of list.tags) {
        if (!seen.has(tag.id)) seen.set(tag.id, tag)
      }
    }
    return [...seen.values()].sort((a, b) => a.name.localeCompare(b.name))
  }, [lists])

  // Apply text search and tag filter
  const filteredActiveLists = useMemo(() => {
    let result = activeLists
    if (search.trim()) {
      const q = search.trim().toLowerCase()
      result = result.filter(l => l.title.toLowerCase().includes(q))
    }
    if (selectedTagIds.size > 0) {
      result = result.filter(l => l.tags.some(t => selectedTagIds.has(t.id)))
    }
    return result
  }, [activeLists, search, selectedTagIds])

  function toggleTag(tagId: string) {
    setSelectedTagIds(prev => {
      const next = new Set(prev)
      next.has(tagId) ? next.delete(tagId) : next.add(tagId)
      return next
    })
  }

  async function handleDelete(list: TodoList) {
    if (list.role !== 'Owner') return
    if (!confirm(`Delete "${list.title}"? This cannot be undone.`)) return
    setDeletingId(list.id)
    try {
      await api.delete(`/lists/${list.id}`)
      setLists(prev => prev.filter(l => l.id !== list.id))
    } catch {
      setError(`Failed to delete "${list.title}". Please try again.`)
    } finally {
      setDeletingId(null)
    }
  }

  useEffect(() => {
    api.get<TodoList[]>('/lists')
      .then(setLists)
      .catch(() => setError('Failed to load lists.'))
      .finally(() => setLoading(false))
  }, [])

  async function handleCreate(e: FormEvent) {
    e.preventDefault()
    setError(null)
    try {
      const created = await api.post<TodoList>('/lists', { title: newTitle })
      setLists(prev => [created, ...prev])
      setNewTitle('')
    } catch (err) {
      const apiErr = err as ApiError
      const msg = apiErr.errors?.Title?.[0] ?? apiErr.title
      setError(msg)
    }
  }

  // Tags on a list that match the active filter (to show as indicators)
  function matchedTags(list: TodoList): Tag[] {
    if (selectedTagIds.size === 0) return []
    return list.tags.filter(t => selectedTagIds.has(t.id))
  }

  const STATUS_CHIPS = [
    { key: 'notStartedCount', label: 'not started', color: '#868e96' },
    { key: 'partialCount',    label: 'partial',     color: '#f59f00' },
    { key: 'completeCount',   label: 'complete',    color: '#2f9e44' },
    { key: 'abandonedCount',  label: 'abandoned',   color: '#e03131' },
  ] as const

  const listRow = (list: TodoList, closed = false) => {
    const matched = matchedTags(list)
    const totalItems = list.notStartedCount + list.partialCount + list.completeCount + list.abandonedCount

    return (
      <Paper key={list.id} p="sm" radius="md" withBorder style={closed ? { opacity: 0.7 } : undefined}>
        <Group gap="sm" wrap="nowrap" align="center">
          {/* Title + matched tag indicators */}
          <Stack gap={4} style={{ minWidth: 0, width: 180, flexShrink: 0 }}>
            <Anchor
              component={Link}
              to={`/lists/${list.id}`}
              fw={600}
              c={closed ? 'dimmed' : undefined}
              truncate
            >
              {list.title}
            </Anchor>
            {matched.length > 0 && (
              <Group gap={4}>
                {matched.map(tag => (
                  <Badge
                    key={tag.id}
                    size="xs"
                    leftSection={<IconTag size={9} />}
                    style={{ background: tag.color, color: getTagTextColor(tag.color) }}
                    variant="filled"
                  >
                    {tag.name}
                  </Badge>
                ))}
              </Group>
            )}
          </Stack>

          {/* Status breakdown */}
          <Group gap="xs" flex={1} wrap="nowrap">
            {totalItems === 0 ? (
              <Text size="xs" c="dimmed">No tasks yet</Text>
            ) : (
              STATUS_CHIPS.map(({ key, label, color }) => {
                const count = list[key]
                if (count === 0) return null
                return (
                  <Text key={key} size="xs" style={{ whiteSpace: 'nowrap', color }}>
                    <Text span fw={600}>{count}</Text> {label}
                  </Text>
                )
              })
            )}
          </Group>

          {/* Owner + delete */}
          <Text size="xs" c="dimmed" style={{ whiteSpace: 'nowrap', flexShrink: 0 }}>
            {list.ownerName}
          </Text>
          {list.role === 'Owner' && (
            <Button
              variant="subtle"
              color="red"
              size="xs"
              loading={deletingId === list.id}
              onClick={() => handleDelete(list)}
              style={{ flexShrink: 0 }}
            >
              Delete
            </Button>
          )}
        </Group>
      </Paper>
    )
  }

  const hasFilters = search.trim() !== '' || selectedTagIds.size > 0

  return (
    <Container size="md" pt="xl">
      <Title order={2} mb="lg">My Lists</Title>

      <form onSubmit={handleCreate}>
        <Group gap="sm" mb="md">
          <TextInput
            flex={1}
            placeholder="New list title…"
            value={newTitle}
            onChange={e => setNewTitle(e.target.value)}
            required
          />
          <Button type="submit">Create</Button>
        </Group>
      </form>

      {error && (
        <Alert color="red" variant="light" icon={<IconAlertCircle size={16} />} mb="md">
          {error}
        </Alert>
      )}

      {loading ? (
        <Group justify="center" mt="xl"><Loader size="sm" /></Group>
      ) : (
        <Stack gap="xl">
          {/* Active lists */}
          <section>
            {/* Section heading row */}
            <Group justify="space-between" align="center" mb="xs">
              <Text size="sm" fw={700} c="dimmed" tt="uppercase" style={{ letterSpacing: '0.06em' }}>
                Active
              </Text>
              {hasFilters && (
                <Button
                  size="xs"
                  variant="subtle"
                  color="gray"
                  onClick={() => { setSearch(''); setSelectedTagIds(new Set()) }}
                >
                  Clear filters
                </Button>
              )}
            </Group>

            {/* Search + tag filter row */}
            {activeLists.length > 0 && (
              <Group gap="sm" wrap="nowrap" mb="sm" align="center">
                {/* Search — fixed width on the left */}
                <TextInput
                  style={{ width: 200, flexShrink: 0 }}
                  size="xs"
                  placeholder="Search lists…"
                  value={search}
                  onChange={e => setSearch(e.target.value)}
                  leftSection={<IconSearch size={12} />}
                />

                {/* Tag pills — scrollable strip on the right */}
                {availableTags.length > 0 && (
                  <div style={{
                    flex: 1,
                    overflowX: 'auto',
                    display: 'flex',
                    gap: 6,
                    alignItems: 'center',
                    padding: '3px 2px',
                    // subtle fade on the right edge to hint there's more
                    maskImage: 'linear-gradient(to right, black 85%, transparent 100%)',
                  }}>
                    {availableTags.map(tag => {
                      const active = selectedTagIds.has(tag.id)
                      return (
                        <Badge
                          key={tag.id}
                          size="sm"
                          variant={active ? 'filled' : 'outline'}
                          style={{
                            cursor: 'pointer',
                            flexShrink: 0,
                            background: active ? tag.color : 'transparent',
                            color: active ? getTagTextColor(tag.color) : tag.color,
                            borderColor: tag.color,
                          }}
                          onClick={() => toggleTag(tag.id)}
                          leftSection={<IconTag size={10} />}
                        >
                          {tag.name}
                        </Badge>
                      )
                    })}
                  </div>
                )}
              </Group>
            )}

            {activeLists.length === 0 ? (
              <Text size="sm" c="dimmed">No active lists. Create one above.</Text>
            ) : filteredActiveLists.length === 0 ? (
              <Text size="sm" c="dimmed">
                No lists match your{search.trim() ? ' search' : ''}{search.trim() && selectedTagIds.size > 0 ? ' and' : ''}{selectedTagIds.size > 0 ? ' tag filter' : ''}.
              </Text>
            ) : (
              <Stack gap="sm">{filteredActiveLists.map(l => listRow(l))}</Stack>
            )}
          </section>

          {/* Closed lists */}
          {closedLists.length > 0 && (
            <section>
              <Text size="xs" fw={600} c="dimmed" tt="uppercase" mb="xs" style={{ letterSpacing: '0.05em' }}>
                Closed
              </Text>
              <Stack gap="sm">{closedLists.map(l => listRow(l, true))}</Stack>
            </section>
          )}
        </Stack>
      )}
    </Container>
  )
}
