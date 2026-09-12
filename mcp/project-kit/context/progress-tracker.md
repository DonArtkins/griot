# Progress Tracker — MCP Server [own-stack]

## Current State

**Phase P5** in `docs/planning/IMPLEMENTATION-ROADMAP.md`. Kit written (7 specs). **Not started.** Specs 01–02 are technically unblocked (Node 20 only) but the phase is scheduled after **P4 (infra)** because spec 04 needs a Railway/Docker topology and spec 03 needs backend 09's service token — running the whole roster against a deployed, stable API avoids re-work.

| Spec | Title | Status | Blocked by |
|---|---|---|---|
| 01 | MCP server setup (stdio + Streamable HTTP) | Pending (P5 entry point) | Node 20 only — technically free |
| 02 | Tool roster implementation (8 tools, ids are contracts) | Pending | mcp 01 |
| 03 | Service-token GraphQL client (real-user OBO principal) | Pending | mcp 02, backend 05 ✅ + **09 ✅** |
| 04 | Container + Railway deploy | Pending | mcp 03, **infra 02–04, 06** |
| 05 | Contract testing + MCP Inspector smoke | Pending | mcp 01–03 |
| 06 | Report & audit tools (v2 roster) | Pending (**P5 wave**) | mcp 02, backend 20/24/25, ai 06/07 |
| 07 | Tenant-scoped tools v3 (org-aware manifest per role tier) | 📋 Spec written (PLANNED) — multi-tenant wave | mcp 06, backend 25/29/30/32/34/35 |

## Next Steps

1. Do not start before backend 09 ✅ (P0) and infra 02–04/06 ✅ (P4).
2. Then branch `feature/mcp/01-mcp-server-setup` and implement spec 01 only.
3. Verification gates: `npm run lint && npm run typecheck && npm test` green (contract tests, mocked GraphQL) + MCP Inspector stdio smoke.

### Roadmap order

`mcp 01 → 02 → 03 → 04 → 05 → 06`

**Why MCP is late, not early:** MCP is a *tool surface over an existing API*, so it has no downstream consumers inside Griot — nothing else in the repo waits on it. Its two hard upstreams (backend 09 for the service token, infra 02–04/06 for the deploy target) both resolve earlier, so scheduling it in P5 costs nothing and removes all re-work risk. If a scheduling gap opens, mcp 01–02 may be pulled forward.

## Session Notes

- **2026-09-12 (tracker repair)** — Corrected current counts, table structure, and prerequisite status; kept historical checkpoints inside Session Notes. Backend 29–31 are implemented; backend 32 is next. This system's features remain pending; tenant extensions follow their owning specs' dependencies.

- **2026-09-11 (multi-tenant wave sync)** — PLANNED wave per `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md`: specs 01–06 each gained a "Multi-Tenant Update (2026-09-11 — PLANNED)" section (tenant context from the OBO principal's `org` claim; role-aware results per backend 25 tiers; delegation gains `OrganizationId`; env/deploy unchanged; cross-tenant negative tool tests; per-org audit-summary + client progress report variant) and NEW spec 07 (tenant-scoped tool manifest v3) was written. No production code; existing statuses untouched.

- **2026-09-11 (superpowers wave sync)** — Added PLANNED mcp spec 06 (v2 report/audit tools: list_reports, get_report, generate_report, download_report, system_audit, get_audit_log, get_metrics). V1 8-tool roster untouched until mcp 02 ships; v2 needs backend 20/24/25 + CreateReport scope + ai 06/07. Never OTP/auth/delete/invite/member tools. No production code; contract-sync run.

- **2026-09-10 (backend 09 sync)** — Backend service authentication is delivered as real-user OBO (`ai-on-behalf-of`, four scopes). Planned MCP clients must send `X-On-Behalf-Of` from trusted caller context with Bearer `GRIOT_SERVICE_TOKEN`; no synthetic member is created. Spec 03, architecture and agent instructions are synchronized. No MCP production code was implemented; P5 still follows infra. Backend verification passed all 71 SQL-enabled tests after the approved fixture repair.

- **2026-09-03** — MCP kit created (AGENTS, skills, contexts, 5 specs; the v1 roster was then documented as 9 tools including `update_task_status`, which backend 09's scope review later removed — the fixed v1 roster is now **8 tools**).
- **2026-09-10** — Orchestration boundary ratified: MCP is a data/tool surface, never an orchestration trigger — external MCP clients reach data through backend GraphQL only and never receive Trigger.dev credentials (`research/ai-integration.md` §2a).
- **2026-09-10 (2)** — Tracker created during the cross-system audit (this system previously had none). Phase P5 position + rationale recorded above.

### Audit synchronization — 2026-09-11 (historical)

Implemented through backend 20 (observability pipeline); backend 29 (multi-tenant foundation) implemented 2026-09-11 on `feature/backend/29-multi-tenant-foundation-organizations` — roadmap §P0.5 next is backend 30 after 29 completes its outstanding gates. Future planning is not completed implementation. P0 (2026-09-11): backend 29 ✅ → 30 ✅ → 31 ✅ → 32 → 33 → 34 → 35 → 18 → 19 → 22 → 21 → 23 → 11 → 28 → 24 → 25 → 26 → 27 → 10. P2: ai 01 → ai 02 → web 10 → ai 03 → ai 04 → ai 05 → ai 06 → ai 07 → web 11 → ai 08 → ai 09 → web 12 → ai 10 → ai 11 → ai 12. Full requirement/review ledger: `docs/planning/AI-SYSTEM-AUDIT-2026-09-11.md`.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
