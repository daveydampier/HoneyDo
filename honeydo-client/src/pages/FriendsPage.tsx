import { useState, useEffect, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import type { FriendsResult, FriendInfo, ReceivedRequestInfo, SentRequestInfo, SendRequestResult, ApiError } from '../api/types'
import AvatarCircle from '../components/AvatarCircle'

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
    <div style={{ maxWidth: 640, margin: '40px auto', padding: '0 16px' }}>
      <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <h1>Friends</h1>
        <button onClick={() => navigate('/')} style={{ background: 'none', border: 'none', color: '#666', fontSize: 14, cursor: 'pointer' }}>← Back to lists</button>
      </header>

      <section style={{ marginBottom: 32 }}>
        <h2 style={{ fontSize: 16, marginBottom: 12 }}>Add a Friend</h2>
        <form onSubmit={handleSendRequest} style={{ display: 'flex', gap: 8 }}>
          <input
            style={{ flex: 1 }}
            type="email"
            placeholder="Friend's email address…"
            value={email}
            onChange={e => setEmail(e.target.value)}
            required
          />
          <button type="submit">Send Request</button>
        </form>
        {sendError && <p style={{ color: 'red', fontSize: 14, marginTop: 8 }}>{sendError}</p>}
        {sendSuccess && <p style={{ color: 'green', fontSize: 14, marginTop: 8 }}>{sendSuccess}</p>}
      </section>

      {loading ? <p>Loading…</p> : (
        <>
          {data.pendingReceived.length > 0 && (
            <section style={{ marginBottom: 32 }}>
              <h2 style={{ fontSize: 16, marginBottom: 12 }}>Pending Requests</h2>
              <ul style={{ listStyle: 'none', display: 'flex', flexDirection: 'column', gap: 8 }}>
                {data.pendingReceived.map((req: ReceivedRequestInfo) => (
                  <li key={req.requesterId} style={{ background: '#fff', borderRadius: 8, padding: '12px 16px', display: 'flex', alignItems: 'center', gap: 12 }}>
                    <div style={{ flex: 1 }}>
                      <div style={{ fontWeight: 600 }}>{req.displayName}</div>
                      <div style={{ fontSize: 13, color: '#666' }}>{req.email}</div>
                    </div>
                    <button
                      onClick={() => handleRespond(req.requesterId, true)}
                      style={{ fontSize: 13, background: '#0a84ff', color: '#fff', border: 'none', borderRadius: 4, padding: '4px 10px', cursor: 'pointer' }}
                    >
                      Accept
                    </button>
                    <button
                      onClick={() => handleRespond(req.requesterId, false)}
                      style={{ fontSize: 13, background: 'none', border: '1px solid #ccc', borderRadius: 4, padding: '4px 10px', cursor: 'pointer' }}
                    >
                      Decline
                    </button>
                  </li>
                ))}
              </ul>
            </section>
          )}

          {data.pendingSent.length > 0 && (
            <section style={{ marginBottom: 32 }}>
              <h2 style={{ fontSize: 16, marginBottom: 12 }}>Sent Requests</h2>
              <ul style={{ listStyle: 'none', display: 'flex', flexDirection: 'column', gap: 8 }}>
                {data.pendingSent.map((req: SentRequestInfo) => (
                  <li key={req.addresseeId} style={{ background: '#fff', borderRadius: 8, padding: '12px 16px', display: 'flex', alignItems: 'center', gap: 12 }}>
                    <div style={{ flex: 1 }}>
                      <div style={{ fontWeight: 600 }}>{req.displayName}</div>
                      <div style={{ fontSize: 13, color: '#666' }}>{req.email}</div>
                    </div>
                    <span style={{ fontSize: 13, color: '#888' }}>Pending</span>
                    <button
                      onClick={() => handleCancelRequest(req.addresseeId)}
                      style={{ fontSize: 13, background: 'none', border: 'none', color: '#c00', cursor: 'pointer' }}
                    >
                      Cancel
                    </button>
                  </li>
                ))}
              </ul>
            </section>
          )}

          <section>
            <h2 style={{ fontSize: 16, marginBottom: 12 }}>
              Friends {data.friends.length > 0 && <span style={{ fontWeight: 400, color: '#666' }}>({data.friends.length})</span>}
            </h2>
            {data.friends.length === 0 ? (
              <p style={{ color: '#666' }}>No friends yet. Send a request above.</p>
            ) : (
              <ul style={{ listStyle: 'none', display: 'flex', flexDirection: 'column', gap: 8 }}>
                {data.friends.map((friend: FriendInfo) => (
                  <li key={friend.profileId} style={{ background: '#fff', borderRadius: 8, padding: '12px 16px', display: 'flex', alignItems: 'center', gap: 12 }}>
                    <AvatarCircle avatarUrl={friend.avatarUrl} displayName={friend.displayName} size={40} />
                    <div style={{ flex: 1 }}>
                      <div style={{ fontWeight: 600 }}>{friend.displayName}</div>
                      <div style={{ fontSize: 13, color: '#666' }}>{friend.email}</div>
                    </div>
                    <button
                      onClick={() => handleRemove(friend.profileId, friend.displayName)}
                      style={{ background: 'none', border: 'none', color: '#c00', fontSize: 13, cursor: 'pointer' }}
                    >
                      Remove
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </section>
        </>
      )}
    </div>
  )
}
