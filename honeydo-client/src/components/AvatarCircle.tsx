import { Avatar } from '@mantine/core'

interface Props {
  avatarUrl: string | null | undefined
  displayName: string
  size?: number
}

/**
 * Renders a circular avatar image, falling back to the user's initials
 * when no avatar URL is set.
 */
export default function AvatarCircle({ avatarUrl, displayName, size = 32 }: Props) {
  const initial = displayName.trim().charAt(0).toUpperCase()

  return (
    <Avatar
      src={avatarUrl ?? undefined}
      radius="xl"
      size={size}
      color="brand"
      style={{ flexShrink: 0 }}
    >
      {initial}
    </Avatar>
  )
}
