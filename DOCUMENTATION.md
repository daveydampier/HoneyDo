# HoneyDo — Technical Documentation

## Table of Contents

1. [Application Overview](#1-application-overview)
2. [Architecture & Design Decisions](#2-architecture--design-decisions)
3. [Domain Model](#3-domain-model)
4. [Data Design & Reasoning](#4-data-design--reasoning)
5. [Validation Design & Reasoning](#5-validation-design--reasoning)
6. [API Design & Reasoning](#6-api-design--reasoning)
7. [Authentication & Security](#7-authentication--security)
8. [Error Handling](#8-error-handling)
9. [Complete CRUD Operation Reference](#9-complete-crud-operation-reference)
10. [Configuration Reference](#10-configuration-reference)
11. [Frontend Reference](#11-frontend-reference)

---

## 1. Application Overview

HoneyDo is a collaborative to-do list application with a React frontend and an ASP.NET Core API backend. Users can create lists, add tasks with notes and due dates, invite collaborators, maintain a friends graph, and close lists once all tasks are resolved.

**Technology stack:**
| Layer | Technology |
|---|---|
| API | ASP.NET Core Web API (.NET 10) |
| ORM | Entity Framework Core + SQLite |
| Mediator | MediatR (vertical slice) |
| Validation | FluentValidation (pipeline behavior) |
| Auth | JWT Bearer tokens |
| Password hashing | BCrypt.Net |
| Email | MailKit (SMTP with dev console fallback) |
| Testing | xUnit + WebApplicationFactory + EF InMemory |
| Frontend | React + TypeScript (Vite) |

---

## 2. Architecture & Design Decisions

### 2.1 Vertical Slice with MediatR

Each feature is implemented as a self-contained slice: a command or query record, its validator, and its handler — all in one file under `Features/{Domain}/`. Controllers are thin dispatch points that extract the caller's identity from the JWT, build the appropriate command/query, and return the result.

**Rationale:** Vertical slices keep related logic co-located and eliminate the need to navigate across multiple layers (service → repository → context) to understand a single feature. When a requirement changes, exactly one file changes.

### 2.2 CQRS Separation

Read operations (`GetXxxQuery`) and write operations (`XxxCommand`) are separate MediatR messages with separate handlers. They share no base class.

**Rationale:** Commands and queries have different concerns — commands enforce business rules and produce side effects, queries optimise for projection efficiency. Keeping them separate prevents queries from accidentally picking up command-side logic and vice versa.

### 2.3 Validation Pipeline Behavior

`ValidationBehavior<TRequest, TResponse>` is an open-generic MediatR `IPipelineBehavior` registered via the standard DI container. It runs all `IValidator<TRequest>` implementations before the handler executes.

**Rationale:** Cross-cutting validation belongs in the pipeline, not in individual handlers. Handlers can assume their inputs are structurally valid and focus exclusively on business logic.

### 2.4 Global Exception Middleware

`ExceptionMiddleware` is the first middleware in the pipeline. It catches all unhandled exceptions and maps them to consistent JSON error responses.

**Rationale:** Controllers never contain try/catch blocks. Exceptions are the natural flow-control mechanism for error conditions (not found, forbidden, validation failure), and middleware ensures the response shape is uniform regardless of which feature throws.

### 2.5 Soft Deletes via Global Query Filters

`Profile`, `TodoList`, and `TodoItem` have a `DeletedAt` nullable timestamp. EF Core global query filters (`HasQueryFilter(x => x.DeletedAt == null)`) automatically exclude deleted records from all queries without any caller effort.

**Rationale:** Soft deletes preserve referential integrity (activity logs, item history) and allow potential recovery. Global filters ensure deleted records are invisible by default.

### 2.6 Role-Based List Access

Each list has a many-to-many `ListMember` join table with a `MemberRole` enum (`Owner`, `Contributor`). All access checks query `ListMembers` directly in the handler — there is no separate authorisation layer.

**Rationale:** List ownership is a domain concept, not an infrastructure one. Embedding the role check directly in each handler makes the authorisation logic explicit and co-located with the business rule that depends on it.

### 2.7 Auto-Migration on Startup

`Database.Migrate()` is called in `Program.cs` during application startup inside a scoped service scope. This applies any pending EF migrations automatically when the API starts.

**Rationale:** Eliminates the need to manually run `dotnet ef database update` when deploying or after adding new migrations. Migrations are applied exactly once per pending migration, with EF Core's `__EFMigrationsHistory` table preventing re-application.

### 2.8 Email with Dev Console Fallback

`IEmailService` / `SmtpEmailService` sends email via MailKit. When `Email:Smtp:Host` is empty or missing in configuration, the full email body is logged to the console instead of sending — this allows invite links to be tested locally without any SMTP setup.

**Rationale:** Requiring a real mail server for local development would add friction for every developer. The fallback makes the feature fully exercisable with only the console output.

---

## 3. Domain Model

```
Profile ──< ListMember >── TodoList ──< TodoItem >── TodoItemTag >── Tag
    │                                                                  │
    └── SentRequests / ReceivedRequests (Friend)     Profile ────────┘
    └── Tags (owned by Profile)
    └── ActivityLogs
    └── Invitations (as Inviter)
```

### Entities

| Entity | Purpose |
|---|---|
| `Profile` | User account. Stores credentials, display info, and soft-delete marker. |
| `TodoList` | Container for items. Has members, a title, and optional `ClosedAt` timestamp. Soft-deletable. |
| `ListMember` | Join table between Profile and TodoList carrying the member's `Role`. |
| `TodoItem` | An individual task. Has content, status, optional notes, due date, and assignment. Soft-deletable. |
| `TaskStatus` | Lookup table (seeded): Not Started, Partial, Complete, Abandoned. |
| `Tag` | A user-owned label with a hex colour. Scoped to the owning profile. |
| `TodoItemTag` | Join table between TodoItem and Tag (many-to-many). |
| `Friend` | A directed relationship row tracking a friend request lifecycle via `Status`. |
| `Invitation` | An email invitation sent to an unregistered address by a user. Token-based, single-use. |
| `ActivityLog` | Immutable audit record for list-level actions. |

### Enums

| Enum | Values |
|---|---|
| `MemberRole` | `Owner`, `Contributor` |
| `FriendStatus` | `Pending`, `Accepted`, `Blocked` |

---

## 4. Data Design & Reasoning

### 4.1 Composite Primary Keys

`ListMember (ListId, ProfileId)`, `TodoItemTag (ItemId, TagId)`, and `Friend (RequesterId, AddresseeId)` use composite PKs rather than surrogate integer IDs.

**Reasoning:** The combination of FKs is naturally unique and semantically meaningful. A surrogate key would add no information while masking the actual constraint.

### 4.2 Enum Stored as String

`ListMember.Role`, `Friend.Status`, and related enums are stored as `varchar` strings rather than integers.

**Reasoning:** Strings are human-readable when inspecting the database directly, survive enum reordering without a migration, and make the schema self-documenting.

### 4.3 DueDate as TEXT

`TodoItem.DueDate` is stored as a nullable `TEXT` column (max 10 chars, enforced format `YYYY-MM-DD`) rather than a `DateTime` or `DateOnly`.

**Reasoning:** Due dates are calendar dates with no time or timezone component. A plain date string sorts and compares correctly as text, maps trivially to the frontend's date input, and avoids SQLite datetime handling quirks.

### 4.4 Friend Model (Directed, Single-Row)

A friendship is represented as a single row `(RequesterId, AddresseeId, Status)`. Declining removes the row (not `Blocked`) so the requester can try again.

**Reasoning:** A symmetric two-row model introduces update anomalies. A single directed row with a status column keeps state in one place. Removing the row on decline preserves re-send capability.

### 4.5 DeleteBehavior.Restrict on Friend FKs

**Reasoning:** If a Profile is soft-deleted, the Friend row should remain for historical accuracy. `Restrict` prevents accidental hard-cascade deletion.

### 4.6 DeleteBehavior.SetNull on TodoItem.AssignedTo

**Reasoning:** Removing a list member should not destroy their in-progress work. Null assignee is a valid, meaningful state ("unassigned").

### 4.7 ClosedAt on TodoList

`TodoList.ClosedAt` is a nullable `DateTime?` column. A null value means the list is active; a non-null value means the list is closed and the timestamp records when.

**Reasoning:** Closing is a one-way state transition (currently) that should be recorded as a point-in-time event rather than a boolean flag. Storing the timestamp makes "closed on" display trivial and preserves auditability. A boolean flag would lose when the close occurred.

### 4.8 Invitation Token as URL-Safe Base64

Invitation tokens are 22-character URL-safe base64 strings derived from a `Guid.NewGuid()` (128 bits of entropy). `+` and `/` are replaced with `-` and `_`; trailing padding is stripped.

**Reasoning:** 128 bits of randomness is cryptographically unguessable. URL-safe encoding avoids percent-encoding in query strings. Guid-derived tokens are simple to generate without an external library.

### 4.9 EF Bidirectional Friend Query

Friend queries (for both the friends list and addable-friends list) use two separate EF LINQ queries — one for the "sent" direction and one for the "received" direction — concatenated in memory.

**Reasoning:** EF Core cannot translate a conditional navigation property expression (e.g. `? :` ternary selecting between `f.Addressee` and `f.Requester`) to SQL. Two simple queries are more readable, testable, and performant than a raw SQL workaround.

### 4.10 DueDate Sort: Nulls Last

When sorting items by due date ascending, the query uses `OrderBy(i => i.DueDate == null).ThenBy(i => i.DueDate)`. This ensures items with no due date always appear at the bottom regardless of sort direction.

**Reasoning:** SQLite sorts NULLs before non-null values in ascending order. Surfacing undated tasks above dated ones is counterintuitive — users want to see approaching deadlines first.

---

## 5. Validation Design & Reasoning

### 5.1 Validation Layer: FluentValidation via Pipeline

All input validation is declared in `AbstractValidator<TCommand>` classes co-located with their command. The `ValidationBehavior` pipeline collects all failures before the handler runs and throws a single `ValidationException` with all messages.

### 5.2 Business Rule Validation vs. Structural Validation

Structural validation (field lengths, formats, required-ness) lives in `AbstractValidator<T>`. Business rule validation (duplicate email, not-self friend request, non-member assignment) lives in the handler and throws `ValidationException` directly with a `ValidationFailure`.

**Reasoning:** FluentValidation validators are stateless and cannot query the database. Keeping structural rules in validators and business rules in handlers provides a clean separation.

### 5.3 Specific Validation Rules and Reasoning

| Field | Rule | Reasoning |
|---|---|---|
| `Email` | `EmailAddress()` + `MaximumLength(256)` | RFC 5321 max; 256 gives a round number with headroom. |
| `Password` (register) | `MinimumLength(8)` | NIST SP 800-63B minimum. |
| `DisplayName` | `NotEmpty()` + `MaximumLength(256)` | Arbitrary display names; 256 chars is a practical ceiling. |
| `Title` (list) | `NotEmpty()` + `MaximumLength(256)` | Prevents empty lists; 256 is a practical UI ceiling. |
| `Content` (item) | `NotEmpty()` + `MaximumLength(512)` | Task descriptions can be longer than names. |
| `Notes` (item) | `MaximumLength(256)` | Supplementary field; 256 chars for a brief annotation. |
| `DueDate` | `Matches(@"^\d{4}-\d{2}-\d{2}$")` | Enforces `YYYY-MM-DD` format; rejects ambiguous formats. |
| `StatusId` | `InclusiveBetween(1, 4)` | Maps to the four seeded `TaskStatus` rows. |
| `Color` (tag) | `Matches(@"^#[0-9A-Fa-f]{6}$")` | Strict CSS hex colour; ensures correct rendering. |
| `Page` | `GreaterThanOrEqualTo(1)` | 1-based paging; page 0 is semantically meaningless. |
| `PageSize` | `InclusiveBetween(1, 100)` | Prevents zero-size requests and caps result set size. |
| `Role` (member) | Valid enum value | Prevents invalid role strings from being persisted. |
| `Name` (tag) | `NotEmpty()` + `MaximumLength(100)` | Tags are short labels. |

### 5.4 Close List Validation

The `CloseListCommand` handler (not a FluentValidation validator) enforces:
1. Caller must be the list Owner
2. List must not already be closed
3. List must have at least one task
4. All tasks must be in status Partial (2), Complete (3), or Abandoned (4) — none may be Not Started (1)

**Reasoning:** These are business rules that require database queries (item statuses, existing `ClosedAt` value), making them unsuitable for a stateless FluentValidation validator.

### 5.5 Login Uses NotFoundException, Not ValidationException

Invalid credentials throw `NotFoundException` (→ 404) rather than a 400 validation error to prevent user enumeration.

---

## 6. API Design & Reasoning

### 6.1 Resource Hierarchy

Items and members are nested under lists (`/api/lists/{listId}/items`, `/api/lists/{listId}/members`) because they have no meaningful existence outside their parent list. Tags are a top-level resource (`/api/tags`) because they are owned by the user, not by a list.

### 6.2 PATCH for Partial Updates

`PATCH` is used for all update endpoints. Item updates support explicit `ClearDueDate` and `ClearAssignee` boolean flags to resolve the `null` = "clear" vs "leave unchanged" ambiguity.

### 6.3 Add Member by Profile ID vs. Email

Two endpoints exist for adding list members:
- `POST /api/lists/{listId}/members` — adds by email address (general purpose)
- `POST /api/lists/{listId}/members/{profileId}` — adds by profile ID (used by the "add friend" flow)

**Reasoning:** The friends picker UI already has the friend's `profileId` from the `addable-friends` endpoint. Requiring the caller to look up the email to pass back is unnecessary round-tripping. The profileId endpoint also avoids re-validating email format.

### 6.4 Addable Friends Endpoint

`GET /api/lists/{listId}/addable-friends` returns the caller's accepted friends who are not already members of the specified list, sorted by display name.

**Reasoning:** Computing the exclusion set (current members) on the client requires two separate API calls and client-side set subtraction. A dedicated server-side endpoint returns only actionable data and is more efficient.

### 6.5 Friend Request Returns Invitation Result

`POST /api/friends` returns `200 { invitationSent: bool }` rather than `204 No Content`:
- `invitationSent: false` — the email matched an existing account; a pending friend request was created
- `invitationSent: true` — no account found; an invitation email was sent to that address

**Reasoning:** The caller needs to know which path was taken to display the appropriate success message. `204` would force the UI to guess or make a follow-up request.

### 6.6 List Close Endpoint

`POST /api/lists/{listId}/close` closes a list. It returns the updated `TodoListResponse` (with `closedAt` populated) so the client can update its local state without a follow-up GET.

**Reasoning:** POST (not PATCH) is used because closing is a domain action with specific preconditions, not a generic field update. It cannot be undone via the same endpoint. Using a named action route (`.../close`) makes the intent explicit.

### 6.7 HTTP Status Codes

| Scenario | Code |
|---|---|
| Resource created | `201 Created` + `Location` header |
| Successful update / action | `200 OK` + updated resource |
| Delete / no-content action | `204 No Content` |
| Validation failure | `400 Bad Request` |
| Invalid/missing token | `401 Unauthorized` |
| Insufficient permissions | `403 Forbidden` |
| Resource not found | `404 Not Found` |
| Server fault | `500 Internal Server Error` |

---

## 7. Authentication & Security

### 7.1 JWT Bearer Tokens

JWT tokens are issued on register and login. All endpoints except `/api/auth/register` and `/api/auth/login` require a valid `Authorization: Bearer <token>` header.

**Token claims:**
| Claim | Value |
|---|---|
| `NameIdentifier` | Profile GUID |
| `Email` | Profile email |
| `Name` | Profile display name |

**Configuration:**
- `Jwt:Key` — HMAC-SHA256 signing secret (minimum 32 characters)
- `Jwt:Issuer` — Validated issuer string
- `Jwt:Audience` — Validated audience string
- `Jwt:ExpiryHours` — Token lifetime (default: 24 hours)
- `ClockSkew: TimeSpan.Zero` — No leeway on token expiry; tokens expire exactly when they say

### 7.2 Password Hashing

BCrypt is used for password hashing. Passwords are never stored or logged in plaintext.

### 7.3 CORS

CORS is configured from `AllowedOrigins` in appsettings (comma-separated). Any header and method is allowed for those origins.

---

## 8. Error Handling

`ExceptionMiddleware` is the outermost middleware and handles all exceptions.

| Exception Type | HTTP Status | Response Title |
|---|---|---|
| `NotFoundException` | 404 | "Not Found" |
| `UnauthorizedException` | 401 | "Unauthorized" |
| `ForbiddenException` | 403 | "Forbidden" |
| `ValidationException` | 400 | "Validation Failed" |
| Any other exception | 500 | Exception type + message (Development) / "An unexpected error occurred." (Production) |

---

## 9. Complete CRUD Operation Reference

### Authentication

| Operation | Method | Route | Auth | Request Body | Success Response |
|---|---|---|---|---|---|
| Register | `POST` | `/api/auth/register` | None | `{ email, password, displayName }` | `201` `{ token, profileId, displayName }` |
| Login | `POST` | `/api/auth/login` | None | `{ email, password }` | `200` `{ token, profileId, displayName }` |

---

### Profile

| Operation | Method | Route | Auth | Request Body | Success Response |
|---|---|---|---|---|---|
| Get profile | `GET` | `/api/profile` | Bearer | — | `200` `{ id, email, displayName, phoneNumber, avatarUrl, createdAt }` |
| Update profile | `PATCH` | `/api/profile` | Bearer | `{ displayName, phoneNumber?, avatarUrl? }` | `200` ProfileResponse |
| Change password | `PATCH` | `/api/profile/password` | Bearer | `{ currentPassword, newPassword }` | `204` |

---

### Lists

| Operation | Method | Route | Auth | Request Body | Success Response |
|---|---|---|---|---|---|
| Get all lists | `GET` | `/api/lists` | Bearer | — | `200` `TodoListResponse[]` |
| Get list by ID | `GET` | `/api/lists/{listId}` | Bearer | — | `200` `TodoListResponse` |
| Create list | `POST` | `/api/lists` | Bearer | `{ title }` | `201` + Location + `TodoListResponse` |
| Rename list | `PATCH` | `/api/lists/{listId}` | Bearer (Owner) | `{ title }` | `200` `TodoListResponse` |
| Close list | `POST` | `/api/lists/{listId}/close` | Bearer (Owner) | — | `200` `TodoListResponse` |
| Delete list | `DELETE` | `/api/lists/{listId}` | Bearer (Owner) | — | `204` |
| Get addable friends | `GET` | `/api/lists/{listId}/addable-friends` | Bearer (Member) | — | `200` `AddableFriendResponse[]` |

**TodoListResponse shape:**
```json
{
  "id": "guid",
  "title": "string",
  "role": "Owner|Contributor",
  "ownerName": "string",
  "memberCount": 0,
  "itemCount": 0,
  "createdAt": "datetime",
  "updatedAt": "datetime",
  "closedAt": "datetime|null"
}
```

**AddableFriendResponse shape:**
```json
{
  "profileId": "guid",
  "displayName": "string",
  "email": "string",
  "avatarUrl": "string|null"
}
```

**List close business rules:**
- Only the Owner can close a list
- The list must have at least one task
- All tasks must be in status Partial (2), Complete (3), or Abandoned (4) — no Not Started (1) tasks allowed
- An already-closed list cannot be closed again (400)
- Closed lists are read-only in the UI (no new tasks, no edits, no status cycling)

---

### Items

| Operation | Method | Route | Auth | Request Body / Query | Success Response |
|---|---|---|---|---|---|
| Get items (paged) | `GET` | `/api/lists/{listId}/items` | Bearer (Member) | `?page&pageSize&search&statusIds&sortBy&ascending` | `200` `PagedResult<TodoItemResponse>` |
| Get item by ID | `GET` | `/api/lists/{listId}/items/{itemId}` | Bearer (Member) | — | `200` `TodoItemResponse` |
| Create item | `POST` | `/api/lists/{listId}/items` | Bearer (Member) | `{ content, notes?, dueDate?, assignedToId? }` | `201` `TodoItemResponse` |
| Update item | `PATCH` | `/api/lists/{listId}/items/{itemId}` | Bearer (Member) | `{ content?, statusId?, notes?, dueDate?, assignedToId?, clearDueDate?, clearAssignee? }` | `200` `TodoItemResponse` |
| Delete item | `DELETE` | `/api/lists/{listId}/items/{itemId}` | Bearer (Member) | — | `204` |

**Query parameters for GET items:**

| Parameter | Type | Default | Description |
|---|---|---|---|
| `page` | int | 1 | Page number (≥ 1) |
| `pageSize` | int | 20 | Items per page (1–100) |
| `search` | string | — | Substring match on Content and Notes |
| `statusIds` | int[] | — | Filter to specific status IDs (1–4) |
| `sortBy` | enum | `CreatedAt` | `CreatedAt`, `DueDate`, or `Content` |
| `ascending` | bool | `true` | Sort direction. When sorting by DueDate, items with no due date always appear last regardless of direction. |

**TodoItemResponse shape:**
```json
{
  "id": "guid",
  "listId": "guid",
  "content": "string",
  "status": { "id": 1, "name": "Not Started" },
  "notes": "string|null",
  "dueDate": "YYYY-MM-DD|null",
  "assignedTo": { "id": "guid", "displayName": "string" } | null,
  "tags": [{ "id": "guid", "name": "string", "color": "#RRGGBB" }],
  "createdAt": "datetime",
  "updatedAt": "datetime"
}
```

**Notes field behaviour:**
- Sending `notes: "some text"` in a PATCH sets the notes
- Sending `notes: ""` (empty string) clears any existing notes
- Omitting `notes` from the PATCH body leaves notes unchanged

**Task status values:**

| ID | Name | Allows list close? |
|---|---|---|
| 1 | Not Started | No |
| 2 | Partial | Yes |
| 3 | Complete | Yes |
| 4 | Abandoned | Yes |

---

### Members

| Operation | Method | Route | Auth | Request Body | Success Response |
|---|---|---|---|---|---|
| Get members | `GET` | `/api/lists/{listId}/members` | Bearer (Member) | — | `200` `MemberResponse[]` |
| Add member by email | `POST` | `/api/lists/{listId}/members` | Bearer (Owner) | `{ email, role }` | `200` `MemberResponse` |
| Add friend as member | `POST` | `/api/lists/{listId}/members/{profileId}` | Bearer (Owner) | — | `200` `MemberResponse` |
| Update member role | `PATCH` | `/api/lists/{listId}/members/{profileId}` | Bearer (Owner) | `{ role }` | `200` `MemberResponse` |
| Remove member | `DELETE` | `/api/lists/{listId}/members/{profileId}` | Bearer (Owner) | — | `204` |

**MemberResponse shape:**
```json
{
  "profileId": "guid",
  "displayName": "string",
  "avatarUrl": "string|null",
  "role": "Owner|Contributor",
  "joinedAt": "datetime"
}
```

**Member business rules:**
- Only the Owner can add, remove, or change roles of members
- The Owner's own role cannot be changed or removed
- Adding a profile already on the list returns 400
- `POST /members/{profileId}` always adds the friend as `Contributor`
- `POST /members/{profileId}` requires the target to be an accepted friend of the caller (enforced at the UI layer via `addable-friends`; the endpoint itself only validates membership)

---

### Tags

| Operation | Method | Route | Auth | Request Body | Success Response |
|---|---|---|---|---|---|
| Get tags | `GET` | `/api/tags` | Bearer | — | `200` `TagDto[]` |
| Create tag | `POST` | `/api/tags` | Bearer | `{ name, color }` | `200` `TagDto` |
| Delete tag | `DELETE` | `/api/tags/{tagId}` | Bearer (Owner) | — | `204` |
| Apply tag to item | `POST` | `/api/lists/{listId}/items/{itemId}/tags/{tagId}` | Bearer (Member) | — | `204` |
| Remove tag from item | `DELETE` | `/api/lists/{listId}/items/{itemId}/tags/{tagId}` | Bearer (Member) | — | `204` |

---

### Friends

| Operation | Method | Route | Auth | Request Body | Success Response |
|---|---|---|---|---|---|
| Get friends & requests | `GET` | `/api/friends` | Bearer | — | `200` `FriendsResult` |
| Send friend request | `POST` | `/api/friends` | Bearer | `{ email }` | `200` `{ invitationSent: bool }` |
| Respond to request | `PATCH` | `/api/friends/{requesterId}` | Bearer | `{ accept: bool }` | `204` |
| Remove friend / cancel request | `DELETE` | `/api/friends/{friendId}` | Bearer | — | `204` |

**FriendsResult shape:**
```json
{
  "friends": [{ "profileId": "guid", "displayName": "string", "email": "string", "avatarUrl": "string|null" }],
  "pendingReceived": [{ "requesterId": "guid", "displayName": "string", "email": "string", "avatarUrl": "string|null", "createdAt": "datetime" }],
  "pendingSent": [{ "addresseeId": "guid", "displayName": "string", "email": "string", "avatarUrl": "string|null", "createdAt": "datetime" }]
}
```

**Friend business rules:**
- Cannot send a request to yourself (400)
- If the email matches an existing account, a pending friend request is created and `invitationSent: false` is returned
- If the email does not match any account, an invitation email is sent and `invitationSent: true` is returned; a pending `Invitation` row is created
- Duplicate invitations to the same unregistered email are silently ignored (idempotent)
- Declining a request removes the row — the requester can send again
- `DELETE /api/friends/{friendId}` works whether the relationship is accepted (remove friend) or pending-sent (cancel request)

---

### Invitations

| Operation | Method | Route | Auth | Request Body | Success Response |
|---|---|---|---|---|---|
| Accept invitation | `POST` | `/api/invitations/accept` | Bearer | `{ token }` | `204` |

**Invitation flow:**
1. User A sends a friend request to an email address that has no account
2. API creates an `Invitation` row and sends an email with a link: `{AppUrl}/register?invite={token}&email={email}`
3. Recipient clicks the link, arrives at the register page with email pre-filled and an invite banner
4. After registering, the client calls `POST /api/invitations/accept` with the token
5. API creates a pending `Friend` row from User A to the new user and marks the invitation as accepted
6. New user is redirected to the Friends page where they can see and accept the request

**Invitation business rules:**
- Tokens are 22-character URL-safe base64 strings (128-bit GUID entropy)
- Tokens are single-use; replaying an accepted token returns 404
- Inviting yourself is rejected (400)
- If a friendship already exists between the inviter and acceptor, the accept is silently successful

---

## 10. Configuration Reference

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=honeydo.db"
  },
  "Jwt": {
    "Key": "<secret — minimum 32 characters>",
    "Issuer": "HoneyDo",
    "Audience": "HoneyDo",
    "ExpiryHours": "24"
  },
  "AppUrl": "http://localhost:5173",
  "AllowedOrigins": "http://localhost:5173",
  "Email": {
    "FromName": "HoneyDo",
    "FromAddress": "noreply@honeydo.app",
    "Smtp": {
      "Host": "",
      "Port": "587",
      "Username": "",
      "Password": ""
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  }
}
```

**Email configuration notes:**
- Leave `Email:Smtp:Host` empty for development — the full email body (including invite links) will be logged to the API console instead of sent
- Set `AppUrl` to the frontend's base URL so that invite links in emails point to the correct host

### Running Locally

```bash
# Terminal 1 — API (auto-applies pending migrations on startup)
cd HoneyDo
dotnet run

# Terminal 2 — Frontend
cd honeydo-client
npm run dev
```

- API: `http://localhost:5277`
- Frontend / Vite dev proxy: `http://localhost:5173`

The Vite dev server proxies all `/api/*` requests to `http://localhost:5277`, so the frontend always uses relative `/api/...` paths.

### Migrations

Migrations are applied automatically via `Database.Migrate()` in `Program.cs` on every API startup. No manual `dotnet ef database update` step is needed during normal development.

To generate a new migration after changing the domain model:

```bash
# Stop the running API first (it locks HoneyDo.exe), then:
dotnet ef migrations add <MigrationName> --project HoneyDo/HoneyDo.csproj

# Restart the API — it will apply the migration automatically
dotnet run --project HoneyDo/HoneyDo.csproj
```

### Migration History

| Migration | Description |
|---|---|
| `20260420123221_Initial` | Base schema: Profiles, TodoLists, ListMembers, TodoItems, TaskStatuses, Tags, TodoItemTags, ActivityLogs |
| `20260421145325_AddFriends` | Friend relationship table |
| `20260422171449_AddInvitations` | Invitation table for email-based friend invites |
| `20260422180000_AddListClose` | `ClosedAt` nullable column on `TodoLists` |

---

## 11. Frontend Reference

### Pages

| Page | Route | Description |
|---|---|---|
| `ListsPage` | `/` | Shows active lists and closed lists in separate sections. Create new lists. |
| `ListDetailPage` | `/lists/:listId` | Task list with sorting, notes, and member management. Close list button for owners. |
| `FriendsPage` | `/friends` | Send/accept/decline friend requests. View current friends. |
| `ProfilePage` | `/profile` | View and update display name, phone number, avatar URL, and password. |
| `LoginPage` | `/login` | Email/password authentication. |
| `RegisterPage` | `/register` | Account creation. Handles invite links (`?invite=token&email=...`). |

### List Detail Page Features

- **Sort toolbar**: Switch between Due Date and Created Date sorting; toggle ascending/descending. Default is Due Date ascending (earliest first, undated tasks at bottom).
- **Column headers**: Status · Task · Created · Due Date · (actions)
- **Notes**: Displayed as italic gray text below task content. Click "+ Add note" or "Edit" to open the edit form which includes a notes textarea with a 256-character counter.
- **Members panel**: Toggle with the Members button in the header. Shows all members with role badges. Owners can remove contributors and add accepted friends as collaborators directly.
- **Close List button**: Green when all tasks are resolved (Partial/Complete/Abandoned); grayed-out with a tooltip when not eligible. Confirm dialog before closing.
- **Read-only mode**: When a list is closed, the new-task form is hidden, status buttons become plain labels, and Edit/Delete/Add Note actions are hidden.

### Lists Page Sections

- **Active**: All lists without a `closedAt` value, in their natural API order.
- **Closed**: All lists with a `closedAt` value, sorted most-recently-closed first. Dimmed styling (`opacity: 0.8`, `#fafafa` background). Only shown when at least one closed list exists.

### TypeScript API Types (`src/api/types.ts`)

```typescript
TodoList         // id, title, role, ownerName, memberCount, itemCount, createdAt, updatedAt, closedAt
TodoItem         // id, listId, content, status, notes, dueDate, assignedTo, tags, createdAt, updatedAt
Member           // profileId, displayName, avatarUrl, role, joinedAt
AddableFriend    // profileId, displayName, email, avatarUrl
FriendInfo       // profileId, displayName, email, avatarUrl
ReceivedRequestInfo  // requesterId, displayName, email, avatarUrl, createdAt
SentRequestInfo      // addresseeId, displayName, email, avatarUrl, createdAt
FriendsResult    // friends, pendingReceived, pendingSent
SendRequestResult    // invitationSent: boolean
```
