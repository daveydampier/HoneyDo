import { useState, useEffect, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { api } from '../api/client'
import type { TodoList, Profile, ApiError } from '../api/types'
import AvatarCircle from '../components/AvatarCircle'
import {
  Container, Group, Title, Text, TextInput, Button,
  Paper, Stack, Anchor, Loader, Alert, UnstyledButton,
} from '@mantine/core'
import { IconSearch, IconAlertCircle } from '@tabler/icons-react'

export default function ListsPage() {
  const { displayName, logout } = useAuth()
  const navigate = useNavigate()
  const [lists, setLists] = useState<TodoList[]>([])
  const [newTitle, setNewTitle] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const [avatarUrl, setAvatarUrl] = useState<string | null>(null)
  const [search, setSearch] = useState('')

  useEffect(() => {
    api.get<Profile>('/profile')
      .then(p => setAvatarUrl(p.avatarUrl))
      .catch(() => {})
  }, [])

  const activeLists = lists.filter(l => !l.closedAt)
  const filteredActiveLists = search.trim() === ''
    ? activeLists
    : activeLists.filter(l => l.title.toLowerCase().includes(search.trim().toLowerCase()))
  const closedLists = [...lists.filter(l => !!l.closedAt)]
    .sort((a, b) => new Date(b.closedAt!).getTime() - new Date(a.closedAt!).getTime())

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

  const listRow = (list: TodoList) => (
    <Paper key={list.id} p="sm" radius="md" withBorder>
      <Group gap="sm" wrap="nowrap">
        <Anchor component={Link} to={`/lists/${list.id}`} fw={600} flex={1} truncate>
          {list.title}
        </Anchor>
        <Text size="xs" c="dimmed" style={{ whiteSpace: 'nowrap' }}>
          {list.itemCount} item{list.itemCount !== 1 ? 's' : ''} · {list.ownerName}
        </Text>
        {list.role === 'Owner' && (
          <Button
            variant="subtle"
            color="red"
            size="xs"
            loading={deletingId === list.id}
            onClick={() => handleDelete(list)}
          >
            Delete
          </Button>
        )}
      </Group>
    </Paper>
  )

  const closedRow = (list: TodoList) => (
    <Paper key={list.id} p="sm" radius="md" withBorder style={{ opacity: 0.7 }}>
      <Group gap="sm" wrap="nowrap">
        <Anchor component={Link} to={`/lists/${list.id}`} fw={600} flex={1} truncate c="dimmed">
          {list.title}
        </Anchor>
        <Text size="xs" c="dimmed" style={{ whiteSpace: 'nowrap' }}>
          {list.itemCount} item{list.itemCount !== 1 ? 's' : ''} · {list.ownerName}
        </Text>
        {list.role === 'Owner' && (
          <Button
            variant="subtle"
            color="red"
            size="xs"
            loading={deletingId === list.id}
            onClick={() => handleDelete(list)}
          >
            Delete
          </Button>
        )}
      </Group>
    </Paper>
  )

  return (
    <Container size="md" pt="xl">
      <Group justify="space-between" mb="lg">
        <Title order={1}>HoneyDo</Title>
        <Group gap="xs">
          <UnstyledButton
            onClick={() => navigate('/profile')}
            style={{ display: 'flex', alignItems: 'center', gap: 8 }}
          >
            <AvatarCircle avatarUrl={avatarUrl} displayName={displayName ?? ''} size={28} />
            <Text size="sm">{displayName}</Text>
          </UnstyledButton>
          <Button variant="subtle" color="gray" size="xs" onClick={() => navigate('/friends')}>
            Friends
          </Button>
          <Button variant="subtle" color="gray" size="xs" onClick={logout}>
            Sign out
          </Button>
        </Group>
      </Group>

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
            <Group gap="sm" mb="xs" align="center">
              <Text size="xs" fw={600} c="dimmed" tt="uppercase" style={{ letterSpacing: '0.05em', flexShrink: 0 }}>
                Active
              </Text>
              {activeLists.length > 0 && (
                <TextInput
                  flex={1}
                  size="xs"
                  placeholder="Search lists…"
                  value={search}
                  onChange={e => setSearch(e.target.value)}
                  leftSection={<IconSearch size={12} />}
                />
              )}
            </Group>
            {activeLists.length === 0 ? (
              <Text size="sm" c="dimmed">No active lists. Create one above.</Text>
            ) : filteredActiveLists.length === 0 ? (
              <Text size="sm" c="dimmed">No lists match "{search.trim()}".</Text>
            ) : (
              <Stack gap="sm">{filteredActiveLists.map(listRow)}</Stack>
            )}
          </section>

          {/* Closed lists */}
          {closedLists.length > 0 && (
            <section>
              <Text size="xs" fw={600} c="dimmed" tt="uppercase" mb="xs" style={{ letterSpacing: '0.05em' }}>
                Closed
              </Text>
              <Stack gap="sm">{closedLists.map(closedRow)}</Stack>
            </section>
          )}
        </Stack>
      )}
    </Container>
  )
}
