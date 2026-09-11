# Risk Register / Failure Modes — Griot

> A deliberately paranoid list: failure scenario → blast radius → mitigation → detection. This is the real answer to "how do I make sure it doesn't crash on launch day". Populated from what's already designed (not hypothetical).

| # | Failure scenario | Blast radius | Mitigation (designed) | Detection |
|---|---|---|---|---|
| 1 | **Redis down** | Rate limiting + refresh metadata + token budgets fail; login hammering unprotected | **Policy: fail-closed for rate limiting** (deny the request if Redis is unreachable — never fail-open). Login/refresh behavior when Redis is unavailable: `POST /api/auth/login` → HTTP 503 (service unavailable, retry later); `POST /api/auth/refresh` → HTTP 503. Both endpoints MUST NOT bypass the rate-limit window; denying early is safer than allowing unlimited auth attempts. Documented in auth ADR. Aligns with `/health` Redis ping (MONITORING.md §1) — the health check will report degraded before these paths are hit in production. | `/health` checks Redis ping; alert |
| 2 | **Railway cold start (free tier)** | First request after idle is slow (multi-second) | Keep 1 running instance; health-check ping every 60 s (no free-tier spin-down) | uptime ping latency alert |
| 3 | **Refresh-token race/replay** | Session takeover if a stolen token is reused | Rotation + family-revoke on reuse; integration test (qa) | Watchdog: repeated 401 on refresh → alert |
| 4 | **N+1 query under load** | Board/dashboard slow at scale | DataLoader for assignee/comments; indexes on FKs; k6 baseline | k6 in CI + dashboard p95 check |
| 5 | **SQL Server fills disk / connection pool** | API errors | Pool limits; retention pruning (90-day log); volume monitoring | Railway volume alert |
| 6 | **LLM budget exceeded** | Cost spike on Copilot | Per-workspace Redis token budget + alarms; propose-before-write limits usage | budget metrics |
| 7 | **Prompt injection via user text** | Agent manipulated into unrelated writes | User text = data; tools scope per workspace; OBO role `ai-on-behalf-of` (4 scopes, no deletes/invites). **Write gate:** Copilot mutations are proposed → approved → executed by the app (never the agent); deterministic scheduled notifications intentionally bypass approval BY DESIGN but require an authorized OBO identity + idempotency; MCP write tools define confirmation/provenance controls before implementation | audit logs review |
| 8 | **MCP tool abuse (external client)** | Data exfiltration / load | Service token + OBO scope (4 claims, 403 on deletes/invites/bulk); per-workspace scoping; rate limit on MCP HTTP | MCP access logs |
| 9 | **Deploy broke production** | Site down | Vercel instant rollback; Railway rollback previous deploy; runbook | uptime ping + `/health` |
| 10 | **Attachments blob fills disk** | Storage exhaustion | Metadata in DB; blob store (v2), size limits; prune | storage metrics |
| 11 | **Dependency CVE** | Supply-chain compromise | lockfiles + CI dependency audit (Weekly-6 gate); no `latest` floats | `npm audit`/`dotnet list package` in CI |
| 12 | **Webhook HMAC misconfig** | Fake Trigger callbacks | HMAC `X-Trigger-Signature` verified in middleware | webhook error logs |
| 13 | **Overdue data retention** | Compliance / storage | 90-day prune + append-only audit | nightly prune job logs |
| 14 | **User ran out of local disk (dev)** | Dev env break | docs note; compose volumes on external disk | — |
| 15 | **GraphQL depth/bomb** | Memory outage | query-cost guard + depth limit + timeouts | 400s/429s logged to ErrorLogs |
| 16 | **AI privilege escalation via blanket tool access** | AI agent sees data its invoking user's role could not view | Per-role capability manifest resolved server-side (backend 25) — raw-log/forensic tools are ABSENT from non-SuperAdmin/Dev sessions; 403 before domain lookup | capability-gateway contract test |
| 17 | **AI-initiated mass email** | Irreversible external blast radius | Broadcasts require preview + recipient-count + explicit SuperAdmin confirm (backend 27 / ai 10); cancellation before fire; AuditLogs row records direction + count | broadcast status list + audit query |
| 18 | **SuperAdmin alert fatigue** | Desensitized operators miss real incidents | Severity thresholds + auth-path immediate alert; alert audit rows; plain-language summaries (ai 10) | alert metrics |

## Top-3 risks to watch day 1
1. Redis single point — **fail-closed policy in effect**: login + refresh return 503 when Redis is unreachable; rate-limit window is never bypassed (see risk #1 above and MONITORING.md §1).
2. Railway cold start (keep instance warm + uptime ping).
3. Refresh replay (covered by test; watch 401 spikes).

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**

## Implemented authentication contract (Feature 07)

Use the [auth contract](../api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.
