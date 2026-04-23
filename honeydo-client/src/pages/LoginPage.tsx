import { useState, type FormEvent } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import type { ApiError } from '../api/types'
import {
  Container, Title, TextInput, PasswordInput,
  Button, Alert, Anchor, Stack, Text,
} from '@mantine/core'
import { IconAlertCircle } from '@tabler/icons-react'

export default function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      await login(email, password)
      navigate('/')
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.status === 404 ? 'Invalid email or password.' : apiErr.title)
    } finally {
      setLoading(false)
    }
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
          <Button type="submit" loading={loading} fullWidth mt="xs">
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
