# Optimization Recommendations — Griot

> Comprehensive audit findings and prioritized optimization opportunities for efficiency, speed, and accuracy. Based on systematic review of capacity planning, architecture, ERD, API surface, and frontend patterns.

**Audit completed:** 2026-08-26  
**Scope:** Database, API, caching, frontend, storage, and monitoring infrastructure

---

## Executive Summary

**Current state:** Griot is designed for ~1,500 concurrent users with good latency targets (<500ms board/dashboard reads) but has critical gaps preventing production scale:
1. **Blob storage missing** — attachments use local/disk only; breaks at scale
2. **Monitoring minimal** — no comprehensive observability beyond /health endpoint
3. **Caching underutilized** — Redis only for rate limiting; no query or response caching
4. **Database indexes incomplete** — missing partition-ready and covering indexes
5. **GraphQL N+1 risk** — DataLoader planned but not documented in specs
6. **Frontend optimization gaps** — no Redis integration, no offline queue persistence strategy for mobile

**Priority:** Fix blob storage + monitoring **immediately** (both are production blockers); defer database/caching optimizations until k6 baseline proves they're needed.

---

## 1. CRITICAL: Blob Storage Integration

### Problem
- Attachments table has `StorageUrl` field but v1 implementation is **local/disk only**
- No file size limits documented in API specs
- Breaks at scale: Railway ephemeral filesystem + no CDN + no size caps = user-facing failures
- Missing from ERD constraints: no `SizeBytes` validation, no quota tracking per workspace

### Recommendation: **Blob Storage Strategy** (Vercel Blob → R2 migration path)

| Solution | Storage | Egress | Why |
|---|---|---|---|
| **Vercel Blob (v1)** | **1 GB FREE** + $0.023/GB overage | **10 GB/mo FREE** + $0.05/GB overage | Free tier covers demo/early prod; excellent DX; already on Vercel stack |
| **Cloudflare R2 (migration)** | **$0.015/GB** | **$0 (FREE)** | Zero-egress = massive savings at scale; S3-compatible API; migrate when egress >100 GB/month |
| AWS S3 | $0.023/GB | $0.09/GB | Industry standard but 6× more expensive egress than R2 |
| Azure Blob | $0.023/GB | $0.087/GB | Similar to S3; no advantage for this workload |

**Math (10 TB/month egress at 1,500 users):**
- R2: $150 storage + $0 egress = **$150/month**
- Vercel Blob: $230 storage + $500 egress = **$730/month**
- S3: $230 storage + $900 egress = **$1,130/month**

**Decision:** Start with **Vercel Blob free tier** (leveraging existing Vercel account: 1 GB storage + 10 GB transfer/month included at no cost = $0); migrate to **R2 for production** when egress consistently exceeds 100 GB/month (cost savings of $580/month become significant). See Phase 1/Phase 3 roadmap in CAPACITY-PLAN.md.

### Implementation checklist
- [ ] Add `MaxFileSizeBytes` constant (25 MB per attachment, 100 MB workspace quota)
- [ ] Update `AttachmentService` to use Vercel Blob SDK (`@vercel/blob`)
- [ ] Migrate `StorageUrl` semantics: local paths → public blob URLs
- [ ] Add file-type validation (allow images/docs, block executables)
- [ ] Create migration script: local files → blob storage (one-time, pre-production)
- [ ] Update API docs: `POST /api/tasks/{id}/attachments` rate limits + size caps
- [ ] Add blob observability: track storage size + transfer via Vercel dashboard

**Effort:** 2–3 days (backend integration + migration script)  
**Impact:** **Blocks production scale** — must ship before public launch  
**Owner:** backend system (feature spec required)

---

## 2. CRITICAL: Netdata Monitoring Integration

### Problem
- Current monitoring is **minimal**: /health endpoint, uptime ping, Railway/Vercel logs
- No per-second metrics, no anomaly detection, no per-process resource tracking
- Netdata presentation shows: already deployed agents have **disk-usage issue** (unbounded dbengine growth)
- No visibility into SQL Server query performance, Redis ops/sec, or API p95 latency distribution

### Netdata findings (from R&D presentation)

**What it provides:**
- **Per-second metrics** (not 5-min averages) — catches short spikes others miss
- **800+ auto-detected collectors** — databases, containers, web servers, hardware sensors
- **Zero-config anomaly detection** (unsupervised ML) — flags abnormal metrics before manual review
- **Local dashboard** at `:19999` on each node — no external dependencies
- **Free tier:** unlimited nodes with local storage, 5 nodes on Netdata Cloud, 90-day alert history

**Known drawback (already hit in production):**
- **Disk growth** — per-second history for every metric grows unbounded by default
- **Fix (5 min config):**
  ```ini
  # /etc/netdata/netdata.conf
  [db]
      dbengine tier 0 retention = 604800  # 7 days (down from 14d default)
      dbengine multihost disk space MB = 2048  # hard cap per node
  ```

### Recommendation: **Deploy Netdata immediately** with tuned retention

**Deployment targets:**
- Railway API nodes (ASP.NET + SQL Server + Redis)
- Railway database nodes (SQL Server standalone if separate)
- Container-heavy hosts (stream metrics to parent node instead of storing locally)

**Configuration (apply fleet-wide):**
- Set tier-0 retention to 7 days (tier-1 monthly, tier-2 yearly)
- Cap dbengine disk space to 2 GB per node
- Disable unused collectors (trim `go.d.conf`/`python.d.conf` for generic hosts)
- Wire alerts to Slack/email via `health_alarm_notify.conf`

**Comparison vs Prometheus + Grafana:**
| | Netdata | Prometheus + Grafana |
|---|---|---|
| **Setup** | Zero-config, live in minutes | Manual exporters + scrape config |
| **Resolution** | Per-second, out of the box | 15–60s typical |
| **Model** | Agent-based (local storage) | Central pull + TSDB |
| **Dashboards** | Auto-built (less customizable) | Fully custom (requires Grafana setup) |
| **Best for** | Instant node-level troubleshooting | Centralized querying at scale |

**Decision:** Use **Netdata for per-node instant visibility**; defer Prometheus+Grafana until we need cross-team custom dashboards (post-bootcamp).

### Implementation checklist
- [ ] One-line install on Railway API + DB nodes: `sh https://get.netdata.cloud/kickstart.sh`
- [ ] Tune retention + disk caps (`/etc/netdata/netdata.conf`)
- [ ] Claim nodes to Netdata Cloud (free tier: 5 nodes)
- [ ] Configure alerts (CPU, RAM, disk, SQL slow queries, Redis ops) → Slack
- [ ] Document dashboard sections for on-call: System Overview, Applications, Disks, Alarms

**Effort:** 1 day (install + config + documentation)  
**Impact:** **High** — catches production issues before users report them  
**Owner:** infra system (feature spec required)

---

## 3. Database Optimization Opportunities

### Current state
- 19 indexes from ERD (all FKs + hot-path queries)
- 2 stored procedures (`usp_BulkUpdateTaskStatus`, `usp_GetDashboardSummary`)
- Single SQL Server writer (no read replicas)
- No query performance monitoring beyond Railway metrics

### Gaps identified

#### 3.1 Missing indexes
| Index | Purpose | Why it matters |
|---|---|---|
| `ActivityLogs(CreatedAt DESC)` | Partition-ready for 90-day pruning | Feed queries + monthly archive jobs |
| `TaskItems(BoardId, ColumnId, Position) INCLUDE (Title, Status, AssigneeId)` | Covering index for board reads | Eliminates key lookups on dashboard/board queries |
| `ApiLogs(CreatedAt DESC)` | Time-range queries for observability | P95 latency analysis + request tracing |
| `ErrorLogs(FixStatus, CreatedAt)` | Filtered scans for open/investigating errors | Error dashboard + automated alerting |

**Recommendation:** Add these indexes **after k6 baseline** (Week 6) — only if p95 board reads exceed 500ms under load. Don't over-index pre-launch.

#### 3.2 Read replica (deferred)
- **When:** DAU > 2–3k or p95 board reads > 500ms despite indexes
- **How:** Railway SQL Server add-on (readable secondary) or Azure SQL geo-replica
- **Benefit:** 10× read throughput (split reads/writes)
- **Cost:** ~$50–200/month (Railway) or ~$100–500/month (Azure SQL Business Critical)

**Decision:** Defer until k6 proves single-writer is the bottleneck.

#### 3.3 Stored procedure candidates (low priority)
- Current procs cover dashboard + bulk-status (the two heaviest queries)
- **Candidate:** `usp_GetBoardWithTasks(@BoardId)` — single round-trip for board + columns + tasks + assignees
- **Benefit:** Eliminates N+1 if DataLoader fails; reduces latency by ~50–100ms
- **Effort:** 2–3 hours (write proc + wire up Dapper call)

**Decision:** Implement **only if** DataLoader proves insufficient (Week 3 GraphQL integration).

---

## 4. Caching Strategy Enhancements

### Current state
- Redis 7 used for: rate limiting, refresh tokens, AI budget tracking
- **No query caching, no response caching, no computed-data caching**
- Apollo `InMemoryCache` (web) + TanStack Query cache (web) — client-side only
- Mobile has no documented offline-queue persistence strategy

### Opportunities

#### 4.1 GraphQL response caching (HotChocolate + Redis)
- **What:** Cache board/dashboard queries by `(workspaceId, userId, timestamp)` key
- **Benefit:** Hot boards served from Redis (~1ms) instead of SQL (~50–200ms)
- **Invalidation:** On task create/update/move, purge affected board keys
- **Configuration:**
  ```csharp
  services.AddGraphQLServer()
      .AddQueryCachePipeline()
      .AddRedisQueryStorage(sp => sp.GetRequiredService<IConnectionMultiplexer>());
  ```
- **Cost:** Redis memory ~ 1–5 MB per cached board × 100 hot boards = 100–500 MB

**Recommendation:** Add this **after k6 baseline** if p95 GraphQL reads > 200ms.

#### 4.2 Dashboard summary caching
- Current: `usp_GetDashboardSummary` runs on every dashboard load (hundreds/day per user)
- **Optimization:** Cache summary in Redis with 60s TTL, purge on workspace-level writes
- **Benefit:** 90%+ cache-hit ratio → dashboard p95 drops from 400ms → 50ms
- **Implementation:**
  ```csharp
  var cacheKey = $"dashboard:summary:{workspaceId}";
  var cached = await redis.StringGetAsync(cacheKey);
  if (cached.HasValue) return JsonSerializer.Deserialize<DashboardSummary>(cached);
  var summary = await db.QueryAsync<DashboardSummary>("usp_GetDashboardSummary", ...);
  await redis.StringSetAsync(cacheKey, JsonSerializer.Serialize(summary), TimeSpan.FromSeconds(60));
  return summary;
  ```

**Recommendation:** Implement **Week 3** (low-hanging fruit, high impact).

#### 4.3 Mobile offline queue persistence
- **Current gap:** NFR.md §9 says "queued writes persist until success or explicit discard" but no implementation strategy documented
- **Options:**
  1. `sqflite` (local SQLite) — store pending writes with `idempotencyKey`, replay on reconnect
  2. `shared_preferences` (small JSON) — simpler but not transactional
  3. Riverpod `StateNotifier` + file persistence (custom serialization)

**Recommendation:** Use `sqflite` for offline queue (transactional, indexed, queryable). Document in `mobile/project-kit/feature-specs/XX-offline-queue.md`.

---

## 5. API Performance Bottlenecks

### Current state
- REST + GraphQL call same services (no drift)
- 2 stored procedures (bulk-status, dashboard)
- **GraphQL DataLoader planned** but not documented in specs

### Gaps identified

#### 5.1 GraphQL N+1 prevention (DataLoader)
- **Risk:** `Task.assignee` and `Task.comments` queries trigger N+1 without DataLoader
- **Symptom:** Board with 50 tasks = 1 board query + 50 assignee queries + 50 comment queries = 101 DB round-trips
- **Fix:** HotChocolate DataLoader batches per-request:
  ```csharp
  public class AssigneeDataLoader : BatchDataLoader<Guid, User>
  {
      protected override async Task<IReadOnlyDictionary<Guid, User>> LoadBatchAsync(
          IReadOnlyList<Guid> keys, CancellationToken ct)
      {
          var users = await _userRepo.GetByIdsAsync(keys, ct);
          return users.ToDictionary(u => u.Id);
      }
  }
  ```

**Recommendation:** Document DataLoader in `backend/project-kit/context/graphql-surface.md` + implement **Week 2 Feature 04** (GraphQL schema).

#### 5.2 Bulk endpoints (complete)
- `PATCH /api/tasks/bulk-status` — ✅ atomic TVP proc, 409 on invalid ID
- **No other bulk endpoints needed** for v1 (moves are single-task, comments/attachments are append-only)

#### 5.3 Pagination (designed, not validated)
- Tasks: `(ColumnId, Position)` stable order + skip/take
- ActivityLogs: `(CreatedAt DESC)` + cursor or skip/take
- **Gap:** No documented page size caps (risk: client requests 10,000 tasks → OOM)

**Recommendation:** Add `MaxPageSize` constant (default 100, max 1,000) + validation middleware.

---

## 6. Frontend Optimization Opportunities

### Web (React 18 + Vite + MUI)

#### 6.1 Apollo cache integration with Redis (backend)
- **Current:** Apollo `InMemoryCache` client-side only
- **Enhancement:** Wire GraphQL response caching (backend Redis) so Apollo cache is pre-warmed on first load
- **Benefit:** First-paint board render 200–300ms faster

**Recommendation:** Defer until backend Redis caching is proven (see §4.1).

#### 6.2 Code splitting + lazy loading
- **Current gap:** No documented strategy in `web/project-kit/`
- **Recommendation:** Lazy-load routes:
  ```tsx
  const Dashboard = lazy(() => import('./features/dashboard/DashboardPage'));
  const Board = lazy(() => import('./features/boards/BoardPage'));
  ```
- **Benefit:** Initial bundle ~40% smaller (dashboard/board screens aren't blocking login)

**Effort:** 2–3 hours  
**Impact:** Medium (improves LCP for public shell)

#### 6.3 Image optimization
- **Current:** No documented strategy for avatars, board thumbnails, or public hero images
- **Recommendation:** Use `next/image` patterns (Vercel Image Optimization) or Cloudinary for avatars
- **Benefit:** Automatic WebP/AVIF + responsive sizes + lazy loading

**Recommendation:** Defer until blob storage is integrated (avatar URLs will change).

### Mobile (Flutter 3.19+)

#### 6.4 Offline queue persistence (see §4.3)
- **Critical gap:** NFR.md promises "queued writes persist until success or discard" but no implementation strategy
- **Recommendation:** Document + implement `sqflite` queue in `mobile/project-kit/feature-specs/XX-offline-queue.md`

#### 6.5 Battery-aware sync strategy (designed)
- **Current:** "refresh-on-focus + pull-to-refresh" (no always-on socket)
- **Validation:** Document WorkManager + battery optimization strategy for Android background tasks

**Recommendation:** Verify Android `WorkManager` constraints in feature spec (Week 4).

---

## 7. Prioritized Roadmap

### Phase 1: Production blockers (ship before public launch)
| # | Item | Effort | Impact | Owner |
|---|---|---|---|---|
| 1 | Blob storage integration (Vercel Blob → R2 later) | 2–3 days | **Critical** (blocks scale) | backend |
| 2 | Netdata monitoring deployment + config | 1 day | **High** (observability) | infra |
| 3 | Dashboard summary caching (Redis + 60s TTL) | 4 hours | **High** (UX improvement) | backend |
| 4 | GraphQL DataLoader documentation + implementation | 1 day | **High** (prevents N+1) | backend |
| 5 | API pagination caps (`MaxPageSize` validation) | 2 hours | **Medium** (prevents abuse) | backend |

### Phase 2: Post-k6 baseline (only if needed)
| # | Item | Trigger | Effort | Owner |
|---|---|---|---|---|
| 6 | Missing database indexes (covering + partition-ready) | p95 board > 500ms | 1 day | backend |
| 7 | GraphQL response caching (HotChocolate + Redis) | p95 GraphQL > 200ms | 1 day | backend |
| 8 | Read replica (SQL Server secondary) | DAU > 2–3k or p95 > 500ms despite indexes | 2 days | infra |

### Phase 3: Post-bootcamp enhancements
| # | Item | Why deferred | Effort | Owner |
|---|---|---|---|---|
| 9 | Migrate Vercel Blob → Cloudflare R2 | Cost optimization (>100 GB/month egress) | 1 day | backend |
| 10 | Prometheus + Grafana for custom dashboards | Need cross-team observability | 3–5 days | infra |
| 11 | Mobile offline queue (`sqflite` persistence) | v1 has minimal offline tolerance | 2 days | mobile |
| 12 | Web code splitting + lazy routes | LCP already <2.5s (Lighthouse target) | 3 hours | web |

---

## 8. Cost-Benefit Analysis

### Blob storage (Vercel Blob free tier vs R2)
**Assumptions:** 1,500 users, 50 MB avg attachments per user, 10 TB/month egress at production scale

| Solution | Storage cost | Egress cost | **Total/month** | Notes |
|---|---|---|---|
| **Vercel Blob (v1 — free tier)** | $0 (1 GB free) + $1.70 (74 GB × $0.023) | $0 (10 GB free) | **~$2/month** | Storage exceeds free 1 GB by 74 GB |
| **Vercel Blob (prod scale — 10 TB egress)** | $1,725 (75 GB × $0.023) | $500 (10 TB × $0.05) | **$2,225/month** | Egress becomes dominant cost |
| **Cloudflare R2 (prod scale)** | $1,125 (75 GB × $0.015) | $0 (FREE) | **$1,125/month** | Zero-egress pricing |
| **Savings (R2 vs Vercel at scale)** | $600 | $500 | **$1,100/month (49%)** | Migration justified at >2 TB/month egress |

**Recommendation:** Start Vercel Blob free tier (minimal cost for v1), migrate to R2 when monthly egress consistently exceeds 100 GB (break-even: ~$5/month Vercel egress cost vs migration effort).

### Monitoring (Netdata free vs paid)
**Netdata Community (free):**
- Unlimited local agents
- 5 nodes on Netdata Cloud
- 90-day alert history
- Community support only

**Netdata Business (~$4.50/node/month):**
- Unlimited nodes on Cloud
- Metric Correlations + AI Co-SRE root-cause
- Priority support + SLA

**Recommendation:** Start **free tier** (5 Railway nodes = API + DB + Redis + worker + staging); trial Business on production-critical nodes only if AI root-cause proves valuable.

### Caching (Redis response cache)
**Assumptions:** 100 hot boards × 2 MB each = 200 MB Redis memory

| Resource | Cost | Benefit |
|---|---|---|
| Redis memory (+200 MB) | ~$5–10/month (Railway Redis scale-up) | p95 board reads: 200ms → 10ms |
| Engineering effort | 1 day (HotChocolate + invalidation logic) | 90%+ cache-hit ratio |

**ROI:** High (low cost, massive latency improvement). Implement after k6 baseline confirms need.

---

## 9. Measurement & Validation

### Success metrics (post-implementation)
| Optimization | Metric | Baseline | Target |
|---|---|---|---|
| Blob storage | Attachment upload success rate | N/A (local-only) | >99.9% |
| Netdata | Mean time to detect (MTTD) incidents | Unknown | <5 min |
| Dashboard caching | p95 dashboard summary latency | 400ms (est.) | <100ms |
| GraphQL DataLoader | p95 board read latency | 500ms (target) | <300ms |
| Missing indexes | p95 ActivityLogs query latency | Unknown | <200ms |

### K6 baseline requirements (Week 6)
- **Before any database/caching optimizations**, run k6 load tests:
  - 500 concurrent users, 5 req/s/user for 10 minutes
  - Measure: p50/p95/p99 latency, error rate, SQL query time, Redis ops/sec
  - **Gate:** p95 board < 500ms, dashboard < 500ms, GraphQL < 200ms
- **If gates fail**, apply Phase 2 optimizations (indexes, caching, replica) in priority order

### Monitoring dashboards (post-Netdata)
- **System Overview:** CPU, RAM, load average (first stop for "is this healthy")
- **Applications:** per-process CPU/RAM/disk I/O (answers "which process is eating resources")
- **Disks:** I/O utilization, space, Netdata DB usage (catch storage growth early)
- **Alarms:** every active alert across all nodes (single source of truth)

---

## 10. Open Questions & Risks

### Blob storage
- **Q:** Should we support client-side direct uploads (presigned URLs) or only server-proxied?
- **A:** Start server-proxied (simpler auth); add presigned URLs if upload latency > 2s on large files

### Monitoring
- **Q:** Do we need Prometheus + Grafana in addition to Netdata?
- **A:** Not for v1. Netdata covers per-node instant visibility; Prometheus is for cross-team custom dashboards (defer post-bootcamp)

### Caching
- **Q:** Should Apollo cache be persisted to `localStorage` for offline-first web?
- **A:** Not for v1 (web is online-first). Mobile gets offline queue via `sqflite`; web can add `apollo-cache-persist` post-bootcamp

### Database
- **Q:** When do we partition `ActivityLogs` by month?
- **A:** When 90-day pruning job takes >5 min or ActivityLogs >10M rows (unlikely at 1,500 users)

---

## 11. References

- **Capacity planning:** `docs/planning/CAPACITY-PLAN.md`
- **NFR (latency targets):** `docs/planning/NFR.md`
- **Database design:** `docs/database/DATABASE-DESIGN.md`
- **API surface:** `backend/project-kit/context/api-surface.md`
- **Netdata presentation:** `research/Netdata_RD_Presentation.pptx` (16 slides, R&D session)
- **Vercel Blob docs:** https://vercel.com/docs/storage/vercel-blob/usage-and-pricing
- **Cloudflare R2 pricing:** https://cloudflare.com/products/r2/ ($0.015/GB storage, $0 egress)
- **HotChocolate caching:** https://chillicream.com/docs/hotchocolate/v13/performance/query-caching

---

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**
