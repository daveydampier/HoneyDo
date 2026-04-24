import '@testing-library/jest-dom'
import { vi } from 'vitest'
import { server } from './server'

// jsdom doesn't implement window.matchMedia. Mantine's MantineProvider calls it
// on mount to detect the OS color scheme. Stub it out so all tests can render
// Mantine components without throwing.
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation((query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
})

beforeAll(() => server.listen({ onUnhandledRequest: 'warn' }))
afterEach(() => server.resetHandlers())
afterAll(() => server.close())
