# MCP Feature Spec 07 — Tenant-Scoped Tool Manifest v3 [own-stack]

**Status:** PLANNED — the org-aware roster evolution served per role tier from the backend 25 capability manifest. V1 (mcp 02) and v2 (mcp 06) rosters are unchanged; v3 ids are new contracts appended after backend 29/30/32/34/35 exist.

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented**

## What This Delivers

The v3 tool roster: org-aware tools resolved against the active organization from the delegated OBO principal — a companies list for the SuperAdmin tier, client-portal read-only tools for the Client tier, and handoff/maintenance read tools — with the manifest served per role tier (backend 25). Canonical tenant contract: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md`.

## The v3 tool roster (ids are contracts)

| Tool id | Input (zod) → Output | Tier | Backend surface |
|---|---|---|---|
| `list_organizations` | `{page?}` → `{items: OrganizationMeta[]}` | SuperAdmin | `GET /api/organizations` (spec 32) |
| `get_organization` | `{organizationId}` → `OrganizationDetail` (incl. lifecycle events) | SuperAdmin | `GET /api/organizations/{id}` (spec 32) |
| `get_project_progress` | `{projectId}` → `ClientProgressView` (percent-complete, milestones, activity digest) | Client | backend 34 client progress DTO |
| `list_project_feedback` | `{projectId, page?}` → own `ClientFeedback[]` | Client | backend 34 `ClientFeedback` reads |
| `get_project_handoff` | `{projectId}` → `HandoffChecklist` | Client + Admin/PM read | backend 35 `ProjectHandoffs` |
| `list_handoff_documents` | `{projectId}` → `HandoffDocumentMeta[]` | Client + Admin/PM read | backend 35 `HandoffDocuments` |
| `list_maintenance_requests` | `{projectId, page?}` → maintenance rows | Client + Admin/PM read | backend 35 maintenance phase |

## Dependencies

- mcp 02 (v1, unchanged) · mcp 06 (v2 pattern) · mcp 03 (delegation gains `OrganizationId`) · backend 25 (per-role-tier manifest/data tiers) · backend 29/30 (tenancy foundation + JWT v2 `org` claim) · backend 32 (organizations + lifecycle) · backend 34 (client portal) · backend 35 (handoff/maintenance) · infra 04 (deployed transport).

## Context To Read First

- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` (§3 roles, §6 client surface, §7 handoff/maintenance)
- `mcp/project-kit/context/{tool-roster,security}.md`
- backend spec 25 (capability manifest + role tiers) + `diagrams/erd/multi-tenant-amendment.md`

## Agent Skills To Use

- `mcp/.agents/skills/mcp-sdk-tools/SKILL.md`, `mcp/.agents/skills/mcp-contract-testing/SKILL.md`
- `.agents/skills/jwt-decode/SKILL.md` (verify `org`/`role`/`perms` claim shapes)

## Files Owned

- `mcp/src/tools/organizations/**`, `mcp/src/tools/client-portal/**`, `mcp/src/tools/handoff/**`, `mcp/src/lib/manifest.ts`

## Implementation Notes

- Manifest served per role tier: on session bind, fetch the backend 25 capability manifest for the principal and register only its intersection with the v3 ids above — a Client-tier session never sees SuperAdmin tools and vice versa.
- All v3 tools are **read-only**; feedback submission stays an in-app / ai 13 surface (no write tool in MCP this wave); never OTP/auth/delete/invite/member tools.
- Org resolution identical to mcp 03: the server-resolved delegation's `OrganizationId` is authoritative; client- or model-supplied org ids are rejected before dispatch.
- Cross-tenant tests per mcp 05: org A principal + org B ids → empty/denial, no rows, counts or metadata leakage.

## Separation of Concerns

- Tool logic stays pure `(graphqlClient, ctx, input) → output`; the backend owns tenancy, RBAC tiers and lifecycle state; MCP never filters org data locally.

## Docker & Deploy

- No new container, port, env or secret; rides the existing stdio + Streamable HTTP transports (mcp 04).

## Out of Scope

- Write tools for feedback/handoff/maintenance actions (a later wave, after backend 34/35 ship + approval provenance).
- SuperAdmin mutation tools — onboard/suspend/offboard stay backend 32/33 + web 14 only.
- Cross-org rollup reports.

## Acceptance Criteria

- [ ] Manifest-per-tier: a SuperAdmin session lists orgs; a Client session lists only client-portal tools; a Client session requesting `list_organizations` is denied before dispatch
- [ ] All v3 tools return backend-scoped results for the active org; org B ids from an org A principal return empty/denial with no metadata leakage
- [ ] Contract tests per v3 tool (mocked GraphQL) green; roster allow-list test asserts no auth/OTP/delete/invite/member tools and no write tools
- [ ] MCP Inspector smoke passes for `get_project_progress` against a seeded client-scoped project
- [ ] `npm run lint && npm run typecheck && npm test` green

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.