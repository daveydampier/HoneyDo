import { useState, useEffect, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { api } from '../api/client'
import type { TodoList, ApiError } from '../api/types'

export default function ListsPage() {
  const { displayName, logout } = useAuth()
  const navigate = useNavigate()
  const [lists, setLists] = useState<TodoList[]>([])
  const [newTitle, setNewTitle] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [deletingId, setDeletingId] = useState<string | null>(null)

  const activeLists = lists.filter(l => !l.closedAt)
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
    <li key={list.id} style={{ background: '#fff', borderRadius: 8, padding: '12px 16px', display: 'flex', alignItems: 'center', gap: 12 }}>
      <Link to={`/lists/${list.id}`} style={{ fontWeight: 600, flex: 1 }}>{list.title}</Link>
      <span style={{ fontSize: 12, color: '#666', whiteSpace: 'nowrap' }}>
        {list.itemCount} item{list.itemCount !== 1 ? 's' : ''} · Owner: {list.ownerName}
      </span>
      {list.role === 'Owner' && (
        <button
          onClick={() => handleDelete(list)}
          disabled={deletingId === list.id}
          style={{ background: 'none', border: 'none', color: '#c00', fontSize: 13, cursor: 'pointer', whiteSpace: 'nowrap' }}
        >
          {deletingId === list.id ? 'Deleting…' : 'Delete'}
        </button>
      )}
    </li>
  )

  return (
    <div style={{ maxWidth: 640, margin: '40px auto', padding: '0 16px' }}>
      <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <h1>HoneyDo</h1>
        <span style={{ fontSize: 14, display: 'flex', alignItems: 'center', gap: 12 }}>
          <button onClick={() => navigate('/friends')} style={{ background: 'none', border: 'none', color: '#666', fontSize: 14, cursor: 'pointer' }}>Friends</button>
          <button onClick={() => navigate('/profile')} style={{ background: 'none', border: 'none', color: 'inherit', fontSize: 14, cursor: 'pointer' }}>{displayName}</button>
          <button onClick={logout} style={{ background: 'none', border: 'none', color: '#666', fontSize: 14, cursor: 'pointer' }}>Sign out</button>
        </span>
      </header>

      <form onSubmit={handleCreate} style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
        <input
          style={{ flex: 1 }}
          placeholder="New list title…"
          value={newTitle}
          onChange={e => setNewTitle(e.target.value)}
          required
        />
        <button type="submit">Create</button>
      </form>
      {error && <p style={{ color: 'red', fontSize: 14, marginBottom: 12 }}>{error}</p>}

      {loading ? <p>Loading…</p> : (
        <>
          {/* Active lists */}
          <section style={{ marginBottom: closedLists.length > 0 ? 32 : 0 }}>
            <h2 style={{ fontSize: 13, fontWeight: 600, color: '#999', textTransform: 'uppercase', letterSpacing: '0.05em', margin: '0 0 8px' }}>
              Active
            </h2>
            {activeLists.length === 0 ? (
              <p style={{ color: '#666', fontSize: 14 }}>No active lists. Create one above.</p>
            ) : (
              <ul style={{ listStyle: 'none', display: 'flex', flexDirection: 'column', gap: 8 }}>
                {activeLists.map(listRow)}
              </ul>
            )}
          </section>

          {/* Closed lists */}
          {closedLists.length > 0 && (
            <section>
              <h2 style={{ fontSize: 13, fontWeight: 600, color: '#999', textTransform: 'uppercase', letterSpacing: '0.05em', margin: '0 0 8px' }}>
                Closed
              </h2>
              <ul style={{ listStyle: 'none', display: 'flex', flexDirection: 'column', gap: 8 }}>
                {closedLists.map(list => (
                  <li key={list.id} style={{ background: '#fafafa', borderRadius: 8, padding: '12px 16px', display: 'flex', alignItems: 'center', gap: 12, opacity: 0.8 }}>
                    <Link to={`/lists/${list.id}`} style={{ fontWeight: 600, flex: 1, color: '#888' }}>{list.title}</Link>
                    <span style={{ fontSize: 12, color: '#999', whiteSpace: 'nowrap' }}>
                      {list.itemCount} item{list.itemCount !== 1 ? 's' : ''} · Owner: {list.ownerName}
                    </span>
                    {list.role === 'Owner' && (
                      <button
                        onClick={() => handleDelete(list)}
                        disabled={deletingId === list.id}
                        style={{ background: 'none', border: 'none', color: '#c00', fontSize: 13, cursor: 'pointer', whiteSpace: 'nowrap' }}
                      >
                        {deletingId === list.id ? 'Deleting…' : 'Delete'}
                      </button>
                    )}
                  </li>
                ))}
              </ul>
            </section>
          )}
        </>
      )}
    </div>
  )
}
