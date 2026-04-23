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
    <div style={{
      width: size,
      height: size,
      borderRadius: '50%',
      background: '#d0e4ff',
      overflow: 'hidden',
      flexShrink: 0,
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      fontSize: size * 0.4,
      fontWeight: 600,
      color: '#0a84ff',
      border: '1px solid #c0d8f8',
      userSelect: 'none',
    }}>
      {avatarUrl
        ? <img src={avatarUrl} alt={displayName} style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
        : initial
      }
    </div>
  )
}
