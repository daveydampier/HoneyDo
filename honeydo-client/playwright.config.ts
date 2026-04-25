import { defineConfig, devices } from '@playwright/test'

/**
 * Playwright E2E configuration.
 *
 * Tests run against the Vite dev server. API calls are intercepted via
 * page.route() inside each test — no real backend required.
 *
 * Local:  playwright will reuse an already-running dev server.
 * CI:     playwright starts the dev server fresh (reuseExistingServer: false).
 */
export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  /* Fail fast on CI if a test is accidentally left with .only */
  forbidOnly: !!process.env.CI,
  /* One retry on CI to absorb flake; none locally for fast feedback */
  retries: process.env.CI ? 1 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: process.env.CI ? 'github' : 'html',
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
  },
  expect: {
    // Pages using React 19 use() + Suspense fetch during the initial render.
    // On slow CI machines the Suspense resolution + React re-render can take
    // a couple of seconds longer than the default 5 s window.
    timeout: 15_000,
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: {
    command: 'npm run dev',
    url: 'http://localhost:5173',
    reuseExistingServer: !process.env.CI,
    timeout: 30_000,
  },
})
