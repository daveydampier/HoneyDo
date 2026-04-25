import { useState, use } from 'react'
import { useParams, Link } from 'react-router-dom'
import { api } from '../api/client'
import type { ActivityLogEntry, TodoList } from '../api/types'
import {
  Container, Title, Text, Anchor,
  Timeline,
} from '@mantine/core'
import {
  IconPlus, IconCheck, IconTrash, IconUserPlus,
  IconLock, IconActivity,
  IconTag, IconNotes,
} from '@tabler/icons-react'

const ACTION_LABELS: Record<string, string> = {
  ItemCreated:   'added a task',
  StatusChanged: 'updated a task status',
  ItemDeleted:   'deleted a task',
  MemberAdded:   'added a member',
  ListClosed:    'closed the list',
  TagAdded:      'applied a tag',
  TagRemoved:    'removed a tag',
  NotesUpdated:  'updated task notes',
}

const ACTION_ICONS: Record<string, React.ReactNode> = {
  ItemCreated:   <IconPlus size={12} />,
  StatusChanged: <IconCheck size={12} />,
  ItemDeleted:   <IconTrash size={12} />,
  MemberAdded:   <IconUserPlus size={12} />,
  ListClosed:    <IconLock size={12} />,
  TagAdded:      <IconTag size={12} />,
  TagRemoved:    <IconTag size={12} />,
  NotesUpdated:  <IconNotes size={12} />,
}

function friendlyAction(actionType: string): string {
  return ACTION_LABELS[actionType] ?? actionType
}

function formatTimestamp(iso: string): string {
  const d = new Date(iso)
  return d.toLocaleString(undefined, {
    month:  'short',
    day:    'numeric',
    year:   'numeric',
    hour:   'numeric',
    minute: '2-digit',
  })
}

export default function ActivityPage() {
  const { listId } = useParams<{ listId: string }>()

  const [dataPromise] = useState(() => Promise.all([
    api.get<TodoList>(`/lists/${listId}`),
    api.get<ActivityLogEntry[]>(`/lists/${listId}/activity`),
  ] as const))

  const [list, logs] = use(dataPromise)

  return (
    <Container size="md" pt="xl">
      <Anchor component={Link} to={`/lists/${listId}`} size="sm" c="dimmed">
        ← Back to list
      </Anchor>

      <Title order={1} mt="sm" mb={4}>{list.title}</Title>
      <Text size="sm" c="dimmed" mb="xl">Activity log · newest first</Text>

      {logs.length === 0 ? (
        <Text size="sm" c="dimmed">No activity recorded yet.</Text>
      ) : (
        <Timeline active={logs.length - 1} bulletSize={24} lineWidth={2}>
          {logs.map(log => (
            <Timeline.Item
              key={log.id}
              bullet={ACTION_ICONS[log.actionType] ?? <IconActivity size={12} />}
              title={
                <Text size="sm">
                  <Text span fw={600}>{log.actorName}</Text>
                  {' '}
                  <Text span c="dimmed">{friendlyAction(log.actionType)}</Text>
                </Text>
              }
            >
              {log.detail && (
                <Text size="xs" fs="italic" c="dimmed" mt={2}>{log.detail}</Text>
              )}
              <Text size="xs" c="dimmed" mt={2}>{formatTimestamp(log.timestamp)}</Text>
            </Timeline.Item>
          ))}
        </Timeline>
      )}
    </Container>
  )
}
