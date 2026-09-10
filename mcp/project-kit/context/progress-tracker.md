# Progress Tracker — MCP Server [own-stack]

## Current State

**Phase P5** in `docs/planning/IMPLEMENTATION-ROADMAP.md`. Kit written (5 specs). **Not started.** Specs 01–02 are technically unblocked (Node 20 only) but the phase is scheduled after **P4 (infra)** because spec 04 needs a Railway/Docker topology and spec 03 needs backend 09's service token — running the whole roster against a deployed, stable API avoids re-work.

| Spec | Title | Status | Blocked by |
|---|---|---|---|
| 01 | MCP server setup (stdio + Streamable HTTP) | Pending (P5 entry point) | Node 20 only — technically free |
| 02 | Tool roster implementation (9 tools, ids are contracts) | Pending | mcp 01 |
| 03 | Service-token GraphQL client (real-user OBO principal) | Pending | mcp 02, backend 05 ✅ + **09 ✅** |
| 04 | Container + Railway deploy | Pending | mcp 03, **infra 02–04, 06** |
| 05 | Contract testing + MCP Inspector smoke | Pending | mcp 01–03 |

## Roadmap Order (canonical, from IMPLEMENTATION-ROADMAP.md P5)

`mcp 01 → 02 → 03 → 04 → 05`

**Why MCP is late, not early:** MCP is a *tool surface over an existing API*, so it has no downstream consumers inside Griot — nothing else in the repo waits on it. Its two hard upstreams (backend 09 for the service token, infra 02–04/06 for the deploy target) both resolve earlier, so scheduling it in P5 costs nothing and removes all re-work risk. If a scheduling gap opens, mcp 01–02 may be pulled forward.

## Next Steps

1. Do not start before backend 09 ✅ (P0) and infra 02–04/06 ✅ (P4).
2. Then branch `feature/mcp/01-mcp-server-setup` and implement spec 01 only.
3. Verification gates: `npm run lint && npm run typecheck && npm test` green (contract tests, mocked GraphQL) + MCP Inspector stdio smoke.

## Session Notes

- **2026-09-10 (backend 09 sync)** — Backend service authentication is delivered as real-user OBO (`ai-on-behalf-of`, four scopes). Planned MCP clients must send `X-On-Behalf-Of` from trusted caller context with Bearer `GRIOT_SERVICE_TOKEN`; no synthetic member is created. Spec 03, architecture and agent instructions are synchronized. No MCP production code was implemented; P5 still follows infra. Backend verification passed all 71 SQL-enabled tests after the approved fixture repair.

- **2026-09-03** — MCP kit created (AGENTS, skills, contexts, 5 specs; 9-tool roster fixed as a contract).
- **2026-09-10** — Orchestration boundary ratified: MCP is a data/tool surface, never an orchestration trigger — external MCP clients reach data through backend GraphQL only and never receive Trigger.dev credentials (`research/ai-integration.md` §2a).
- **2026-09-10 (2)** — Tracker created during the cross-system audit (this system previously had none). Phase P5 position + rationale recorded above.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
