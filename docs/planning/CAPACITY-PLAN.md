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

**So: v1 comfortably serves ~1,500+ concurrent users** (far above demo needs) with 1–2 backend instances. The real limits are: single SQL Server writer, per-process EF contexts, no read replica yet, and polling fan-out — none of which matter at v1 scale.

## 2. Where the limits actually are (and why)

| Constraint | Current design | Breaks down around | Why |
|---|---|---|---|
| SQL Server writes | single primary | ~1–2k writes/s sustained (rough) | single-writer bottleneck |
| Board/dashboard reads | indexed queries + DataLoader | ~10k+ reads/s sustained | no read replica / cache |
| Fan-out (notifications) | poll/next-load + realtime for web | many clients churn | websocket/poll cost |
| Attachments | metadata only / local blob | large files + many | blob store needed |
| AI (LLM calls) | Trigger cloud + token budget | cost, not throughput | per-workspace budget |
| Process state | stateless API (good) | — | scales horizontally already |

## 3. Scaling path (in ROI order), with tools & why vs today

1. **Indexes + procs (already designed)** — free wins; ship first.
2. **Read replica** (SQL Server readable secondary) — split reads/writes; **tool**: Railway SQL Server add-on replica or Azure SQL; **why**: board reads dominate; removes the single-reader ceiling.
3. **Redis response caching** at GraphQL + dashboard — **tool**: HotChocolate + Redis cache; **why**: hot boards served from memory instead of SQL; **vs today**: we already have Redis for auth, so it's an extension, not a new dependency.
4. **Scale the API horizontally** — stateless already; add a 2nd Railway instance behind its load balancer; session/refresh live in Redis/DB so no sticky sessions.
5. **Async fan-out + events** — introduce a queue (BullMQ/NATS/Redis Streams) + SignalR/WebSockets for notifications; **why**: poll/next-load doesn't scale to "live" UX; **vs today**: Trigger.dev handles AI jobs; the queue adds generic background work.
6. **Blob storage for attachments** — S3/Cloudinary; keeps DB small; **vs today**: local/disk metadata-only.
7. **CQRS / event-sourcing for ActivityLogs** — only at large scale; partition by workspace/time; the log is append-mostly.

**Comparison table (new vs current):**

| Concern | Current (v1) | Scaling add-on | What it unlocks |
|---|---|---|---|
| Reads | SQL Server direct | Read replica + Redis cache | 10× read throughput |
| Writes | single writer | queue + partition (later) | 5–10× write throughput |
| Realtime | poll + web WS | SignalR + queue | live UX at scale |
| Attachments | disk/local | Cloudinary/S3 | unbounded, cheap storage |
| Orchestration | Trigger.dev | + generic queue | any background job |

**At what point each is worth it:** replica + cache when DAU > ~2–3k or p95 board > 500 ms; queue when background jobs grow past Trigger's fit; blobs when attachments matter; CQRS only at 6-figure DAU.

## 4. Capacity numbers (back-of-envelope)

- **ActivityLogs** write-heavy: 1 row per action. 1k users × 20 actions/day = 20k rows/day ≈ **600k/month** — prune/partition by month (retention in NFR).
- **TaskItems**: growth is modest (boards, not feeds): 1k users × 50 tasks/sprint ≈ 50k/month.
- **Redis memory**: session + rate + budget metadata ~ a few MB for 10k sessions (each refresh token ~200 bytes). Fine.
- **Railway tier**: Hobby 1–2 services comfortably; upgrade only when the baseline numbers are exceeded (NDR: don't over-engineer).
- **p95 latency budget**: dashboard summary < 500 ms, board < 500 ms, login < 300 ms, GraphQL read < 200 ms average (NFR doc).

## 5. Verdict

- **Now**: production-ready for cohort/demo + early public (~1,500 concurrent comfortably).
- **Next (post-bootcamp, in ROI order)**: read replica → Redis cache → queue/fan-out → blob store → CQRS. No rewrite needed at any step; the architecture was designed so each is additive.

---

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**