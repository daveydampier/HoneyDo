import { useState, useEffect } from 'react'
import { useParams, Link } from 'react-router-dom'
import { api } from '../api/client'
import type { ActivityLogEntry, TodoList } from '../api/types'

const ACTION_LABELS: Record<string, string> = {
  ItemCreated:   'added a task',
  StatusChanged: 'updated a task status',
  ItemDeleted:   'deleted a task',
  MemberAdded:   'added a member',
  ListClosed:    'closed the list',
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
  const [list, setList]       = useState<TodoList | null>(null)
  const [logs, setLogs]       = useState<ActivityLogEntry[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError]     = useState<string | null>(null)

  useEffect(() => {
    if (!listId) return
    Promise.all([
      api.get<TodoList>(`/lists/${listId}`),
      api.get<ActivityLogEntry[]>(`/lists/${listId}/activity`),
    ])
      .then(([listRes, logsRes]) => {
        setList(listRes)
        setLogs(logsRes)
      })
      .catch(() => setError('Failed to load activity log.'))
      .finally(() => setLoading(false))
  }, [listId])

  return (
    <div style={{ maxWidth: 640, margin: '40px auto', padding: '0 16px' }}>
      <Link to={`/lists/${listId}`} style={{ fontSize: 14, color: '#666' }}>
        ← Back to list
      </Link>

      <h1 style={{ margin: '16px 0 4px' }}>
        {list?.title ?? 'Activity'}
      </h1>
      <p style={{ margin: '0 0 24px', fontSize: 14, color: '#888' }}>
        Activity log · newest first
      </p>

      {loading && <p>Loading…</p>}
      {error   && <p style={{ color: 'red', fontSize: 14 }}>{error}</p>}

      {!loading && !error && logs.length === 0 && (
        <p style={{ color: '#888', fontSize: 14 }}>No activity recorded yet.</p>
      )}

      {!loading && !error && logs.length > 0 && (
        <ul style={{ listStyle: 'none', display: 'flex', flexDirection: 'column', gap: 0 }}>
          {logs.map((log, i) => (
            <li
              key={log.id}
              style={{
                display:       'flex',
                gap:           16,
                paddingBottom: 20,
                position:      'relative',
              }}
            >
              {/* Timeline spine */}
              <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', flexShrink: 0 }}>
                <div style={{
                  width:        10,
                  height:       10,
                  borderRadius: '50%',
                  background:   '#0a84ff',
                  flexShrink:   0,
                  marginTop:    4,
                }} />
                {i < logs.length - 1 && (
                  <div style={{ width: 2, flex: 1, background: '#e0e0e0', marginTop: 4 }} />
                )}
              </div>

              {/* Entry content */}
              <div style={{ paddingBottom: 4 }}>
                <span style={{ fontWeight: 600 }}>{log.actorName}</span>
                {' '}
                <span style={{ color: '#333' }}>{friendlyAction(log.actionType)}</span>
                {log.detail && (
                  <div style={{ fontSize: 13, color: '#555', marginTop: 2, fontStyle: 'italic' }}>
                    {log.detail}
                  </div>
                )}
                <div style={{ fontSize: 12, color: '#999', marginTop: 2 }}>
                  {formatTimestamp(log.timestamp)}
                </div>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
