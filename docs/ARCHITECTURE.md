# Griot — Architecture (Overall System)

> The single document that explains the **entire system**: how all seven systems fit together as one web, how requests flow, how it scales, how it's secured, and how it's operated. The per-system kits (`<system>/project-kit/`) hold the feature-level detail; this doc links them.

---

## 1. What Griot is

Griot is a project-management web app ("the accurate, shared record of what happened and what's next") with an AI copilot, built for the **GTP 2026 Bootcamp** on the bootcamp-exact stack. It ships **seven systems as one monorepo**:

| # | System | Folder | Weekly source | Role |
|---|---|---|---|---|
| 1 | Backend / API | `backend/` | Week 2 | Data + business rules + REST + GraphQL |
| 2 | Web | `web/` | Week 3 | Browser app (Public + App shells) |
| 3 | Mobile | `mobile/` | Week 4 | Android companion (Flutter) |
| 4 | DevOps / Infra | `infra/` | Week 5 | Docker/Compose, Vercel, Railway, CI/CD |
| 5 | Quality Engineering | `qa/` | Weeks 6–7 | Test lifecycle, gates, reporting |
| 6 | AI agents | `ai/` | [own-stack] | Trigger.dev v3 agents + Copilot |
| 7 | MCP server | `mcp/` | [own-stack] | External-AI access via MCP tools |

Each system is self-contained: its own `AGENTS.md`, `.agents/skills/`, `project-kit/` (context + feature-specs + diagrams + examples). **The root `AGENTS.md` + this doc + root `project-kit/context/` link them into one system.**

---

## 2. The big picture (one web)

```ascii
                        ┌──────────────────────────────────────────────────┐
                        │                    PUBLIC INTERNET               │
                        │   [Web browser]         [Mobile (Flutter)]       │
                        │         │                     │                  │
                        │         ▼                     ▼                  │
                        │   ┌──────────┐  realtime WS ┌─────────────────┐  │
                        │   │  Vercel  │<────────────>│ Trigger.dev v3  │  │
                        │   │ web app  │   (Copilot)  │  ai/ (agents)   │  │
                        │   └────┬─────┘              └────────┬────────┘  │
                        │        │ HTTPS REST+GraphQL          │ GraphQL   │
                        │        ▼                             │ service   │
                        │   ┌──────────────────────────────┐  │ token     │
                        │   │   Railway:  api (ASP.NET 8)  │<─┘           │
                        │   │   REST /api + GraphQL /graphql│              │
                        │   │   + mcp (Streamable HTTP)    │  service     │
                        │   └───┬──────────────────────────┘  token       │
                        │       │ EF Core + Dapper          ▲              │
                        │   ┌───▼───────────┐               │              │
                        │   │ SQL Server    │   Redis ◄─────┘ mcp tools    │
                        │   │ Postgres(aux) │   (rate/refresh/budget)      │
                        │   └───────────────┘                                │
                        │   [External AI clients] ──MCP──► mcp/server        │
                        └──────────────────────────────────────────────────┘
        CI/CD: GitHub Actions ──► Vercel / Railway / Trigger  (deploys)
        QA: xUnit · Jest+RTL · Flutter · Cypress · Newman · k6  (gates)
```

_Rule that never changes: **AI (`ai/` + `mcp/`) only talks to the backend API** via `GRIOT_SERVICE_TOKEN`. No system touches another's folder._

---

## 3. Request flows (how the system actually works)

### 3.1 Web read (dashboard/board)
1. Browser → Vercel (static assets + env).
2. JS → `POST /graphql` (Apollo) or `GET /api/...` (Axios) with access JWT in memory.
3. Backend middleware validates JWT → principal (`sub`, `email`, `jti`).
4. Controllers/resolvers call `Griot.Application` services → repos → SQL Server.
5. DataLoader batches `assignee`+`comments` (N+1 safe).
6. Response → cache (Apollo/TanStack) → UI. Web never talks to a DB.

### 3.2 AI copilot read/write
1. User asks Copilot in web → Trigger realtime WS → `ai/` agent.
2. Agent tool-calls → backend GraphQL with `GRIOT_SERVICE_TOKEN` + `X-On-Behalf-Of` → real-user **OBO** principal (role `ai-on-behalf-of`, 4 scopes, no deletes/invites).
3. Trivial lookups short-circuit; LLM only where needed; cost cap checked (Redis).
4. Writes: agent returns a **proposed action** → web renders approval card → user approves → **web app** calls REST → backend writes → caches update. The agent NEVER writes directly.
5. Agent operates a **Level 4 reasoning loop**: observes state, plans multi-step actions (e.g. "close overdue tasks, notify owners"), and requires human approval before executing destructive or multi-step writes. Also generates System Reports.
5. Every tool call → `ActivityLogs`; state-changing writes also → `AuditLogs` (traceable runId ↔ payloadHash ↔ audit row).

### 3.3 Scheduled AI (digest/reminders)
Trigger cron → agent → GraphQL (service token) → backend creates notifications. Durable + idempotent. Also triggers scheduled System Reports (digests) persisted to the database and accessible via the UI.

### 3.4 Mobile
Flutter app → same REST + GraphQL endpoints; refresh token in `flutter_secure_storage`; 401→refresh→retry-once.

### 3.5 External AI clients (MCP)
## 4. Data & storage

- **SQL Server 2022** (primary): 16 tables (13 core + `ApiLogs`, `ErrorLogs`, `AuditLogs`) + 5 enums. EF Core 8 (95% CRUD) + Dapper (2 procs). Full design: `docs/database/DATABASE-DESIGN.md`.
- **PostgreSQL 16** (secondary/test): cohort exercises.
- **Redis 7**: rate limiting, refresh-token/session metadata, AI token budgets. **Optimization phases:** Phase 1 adds dashboard summary caching (60s TTL); Phase 2 adds GraphQL response caching (board queries cached by `workspaceId + userId + timestamp` key, invalidated on writes).
- **Cloudinary (v1 — free tier)**: Attachment storage via `CloudinaryDotNet` (free tier ≈ 25 credits: 25 GB storage + 25 GB bandwidth/month). Media served via Cloudinary's CDN (`res.cloudinary.com`). **File limits:** 25 MB per file, 100 MB per workspace quota. **Migration path (Phase 3):** Documented migration to Cloudflare R2 when egress >100 GB/month (zero-egress pricing saves ~$50/month per TB vs Cloudinary's ~$0.50–1/GB-class overage). Decision rationale: free tier unblocks v1 launch immediately; R2 migration deferred until scale justifies effort.
- **Netdata monitoring (Phase 1)**: Per-second metrics with anomaly detection on all Railway nodes (API, DB, Redis). Tuned retention (7-day tier-0, 2 GB disk cap per node) prevents disk growth. Free Community tier covers 5 nodes with Slack alerting. See `infra/project-kit/feature-specs/07-netdata-monitoring.md`.

## 5. Auth & security (defense in depth)

| Layer | Control |
|---|---|
| Password | Argon2 (per-user salt) |
| 2FA      | Brevo Email OTP (hashed codes, 10-min expiry, rate-limited) |
| Access | JWT 15-min (sub/email/jti), signed; rejected bad iss/aud |
| Refresh | opaque 256-bit, SHA-256 at rest, **rotation + family-revoke on reuse** |
| Rate limit | Redis sliding window `/auth/login` + GraphQL query-cost guard |
| CORS | allow-list (Vercel origin) |
| AI | `GRIOT_SERVICE_TOKEN` + `X-On-Behalf-Of` → real-user OBO principal (no deletes/invites); HMAC webhook |
| Ownership | 404 not 403 (never disclose existence) |
| Audit | `AuditLogs` before/after snapshots on every state change; `ApiLogs`/`ErrorLogs` full trace |
| Secrets | .env git-ignored; runtime env injection; no keys in images |
| Supply chain | lockfiles per app; CI dependency audit (Week-6 gate) |

## 6. Scaling & capacity (the "how many users + how do I grow" doc)

Full treatment: `docs/planning/CAPACITY-PLAN.md` + `docs/planning/OPTIMIZATION-RECOMMENDATIONS.md`. Summary:

- **Baseline (current architecture, v1)** comfortably handles **~500–1,500 concurrent users** on Railway Hobby (1–2 backend instances) with p95 < 500 ms, given board-heavy reads and the index strategy. It handles far more with the optimizations below.
- **Bottlenecks today**: single SQL Server writer, per-process EF contexts, no read replicas, webhooks/poll for fan-out.
- **Optimization roadmap (3 phases):**
  - **Phase 1 (production blockers — ship before launch):** Cloudinary blob storage, Netdata monitoring, dashboard summary caching (Redis, 60s TTL), GraphQL DataLoader (N+1 prevention), API pagination caps (MaxPageSize = 1,000). Effort: 4–8 days. Impact: unblocks scale, p95 dashboard drops 400ms → 100ms.
  - **Phase 2 (post-k6 baseline — only if needed):** Missing database indexes (covering + partition-ready), GraphQL response caching (HotChocolate + Redis), read replica (SQL Server secondary). Trigger: p95 board reads >500ms or DAU >2–3k. Effort: 3–5 days.
  - **Phase 3 (post-bootcamp):** Cloudflare R2 migration (when egress >100 GB/month), Prometheus + Grafana (custom dashboards), mobile offline queue (`sqflite` persistence), web code splitting. Trigger: cost/scale justifies effort.
- **To scale to 10k+ / millions** (in order of ROI):
  1. Add indexes from the ERD + proc the dashboard (already designed).
  2. Move reads to a **read replica** (SQL Server secondary readable) + cache hot boards in Redis.
  3. Add a second Railway instance behind its load balancer (stateless API → ready).
  4. Introduce **Redis/response caching** at the GraphQL layer (HotChocolate in-memory + Redis).
  5. Move attachment blobs to S3/Cloudinary, **background jobs** for heavy writes, **CDC/events** for realtime fan-out (SignalR/WebSockets) instead of polling.
  6. At very large scale: CQRS, partitioned/event-sourced `ActivityLogs`, horizontal sharding, CDN for static.
- **Comparison vs alternatives**: EF Core + SQL Server scales well to mid-size with read replicas; the standard FAANG path is then Redis + queue (BullMQ/NATS) + replicas + CDN — not a rewrite. See `docs/planning/CAPACITY-PLAN.md` for numbers + tooling comparison.

## 7. Observability & operations

- **Netdata (Phase 1):** Per-second metrics with anomaly detection on all Railway nodes. Local dashboards at `:19999` per node + centralized Netdata Cloud view (free tier: 5 nodes). Tuned retention prevents disk growth (7-day tier-0, 2 GB cap/node). Slack alerting for CPU >80%, RAM >90%, disk >90%, API process down. See `infra/project-kit/feature-specs/07-netdata-monitoring.md`.
- **Application logging:** `ApiLogs`/`ErrorLogs`/`AuditLogs` give request tracing, error lifecycle, and change history (planned pre-implementation). Railway/Vercel logs for stdout/stderr.
- **Health checks:** `/health` endpoint (liveness + readiness) checks DB ping + Redis ping. Uptime ping (Better Uptime/UptimeRobot) → 60s polling.
- **Week-6/7 gates:** k6 load baseline (500 concurrent users, p95 <500ms), OWASP security review, monitoring docs (`docs/observability/MONITORING.md`).

## 8. Production readiness

- Docs: `README`, `LICENSE` (MIT), `CONTRIBUTING`, `SECURITY`, `CODE_OF_CONDUCT`, `CHANGELOG`, `docs/` (architecture, database, planning, deployment, seo, observability, api).
- Public-ready: SEO + sitemap + `llms.txt` for LLM/bot crawling (`docs/seo/SEO-DEPLOYMENT.md`).

---

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**
_Griot — the record of what the team built, and how well they built it._

## Implemented authentication contract (Feature 07)

Use the [auth contract](api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

## Scheduled AI identity (planned consumers)

Scheduled jobs read via GraphQL and write via supported REST using Bearer GRIOT_SERVICE_TOKEN plus X-On-Behalf-Of from the backend-stored authorized schedule. A live backend user/workspace/scope/expiry delegation and current membership are required; a cron task cannot pick an arbitrary user. Current scope vocabulary is ReadWorkspace/CreateTask/AddComment/CreateNotification; no synthetic system user, status-update or notification-create GraphQL mutation exists. Durable job recovery/callback processing is a backend 20 prerequisite.
