import { useState, useEffect, useRef, type FormEvent, type ChangeEvent } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import type { Profile, ApiError } from '../api/types'

const ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/gif']
const MAX_BYTES = 2 * 1024 * 1024 // 2 MB

export default function ProfilePage() {
  const [profile, setProfile] = useState<Profile | null>(null)
  const [loading, setLoading] = useState(true)

  // Profile form
  const [displayName, setDisplayName] = useState('')
  const [phoneNumber, setPhoneNumber] = useState('')
  const [avatarUrl, setAvatarUrl] = useState<string | null>(null)
  const [urlDraft, setUrlDraft] = useState('')           // URL text input (manual entry)
  const [profileError, setProfileError] = useState<Record<string, string[]>>({})
  const [profileSuccess, setProfileSuccess] = useState(false)
  const [profileSaving, setProfileSaving] = useState(false)

  // Avatar upload
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [avatarUploading, setAvatarUploading] = useState(false)
  const [avatarError, setAvatarError] = useState<string | null>(null)

  // Password form
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [passwordError, setPasswordError] = useState<Record<string, string[]>>({})
  const [passwordSuccess, setPasswordSuccess] = useState(false)
  const [passwordSaving, setPasswordSaving] = useState(false)

  // Whether the current avatar came from a file upload (data URL) vs a manual URL
  const isUploadedAvatar = avatarUrl?.startsWith('data:') ?? false

  useEffect(() => {
    api.get<Profile>('/profile')
      .then(p => {
        setProfile(p)
        setDisplayName(p.displayName)
        setPhoneNumber(p.phoneNumber ?? '')
        setAvatarUrl(p.avatarUrl ?? null)
        setUrlDraft(p.avatarUrl?.startsWith('data:') ? '' : (p.avatarUrl ?? ''))
      })
      .finally(() => setLoading(false))
  }, [])

  async function handleAvatarFileChange(e: ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!fileInputRef.current) fileInputRef.current = e.target
    // Reset so the same file can be re-selected
    e.target.value = ''
    if (!file) return

    setAvatarError(null)

    if (!ALLOWED_TYPES.includes(file.type)) {
      setAvatarError('Only JPEG, PNG, WebP, and GIF images are allowed.')
      return
    }
    if (file.size > MAX_BYTES) {
      setAvatarError('Image must be 2 MB or smaller.')
      return
    }

    setAvatarUploading(true)
    try {
      const formData = new FormData()
      formData.append('file', file)
      const updated = await api.upload<Profile>('/profile/avatar', formData)
      setProfile(updated)
      setAvatarUrl(updated.avatarUrl)
      setUrlDraft('')
      setProfileSuccess(false)
    } catch {
      setAvatarError('Upload failed. Please try again.')
    } finally {
      setAvatarUploading(false)
    }
  }

  async function handleRemoveAvatar() {
    if (!confirm('Remove your avatar?')) return
    setAvatarError(null)
    try {
      const updated = await api.patch<Profile>('/profile', {
        displayName,
        phoneNumber: phoneNumber || null,
        avatarUrl: null,
      })
      setProfile(updated)
      setAvatarUrl(null)
      setUrlDraft('')
    } catch {
      setAvatarError('Failed to remove avatar. Please try again.')
    }
  }

  async function handleProfileSave(e: FormEvent) {
    e.preventDefault()
    setProfileError({})
    setProfileSuccess(false)
    setProfileSaving(true)

    // Determine which avatarUrl to persist:
    // - If the user typed a URL, use that
    // - If the current avatar is an uploaded image (data URL), preserve it
    // - Otherwise null (no avatar)
    const resolvedAvatarUrl = urlDraft.trim() || (isUploadedAvatar ? avatarUrl : null)

    try {
      const updated = await api.patch<Profile>('/profile', {
        displayName,
        phoneNumber: phoneNumber || null,
        avatarUrl: resolvedAvatarUrl,
      })
      setProfile(updated)
      setAvatarUrl(updated.avatarUrl)
      if (!updated.avatarUrl?.startsWith('data:')) setUrlDraft(updated.avatarUrl ?? '')
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

        {/* ── Avatar section ── */}
        <div style={{ marginBottom: 20 }}>
          <p style={{ fontSize: 13, fontWeight: 500, marginBottom: 10 }}>
            Avatar <span style={{ color: '#999', fontWeight: 400 }}>(optional)</span>
          </p>

          {/* Preview */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 16, marginBottom: 10 }}>
            <div style={{
              width: 72,
              height: 72,
              borderRadius: '50%',
              background: '#e8e8e8',
              overflow: 'hidden',
              flexShrink: 0,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontSize: 28,
              color: '#bbb',
              border: '1px solid #ddd',
            }}>
              {avatarUrl
                ? <img src={avatarUrl} alt="Avatar" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                : '👤'
              }
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
              {/* Hidden file input */}
              <input
                ref={fileInputRef}
                type="file"
                accept={ALLOWED_TYPES.join(',')}
                style={{ display: 'none' }}
                onChange={handleAvatarFileChange}
              />
              <button
                type="button"
                onClick={() => fileInputRef.current?.click()}
                disabled={avatarUploading}
                style={{
                  fontSize: 13,
                  padding: '6px 14px',
                  borderRadius: 6,
                  border: '1px solid #ccc',
                  background: 'none',
                  cursor: avatarUploading ? 'not-allowed' : 'pointer',
                  color: '#333',
                  opacity: avatarUploading ? 0.6 : 1,
                }}
              >
                {avatarUploading ? 'Uploading…' : isUploadedAvatar ? 'Replace photo' : 'Upload photo'}
              </button>
              {avatarUrl && (
                <button
                  type="button"
                  onClick={handleRemoveAvatar}
                  style={{ fontSize: 12, background: 'none', border: 'none', color: '#c00', cursor: 'pointer', textAlign: 'left', padding: 0 }}
                >
                  Remove
                </button>
              )}
            </div>
          </div>

          <p style={{ fontSize: 11, color: '#aaa', margin: '0 0 4px' }}>
            JPEG, PNG, WebP or GIF · max 2 MB
          </p>

          {avatarError && <p style={{ color: 'red', fontSize: 12, margin: '4px 0 0' }}>{avatarError}</p>}

          {/* Manual URL input — only shown when avatar is not an uploaded image */}
          {!isUploadedAvatar && (
            <label style={{ fontSize: 13, display: 'flex', flexDirection: 'column', gap: 4, marginTop: 12 }}>
              <span style={{ color: '#666' }}>Or enter a URL</span>
              <input
                type="url"
                value={urlDraft}
                onChange={e => { setUrlDraft(e.target.value); setProfileSuccess(false) }}
                placeholder="https://…"
              />
              {profileError.AvatarUrl && <span style={{ color: 'red', fontSize: 12 }}>{profileError.AvatarUrl[0]}</span>}
            </label>
          )}
        </div>

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
