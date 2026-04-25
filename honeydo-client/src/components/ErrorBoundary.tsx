import React, { Component, type ReactNode } from 'react'
import { Container, Text, Button, Stack } from '@mantine/core'

interface Props {
  children: ReactNode
}

interface State {
  hasError: boolean
}

/**
 * Catches errors thrown during render — including rejected promises surfaced
 * by React 19's `use()` hook — and shows a recoverable error state instead
 * of crashing the whole tree.
 */
export default class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false }

  static getDerivedStateFromError(): State {
    return { hasError: true }
  }

  componentDidCatch(error: Error, info: React.ErrorInfo) {
    // Log in all environments — React silences these in production otherwise.
    console.error('[ErrorBoundary] caught error:', error.message)
    console.error('[ErrorBoundary] component stack:', info.componentStack)
  }

  handleRetry = () => {
    this.setState({ hasError: false })
  }

  render() {
    if (this.state.hasError) {
      return (
        <Container size="sm" pt={80}>
          <Stack align="center" gap="md">
            <Text size="lg" fw={600}>Something went wrong</Text>
            <Text size="sm" c="dimmed">
              We couldn't load this page. Check your connection and try again.
            </Text>
            <Button variant="outline" onClick={this.handleRetry}>
              Try again
            </Button>
          </Stack>
        </Container>
      )
    }

    return this.props.children
  }
}
