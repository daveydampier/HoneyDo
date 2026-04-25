# HoneyDo

A collaborative to-do list application. Users create lists, add tasks with notes and due dates, invite collaborators, manage a friends graph, and close lists once all tasks are resolved.

- **Backend:** ASP.NET Core Web API (.NET 10), EF Core + SQLite, MediatR vertical slices, FluentValidation, JWT bearer auth, BCrypt password hashing
- **Frontend:** React + TypeScript (Vite), Mantine v9 UI, OpenAPI-generated TypeScript types
- **Tests:** xUnit + WebApplicationFactory (backend), Vitest + Testing Library + MSW + jest-axe (frontend), Playwright Chromium (E2E)
- **CI:** GitHub Actions (parallel backend / frontend / E2E jobs), Dependabot weekly

For the full technical reference — architecture, domain model, complete CRUD reference, validation rules, tradeoffs, and future work — see [`Documentation/DOCUMENTATION.md`](Documentation/DOCUMENTATION.md).

---

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | 10.0+ | `dotnet --version` |
| Node.js | 20+ | `node --version` |
| npm | 10+ | Ships with Node |
| Docker (optional) | latest | Only if running via `docker compose` |

---

## First-Time Setup

```bash
# 1. Clone the repository
git clone <repo-url> HoneyDo
cd HoneyDo

# 2. Wire up the pre-commit hook (runs `dotnet test` before every commit)
git config core.hooksPath .githooks

# 3. Restore backend dependencies
dotnet restore HoneyDo/HoneyDo.csproj

# 4. Install frontend dependencies
#    --legacy-peer-deps: openapi-typescript declares peer "typescript@^5.x" but
#    the project uses TypeScript 6. The package works fine; the peer dep
#    declaration just hasn't been updated upstream yet.
cd honeydo-client
npm ci --legacy-peer-deps
cd ..
```

### JWT signing key

`appsettings.json` deliberately ships with no `Jwt:Key` value — the app fails fast on startup if the key is missing or shorter than 32 bytes, so production cannot accidentally run with a default secret.

- **Development:** `appsettings.Development.json` contains a clearly-labelled dev-only key. `dotnet run` will use it automatically when `ASPNETCORE_ENVIRONMENT=Development` (the default for local runs).
- **Production:** Supply via environment variable (`Jwt__Key=<secret>`) or .NET User Secrets. Minimum 32 bytes for HMAC-SHA256.

### SMTP (optional)

Leave `Email:Smtp:Host` empty for development — the full email body (including invite links) is logged to the API console instead of sent. No SMTP server required to exercise the friend-invite flow locally.

---

## Running Locally

### Native (recommended for development)

```bash
# Terminal 1 — API (auto-applies pending migrations on startup)
cd HoneyDo
dotnet run

# Terminal 2 — Frontend
cd honeydo-client
npm run dev
```

- API: http://localhost:5277
- Frontend (Vite dev server): http://localhost:5173

The Vite dev server proxies `/api/*` to `http://localhost:5277`, so the frontend uses relative `/api/...` paths everywhere.

### Docker Compose

```bash
docker compose up --build
```

Mirrors the production deploy: same images, same env layout, single command for the full stack. Use this when you want to validate the deployable artifact rather than iterate on code.

---

## Running Tests

```bash
# Backend unit + integration tests (xUnit + WebApplicationFactory + EF InMemory)
dotnet test HoneyDo.Tests/HoneyDo.Tests.csproj

# Frontend unit + integration tests (Vitest)
cd honeydo-client && npm test

# Frontend E2E (Playwright — auto-starts the Vite dev server if not running)
cd honeydo-client && npm run test:e2e
```

The pre-commit hook (wired up via `git config core.hooksPath .githooks` above) runs `dotnet test` before every commit. If a test fails, the commit is aborted.

---

## Migrations

Migrations are applied automatically by `Database.Migrate()` in `Program.cs` on every API startup. No manual `dotnet ef database update` step is needed in normal development.

To generate a new migration after changing the domain model:

```bash
# Stop the running API first (it locks honeydo.db), then:
dotnet ef migrations add <MigrationName> --project HoneyDo/HoneyDo.csproj

# Restart the API — it will apply the migration automatically
dotnet run --project HoneyDo/HoneyDo.csproj
```

---

## Regenerating TypeScript API Types

Frontend types in `honeydo-client/src/api/generated.ts` are auto-generated from the backend's OpenAPI spec. Regenerate whenever backend DTOs or endpoints change:

```bash
# Step 1: Regenerate the OpenAPI spec from the backend.
#         Requires Jwt:Key to be set (the build briefly starts the app).
$env:Jwt__Key = "your-dev-key-here"
dotnet build HoneyDo/HoneyDo.csproj /p:GenerateApiSpec=true
# → writes honeydo-client/HoneyDo.json

# Step 2: Regenerate the TS types from the spec
cd honeydo-client && npm run generate
# → writes src/api/generated.ts
```

`src/api/types.ts` re-exports from `generated.ts` under stable names. All page imports go through `types.ts`, so only that file needs adjusting if a generated shape changes.

---

## Project Layout

```
HoneyDo/
├── HoneyDo/                    # ASP.NET Core API (vertical slices under Features/)
│   ├── Features/               # MediatR commands/queries/handlers per feature
│   ├── Domain/                 # EF entities, enums, DbContext, migrations
│   ├── Common/                 # Cross-cutting: middleware, behaviors, exceptions
│   └── Program.cs              # DI, middleware pipeline, JWT setup, auto-migrate
├── HoneyDo.Tests/              # xUnit + WebApplicationFactory backend tests
├── honeydo-client/             # React + TypeScript + Vite frontend
│   ├── src/pages/              # Top-level routed pages
│   ├── src/api/                # Generated types + typed client
│   ├── src/test/               # MSW handlers, fixtures, render helpers
│   └── e2e/                    # Playwright specs
├── Documentation/              # Full technical reference
└── docker-compose.yml          # Multi-service local/prod orchestration
```

---

## Explanation Notes

### Architecture in one paragraph

Each feature lives in a single file under `HoneyDo/Features/{Domain}/` containing its command/query record, FluentValidation validator, and MediatR handler. Controllers are thin dispatch points that read the caller's identity from the JWT and forward to MediatR. Cross-cutting validation runs as an `IPipelineBehavior` before any handler executes, and a global exception middleware translates `NotFoundException` / `ForbiddenException` / `ValidationException` to the appropriate HTTP status codes — handlers never contain try/catch. Soft deletes are enforced via EF Core global query filters so deleted rows are invisible by default. The frontend is fully behind auth (no SSR needed), uses TypeScript types generated from the backend's OpenAPI spec to enforce contract sync at compile time, and renders with Mantine.

### Key Assumptions

- **Single deploy, low concurrency.** SQLite, in-process auto-migrate, and a stateless JWT model with no revocation list are all appropriate for one app instance and household-scale collaboration. They are explicitly the wrong defaults for HA / multi-instance production. See "Known Tradeoffs" in DOCUMENTATION.md for migration paths.
- **Trusted clients.** No client-side rate limiting, no anti-CSRF tokens (because the API uses bearer tokens, not cookies), and no request signing. The auth model assumes the JWT in `localStorage` is the security boundary.
- **Calendar dates, not timestamps.** Due dates are calendar dates with no time or timezone component, stored as `YYYY-MM-DD` text. This matches the HTML date input directly and avoids timezone bugs at the cost of richer SQL date arithmetic.
- **Closed lists are immutable.** Closing a list is a one-way action with explicit preconditions (every task must be Partial / Complete / Abandoned). Re-opening would be a new feature, not a config change.
- **Friendship is required for ad-hoc collaboration.** Adding a friend to a list uses the friend graph rather than letting owners type any email; this prevents the "cold-add" pattern where you discover you're a contributor on a stranger's list.

### Scalability — Known Limits and First Moves

The app is sized for single-deploy, single-database, household-scale collaboration. The first scaling change in each direction:

- **Database:** SQLite serialises writes via a file lock — first to break under load. Switch to PostgreSQL by changing `UseSqlite` → `UseNpgsql` and the connection string. EF migrations are portable; no application code changes.
- **App instances:** Auto-migrate on startup is fine for one instance, racy for many. Move migrations to a one-shot deploy step and have the app crash on startup if the schema doesn't match.
- **Read-heavy endpoints:** `GetLists` joins memberships, profiles, item counts, and tags. Fine on SQLite at this scale, expensive at thousands of lists per user. Add a denormalized `list_summary` table refreshed by domain events; the query becomes a single-table read.
- **Auth:** Stateless JWTs with no revocation are fine until you need to force-logout a compromised account. Move to short-lived access tokens + refresh tokens + a server-side blocklist (Redis).
- **Real-time:** Members don't see each other's edits until refresh. Add SignalR (or WebSocket fallback) broadcasting mutations to subscribed clients.
- **CI:** One pipeline runs both halves on every change. Add path filters so backend-only changes skip the frontend job, frontend-only skips backend.

### Future Work

Tracked in the order I'd implement, with the trigger that would move each from "later" to "now":

| Item | Trigger |
|---|---|
| Replace `useEffect` + `useState` data fetching with TanStack Query | Adding the third page that hits `/lists`, or wanting optimistic updates that survive navigation |
| Add `[Timestamp] byte[] RowVersion` to mutable entities + 409 handling | First reproduced lost-update report from a user |
| Inject `TimeProvider` into handlers, switch tests to `FakeTimeProvider` | First test that needs to assert on a specific timestamp |
| Add ASP.NET Core rate limiter to `/api/auth/*` | Before any internet-facing deploy |
| Move avatar storage from base64-in-DB to S3 + signed URLs | Avatars start appearing in list views (current usage is profile-page only) |
| Real-time push (SignalR) for list/item mutations | First user complaint about not seeing a co-member's edit |
| Cross-browser Playwright (Firefox + WebKit) | First feature that uses a browser-sensitive API |
| Split monorepo CI into two pipelines with path filters | CI run time exceeds 5 minutes consistently |
| List re-open / archive distinction | First user request — currently closing is one-way by design |

A complete list of deliberate tradeoffs (with the reasoning for each) lives in [`Documentation/DOCUMENTATION.md` § Known Tradeoffs](Documentation/DOCUMENTATION.md#known-tradeoffs).
