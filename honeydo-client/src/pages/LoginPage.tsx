import { useState, useActionState, type FormEvent } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import type { ApiError } from '../api/types'
import {
  Container, Title, TextInput, PasswordInput,
  Button, Alert, Anchor, Stack, Text,
} from '@mantine/core'
import { IconAlertCircle } from '@tabler/icons-react'

type LoginPayload = { email: string; password: string }

export default function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  const [error, submitAction, isPending] = useActionState(
    async (_prev: string | null, { email: e, password: p }: LoginPayload) => {
      try {
        await login(e, p)
        navigate('/')
        return null
      } catch (err) {
        const apiErr = err as ApiError
        return apiErr.status === 404 ? 'Invalid email or password.' : apiErr.title
      }
    },
    null,
  )

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    submitAction({ email, password })
  }

  return (
    <Container size={400} pt={80}>
      <Title order={1} mb="lg">Sign in to HoneyDo</Title>
      <form onSubmit={handleSubmit}>
        <Stack gap="sm">
          <TextInput
            type="email"
            label="Email"
            placeholder="you@example.com"
            value={email}
            onChange={e => setEmail(e.target.value)}
            required
          />
          <PasswordInput
            label="Password"
            placeholder="Your password"
            value={password}
            onChange={e => setPassword(e.target.value)}
            required
          />
          {error && (
            <Alert color="tangerine" variant="light" icon={<IconAlertCircle size={16} />}>
              {error}
            </Alert>
          )}
          <Button type="submit" loading={isPending} fullWidth mt="xs">
            Sign in
          </Button>
        </Stack>
      </form>
      <Text size="sm" mt="md">
        Don't have an account?{' '}
        <Anchor component={Link} to="/register">Register</Anchor>
      </Text>
    </Container>
  )
}
