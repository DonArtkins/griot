# Risk Register / Failure Modes — Griot

> A deliberately paranoid list: failure scenario → blast radius → mitigation → detection. This is the real answer to "how do I make sure it doesn't crash on launch day". Populated from what's already designed (not hypothetical).

| # | Failure scenario | Blast radius | Mitigation (designed) | Detection |
|---|---|---|---|---|
| 1 | **Redis down** | Rate limiting + refresh metadata + token budgets fail; login hammering unprotected | Decide **fail-closed for rate limiting** (deny if Redis unreachable) OR fail-open + circuit-breaker; document choice in auth ADR | `/health` checks Redis ping; alert |
| 2 | **Railway cold start (free tier)** | First request after idle is slow (multi-second) | Keep 1 running instance; health-check ping every 60 s (no free-tier spin-down) | uptime ping latency alert |
| 3 | **Refresh-token race/replay** | Session takeover if a stolen token is reused | Rotation + family-revoke on reuse; integration test (qa) | Watchdog: repeated 401 on refresh → alert |
| 4 | **N+1 query under load** | Board/dashboard slow at scale | DataLoader for assignee/comments; indexes on FKs; k6 baseline | k6 in CI + dashboard p95 check |
| 5 | **SQL Server fills disk / connection pool** | API errors | Pool limits; retention pruning (90-day log); volume monitoring | Railway volume alert |
| 6 | **LLM budget exceeded** | Cost spike on Copilot | Per-workspace Redis token budget + alarms; propose-before-write limits usage | budget metrics |
| 7 | **Prompt injection via user text** | Agent manipulated into unrelated writes | User text = data; tools scope per workspace; ai-agent role (no deletes/invites) | audit logs review |
| 8 | **MCP tool abuse (external client)** | Data exfiltration / load | Service token + ai-agent scope; per-workspace scoping; rate limit on MCP HTTP | MCP access logs |
| 9 | **Deploy broke production** | Site down | Vercel instant rollback; Railway rollback previous deploy; runbook | uptime ping + `/health` |
| 10 | **Attachments blob fills disk** | Storage exhaustion | Metadata in DB; blob store (v2), size limits; prune | storage metrics |
| 11 | **Dependency CVE** | Supply-chain compromise | lockfiles + CI dependency audit (Weekly-6 gate); no `latest` floats | `npm audit`/`dotnet list package` in CI |
| 12 | **Webhook HMAC misconfig** | Fake Trigger callbacks | HMAC `X-Trigger-Signature` verified in middleware | webhook error logs |
| 13 | **Overdue data retention** | Compliance / storage | 90-day prune + append-only audit | nightly prune job logs |
| 14 | **User ran out of local disk (dev)** | Dev env break | docs note; compose volumes on external disk | — |
| 15 | **GraphQL depth/bomb** | Memory outage | query-cost guard + depth limit + timeouts | 400s/429s logged to ErrorLogs |

## Top-3 risks to watch day 1
1. Redis single point (mitigate fail-closed rate limit).
2. Railway cold start (keep instance warm + uptime ping).
3. Refresh replay (covered by test; watch 401 spikes).

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**