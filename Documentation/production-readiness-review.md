# HoneyDo — Production Readiness Review

*Audit date: 2026-04-26. Fresh pass over the source — not a restatement of the known tradeoffs.*

---

## 1. Executive Summary

HoneyDo is in servicable shape for a single-instance, low-traffic, trusted-network deploy. The architecture is simple, the test pyramid is real (221 tests, three layers), and the documented tradeoffs are the *right* things to know about. What blocks a public internet deploy are: no rate limiting on the auth endpoints, no HTTPS end-to-end in the Docker stack, no health check endpoints, and avatar blobs stored directly in the database (which will quietly degrade every member-panel query as usage grows). What blocks scale is still SQLite, but the code already treats `MigrateOnStartup` as an opt-in flag, which is the right first step before the HA migration. There are no severe security vulnerabilities in the application logic, but the deployment posture has three gaps that need closing before the URL goes in anyone's browser.

---

## 2. Production Readiness Gaps

### Security

**S1 — No rate limiting on auth endpoints**
`/api/auth/login` and `/api/auth/register` are open to unlimited requests. No rate limiting is configured anywhere in `Program.cs`. An attacker can brute-force passwords or flood registration with no friction.
- **Fix:** `builder.Services.AddRateLimiter(o => o.AddFixedWindowLimiter("auth", opts => { opts.PermitLimit = 10; opts.Window = TimeSpan.FromMinutes(1); opts.QueueLimit = 0; }))` in `Program.cs`; apply `[EnableRateLimiting("auth")]` on `AuthController`. This is in-process and requires no external dependency.
- **Effort:** S
- **Priority:** P0 — must have before any internet-facing deploy

**S2 — Missing security headers: CSP and HSTS**
`SecurityHeadersMiddleware` (`Common/Middleware/SecurityHeadersMiddleware.cs`) adds only two headers: `X-Frame-Options: DENY` and `X-Content-Type-Options: nosniff`. Missing:
- `Content-Security-Policy` — the highest-value XSS mitigation on the frontend; without it, any injected `<script>` runs freely
- `Strict-Transport-Security` — tells browsers to enforce HTTPS for future visits; useless over HTTP, but necessary the moment TLS is added
- `Referrer-Policy: strict-origin-when-cross-origin`
- **Fix:** Add all three to `SecurityHeadersMiddleware`. CSP for an API can be `default-src 'none'` since it returns JSON, not HTML. The SPA's CSP needs to go in nginx.conf (see S3).
- **Effort:** S
- **Priority:** P1

**S3 — nginx.conf adds no security headers for SPA static assets**
`SecurityHeadersMiddleware` covers API responses. `honeydo-client/Web.config` covers the IIS hosting path. But `honeydo-client/nginx.conf` — the actual production path via `docker compose up` — sets zero response headers beyond what nginx defaults to. The React SPA's HTML and JS will be served without `X-Frame-Options`, `X-Content-Type-Options`, or CSP.
- **Fix:** Add an `add_header` block in the `/` location in `nginx.conf`. For the SPA, a strict CSP like `default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; connect-src 'self'` is achievable — the only wrinkle is that Mantine uses inline styles, hence `style-src 'unsafe-inline'`.
- **Effort:** S
- **Priority:** P1

**S4 — No HTTPS in docker-compose; invite emails contain `http://localhost`**
`docker-compose.yml` sets `AppUrl: "http://localhost"`. `AllowedOrigins: "http://localhost"`. Port 80 only. Invitation emails will contain plaintext links. There is no TLS termination anywhere in the stack.
- **Fix:** TLS belongs in front of nginx, not inside it for this stack. Add a Caddy or Traefik sidecar that handles ACME cert provisioning and forwards HTTPS to nginx:80. Update `AppUrl` and `AllowedOrigins` to the real HTTPS domain. This is the deploy change, not the code change.
- **Effort:** M
- **Priority:** P0 for any public deploy

**S5 — Avatar MIME type validated by request header, not by actual bytes**
`UploadAvatarCommand.cs` checks `AllowedTypes.Contains(request.ContentType)` where `ContentType` comes directly from the HTTP `Content-Type` header. A client can upload an SVG file with embedded scripts by sending `Content-Type: image/jpeg`. The file is stored as a data URI in the database and served back into `<img>` tags; if the browser sniffs the content, the SVG scripts run. `X-Content-Type-Options: nosniff` mitigates this, but defense-in-depth calls for validating the actual magic bytes (`FF D8 FF` for JPEG, `89 50 4E 47` for PNG, `52 49 46 46` for WebP) before storage.
- **Fix:** Read the first 12 bytes of `request.Data` and compare to known magic signatures. Reject if they don't match the declared content type.
- **Effort:** S
- **Priority:** P1

---

### Observability

**O1 — No health check endpoint**
`docker-compose.yml` has `restart: unless-stopped` but no `healthcheck` directive. Docker has no way to distinguish a healthy app from a crashed-but-running container. `depends_on: api` in the client service starts nginx before the API is accepting requests — the first few proxied requests will 502.
- **Fix:** Add `app.MapHealthChecks("/health")` (requires `builder.Services.AddHealthChecks()`) in `Program.cs`. Add a Docker `healthcheck` in the compose file: `test: ["CMD", "curl", "-f", "http://localhost:8080/health"]`. Add `condition: service_healthy` to the client's `depends_on`.
- **Effort:** S
- **Priority:** P0

**O2 — Console-only logging; no log aggregation**
Logs go to stdout, which Docker captures — that part is fine. But there is no centralized aggregation, no retention, no alerting. A production exception at 3 AM is invisible until someone SSH-es into the host and runs `docker logs`.
- **Fix:** The cheapest add: configure Serilog with a file sink + rolling retention as a fallback, then route to a cloud sink (Seq, Loki, App Insights) when one is available. No code architecture change needed — just add a Serilog configuration block.
- **Effort:** M
- **Priority:** P1

**O3 — No metrics or distributed tracing**
No Prometheus endpoint, no OpenTelemetry. The `traceId` on error responses (`ExceptionMiddleware.cs:48`) is a good start — correlating client-reported errors with server logs is possible. But there is no SLO dashboard, no latency histogram, no request count.
- **Fix:** `builder.Services.AddOpenTelemetry()` with the ASP.NET Core instrumentation + EF Core instrumentation exports traces to Jaeger or OTLP. Low-effort, high-leverage when debugging.
- **Effort:** M
- **Priority:** P2

---

### Reliability

**R1 — No SMTP timeout or fallback**
`SmtpEmailService` creates a new SMTP connection per call. If the mail server is unreachable, the call blocks for the TCP connect timeout (typically 20–30 seconds). Friend invite requests will appear hung to the user and then return a 500. There is no circuit breaker and no way to degrade gracefully (e.g., queue the send and return success immediately).
- **Fix:** Set `smtpClient.Timeout` to 5000ms. Wrap the `SendAsync` call in a `try/catch` that logs the failure and returns — invitation creation already succeeded before the email send, so the token is valid; the failure is recoverable. The invite token can still be delivered out-of-band.
- **Effort:** S
- **Priority:** P1

**R2 — No concurrency protection on mutable entities**
Update handlers (`UpdateItemCommand`, `UpdateListCommand`) do a fetch-then-save with no `[Timestamp]` / `rowversion` concurrency token. Two members editing the same task simultaneously will silently last-write-wins. This is the documented tradeoff; the right fix (`HasRowVersion()` + `DbUpdateConcurrencyException` catch → 409) is straightforward and should be wired up before the first real multi-user deploy.
- **Effort:** M
- **Priority:** P1

---

### Operations

**O-PS1 — No pre-deploy migration job or runbook**
`Program.cs` correctly gates auto-migration behind `MigrateOnStartup` (only set in `appsettings.Development.json`). The docs say "run as a pre-deploy step." But there is no script, `Makefile` target, or CI job that actually does this. A first deploy against a blank database will leave the schema empty because `MigrateOnStartup` defaults to `false`.
- **Fix:** Add a one-shot Docker service to `docker-compose.yml`:
  ```yaml
  migrate:
    build: { context: ., dockerfile: HoneyDo/Dockerfile }
    command: ["dotnet", "HoneyDo.dll", "--migrate-only"]
    environment: *api-env
    depends_on: [api]
  ```
  Or simpler: add a `make migrate` target that runs `dotnet ef database update` against the production DB before `docker compose up`. Either way, write it down — don't leave this as institutional knowledge.
- **Effort:** S
- **Priority:** P0 (first deploy will fail without this)

**O-PS2 — No `.env.example`; deployers must guess required secrets**
`docker-compose.yml` requires a `${JWT_KEY}` variable from a `.env` file. There is no `.env.example` or `README` section listing what that file must contain. A deployer who doesn't read the compose file carefully will start the API with no JWT key and get a crash.
- **Fix:** Add `.env.example` to the repo root with `JWT_KEY=<generate-with-openssl-rand-base64-32>` and any other prod-required vars.
- **Effort:** XS
- **Priority:** P1

**O-PS3 — No backup strategy for the SQLite volume**
`db-data` is a named Docker volume. It survives container rebuilds but is tied to the host. If the host is reprovisioned, the database is gone. There is no mention of backup in the docs.
- **Fix:** A cron job that runs `sqlite3 /app/data/honeydo.db ".backup /backup/honeydo-$(date +%Y%m%d).db"` into a separate volume (or S3). SQLite's `.backup` command is safe for hot backups. Retention: 7 daily, 4 weekly.
- **Effort:** S
- **Priority:** P1

---

### Data

**D1 — Avatar blobs loaded on every member panel open**
`GetMembersQuery.cs:24` selects `m.Profile.AvatarUrl` for every list member. An avatar uploaded via `UploadAvatarCommand` is stored as a `data:{type};base64,...` string up to ~2.7MB. Opening a list's member panel loads all members' full avatars from the database in a single query. On a list with 5 members who all uploaded 2MB photos, that's 13.5MB transferred from SQLite to the API process on every panel open.
- **Fix (short term):** Add a dedicated `AvatarThumbnailUrl` column for a downsized preview and serve that in member lists. Or — better — move avatar storage to S3/Cloudflare R2 and store only the URL in the database column (which is the right answer at any non-trivial scale, per the existing comment in `ProfileConfiguration.cs`).
- **Effort:** M (short term thumbnail) / L (object storage migration)
- **Priority:** P1 — this is the most likely first visible performance problem

**D2 — `UpdateProfileCommand` allows unlimited-length `AvatarUrl`**
`UpdateProfileCommand.cs` has no validation on `AvatarUrl`. `PATCH /api/profile` accepts an arbitrary-length string in that field. A client can write a 50MB base64 blob without going through the 2MB-enforced upload endpoint. The FluentValidation note in `UpdateProfileCommandValidator` says "no length limit," but the intent was to allow `https://...` URLs and data URIs — it should at minimum reject strings longer than ~3.8MB (the base64 encoding of a 2MB file).
- **Fix:** `RuleFor(x => x.AvatarUrl).MaximumLength(3_800_000).When(x => x.AvatarUrl is not null);` — or better, reject data URIs entirely here and require the upload endpoint.
- **Effort:** XS
- **Priority:** P1

---

## 3. Scalability Assessment

### What scales vertically without code changes

The stateless JWT auth model, the MediatR handler-per-request model, and the EF Core data layer all scale vertically by adding RAM and CPU to the single host. SQLite with WAL mode supports multiple concurrent readers; the EF projections in `GetItemsQuery` and `GetListsQuery` are efficient enough to sustain hundreds of reads/second on a modern SSD before SQLite's write lock becomes the bottleneck. The nginx reverse proxy is already in front of the API — vertical scale means a bigger VM behind the same nginx.

### What breaks first under horizontal scaling

**SQLite file lock.** SQLite uses a file-level write lock. With N>1 API instances accessing `/app/data/honeydo.db` from different containers, writes will conflict and one instance will get `SQLITE_BUSY`. This is not WAL-fixable when the DB is mounted via a network volume — the lock semantics don't hold over NFS/EFS.

**In-process migration guard.** `MigrateOnStartup` is already behind a flag and defaults to false. The risk here is the deploy sequence: if two API instances start simultaneously against a new migration, both will pass the `IsRelational()` check and try to apply it. EF Core's `__EFMigrationsHistory` table provides a soft guard, but concurrent DDL on SQLite is unsafe. On Postgres this is much safer — advisory locks are used. This is already the right design; the remaining gap is the missing pre-deploy job (see O-PS1).

**No shared revocation store.** Stateless JWTs with a 24-hour window mean instance A issuing a logout cannot tell instance B the token is invalidated. This matters only when a compromised token needs to be killed immediately.

**No shared session/cache.** There is no server-side session or application cache today, so there is nothing that breaks — there is just nothing to gain from a shared cache either.

### Recommended first production topology

**Single instance, single VPS, Docker Compose.** This is the right topology for the first real deploy and for demonstrating the project. It sidesteps every horizontal scaling issue without requiring infrastructure complexity. The app will comfortably serve hundreds of users and thousands of requests per day in this configuration before SQLite becomes a bottleneck.

**Migration path to N>1 instances:**

1. Switch SQLite → PostgreSQL (`UseSqlite` → `UseNpgsql`, update `ConnectionStrings`; no application code changes)
2. Move migrations to a pre-deploy step (Flyway or `dotnet ef database update` in CI/CD before `docker compose up`)
3. Add a Redis container; implement JWT blocklist using `IDistributedCache` for logout revocation
4. Add TanStack Query for optimistic updates and client-side cache (reduces read volume significantly)

The code architecture (vertical slices, EF projections, no singleton state) is already horizontal-scale ready. The database is the only thing that isn't.

---

## 4. Recommended Roadmap

In dependency order — each item unblocks or de-risks the next.

| # | Item | Priority | Effort | Blocks |
|---|---|---|---|---|
| 1 | Add rate limiting to `/api/auth/*` | P0 | S | Public deploy |
| 2 | Add `/health` endpoint + docker-compose healthcheck | P0 | S | Reliable deploy |
| 3 | Write `.env.example` + pre-deploy migration runbook/job | P0 | S | First deploy succeeds |
| 4 | Add TLS termination (Caddy sidecar) to docker-compose | P0 | M | HTTPS, invite links work |
| 5 | Add CSP + HSTS to `SecurityHeadersMiddleware`; add security headers to `nginx.conf` | P1 | S | Browser security |
| 6 | Add SMTP timeout + exception swallow | P1 | S | Invite reliability |
| 7 | Fix `UpdateProfileCommand` AvatarUrl unbounded write | P1 | XS | Data integrity |
| 8 | Validate avatar magic bytes in `UploadAvatarCommand` | P1 | S | Content injection |
| 9 | Add automated SQLite backup cron to compose stack | P1 | S | Data safety |
| 10 | Add concurrency token (`[Timestamp] RowVersion`) to `TodoItem` + `TodoList` | P1 | M | Lost-update correctness |
| 11 | Add structured logging (Serilog + sink) | P1 | M | Production visibility |
| 12 | Move avatar storage to object storage (S3 / R2) | P1→P2 | L | Performance, DB size |
| 13 | Switch SQLite → PostgreSQL | P2 | S (code) / M (ops) | Horizontal scale |
| 14 | Add OpenTelemetry (traces + metrics) | P2 | M | SLO visibility |
| 15 | Add TanStack Query | P2 | L | Cache, optimistic UX |

Items 1–4 are the minimum to call this "internet-facing." Items 5–11 are the next 30 days of hardening. Items 12–15 are the scale-up phase.

---

## 5. What's Already Strong

**Vertical slice + MediatR.** The handler-per-feature pattern keeps the codebase navigable and change-contained. Adding a new endpoint means one new file; removing one means one file deletion with no service-layer surgery.

**OpenAPI-generated TypeScript types.** Contract drift between the API and frontend is a compile-time error, not a runtime surprise. This is worth more than it sounds on a project without a separate API gateway.

**Three-layer test pyramid.** 159 backend integration tests (real HTTP, EF in-memory), 48 Vitest unit/integration tests (MSW + real React rendering), 14 Playwright E2E tests (real browser, critical paths). The axe accessibility assertion in every page test file is a nice touch — it's already caught real bugs.

**`MigrateOnStartup` flag.** The auto-migration pattern is opt-in and defaults off. This is the right design for the single-instance → multi-instance transition; most projects get this wrong.

**JWT key validation at startup.** `ServiceExtensions.cs` throws `InvalidOperationException` if the key is missing or shorter than 32 bytes. The app refuses to start in a misconfigured state. This is exactly the right behavior.

**Exception middleware hides stack traces in production.** `ExceptionMiddleware.cs:31` shows `exception.GetType().Name: exception.Message` in Development and a generic message in Production. No stack traces leak to clients.

**Soft deletes via global query filters.** Deleted rows are invisible by default — no handler forgets to filter them. The audit trail is preserved without any caller effort.

**BCrypt for passwords.** Password hashing is correct and done with a maintained library. No home-rolled hashing, no SHA-1.

**Dependabot weekly scans.** Dependency drift is caught automatically. The NuGet + npm vulnerability gates in CI mean a newly-published CVE blocks the next merge.
