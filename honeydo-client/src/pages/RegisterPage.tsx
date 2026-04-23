import { useState, useEffect, type FormEvent } from 'react'
import { useNavigate, useSearchParams, Link } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { api } from '../api/client'
import type { ApiError } from '../api/types'

export default function RegisterPage() {
  const { register } = useAuth()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()

  // Invite params — present when the user arrived via an invitation email link.
  const inviteToken = searchParams.get('invite')
  const inviteEmail = searchParams.get('email') ?? ''

  const [email, setEmail] = useState(inviteEmail)
  const [password, setPassword] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [errors, setErrors] = useState<Record<string, string[]>>({})
  const [loading, setLoading] = useState(false)

  // If the URL supplies an email (from the invite link), keep the field in sync
  // with it on first render but still let the user edit it.
  useEffect(() => {
    if (inviteEmail) setEmail(inviteEmail)
  }, [inviteEmail])

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setErrors({})
    setLoading(true)
    try {
      await register(email, password, displayName)

      // If this registration came from an invite link, redeem the token now.
      // The API will create the pending friend request from the inviter and mark
      // the invitation as used. We navigate to /friends so the user sees it.
      if (inviteToken) {
        try {
          await api.post<void>('/invitations/accept', { token: inviteToken })
          navigate('/friends')
        } catch {
          // Accepting the invite failed (expired / already used), but the account
          // was created successfully — just go to the home page.
          navigate('/')
        }
      } else {
        navigate('/')
      }
    } catch (err) {
      const apiErr = err as ApiError
      setErrors(apiErr.errors ?? { _: [apiErr.title] })
    } finally {
      setLoading(false)
    }
  }

  const fieldError = (field: string) => errors[field]?.[0] ?? errors[field.toLowerCase()]?.[0]

  return (
    <div style={{ maxWidth: 400, margin: '80px auto', padding: '0 16px' }}>
      <h1 style={{ marginBottom: 8 }}>Create your account</h1>

      {inviteToken && (
        <p style={{ marginBottom: 20, padding: '10px 14px', background: '#f0f7ff', borderRadius: 6, fontSize: 14, color: '#0a84ff' }}>
          🎉 You've been invited to HoneyDo! Create your account below to connect.
        </p>
      )}

      <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
        <input
          type="text"
          placeholder="Display name"
          value={displayName}
          onChange={e => setDisplayName(e.target.value)}
          required
        />
        {fieldError('DisplayName') && <p style={{ color: 'red', fontSize: 14 }}>{fieldError('DisplayName')}</p>}

        <input
          type="email"
          placeholder="Email"
          value={email}
          onChange={e => setEmail(e.target.value)}
          required
        />
        {fieldError('Email') && <p style={{ color: 'red', fontSize: 14 }}>{fieldError('Email')}</p>}

        <input
          type="password"
          placeholder="Password (min 8 chars)"
          value={password}
          onChange={e => setPassword(e.target.value)}
          required
        />
        {fieldError('Password') && <p style={{ color: 'red', fontSize: 14 }}>{fieldError('Password')}</p>}

        <button type="submit" disabled={loading}>
          {loading ? 'Creating account…' : 'Create account'}
        </button>
      </form>

      <p style={{ marginTop: 16, fontSize: 14 }}>
        Already have an account? <Link to="/login">Sign in</Link>
      </p>
    </div>
  )
}
