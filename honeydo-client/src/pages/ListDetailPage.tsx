import { useState, useEffect, type FormEvent } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import type { TodoItem, Tag, PagedResult, TodoList, Member, AddableFriend, ApiError } from '../api/types'
import { useAuth } from '../context/AuthContext'
import { getTagTextColor } from '../utils/tags'

const STATUS_LABELS: Record<number, string> = { 1: 'Not Started', 2: 'Partial', 3: 'Complete', 4: 'Abandoned' }
const NOTES_MAX = 256

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })
}

/** Returns true if a YYYY-MM-DD date string is strictly before today (local time). */
function isDatePast(dateStr: string): boolean {
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  const d = new Date(dateStr + 'T00:00:00')
  return d < today
}

/** Returns true if a task's due date should show an overdue warning. */
function isOverdue(dueDate: string | null | undefined, statusId: number): boolean {
  if (!dueDate) return false
  if (statusId === 3 || statusId === 4) return false  // Complete or Abandoned — not overdue
  return isDatePast(dueDate)
}

export default function ListDetailPage() {
  const { listId } = useParams<{ listId: string }>()
  const { profileId } = useAuth()
  const navigate = useNavigate()

  // List metadata
  const [list, setList] = useState<TodoList | null>(null)

  // Items
  const [items, setItems] = useState<TodoItem[]>([])
  const [loadingItems, setLoadingItems] = useState(true)
  const [content, setContent] = useState('')
  const [dueDate, setDueDate] = useState('')
  const [createError, setCreateError] = useState<string | null>(null)

  // Sorting
  const [sortBy, setSortBy] = useState<'DueDate' | 'CreatedAt'>('DueDate')
  const [ascending, setAscending] = useState(true)

  // Inline editing
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editContent, setEditContent] = useState('')
  const [editNotes, setEditNotes] = useState('')
  const [editError, setEditError] = useState<string | null>(null)

  // Tags
  const [myTags, setMyTags] = useState<Tag[]>([])
  const [editTagIds, setEditTagIds] = useState<Set<string>>(new Set())
  const [togglingTagId, setTogglingTagId] = useState<string | null>(null)

  // Members
  const [members, setMembers] = useState<Member[]>([])
  const [addableFriends, setAddableFriends] = useState<AddableFriend[]>([])
  const [showMembers, setShowMembers] = useState(false)
  const [addingFriend, setAddingFriend] = useState<string | null>(null)
  const [memberError, setMemberError] = useState<string | null>(null)

  const isOwner = list?.role === 'Owner'
  const isClosed = !!list?.closedAt
  const canClose = isOwner && !isClosed && items.length > 0 && items.every(i => i.status.id !== 1)

  useEffect(() => {
    if (!listId) return
    api.get<TodoList>(`/lists/${listId}`)
      .then(setList)
      .catch(() => {})
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
    api.get<Tag[]>('/tags').then(setMyTags).catch(() => {})
  }, [])

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
    } catch {
      // ignore
    }
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
    try {
      await api.delete(`/lists/${listId}/members/${memberProfileId}`)
      setMembers(prev => prev.filter(m => m.profileId !== memberProfileId))
      const friendsRes = await api.get<AddableFriend[]>(`/lists/${listId}/addable-friends`)
      setAddableFriends(friendsRes)
    } catch {
      alert('Failed to remove member. Please try again.')
    }
  }

  async function handleClose() {
    if (!listId) return
    if (!confirm('Close this list? It will become read-only.')) return
    try {
      const updated = await api.post<TodoList>(`/lists/${listId}/close`, {})
      setList(updated)
    } catch {
      alert('Failed to close the list. Make sure all tasks are marked Partial, Complete, or Abandoned.')
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
    try {
      const updated = await api.patch<TodoItem>(`/lists/${listId}/items/${item.id}`, { statusId: next })
      setItems(prev => prev.map(i => i.id === updated.id ? updated : i))
    } catch {
      alert('Failed to update status. Please try again.')
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
    } catch {
      // leave state unchanged on failure
    } finally {
      setTogglingTagId(null)
    }
  }

  async function handleEditSave(id: string) {
    setEditError(null)
    try {
      const updated = await api.patch<TodoItem>(`/lists/${listId}/items/${id}`, {
        content: editContent,
        notes: editNotes,   // empty string clears notes
      })
      setItems(prev => prev.map(i => i.id === updated.id ? updated : i))
      setEditingId(null)
    } catch (err) {
      const apiErr = err as ApiError
      setEditError(
        apiErr.errors?.Content?.[0]
          ?? apiErr.errors?.Notes?.[0]
          ?? apiErr.title
      )
    }
  }

  async function handleDelete(id: string) {
    try {
      await api.delete(`/lists/${listId}/items/${id}`)
      setItems(prev => prev.filter(i => i.id !== id))
    } catch {
      alert('Failed to delete item. Please try again.')
    }
  }

  return (
    <div style={{ maxWidth: 640, margin: '40px auto', padding: '0 16px' }}>
      <Link to="/" style={{ fontSize: 14, color: '#666' }}>← Back to lists</Link>

      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', margin: '16px 0 16px' }}>
        <h1 style={{ margin: 0 }}>
          {list?.title ?? 'Tasks'}
          {isClosed && (
            <span style={{ marginLeft: 12, fontSize: 13, fontWeight: 400, background: '#e8f5e9', color: '#2e7d32', borderRadius: 4, padding: '2px 8px', verticalAlign: 'middle' }}>
              Closed
            </span>
          )}
        </h1>
        <div style={{ display: 'flex', gap: 8 }}>
          {isOwner && !isClosed && (
            <button
              onClick={handleClose}
              disabled={!canClose}
              title={!canClose ? 'All tasks must be Partial, Complete, or Abandoned to close this list' : 'Close this list'}
              style={{
                fontSize: 13,
                background: canClose ? '#2e7d32' : 'none',
                color: canClose ? '#fff' : '#aaa',
                border: `1px solid ${canClose ? '#2e7d32' : '#ddd'}`,
                borderRadius: 6,
                padding: '6px 12px',
                cursor: canClose ? 'pointer' : 'not-allowed',
              }}
            >
              Close List
            </button>
          )}
          <button
            onClick={() => navigate(`/lists/${listId}/activity`)}
            style={{ fontSize: 13, background: 'none', border: '1px solid #ccc', borderRadius: 6, padding: '6px 12px', cursor: 'pointer', color: '#555' }}
          >
            Activity
          </button>
          <button
            onClick={() => setShowMembers(v => !v)}
            style={{ fontSize: 13, background: 'none', border: '1px solid #ccc', borderRadius: 6, padding: '6px 12px', cursor: 'pointer', color: '#555' }}
          >
            {showMembers ? 'Hide members' : `Members (${list?.memberCount ?? '…'})`}
          </button>
        </div>
      </div>

      {isClosed && (
        <div style={{ marginBottom: 16, padding: '10px 14px', background: '#e8f5e9', borderRadius: 6, fontSize: 13, color: '#2e7d32' }}>
          ✅ This list was closed on {formatDate(list!.closedAt!)} and is now read-only.
        </div>
      )}

      {/* Members panel */}
      {showMembers && (
        <div style={{ background: '#f8f8f8', border: '1px solid #e0e0e0', borderRadius: 10, padding: '16px 20px', marginBottom: 28 }}>
          <h2 style={{ fontSize: 15, margin: '0 0 12px' }}>Members</h2>

          {memberError && <p style={{ color: 'red', fontSize: 13, marginBottom: 8 }}>{memberError}</p>}

          <ul style={{ listStyle: 'none', display: 'flex', flexDirection: 'column', gap: 6, margin: '0 0 16px' }}>
            {members.map(m => (
              <li key={m.profileId} style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <div style={{ flex: 1 }}>
                  <span style={{ fontWeight: 600 }}>{m.displayName}</span>
                  <span style={{
                    marginLeft: 8,
                    fontSize: 11,
                    background: m.role === 'Owner' ? '#0a84ff' : '#888',
                    color: '#fff',
                    borderRadius: 4,
                    padding: '1px 6px'
                  }}>
                    {m.role}
                  </span>
                </div>
                {isOwner && m.role !== 'Owner' && m.profileId !== profileId && (
                  <button
                    onClick={() => handleRemoveMember(m.profileId, m.displayName)}
                    style={{ fontSize: 12, background: 'none', border: 'none', color: '#c00', cursor: 'pointer' }}
                  >
                    Remove
                  </button>
                )}
              </li>
            ))}
          </ul>

          {isOwner && (
            <>
              <h3 style={{ fontSize: 14, margin: '0 0 8px', color: '#444' }}>Add a Friend as Collaborator</h3>
              {addableFriends.length === 0 ? (
                <p style={{ fontSize: 13, color: '#888', margin: 0 }}>
                  All your friends are already on this list, or you have no friends to add yet.
                </p>
              ) : (
                <ul style={{ listStyle: 'none', display: 'flex', flexDirection: 'column', gap: 6, margin: 0 }}>
                  {addableFriends.map(f => (
                    <li key={f.profileId} style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                      <div style={{ flex: 1 }}>
                        <span style={{ fontWeight: 500 }}>{f.displayName}</span>
                        <span style={{ fontSize: 13, color: '#666', marginLeft: 8 }}>{f.email}</span>
                      </div>
                      <button
                        onClick={() => handleAddFriend(f.profileId)}
                        disabled={addingFriend === f.profileId}
                        style={{
                          fontSize: 12,
                          background: '#0a84ff',
                          color: '#fff',
                          border: 'none',
                          borderRadius: 4,
                          padding: '4px 10px',
                          cursor: 'pointer',
                          opacity: addingFriend === f.profileId ? 0.6 : 1
                        }}
                      >
                        {addingFriend === f.profileId ? 'Adding…' : 'Add'}
                      </button>
                    </li>
                  ))}
                </ul>
              )}
            </>
          )}
        </div>
      )}

      {/* Sort toolbar */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 12 }}>
        <span style={{ fontSize: 13, color: '#666' }}>Sort by</span>
        {(['DueDate', 'CreatedAt'] as const).map(opt => (
          <button
            key={opt}
            onClick={() => setSortBy(opt)}
            style={{
              fontSize: 13,
              padding: '3px 10px',
              borderRadius: 4,
              border: '1px solid #ccc',
              background: sortBy === opt ? '#0a84ff' : 'none',
              color: sortBy === opt ? '#fff' : '#555',
              cursor: 'pointer',
            }}
          >
            {opt === 'DueDate' ? 'Due Date' : 'Created Date'}
          </button>
        ))}
        <button
          onClick={() => setAscending(v => !v)}
          title={ascending ? 'Ascending — click to switch to descending' : 'Descending — click to switch to ascending'}
          style={{
            fontSize: 13,
            padding: '3px 10px',
            borderRadius: 4,
            border: '1px solid #ccc',
            background: 'none',
            color: '#555',
            cursor: 'pointer',
            marginLeft: 'auto',
          }}
        >
          {ascending ? '↑ Asc' : '↓ Desc'}
        </button>
      </div>

      {/* New item form — hidden when list is closed */}
      {!isClosed && (
        <form onSubmit={handleCreate} style={{ display: 'flex', flexDirection: 'column', gap: 8, marginBottom: 16 }}>
          <div style={{ display: 'flex', gap: 8 }}>
            <input style={{ flex: 1 }} placeholder="New task…" value={content} onChange={e => setContent(e.target.value)} required />
            <input type="date" value={dueDate} onChange={e => setDueDate(e.target.value)} />
            <button type="submit">Add</button>
          </div>
          {createError && <p style={{ color: 'red', fontSize: 14 }}>{createError}</p>}
        </form>
      )}

      {loadingItems ? <p>Loading…</p> : (
        <>
          {/* Column headers */}
          {items.length > 0 && (
            <div style={{
              display: 'flex',
              alignItems: 'center',
              gap: 12,
              padding: '0 16px',
              marginBottom: 4,
            }}>
              <span style={{ width: 100, flexShrink: 0, fontSize: 11, fontWeight: 600, color: '#999', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Status</span>
              <span style={{ flex: 1, fontSize: 11, fontWeight: 600, color: '#999', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Task</span>
              <span style={{ width: 90, flexShrink: 0, fontSize: 11, fontWeight: 600, color: '#999', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Created</span>
              <span style={{ width: 90, flexShrink: 0, fontSize: 11, fontWeight: 600, color: '#999', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Due Date</span>
              <span style={{ width: 90, flexShrink: 0 }} />
            </div>
          )}

          <ul style={{ listStyle: 'none', display: 'flex', flexDirection: 'column', gap: 8 }}>
          {items.map(item => (
            <li key={item.id} style={{ background: '#fff', borderRadius: 8, padding: '12px 16px' }}>
              {editingId === item.id ? (
                /* ── Edit mode ── */
                <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                  <input
                    value={editContent}
                    onChange={e => setEditContent(e.target.value)}
                    autoFocus
                    placeholder="Task content"
                  />

                  <div style={{ position: 'relative' }}>
                    <textarea
                      value={editNotes}
                      onChange={e => setEditNotes(e.target.value)}
                      placeholder="Add a note… (optional)"
                      maxLength={NOTES_MAX}
                      rows={3}
                      style={{
                        width: '100%',
                        resize: 'vertical',
                        fontSize: 13,
                        padding: '6px 8px',
                        borderRadius: 4,
                        border: '1px solid #ccc',
                        boxSizing: 'border-box',
                        fontFamily: 'inherit',
                      }}
                    />
                    <span style={{
                      position: 'absolute',
                      bottom: 6,
                      right: 8,
                      fontSize: 11,
                      color: editNotes.length > NOTES_MAX - 20 ? '#c00' : '#aaa',
                      pointerEvents: 'none',
                    }}>
                      {editNotes.length}/{NOTES_MAX}
                    </span>
                  </div>

                  {/* Tag picker */}
                  {myTags.length > 0 && (
                    <div>
                      <p style={{ fontSize: 12, color: '#888', margin: '0 0 6px' }}>Tags</p>
                      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
                        {myTags.map(tag => {
                          const applied = editTagIds.has(tag.id)
                          return (
                            <button
                              key={tag.id}
                              type="button"
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
                                cursor: togglingTagId === tag.id ? 'not-allowed' : 'pointer',
                                opacity: togglingTagId === tag.id ? 0.6 : 1,
                              }}
                            >
                              {tag.name}
                            </button>
                          )
                        })}
                      </div>
                    </div>
                  )}

                  {editError && <p style={{ color: 'red', fontSize: 13, margin: 0 }}>{editError}</p>}

                  <div style={{ display: 'flex', gap: 8 }}>
                    <button onClick={() => handleEditSave(item.id)}>Save</button>
                    <button onClick={cancelEdit} style={{ background: 'none', border: '1px solid #ccc' }}>Cancel</button>
                  </div>
                </div>
              ) : (
                /* ── View mode ── */
                <div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                    {isClosed ? (
                      <span style={{ width: 100, flexShrink: 0, fontSize: 12, color: '#888', textAlign: 'center', border: '1px solid #eee', borderRadius: 4, padding: '2px 8px' }}>
                        {STATUS_LABELS[item.status.id]}
                      </span>
                    ) : (
                      <button
                        onClick={() => handleStatusCycle(item)}
                        title="Cycle status"
                        style={{ width: 100, flexShrink: 0, background: 'none', border: '1px solid #ccc', borderRadius: 4, padding: '2px 8px', fontSize: 12, textAlign: 'center' }}
                      >
                        {STATUS_LABELS[item.status.id]}
                      </button>
                    )}
                    <span style={{
                      flex: 1,
                      textDecoration: item.status.id === 3 ? 'line-through' : 'none',
                      color: item.status.id === 4 ? '#aaa' : 'inherit',
                    }}>
                      {item.content}
                    </span>
                    <span style={{ width: 90, flexShrink: 0, fontSize: 12, color: '#888' }}>
                      {formatDate(item.createdAt)}
                    </span>
                    <span style={{
                      width: 90,
                      flexShrink: 0,
                      fontSize: 12,
                      color: isOverdue(item.dueDate, item.status.id) ? '#c00' : '#888',
                      fontWeight: isOverdue(item.dueDate, item.status.id) ? 600 : 400,
                    }}>
                      {item.dueDate
                        ? <>
                            {item.dueDate}
                            {isOverdue(item.dueDate, item.status.id) && (
                              <span title="Overdue" style={{ marginLeft: 4 }}>⚠️</span>
                            )}
                          </>
                        : <span style={{ color: '#ccc' }}>—</span>
                      }
                    </span>
                    <div style={{ width: 90, flexShrink: 0, display: 'flex', gap: 8, justifyContent: 'flex-end' }}>
                      {!isClosed && (
                        <>
                          <button
                            onClick={() => startEdit(item)}
                            style={{ background: 'none', border: 'none', fontSize: 13, color: '#555', cursor: 'pointer' }}
                          >
                            Edit
                          </button>
                          <button
                            onClick={() => handleDelete(item.id)}
                            style={{ background: 'none', border: 'none', fontSize: 13, color: '#c00', cursor: 'pointer' }}
                          >
                            Delete
                          </button>
                        </>
                      )}
                    </div>
                  </div>

                  {/* Tag pills */}
                  {item.tags.length > 0 && (
                    <div style={{ display: 'flex', flexWrap: 'wrap', gap: 4, marginTop: 6, paddingLeft: 112 }}>
                      {item.tags.map(tag => (
                        <span key={tag.id} style={{
                          background: tag.color,
                          color: getTagTextColor(tag.color),
                          borderRadius: 10,
                          padding: '1px 8px',
                          fontSize: 11,
                          fontWeight: 500,
                        }}>
                          {tag.name}
                        </span>
                      ))}
                    </div>
                  )}

                  {/* Notes display */}
                  {item.notes ? (
                    <p style={{
                      margin: '6px 0 0 0',
                      fontSize: 13,
                      color: '#666',
                      fontStyle: 'italic',
                      paddingLeft: 2,
                      whiteSpace: 'pre-wrap',
                      wordBreak: 'break-word',
                    }}>
                      {item.notes}
                    </p>
                  ) : !isClosed ? (
                    <button
                      onClick={() => startEdit(item)}
                      style={{
                        display: 'block',
                        marginTop: 4,
                        paddingLeft: 2,
                        background: 'none',
                        border: 'none',
                        fontSize: 12,
                        color: '#bbb',
                        cursor: 'pointer',
                        textAlign: 'left',
                      }}
                    >
                      + Add note
                    </button>
                  ) : null}
                </div>
              )}
            </li>
          ))}
          {items.length === 0 && <p style={{ color: '#666' }}>No tasks yet. Add one above.</p>}
        </ul>
        </>
      )}
    </div>
  )
}
