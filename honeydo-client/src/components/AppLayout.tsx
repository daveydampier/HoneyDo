import { useEffect, useState } from 'react'
import { Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { api } from '../api/client'
import type { Profile } from '../api/types'
import AvatarCircle from './AvatarCircle'
import {
  AppShell, Group, Text, Button, UnstyledButton, ActionIcon,
  useComputedColorScheme, useMantineColorScheme,
} from '@mantine/core'
import { IconSun, IconMoon } from '@tabler/icons-react'

export default function AppLayout() {
  const { displayName, logout } = useAuth()
  const navigate = useNavigate()
  const [avatarUrl, setAvatarUrl] = useState<string | null>(null)

  const { setColorScheme } = useMantineColorScheme()
  // useComputedColorScheme resolves 'auto' → the actual 'light' | 'dark' the OS chose
  const computed = useComputedColorScheme('light', { getInitialValueInEffect: true })

  useEffect(() => {
    api.get<Profile>('/profile')
      .then(p => setAvatarUrl(p.avatarUrl))
      .catch(() => {})
  }, [])

  return (
    <AppShell
      header={{ height: 56 }}
      padding={0}
    >
      <AppShell.Header>
        <Group h="100%" px="md" justify="space-between">
          {/* Branding */}
          <Text fw={700} size="lg" style={{ cursor: 'pointer' }} onClick={() => navigate('/')}>
            🍯 HoneyDo
          </Text>

          {/* Nav actions */}
          <Group gap="xs">
            <UnstyledButton
              onClick={() => navigate('/profile')}
              style={{ display: 'flex', alignItems: 'center', gap: 8 }}
            >
              <AvatarCircle avatarUrl={avatarUrl} displayName={displayName ?? ''} size={28} />
              <Text size="sm">{displayName}</Text>
            </UnstyledButton>
            <Button variant="subtle" color="gray" size="xs" onClick={() => navigate('/friends')}>
              Friends
            </Button>

            {/* Quick dark/light toggle — flips between the two explicit modes */}
            <ActionIcon
              variant="subtle"
              color="gray"
              size="sm"
              aria-label="Toggle color scheme"
              onClick={() => setColorScheme(computed === 'dark' ? 'light' : 'dark')}
            >
              {computed === 'dark' ? <IconSun size={16} /> : <IconMoon size={16} />}
            </ActionIcon>

            <Button variant="subtle" color="gray" size="xs" onClick={logout}>
              Sign out
            </Button>
          </Group>
        </Group>
      </AppShell.Header>

      <AppShell.Main>
        <Outlet />
      </AppShell.Main>
    </AppShell>
  )
}
