# Feature 10 — API Documentation (Postman-first, human + agent consumable)

## Type

NEW FEATURE (DOCUMENTATION)

## What This Delivers

The API documentation surface for Griot: the **Postman collection is the machine-readable contract**, and a rendered human/agent-facing doc site lives at `docs/api/`. Built from the API surface diagram + `api-surface.md`, so Postman testing (feature 08), the docs, and implementation all agree.

## Dependencies

- Backend features 04–09 and the completed P0 surface: 20 → 18 → 19 → 22 → 21 → 23 → 11 → 28 → 24 → 25 → 26 → 27. This final gate documents every shipped route and capability contract, including backend 25–28; no surface is excluded.
- Backend feature 08 (Postman collection is the source).
- API surface map diagram (`PROMPTS/week-02/13-diagram-api-surface.md` — a diagram *prompt* #13, not a backend feature spec).

## Context To Read First

- `backend/project-kit/context/api-surface.md`
- `PROMPTS/week-02/13-diagram-api-surface.md` (diagram)
- `docs/api/README.md`

## Agent Skills To Use

- Root `.agents/skills/context7/SKILL.md` (verify the docs generator choice)
- Root `.agents/skills/documentation-standards/SKILL.md` (if implemented)

## Files Owned

- `docs/api/**` (rendered reference + README)
- (optional) OpenAPI export of the REST controllers via NSwag/Swashbuckle

## Files

CREATE: `docs/api/REST.md` — every REST route, method, body/response shape, errors, auth, examples (from the Postman collection + api-surface).
CREATE: `docs/api/GraphQL.md` — queries/mutations/types, DataLoader note, auth (from `/graphql?sdl`).
CREATE: `docs/api/CHANGELOG.md` — API contract changes tracked (contract-sync).
MODIFY: `docs/api/README.md` — index + how to render (Stoplight/Postman published, or Redoc/Scalar from OpenAPI).

## Setup / Initialization

```bash
# Option A (recommended): publish the Postman collection (Postman → Publish) → docs URL.
# Option B: add Swashbuckle/NSwag to Griot.Api to emit openapi.json → feed Redoc/Scalar.
# GraphQL: /graphql?sdl is always up-to-date.
```

## Implementation Notes

- REST + GraphQL + Postman + diagram must never drift (contract-sync gate).
- Include auth (Bearer + refresh rotation), error codes (400/401/403/404/409/429), and examples for the 4 hot paths (login, board, create-task, bulk-status).
- The docs are consumed by the web/mobile/AI/MCP agents AND by Postman testers.

## Separation of Concerns

- Docs live in `docs/api/` (root) + derived from backend's `api-surface.md`; the Postman collection is owned by backend feature 08.

## Out of Scope

- Interactive playground beyond `/graphql` dev (v2).

## Acceptance Criteria

- [ ] `docs/api/` complete; matches `api-surface.md` + Postman + diagram
- [ ] REST + GraphQL + auth + errors + hot-path examples all documented, including backend 25–28 log/capability, conversation, notice and lifecycle-evidence routes
- [ ] A route change updates docs + collection + api-surface in the same branch

## Implemented authentication contract (Feature 07)

Use the [auth contract](../../../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

## Multi-Tenant Update (2026-09-11 — PLANNED)

- **New route families to document** (all PLANNED, owned by specs 29–35 + revisions 36–51): `/api/organizations` platform routes (SuperAdmin create/list/suspend/reactivate/transfer-ownership/offboard — specs 32/33), `/api/auth/select-organization` + `GET /api/auth/organizations` (spec 30/42 revision), `/api/organizations/{id}/roles` + invites (specs 31/46), client portal routes `/api/client/projects` + `/api/client/projects/{id}/feedback` + maintenance requests (specs 34/35), handoff routes `/api/projects/{id}/handoff*` (specs 35/50).
- **JWT v2 claims documented:** access tokens carry `name`, `org` (active OrganizationId), `role` (effective role incl. `super_admin` / `custom:{roleId}`), `perms` (space-separated permission keys); refresh token stays **opaque 64-hex — not a JWT, by design**; docs must never imply the refresh token decodes.
- **Error catalogue gains** `403 org_suspended` (suspended/offboarding org writes blocked, reads allowed — spec 32) and the 404-not-403 cross-tenant convention (no existence leak — spec 39 revision).
- `docs/api/CHANGELOG.md` gains the multi-tenant wave entry; the contract-sync gate must include docs/api + Postman + `backend/project-kit/context/api-surface.md` in the same branch for every tenant route change.
- Postman-first rule holds: the collection (spec 43 revision) is the machine-readable source for the new org/company/client/handoff folders; REST.md/GraphQL.md are derived from it.
- Hot-path examples gain an **org-switched login** example (login → `select-organization` → board read) alongside the existing four.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
