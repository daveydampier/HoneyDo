import { useState, useEffect, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import type { FriendsResult, FriendInfo, ReceivedRequestInfo, SentRequestInfo, SendRequestResult, ApiError } from '../api/types'
import AvatarCircle from '../components/AvatarCircle'
import {
  Container, Group, Title, Text, TextInput, Button,
  Paper, Stack, Alert, Badge, Loader,
} from '@mantine/core'
import { IconAlertCircle, IconCircleCheck } from '@tabler/icons-react'

export default function FriendsPage() {
  const navigate = useNavigate()
  const [data, setData] = useState<FriendsResult>({ friends: [], pendingReceived: [], pendingSent: [] })
  const [loading, setLoading] = useState(true)
  const [email, setEmail] = useState('')
  const [sendError, setSendError] = useState<string | null>(null)
  const [sendSuccess, setSendSuccess] = useState<string | null>(null)

  async function load() {
    try {
      const result = await api.get<FriendsResult>('/friends')
      setData(result)
    } catch {
      // ignore
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  async function handleSendRequest(e: FormEvent) {
    e.preventDefault()
    setSendError(null)
    setSendSuccess(null)
    try {
      const result = await api.post<SendRequestResult>('/friends', { email })
      const sentTo = email
      setEmail('')
      setSendSuccess(
        result.invitationSent
          ? `No account found for ${sentTo} — we've sent them an invitation to join HoneyDo!`
          : 'Friend request sent!'
      )
      await load()
    } catch (err) {
      const apiErr = err as ApiError
      setSendError(apiErr.errors?.Email?.[0] ?? apiErr.title)
    }
  }

  async function handleRespond(requesterId: string, accept: boolean) {
    try {
      await api.patch<void>(`/friends/${requesterId}`, { accept })
      await load()
    } catch {
      alert('Failed to respond to request. Please try again.')
    }
  }

  async function handleRemove(friendId: string, displayName: string) {
    if (!confirm(`Remove ${displayName} from your friends?`)) return
    try {
      await api.delete(`/friends/${friendId}`)
      setData(prev => ({ ...prev, friends: prev.friends.filter(f => f.profileId !== friendId) }))
    } catch {
      alert('Failed to remove friend. Please try again.')
    }
  }

  async function handleCancelRequest(addresseeId: string) {
    try {
      await api.delete(`/friends/${addresseeId}`)
      setData(prev => ({ ...prev, pendingSent: prev.pendingSent.filter(r => r.addresseeId !== addresseeId) }))
    } catch {
      alert('Failed to cancel request. Please try again.')
    }
  }

  return (
    <Container size="md" pt="xl">
      <Group justify="space-between" mb="lg">
        <Title order={1}>Friends</Title>
        <Button variant="subtle" color="gray" size="xs" onClick={() => navigate('/')}>
          ← Back to lists
        </Button>
      </Group>

      {/* Add a friend */}
      <Stack gap="xs" mb="xl">
        <Text fw={600} size="md">Add a Friend</Text>
        <form onSubmit={handleSendRequest}>
          <Group gap="sm">
            <TextInput
              flex={1}
              type="email"
              placeholder="Friend's email address…"
              value={email}
              onChange={e => setEmail(e.target.value)}
              required
            />
            <Button type="submit">Send Request</Button>
          </Group>
        </form>
        {sendError && (
          <Alert color="red" variant="light" icon={<IconAlertCircle size={16} />}>
            {sendError}
          </Alert>
        )}
        {sendSuccess && (
          <Alert color="green" variant="light" icon={<IconCircleCheck size={16} />}>
            {sendSuccess}
          </Alert>
        )}
      </Stack>

      {loading ? (
        <Group justify="center" mt="xl"><Loader size="sm" /></Group>
      ) : (
        <Stack gap="xl">
          {/* Pending received */}
          {data.pendingReceived.length > 0 && (
            <section>
              <Text fw={600} mb="sm">Pending Requests</Text>
              <Stack gap="sm">
                {data.pendingReceived.map((req: ReceivedRequestInfo) => (
                  <Paper key={req.requesterId} p="sm" radius="md" withBorder>
                    <Group gap="sm">
                      <Stack gap={2} flex={1}>
                        <Text fw={600} size="sm">{req.displayName}</Text>
                        <Text size="xs" c="dimmed">{req.email}</Text>
                      </Stack>
                      <Button size="xs" onClick={() => handleRespond(req.requesterId, true)}>
                        Accept
                      </Button>
                      <Button size="xs" variant="outline" color="gray" onClick={() => handleRespond(req.requesterId, false)}>
                        Decline
                      </Button>
                    </Group>
                  </Paper>
                ))}
              </Stack>
            </section>
          )}

          {/* Pending sent */}
          {data.pendingSent.length > 0 && (
            <section>
              <Text fw={600} mb="sm">Sent Requests</Text>
              <Stack gap="sm">
                {data.pendingSent.map((req: SentRequestInfo) => (
                  <Paper key={req.addresseeId} p="sm" radius="md" withBorder>
                    <Group gap="sm">
                      <Stack gap={2} flex={1}>
                        <Text fw={600} size="sm">{req.displayName}</Text>
                        <Text size="xs" c="dimmed">{req.email}</Text>
                      </Stack>
                      <Badge color="yellow" variant="light">Pending</Badge>
                      <Button
                        variant="subtle"
                        color="red"
                        size="xs"
                        onClick={() => handleCancelRequest(req.addresseeId)}
                      >
                        Cancel
                      </Button>
                    </Group>
                  </Paper>
                ))}
              </Stack>
            </section>
          )}

          {/* Friends list */}
          <section>
            <Text fw={600} mb="sm">
              Friends{' '}
              {data.friends.length > 0 && (
                <Text span c="dimmed" fw={400}>({data.friends.length})</Text>
              )}
            </Text>
            {data.friends.length === 0 ? (
              <Text size="sm" c="dimmed">No friends yet. Send a request above.</Text>
            ) : (
              <Stack gap="sm">
                {data.friends.map((friend: FriendInfo) => (
                  <Paper key={friend.profileId} p="sm" radius="md" withBorder>
                    <Group gap="sm">
                      <AvatarCircle avatarUrl={friend.avatarUrl} displayName={friend.displayName} size={40} />
                      <Stack gap={2} flex={1}>
                        <Text fw={600} size="sm">{friend.displayName}</Text>
                        <Text size="xs" c="dimmed">{friend.email}</Text>
                      </Stack>
                      <Button
                        variant="subtle"
                        color="red"
                        size="xs"
                        onClick={() => handleRemove(friend.profileId, friend.displayName)}
                      >
                        Remove
                      </Button>
                    </Group>
                  </Paper>
                ))}
              </Stack>
            )}
          </section>
        </Stack>
      )}
    </Container>
  )
}
