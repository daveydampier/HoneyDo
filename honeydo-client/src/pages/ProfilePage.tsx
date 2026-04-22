import { useState, useEffect, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import type { Profile, ApiError } from '../api/types'

export default function ProfilePage() {
  const [profile, setProfile] = useState<Profile | null>(null)
  const [loading, setLoading] = useState(true)

  const [displayName, setDisplayName] = useState('')
  const [phoneNumber, setPhoneNumber] = useState('')
  const [avatarUrl, setAvatarUrl] = useState('')
  const [profileError, setProfileError] = useState<Record<string, string[]>>({})
  const [profileSuccess, setProfileSuccess] = useState(false)
  const [profileSaving, setProfileSaving] = useState(false)

  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [passwordError, setPasswordError] = useState<Record<string, string[]>>({})
  const [passwordSuccess, setPasswordSuccess] = useState(false)
  const [passwordSaving, setPasswordSaving] = useState(false)

  useEffect(() => {
    api.get<Profile>('/profile')
      .then(p => {
        setProfile(p)
        setDisplayName(p.displayName)
        setPhoneNumber(p.phoneNumber ?? '')
        setAvatarUrl(p.avatarUrl ?? '')
      })
      .finally(() => setLoading(false))
  }, [])

  async function handleProfileSave(e: FormEvent) {
    e.preventDefault()
    setProfileError({})
    setProfileSuccess(false)
    setProfileSaving(true)
    try {
      const updated = await api.patch<Profile>('/profile', {
        displayName,
        phoneNumber: phoneNumber || null,
        avatarUrl: avatarUrl || null,
      })
      setProfile(updated)
      setProfileSuccess(true)
    } catch (err) {
      const apiErr = err as ApiError
      setProfileError(apiErr.errors ?? { _: [apiErr.title] })
    } finally {
      setProfileSaving(false)
    }
  }

  async function handlePasswordChange(e: FormEvent) {
    e.preventDefault()
    setPasswordError({})
    setPasswordSuccess(false)

    if (newPassword !== confirmPassword) {
      setPasswordError({ ConfirmPassword: ['Passwords do not match.'] })
      return
    }

    setPasswordSaving(true)
    try {
      await api.patch('/profile/password', { currentPassword, newPassword })
      setPasswordSuccess(true)
      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
    } catch (err) {
      const apiErr = err as ApiError
      setPasswordError(apiErr.errors ?? { _: [apiErr.title] })
    } finally {
      setPasswordSaving(false)
    }
  }

  if (loading) return <div style={{ maxWidth: 480, margin: '40px auto', padding: '0 16px' }}>Loading…</div>

  return (
    <div style={{ maxWidth: 480, margin: '40px auto', padding: '0 16px' }}>
      <Link to="/" style={{ fontSize: 14, color: '#666' }}>← Back to lists</Link>
      <h1 style={{ margin: '16px 0 8px' }}>Profile</h1>
      <p style={{ fontSize: 13, color: '#888', marginBottom: 32 }}>
        Member since {new Date(profile!.createdAt).toLocaleDateString()}
      </p>

      {/* Profile details */}
      <section style={{ background: '#fff', borderRadius: 8, padding: '20px 24px', marginBottom: 24 }}>
        <h2 style={{ fontSize: 16, marginBottom: 16 }}>Account details</h2>
        <p style={{ fontSize: 13, color: '#666', marginBottom: 16 }}>Email: {profile!.email}</p>
        <form onSubmit={handleProfileSave} style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
          <label style={{ fontSize: 13, display: 'flex', flexDirection: 'column', gap: 4 }}>
            Display name
            <input
              value={displayName}
              onChange={e => { setDisplayName(e.target.value); setProfileSuccess(false) }}
              required
            />
            {profileError.DisplayName && <span style={{ color: 'red', fontSize: 12 }}>{profileError.DisplayName[0]}</span>}
          </label>
          <label style={{ fontSize: 13, display: 'flex', flexDirection: 'column', gap: 4 }}>
            Phone number <span style={{ color: '#999' }}>(optional)</span>
            <input
              type="tel"
              value={phoneNumber}
              onChange={e => { setPhoneNumber(e.target.value); setProfileSuccess(false) }}
              placeholder="+1 555 000 0000"
            />
            {profileError.PhoneNumber && <span style={{ color: 'red', fontSize: 12 }}>{profileError.PhoneNumber[0]}</span>}
          </label>
          <label style={{ fontSize: 13, display: 'flex', flexDirection: 'column', gap: 4 }}>
            Avatar URL <span style={{ color: '#999' }}>(optional)</span>
            <input
              type="url"
              value={avatarUrl}
              onChange={e => { setAvatarUrl(e.target.value); setProfileSuccess(false) }}
              placeholder="https://…"
            />
            {profileError.AvatarUrl && <span style={{ color: 'red', fontSize: 12 }}>{profileError.AvatarUrl[0]}</span>}
          </label>
          {profileError._ && <p style={{ color: 'red', fontSize: 13 }}>{profileError._[0]}</p>}
          {profileSuccess && <p style={{ color: 'green', fontSize: 13 }}>Profile updated.</p>}
          <button type="submit" disabled={profileSaving} style={{ alignSelf: 'flex-start' }}>
            {profileSaving ? 'Saving…' : 'Save changes'}
          </button>
        </form>
      </section>

      {/* Password change */}
      <section style={{ background: '#fff', borderRadius: 8, padding: '20px 24px' }}>
        <h2 style={{ fontSize: 16, marginBottom: 16 }}>Change password</h2>
        <form onSubmit={handlePasswordChange} style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
          <label style={{ fontSize: 13, display: 'flex', flexDirection: 'column', gap: 4 }}>
            Current password
            <input
              type="password"
              value={currentPassword}
              onChange={e => { setCurrentPassword(e.target.value); setPasswordSuccess(false) }}
              required
            />
            {passwordError.CurrentPassword && <span style={{ color: 'red', fontSize: 12 }}>{passwordError.CurrentPassword[0]}</span>}
          </label>
          <label style={{ fontSize: 13, display: 'flex', flexDirection: 'column', gap: 4 }}>
            New password
            <input
              type="password"
              value={newPassword}
              onChange={e => { setNewPassword(e.target.value); setPasswordSuccess(false) }}
              required
            />
            {passwordError.NewPassword && <span style={{ color: 'red', fontSize: 12 }}>{passwordError.NewPassword[0]}</span>}
          </label>
          <label style={{ fontSize: 13, display: 'flex', flexDirection: 'column', gap: 4 }}>
            Confirm new password
            <input
              type="password"
              value={confirmPassword}
              onChange={e => { setConfirmPassword(e.target.value); setPasswordSuccess(false) }}
              required
            />
            {passwordError.ConfirmPassword && <span style={{ color: 'red', fontSize: 12 }}>{passwordError.ConfirmPassword[0]}</span>}
          </label>
          {passwordError._ && <p style={{ color: 'red', fontSize: 13 }}>{passwordError._[0]}</p>}
          {passwordSuccess && <p style={{ color: 'green', fontSize: 13 }}>Password changed.</p>}
          <button type="submit" disabled={passwordSaving} style={{ alignSelf: 'flex-start' }}>
            {passwordSaving ? 'Updating…' : 'Update password'}
          </button>
        </form>
      </section>
    </div>
  )
}
