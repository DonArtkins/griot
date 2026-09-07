# Feature 10 — API Documentation (Postman-first, human + agent consumable)

## Type

NEW FEATURE (DOCUMENTATION)

## What This Delivers

The API documentation surface for Griot: the **Postman collection is the machine-readable contract**, and a rendered human/agent-facing doc site lives at `docs/api/`. Built from the API surface diagram + `api-surface.md`, so Postman testing (feature 07), the docs, and implementation all agree.

## Dependencies

- Backend features 04–08 (routes + auth exist).
- Backend feature 07 (Postman collection is the source).
- Backend feature 13 (API surface map diagram).

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

- Docs live in `docs/api/` (root) + derived from backend's `api-surface.md`; the Postman collection is owned by backend feature 07.

## Out of Scope

- Interactive playground beyond `/graphql` dev (v2).

## Acceptance Criteria

- [ ] `docs/api/` complete; matches `api-surface.md` + Postman + diagram
- [ ] REST + GraphQL + auth + errors + hot-path examples all documented
- [ ] A route change updates docs + collection + api-surface in the same branch

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly.
