# AGENTS.md - Griot MCP Server [own-stack]

## Read This First

You are the agent for the **MCP system** of Griot - the Model Context Protocol server that exposes Griot's data to external AI clients (Claude Desktop, Cursor, VS Code Copilot, Cline) and to Griot's own agents. You build tools, never features; every tool reads/writes through the backend GraphQL with `GRIOT_SERVICE_TOKEN`.

Stack: `@modelcontextprotocol/sdk` + zod, Node 20 (own lockfile). Transports: stdio (local) + Streamable HTTP (Docker/Railway).

## Tool roster (v1) - ids are contracts

`list_projects`, `list_boards`, `get_board`, `get_task`, `create_task`, `add_comment`, `get_activity_feed`, `summarize_project`.

**v2 (PLANNED, mcp 06):** `list_reports`, `get_report`, `generate_report`, `download_report`, `system_audit`, `get_audit_log`, `get_metrics` — need backend 20/24/25 (role-aware capability filtering per OBO user) + `CreateReport` scope + ai 06/07. V1 ids are unchanged until mcp 02 ships.

## Reading Order

1. Root `AGENTS.md` + root `integration-contracts.md` (AI/MCP tool contract).
2. `research/ai-integration.md` sec 6-7.
3. `mcp/project-kit/context/{architecture,tool-roster,security}.md`.
4. Current spec.

## Required Skills

Root shared skills + `mcp/.agents/skills/` (`mcp-sdk-tools`, `mcp-contract-testing`).

## Where This System Sits in the Build Order (canonical: `docs/planning/IMPLEMENTATION-ROADMAP.md`)

**Phase P5 —** after infra (P4), because spec 04 deploys to Railway/Docker (needs infra 02–04/06) and spec 03 needs backend 09's service token. Nothing downstream waits on MCP, so the late slot costs nothing. Own order: **01 → 02 → 03 → 04 → 05 → 06** (01–02 technically unblocked anytime — Node 20 only; 06 = report/audit v2 tools after backend 20/24/25 + ai 06/07). Entry branch: `feature/mcp/01-mcp-server-setup`. Track state in `mcp/project-kit/context/progress-tracker.md`.

## Verification Gates

- `npm run lint && npm run typecheck && npm test` green (contract tests, mocked GraphQL).
- Manual smoke via MCP Inspector (stdio). Works over Streamable HTTP when deployed.
- Service-token-only auth; no deletes/invites exposed.

## Hard Rules

1. Tools never bypass the backend API. Send Bearer `GRIOT_SERVICE_TOKEN` plus `X-On-Behalf-Of: {real User.Id}` from trusted caller context; never use a synthetic member or a model-supplied identity.
2. Write tools mirror the approve-gate: they execute only what the backend allows (real-user OBO principal — 4 scopes, no deletes/invites).
3. MCP is a data/tool surface, not an orchestration trigger: external MCP clients get data through the backend GraphQL only — they never enqueue Trigger.dev tasks or receive Trigger.dev credentials (orchestration contract: `research/ai-integration.md` §2a).

**Engineering Excellence. Production Mindset. Professional Impact. Rocket**

## Implemented authentication contract (Feature 07)

Use the [auth contract](../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

Before committing or pushing implementation, run `python3 scripts/check-contract-sync.py` from
the repository root. Synchronize the owning spec, dependent specs, planning,
research, docs, contexts, agent instructions, diagram sources and progress notes
in the feature branch. Planned behavior must be labeled and must not count as
implemented acceptance evidence. Run the system verification gates as well.

## Trusted MCP identity and writes

**PLANNED transport binding:** stdio is bound to one operator-configured real user and workspace in the local MCP profile; an unbound profile fails closed. Streamable HTTP public endpoints require HTTPS/TLS and a per-user authenticated session mapped server-side to that user/workspace; a shared transport bearer alone is not a user identity. No client header/tool argument/model output may replace that identity. The backend independently requires its unexpired `ServiceToken:Delegations:{userId}` grant (workspace IDs + scopes + UTC expiry) and live membership. Internal `http://mcp:3001` is only the private container hop.

Each transport must test session A attempting to supply B's user/workspace identity, including report IDs and tool arguments: reject before dispatch; no cross-user result or count leakage. MCP writes require recorded user confirmation bound to tool, exact arguments/hash and identity; a client claim that an action is confirmed is insufficient. Until verified approval provenance exists, write tools remain unregistered. Never expose auth/OTP/delete/invite/member/status-update tools. `update_task_status` has no issued scope and is removed from the planned roster; do not map it to CreateTask.

## Audit synchronization — 2026-09-11

Implemented through backend 20 (observability pipeline); backend 29 (multi-tenant foundation) implemented 2026-09-11 on `feature/backend/29-multi-tenant-foundation-organizations` — roadmap §P0.5 next is backend 30 after 29 completes its outstanding gates. Future planning is not completed implementation. P0 (2026-09-11): backend 29 ✅ → 30 → 31 → 32 → 33 → 34 → 35 → 18 → 19 → 22 → 21 → 23 → 11 → 28 → 24 → 25 → 26 → 27 → 10. P2: ai 01 → ai 02 → web 10 → ai 03 → ai 04 → ai 05 → ai 06 → ai 07 → web 11 → ai 08 → ai 09 → web 12 → ai 10 → ai 11 → ai 12. Full requirement/review ledger: `docs/planning/AI-SYSTEM-AUDIT-2026-09-11.md`.
## Multi-Tenant Migration Wave (2026-09-11 — PLANNED)

MCP becomes org-aware (canonical contract: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md`):

- **Tenant context:** every tool resolves the active organization from the delegated OBO principal (JWT v2 `org` claim resolved server-side, backend 29/30) — never from a client header, tool argument or model output; the delegation grant gains `OrganizationId` (mcp 03 bump).
- **Role-aware results per backend 25 tiers:** SuperAdmin/Dev, Admin (ALL projects inside their company only), PM, Member, Client (portal read-only). Raw-log tools stay SuperAdmin/Dev tier.
- **NEW spec 07 (PLANNED):** tenant-scoped tool manifest v3 — companies list (SuperAdmin tier), client-portal read-only tools (Client tier), handoff/maintenance read tools; manifest served per role tier.
- **Specs 01–06** each carry a "Multi-Tenant Update (2026-09-11 — PLANNED)" section; cross-tenant negative tool tests extend mcp 05.
- All of the above is PLANNED — no production code; implemented-status claims elsewhere in this file are unchanged until each spec ships on its own feature branch.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
