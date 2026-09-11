# Backend Feature Spec 18 — Search, Filtering, Pagination & Sorting

**Status:** PLANNED (not implemented — acceptance criteria below are unchecked by definition)

## What This Delivers

One uniform, documented query contract for every list endpoint (REST + GraphQL), replacing today's ad-hoc fixed page size: capped pagination, whitelisted sorting, and server-side search/filtering — the "faster backend" surface that spec 19 (caching) and spec 21 (load-balancing) both assume.

## Dependencies

- Features 04–08, 13–17 (✅ implemented routes this hardens). No schema change.

## Context To Read First

- `backend/project-kit/context/api-surface.md` §Conventions (this spec rewrites the pagination bullet)
- `docs/planning/OPTIMIZATION-RECOMMENDATIONS.md`, `docs/database/DATABASE-DESIGN.md` §4 (indexes these queries hit)

## Files Owned

- `backend/src/Griot.Application/Helpers/PageRequest.cs`, `PagedResult<T>.cs` (shared envelope)
- `backend/src/Griot.Api/Filters/` or service-level validation (no business logic in controllers)
- `backend/src/Griot.Infrastructure/Repositories/TaskRepository.cs` (Dapper dynamic WHERE/ORDER BY — parameterized only)
- `backend/Postman/Griot.postman_collection.json` (folder 14 assertions tightened from tolerant to strict)

## Query contract (applies to ALL list endpoints)

| Param | Type | Rules | Violation |
|---|---|---|---|
| `page` | int ≥ 1 | default 1 | 400 |
| `pageSize` | int 1–100 | default 25 (activity feed 50); **hard cap 100** — silently clamped + `X-Pagination-Clamped: true` | 400 if < 1 or non-int |
| `sortBy` | string | per-endpoint whitelist (below); unknown value → 400 | 400 |
| `sortDir` | `asc` \| `desc` | default `desc` for time-based, `asc` for name-based | 400 |
| `q` | string ≤ 100 chars | LIKE search (escaped; `%`/`_`/`[` escaped server-side) | 400 if > 100 |
| filters | per endpoint | unknown filter keys ignored + `X-Warning` header | — |

Response envelope `PagedResult<T>`: `{ items, page, pageSize, totalCount, totalPages }` + response headers `X-Total-Count`, `X-Page`, `X-Total-Pages`.

## Endpoint matrix (planned)

| Endpoint | Sort whitelist | Filters | Search `q` over |
|---|---|---|---|
| `GET /api/boards/{id}/tasks` | CreatedAt, DueDate, Priority, ColumnId, Status, Position | status, priority, assigneeId, dueFrom, dueTo, columnId | Title, Description |
| `GET /api/workspaces` | CreatedAt, Name | — | Name |
| `GET /api/workspaces/{id}/members` | CreatedAt, DisplayName | role | User.DisplayName, User.Email |
| `GET /api/workspaces/{id}/projects` | CreatedAt, Name | archived | Name |
| `GET /api/boards/{id}/tasks/{id}/comments` | CreatedAt | — | Body |
| `GET /api/notifications` | CreatedAt | type, unreadOnly | Title, Message |
| `GET /api/workspaces/{id}/activity` | CreatedAt (only) | action, userId | — |
| `GET /api/logs/errors` (spec 20) | CreatedAt | fixStatus | ExceptionType, Message |
| `GET /api/logs/audit` (spec 20) | CreatedAt | entityType, entityId, actorId | Action |
| GraphQL `tasks(filter, sort)` | same whitelist via typed `TaskFilter`/`TaskSort` input types | same | same |

## Implementation Notes

- EF paths: `Skip((page-1)*pageSize).Take(pageSize)` + `OrderBy` on the whitelisted column only (never string-interpolated). Dapper bulk/dashboard path: `ORDER BY` from a whitelist map (column name never concatenated from input); all values parameterized.
- `totalCount` via `CountAsync`/`SELECT COUNT(*)` in the same filter state (accept the second round-trip; cache hit ratio handled in spec 19).
- Search on `nvarchar(max)` bodies uses `LIKE @q + '%'` prefix form where possible; full-text search is explicitly out of scope (k6 evidence first).
- GraphQL `tasks` query gains `TaskFilterInput` / `TaskSortInput` mirroring the REST contract — zero drift rule holds.

## Separation of Concerns

Validation of the contract lives in `Griot.Application` (reusable); controllers only bind query strings; SQL shape stays in `Griot.Infrastructure`.

## Docker & Deploy

No compose change. Response headers are safe to cache at the edge? No — `Cache-Control: private` is already set on authed responses (spec 19).

## Out of Scope

Full-text/catalog indexes, cursor pagination for infinite scroll (post-bootcamp), mobile offline search.

## Acceptance Criteria (all pending)

- [ ] Every listed endpoint returns `PagedResult<T>` shape with the three `X-` headers
- [ ] `pageSize=5000` → 400; `sortBy=1;1` → 400 with RFC 7807 `application/problem+json`
- [ ] `q` escaping verified by unit test (`%`/`_` injection returns literal matches only)
- [ ] Board with 300 tasks: page 3 of size 25 returns exactly the whitelisted-ordered slice (xUnit)
- [ ] Postman folder 14 + Tasks requests updated to strict assertions (400s asserted as 400)
- [ ] `docs/database/DATABASE-DESIGN.md` Phase-2 covering index `TaskItems(BoardId, ColumnId, Position)` evaluated with the new ORDER BY shapes (evidence note added)

## Multi-Tenant Update (2026-09-11 — PLANNED)

- **All list endpoints become org-scoped automatically** through the spec-37 EF global query filters bound to `ITenantContext` (JWT `org`) — the query contract on this page (page/pageSize cap/sortBy whitelist/`q` escaping) is applied **on top of** tenant scoping, never instead of it.
- **New list endpoints from the tenant wave adopt `PagedResult<T>` + whitelists** (PLANNED, revisions 46–51): `GET /api/organizations` (SuperAdmin), `/api/organizations/{id}/members`, `/api/organizations/{id}/roles`, `/api/organizations/{id}/invites`, project feedback lists, `/api/organizations/{id}/lifecycle-events`, client progress/feed lists — each gets a per-endpoint whitelist entry in the matrix above.
- **Dapper paths gain `@OrganizationId`** (spec 38 revision): `totalCount`/`SELECT COUNT(*)` runs in the same org-filter state as the page query; stored-proc signatures carry the mandatory org parameter.
- Cross-tenant id semantics: an org-scoped list/page resolving a foreign id returns **empty** → 404 conventions per the spec-39 revision (no existence leak).
- **Pagination whitelist is unchanged** — no new sortable columns beyond the per-endpoint whitelists added by the tenant revisions; `pageSize=5000` → 400 still holds everywhere.
- Postman strict assertions extended to the new org folders (spec 43 revision): envelope shape + three `X-` headers asserted on every tenant list endpoint.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.