import { useState, useEffect, useRef, type FormEvent, type ChangeEvent } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import type { Profile, Tag, ApiError } from '../api/types'
import { TAG_COLORS, getTagTextColor } from '../utils/tags'
import {
  Container, Title, Text, TextInput, PasswordInput, Button,
  Paper, Stack, Group, Alert, Anchor, Avatar, ColorSwatch,
  Badge, ActionIcon,
} from '@mantine/core'
import { IconAlertCircle, IconCircleCheck, IconX } from '@tabler/icons-react'

const ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/gif']
const MAX_BYTES = 2 * 1024 * 1024 // 2 MB

export default function ProfilePage() {
  const [profile, setProfile] = useState<Profile | null>(null)
  const [loading, setLoading] = useState(true)

  // Profile form
  const [displayName, setDisplayName] = useState('')
  const [phoneNumber, setPhoneNumber] = useState('')
  const [avatarUrl, setAvatarUrl] = useState<string | null>(null)
  const [urlDraft, setUrlDraft] = useState('')
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

  // Tags
  const [myTags, setMyTags] = useState<Tag[]>([])
  const [tagName, setTagName] = useState('')
  const [tagColor, setTagColor] = useState(TAG_COLORS[0])
  const [tagCreating, setTagCreating] = useState(false)
  const [tagError, setTagError] = useState<string | null>(null)

  const isUploadedAvatar = avatarUrl?.startsWith('data:') ?? false

  useEffect(() => {
    api.get<Tag[]>('/tags').then(setMyTags).catch(() => {})
  }, [])

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

  async function handleCreateTag(e: FormEvent) {
    e.preventDefault()
    setTagError(null)
    setTagCreating(true)
    try {
      const created = await api.post<Tag>('/tags', { name: tagName.trim(), color: tagColor })
      setMyTags(prev => [...prev, created])
      setTagName('')
    } catch (err) {
      const apiErr = err as ApiError
      setTagError(apiErr.errors?.Name?.[0] ?? apiErr.errors?.Color?.[0] ?? apiErr.title)
    } finally {
      setTagCreating(false)
    }
  }

  async function handleDeleteTag(tagId: string, name: string) {
    if (!confirm(`Delete tag "${name}"? It will be removed from all tasks.`)) return
    try {
      await api.delete(`/tags/${tagId}`)
      setMyTags(prev => prev.filter(t => t.id !== tagId))
    } catch {
      setTagError('Failed to delete tag. Please try again.')
    }
  }

  async function handleProfileSave(e: FormEvent) {
    e.preventDefault()
    setProfileError({})
    setProfileSuccess(false)
    setProfileSaving(true)
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

  if (loading) return (
    <Container size={480} pt="xl">
      <Text c="dimmed">Loading…</Text>
    </Container>
  )

  return (
    <Container size={480} pt="xl">
      <Anchor component={Link} to="/" size="sm" c="dimmed">← Back to lists</Anchor>
      <Title order={1} mt="sm" mb={4}>Profile</Title>
      <Text size="xs" c="dimmed" mb="xl">
        Member since {new Date(profile!.createdAt).toLocaleDateString()}
      </Text>

      {/* Account details */}
      <Paper p="xl" radius="md" withBorder mb="lg">
        <Title order={3} mb="md">Account details</Title>
        <Text size="sm" c="dimmed" mb="md">Email: {profile!.email}</Text>

        {/* Avatar */}
        <Stack gap="xs" mb="md">
          <Text size="sm" fw={500}>
            Avatar <Text span c="dimmed" fw={400}>(optional)</Text>
          </Text>
          <Group gap="md" align="flex-start">
            <Avatar src={avatarUrl} size={72} radius="xl">👤</Avatar>
            <Stack gap="xs">
              <input
                ref={fileInputRef}
                type="file"
                accept={ALLOWED_TYPES.join(',')}
                style={{ display: 'none' }}
                onChange={handleAvatarFileChange}
              />
              <Button
                variant="outline"
                size="xs"
                loading={avatarUploading}
                onClick={() => fileInputRef.current?.click()}
              >
                {isUploadedAvatar ? 'Replace photo' : 'Upload photo'}
              </Button>
              {avatarUrl && (
                <Anchor size="xs" c="red" style={{ cursor: 'pointer' }} onClick={handleRemoveAvatar}>
                  Remove
                </Anchor>
              )}
            </Stack>
          </Group>
          <Text size="xs" c="dimmed">JPEG, PNG, WebP or GIF · max 2 MB</Text>
          {avatarError && (
            <Alert color="red" variant="light" icon={<IconAlertCircle size={14} />} py="xs">
              {avatarError}
            </Alert>
          )}
          {!isUploadedAvatar && (
            <TextInput
              label="Or enter a URL"
              type="url"
              value={urlDraft}
              onChange={e => { setUrlDraft(e.target.value); setProfileSuccess(false) }}
              placeholder="https://…"
              error={profileError.AvatarUrl?.[0]}
            />
          )}
        </Stack>

        <form onSubmit={handleProfileSave}>
          <Stack gap="sm">
            <TextInput
              label="Display name"
              value={displayName}
              onChange={e => { setDisplayName(e.target.value); setProfileSuccess(false) }}
              error={profileError.DisplayName?.[0]}
              required
            />
            <TextInput
              label="Phone number"
              description="Optional"
              type="tel"
              value={phoneNumber}
              onChange={e => { setPhoneNumber(e.target.value); setProfileSuccess(false) }}
              placeholder="+1 555 000 0000"
              error={profileError.PhoneNumber?.[0]}
            />
            {profileError._ && (
              <Alert color="red" variant="light" icon={<IconAlertCircle size={14} />}>
                {profileError._[0]}
              </Alert>
            )}
            {profileSuccess && (
              <Alert color="green" variant="light" icon={<IconCircleCheck size={14} />}>
                Profile updated.
              </Alert>
            )}
            <Button type="submit" loading={profileSaving} style={{ alignSelf: 'flex-start' }}>
              Save changes
            </Button>
          </Stack>
        </form>
      </Paper>

      {/* Tags */}
      <Paper p="xl" radius="md" withBorder mb="lg">
        <Title order={3} mb={4}>My Tags</Title>
        <Text size="xs" c="dimmed" mb="md">
          Tags are personal and can be applied to tasks across any of your lists.
        </Text>

        {myTags.length > 0 && (
          <Group gap="xs" mb="md">
            {myTags.map(tag => (
              <Badge
                key={tag.id}
                style={{ background: tag.color, color: getTagTextColor(tag.color) }}
                variant="filled"
                pr={4}
                rightSection={
                  <ActionIcon
                    size="xs"
                    variant="transparent"
                    color={getTagTextColor(tag.color)}
                    onClick={() => handleDeleteTag(tag.id, tag.name)}
                    title="Delete tag"
                  >
                    <IconX size={10} />
                  </ActionIcon>
                }
              >
                {tag.name}
              </Badge>
            ))}
          </Group>
        )}

        {myTags.length === 0 && (
          <Text size="sm" c="dimmed" mb="md">No tags yet. Create one below.</Text>
        )}

        <form onSubmit={handleCreateTag}>
          <Stack gap="sm">
            <TextInput
              value={tagName}
              onChange={e => { setTagName(e.target.value); setTagError(null) }}
              placeholder="Tag name…"
              maxLength={100}
              required
            />
            <Stack gap={4}>
              <Text size="xs" c="dimmed">Color</Text>
              <Group gap="xs">
                {TAG_COLORS.map(color => (
                  <ColorSwatch
                    key={color}
                    color={color}
                    size={26}
                    onClick={() => setTagColor(color)}
                    style={{
                      cursor: 'pointer',
                      outline: tagColor === color ? `3px solid ${color}` : 'none',
                      outlineOffset: 2,
                      boxShadow: tagColor === color ? '0 0 0 1px #fff inset' : 'none',
                    }}
                  />
                ))}
              </Group>
            </Stack>
            {tagError && (
              <Alert color="red" variant="light" icon={<IconAlertCircle size={14} />}>
                {tagError}
              </Alert>
            )}
            <Button
              type="submit"
              loading={tagCreating}
              disabled={!tagName.trim()}
              style={{ alignSelf: 'flex-start' }}
            >
              Create tag
            </Button>
          </Stack>
        </form>
      </Paper>

      {/* Change password */}
      <Paper p="xl" radius="md" withBorder mb="lg">
        <Title order={3} mb="md">Change password</Title>
        <form onSubmit={handlePasswordChange}>
          <Stack gap="sm">
            <PasswordInput
              label="Current password"
              value={currentPassword}
              onChange={e => { setCurrentPassword(e.target.value); setPasswordSuccess(false) }}
              error={passwordError.CurrentPassword?.[0]}
              required
            />
            <PasswordInput
              label="New password"
              value={newPassword}
              onChange={e => { setNewPassword(e.target.value); setPasswordSuccess(false) }}
              error={passwordError.NewPassword?.[0]}
              required
            />
            <PasswordInput
              label="Confirm new password"
              value={confirmPassword}
              onChange={e => { setConfirmPassword(e.target.value); setPasswordSuccess(false) }}
              error={passwordError.ConfirmPassword?.[0]}
              required
            />
            {passwordError._ && (
              <Alert color="red" variant="light" icon={<IconAlertCircle size={14} />}>
                {passwordError._[0]}
              </Alert>
            )}
            {passwordSuccess && (
              <Alert color="green" variant="light" icon={<IconCircleCheck size={14} />}>
                Password changed.
              </Alert>
            )}
            <Button type="submit" loading={passwordSaving} style={{ alignSelf: 'flex-start' }}>
              Update password
            </Button>
          </Stack>
        </form>
      </Paper>
    </Container>
  )
}
