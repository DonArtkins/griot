# Capacity Plan & Scaling — Griot

> The honest "how many users can my system handle, why, and how do I grow it" answer for the **current architecture as designed** — no hypothetical millions. Written pre-implementation so the numbers constrain the design, not the other way around.

---

## 1. Grounded baseline (v1, as architected)

**Assumed load (denominator for everything):**
- Cohort/demo + early public: **~100–300 daily active users (DAU)**, ~20–60 concurrent at peak.
- Capstone demo day UK/EU cohort: same order.
- Board-heavy read workload: each user opens dashboard + board on login, moves tasks, comments — maybe **1–3 req/s** average, **10–30 req/s** at peak for the cohort.

**What the architecture provides at that load:**
- One backend instance (Railway, 0.5–1 vCPU): ASP.NET Core 8 easily sustains **hundreds of req/s** for board/dashboard reads with the designed indexes (board query p95 < 500 ms target).
- SQL Server 2022 in the same Railway network: **thousands of simple indexed reads/s**; the write path (task create/move) is small.
- Redis absorbs rate-limit + session + budget reads: trivially handles cohort scale.
- Vercel edge serves static web with CDN.

**So: v1 targets ~1,500+ concurrent users** (far above demo needs) with 1–2 backend instances — this is a **design target pending verification**; the system is architected to support this load, but the capacity claim requires reproducible k6 load evidence meeting p95 latency, error-rate, SQL, Redis, and CPU criteria (see qa/ gates and acceptance tests). The real limits are: single SQL Server writer, per-process EF contexts, no read replica yet, and polling fan-out — none of which matter at v1 scale.

## 2. Where the limits actually are (and why)

| Constraint | Current design | Breaks down around | Why |
|---|---|---|---|
| SQL Server writes | single primary | ~1–2k writes/s sustained (rough) | single-writer bottleneck |
| Board/dashboard reads | indexed queries + DataLoader | ~10k+ reads/s sustained | no read replica / cache |
| Fan-out (notifications) | poll/next-load + realtime for web | many clients churn | websocket/poll cost |
| Attachments | metadata only / local blob | large files + many | blob store needed |
| AI (LLM calls) | Trigger cloud + token budget | cost, not throughput | per-workspace budget |
| Process state | stateless API (good) | — | scales horizontally already |

## 3. Scaling path (3-phase optimization roadmap)

See `docs/planning/OPTIMIZATION-RECOMMENDATIONS.md` for full details, cost-benefit analysis, and measurement strategy.

### Phase 1: Production blockers (ship before public launch)
**Effort:** 4–8 days | **Trigger:** Must complete before launch

1. **Vercel Blob storage** — Replace local/disk attachments with Vercel Blob (free tier: 1 GB storage + 10 GB transfer/month). 25 MB file limit, 100 MB workspace quota. See `backend/project-kit/feature-specs/11-blob-storage-integration.md`.
2. **Netdata monitoring** — Per-second metrics with anomaly detection on all Railway nodes. Tuned retention (7-day tier-0, 2 GB disk cap/node) prevents growth. Free tier covers 5 nodes. See `infra/project-kit/feature-specs/07-netdata-monitoring.md`.
3. **Dashboard summary caching** — Redis cache with 60s TTL. **Impact:** p95 dashboard 400ms → 100ms. **Effort:** 4 hours.
4. **GraphQL DataLoader** — Batch assignee + comment queries to prevent N+1. **Impact:** Board with 50 tasks: 101 queries → 3 queries. **Effort:** 1 day.
5. **API pagination caps** — `MaxPageSize = 1,000` validation middleware. **Impact:** Prevents abuse (client requests 10k tasks → OOM). **Effort:** 2 hours.

### Phase 2: Post-k6 baseline (conditional — only if needed)
**Trigger:** k6 evidence showing p95 latency targets missed after Phase 1 optimizations | **Effort:** 3–5 days

1. **Database indexes** — Covering index for board reads (`TaskItems(BoardId, ColumnId, Position) INCLUDE (...)`), partition-ready indexes for time-range queries. **Impact:** 200ms → <100ms p95 board reads.
2. **GraphQL response caching** — HotChocolate + Redis. Cache board queries by `(boardId, workspaceId, userId, timestamp)` key. **Impact:** Hot boards served from Redis (~1ms) instead of SQL (~50–200ms). 90%+ cache-hit ratio expected.
3. **Read replica** — SQL Server readable secondary (Railway or Azure). Route reads to replica, writes to primary. **Impact:** 10× read throughput. **Cost:** ~$50–200/month.

### Phase 3: Post-bootcamp enhancements (deferred)
**Trigger:** Cost/scale justifies effort | **Effort:** varies

1. **Cloudflare R2 migration** — When egress >100 GB/month. Zero-egress pricing saves ~$50/month per TB vs Vercel Blob. **Effort:** 1 day (S3-compatible API swap).
2. **Prometheus + Grafana** — Custom cross-team dashboards. **Trigger:** Need centralized querying at scale. **Effort:** 3–5 days.
3. **Mobile offline queue** — `sqflite` persistence for queued writes. **Trigger:** v1 offline tolerance insufficient. **Effort:** 2 days.
4. **Web code splitting** — Lazy-load dashboard/board routes. **Impact:** Initial bundle ~40% smaller. **Effort:** 3 hours.

**Comparison table (Phase 1 vs Phase 2 vs Phase 3):**

| Concern | Current (v1) | Phase 1 (prod blockers) | Phase 2 (post-k6) | Phase 3 (post-bootcamp) |
|---|---|---|---|---|
| Attachments | ❌ local/disk (breaks) | ✅ Vercel Blob (free tier) | — | Cloudflare R2 (zero egress) |
| Monitoring | ❌ /health only | ✅ Netdata (per-second) | — | + Prometheus/Grafana |
| Dashboard p95 | ~400ms (uncached) | ✅ <100ms (Redis 60s) | — | — |
| Board reads p95 | ~300ms (baseline) | ✅ <200ms (DataLoader) | <100ms (indexes + cache) | — |
| Pagination | ❌ no caps (abuse risk) | ✅ MaxPageSize = 1k | — | — |
| Read throughput | ~1k reads/s (estimate) | ~1k reads/s | ~10k reads/s (replica) | — |
| Mobile offline | ❌ no persistence | — | — | ✅ sqflite queue |

**At what point each is worth it:**
- **Phase 1:** Immediate (production blockers).
- **Phase 2:** Only after k6 baseline proves p95 >500ms despite Phase 1 optimizations.
- **Phase 3:** When cost (R2 egress savings) or scale (10k+ users) justifies effort.

## 4. Capacity numbers (back-of-envelope)

- **ActivityLogs** write-heavy: 1 row per action. 1k users × 20 actions/day = 20k rows/day ≈ **600k/month** — prune/partition by month (retention in NFR).
- **TaskItems**: growth is modest (boards, not feeds): 1k users × 50 tasks/sprint ≈ 50k/month.
- **Redis memory:**
  - **Phase 1 (baseline):** session + rate + budget metadata ~ a few MB for 10k sessions (each refresh token ~200 bytes). Dashboard summary cache: 100 hot workspaces × ~50 KB each = **5 MB**. **Total: ~10 MB**.
  - **Phase 2 (GraphQL caching):** Add 100 hot boards × 2 MB each = **200 MB**. **Total: ~210 MB** (still fits comfortably in Railway Hobby Redis).
- **Vercel Blob storage:** Phase 1 uses free tier (1 GB storage + 10 GB transfer/month). At 1,500 users with 50 MB avg attachments per user: **75 GB storage** (exceeds free tier by 74 GB × $0.023/GB = **$1.70/month**). Transfer at 10% download rate: 7.5 GB/month (within free 10 GB). **Cost: ~$2/month**.
- **Railway tier**: Hobby 1–2 services comfortably; upgrade only when the baseline numbers are exceeded (NDR: don't over-engineer).
- **p95 latency budget:** dashboard summary < 500 ms (Phase 1: <100ms with Redis cache), board < 500 ms (Phase 1: <200ms with DataLoader; Phase 2: <100ms with indexes + cache), login < 300 ms, GraphQL read < 200 ms average (NFR doc).

## 5. Verdict

- **Now**: architecture targets cohort/demo + early public (~1,500 concurrent) — **these are design targets until k6 load evidence confirms them** (QE week baseline + regression tests required before claiming production-ready capacity).
- **Next (phased roadmap)**: **Phase 1** (production blockers): Vercel Blob + dashboard caching + Netdata monitoring. **Phase 2** (post-k6 conditional): database indexes + GraphQL caching + read replica. **Phase 3** (post-bootcamp): R2 migration + code splitting + offline queue. No rewrite needed at any step; the architecture was designed so each is additive. See `OPTIMIZATION-RECOMMENDATIONS.md` for full roadmap.

---

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**