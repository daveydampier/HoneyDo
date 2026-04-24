import { setupServer } from 'msw/node'
import { handlers } from './handlers'

/**
 * MSW Node server. Started/stopped via setup.ts so every test file
 * gets interception without any per-file boilerplate.
 */
export const server = setupServer(...handlers)
