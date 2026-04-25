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
  reporter: process.env.CI ? [['github'], ['list']] : 'html',
  /* Overall per-test timeout — needs to be generous enough to cover
     the webServer cold start on CI plus the assertions themselves. */
  timeout: 60_000,
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on',
    screenshot: 'only-on-failure',
  },
  expect: {
    // Pages using React 19 use() + Suspense fetch during the initial render.
    // Suspense resolution + React re-render on CI can take several seconds;
    // give it a generous window so slow machines don't flake.
    timeout: 30_000,
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: {
    // CI: build a production bundle and serve with vite preview.
    //   - Eliminates Vite dev-server cold-start latency on first page load.
    //   - Production build strips React StrictMode's extra render/mount pass,
    //     so each page fires exactly one API request — matching our mocks.
    // Local: reuse the already-running dev server for fast iteration.
    command: process.env.CI
      ? 'npm run build && npm run preview -- --port 5173'
      : 'npm run dev',
    url: 'http://localhost:5173',
    reuseExistingServer: !process.env.CI,
    // Build + preview can take up to 2 minutes on a slow CI runner.
    timeout: process.env.CI ? 120_000 : 30_000,
  },
})
