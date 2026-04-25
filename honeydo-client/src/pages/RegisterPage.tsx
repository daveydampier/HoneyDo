import { useState, useEffect, useActionState, startTransition, type FormEvent } from 'react'
import { useNavigate, useSearchParams, Link } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { api } from '../api/client'
import type { ApiError } from '../api/types'
import {
  Container, Title, TextInput, PasswordInput,
  Button, Alert, Anchor, Stack, Text,
} from '@mantine/core'
import { IconAlertCircle, IconConfetti } from '@tabler/icons-react'

type RegisterPayload = { email: string; password: string; displayName: string }

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

  // If the URL supplies an email (from the invite link), keep the field in sync
  // with it on first render but still let the user edit it.
  useEffect(() => {
    if (inviteEmail) setEmail(inviteEmail)
  }, [inviteEmail])

  const [errors, submitAction, isPending] = useActionState(
    async (_prev: Record<string, string[]>, { email: e, password: p, displayName: n }: RegisterPayload) => {
      try {
        await register(e, p, n)

        // If this registration came from an invite link, redeem the token now.
        if (inviteToken) {
          try {
            await api.post<void>('/invitations/accept', { token: inviteToken })
            navigate('/friends')
          } catch {
            navigate('/')
          }
        } else {
          navigate('/')
        }
        return {}
      } catch (err) {
        const apiErr = err as ApiError
        return apiErr.errors ?? { _: [apiErr.title] }
      }
    },
    {},
  )

  const fieldError = (field: string) => errors[field]?.[0] ?? errors[field.toLowerCase()]?.[0]

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    startTransition(() => submitAction({ email, password, displayName }))
  }

  return (
    <Container size={400} pt={80}>
      <Title order={1} mb="sm">Create your account</Title>

      {inviteToken && (
        <Alert color="aqua" variant="light" icon={<IconConfetti size={16} />} mb="lg">
          You've been invited to HoneyDo! Create your account below to connect.
        </Alert>
      )}

      <form onSubmit={handleSubmit}>
        <Stack gap="sm">
          <TextInput
            label="Display name"
            placeholder="Your name"
            value={displayName}
            onChange={e => setDisplayName(e.target.value)}
            error={fieldError('DisplayName')}
            required
          />
          <TextInput
            type="email"
            label="Email"
            placeholder="you@example.com"
            value={email}
            onChange={e => setEmail(e.target.value)}
            error={fieldError('Email')}
            required
          />
          <PasswordInput
            label="Password"
            placeholder="Min 8 characters"
            value={password}
            onChange={e => setPassword(e.target.value)}
            error={fieldError('Password')}
            required
          />
          {errors._ && (
            <Alert color="tangerine" variant="light" icon={<IconAlertCircle size={16} />}>
              {errors._[0]}
            </Alert>
          )}
          <Button type="submit" loading={isPending} fullWidth mt="xs">
            Create account
          </Button>
        </Stack>
      </form>

      <Text size="sm" mt="md">
        Already have an account?{' '}
        <Anchor component={Link} to="/login">Sign in</Anchor>
      </Text>
    </Container>
  )
}
