# Dependency Audit — Feature Spec Implementation Order

**Generated:** 2026-09-08 · **Updated:** 2026-09-11
**Purpose:** System-wide audit of all feature spec dependencies to ensure correct implementation order and prevent dependency violations.

> **2026-09-10:** This file audits *within-system* spec ordering. The **cross-system layer order** (which system's which spec comes next, and why) is now canonical in **`docs/planning/IMPLEMENTATION-ROADMAP.md`** — P0 backend 09→20→18→19→22→21→23→11→28→24→25→26→27→10 (2026-09-10 observability/hardening wave + 2026-09-11 AI-superpowers/OTP/BI wave inserted; rationale: `docs/observability/LOGGING-AUDIT-REPORT.md` §3 + ADR-004) → P1 web 01–09 → P2 ai 01–02→web 10→ai 03–12 (web 11/12 inserted at their dependency points) → P3 mobile → P4 infra → P5 mcp (06) → P6 qa. It also resolves the web 10 ↔ ai 02 circular reference (see the roadmap P2 section and the build-order note added to `ai/project-kit/feature-specs/02-copilot-agent-streaming.md`).

> **2026-09-11:** New PLANNED specs — backend **23** (critical-action OTP/step-up), backend **24** (AI reports & export surface + FIFTH OBO scope `CreateReport`), backend **25** (role-tiered log access + AI capability gateway), backend **26** (AI memory & conversations), backend **27** (incident alerting + confirmed broadcasts), backend **28** (project lifecycle report evidence), ai **06–12** (knowledge+auditor · reports PDF/CSV · advanced executor · agentic BI copilot · SuperAdmin ops agent · institutional memory · assignment suggestions), web **11–12** (Reports & Audit Center, AI Workspace sidebar), mcp **06** (v2 report/audit tools). Roadmap P0: … → 21 → 23 → 11 → 28 → 24 → 25 → 26 → 27 → 10; P2 adds ai 06 → ai 07 → web 11 → ai 08 → ai 09 → web 12 → ai 10–12; P5 adds mcp 06. No AI spec touches auth/OTP (backend 23).

## Current audit — 2026-09-11

Canonical P0: backend 09 → 20 → 18 → 19 → 22 → 21 → 23 → 11 → 28 → 24 → 25 → 26 → 27 → 10. Canonical P2: ai 01 → ai 02 → web 10 → ai 03 → ai 04 → ai 05 → ai 06 → ai 07 → web 11 → ai 08 → ai 09 → web 12 → ai 10 → ai 11 → ai 12. Spec counts: backend 28, web 12, mobile 7, infra 7, qa 13, ai 12, mcp 6 (85 total). New memory/evidence/assignment plans are unimplemented and require design approval. Backend 20 remains next after backend 09 review. The September 11 planning records are not backdated; historical September 8–10 notes below describe those earlier snapshots.

The AI 06/07 → web 11, AI 09 → web 12 and backend → AI/UI dependencies were incorrectly bidirectional. Owner specs now list downstream systems as consumers; web 11 precedes ai 08, and web 12 precedes ai 10–12.

## Critical Findings

### ✅ **Backend: Specs 07 and 08 — Ordering Violation Found and Fixed by ID Swap**

**Original Finding:** Backend **Feature 07 (API testing — Postman)** depended on **Feature 08 (Auth: JWT + Argon2 + Redis)**, yet 07 was numbered before 08. Following plain numbering would build the Postman collection before its auth endpoints exist.

**Resolution — spec IDs physically swapped (2026-09-08, branch `fix/backend/swap-specs-07-08`):**
- **07 = Auth: JWT + Argon2 + Redis** ← implement FIRST (file: `07-auth-jwt-argon2-redis.md`)
- **08 = API testing (Postman)** ← implement AFTER 07 (file: `08-api-testing-postman.md`)
- All ~65 references across all 7 systems updated atomically in this branch.

**Canonical next-step rule (review-fixed 2026-09-10):** superseded — 08 is ✅ and 09 is ✅ (delivered on `feature/backend/09-ai-service-token-and-webhooks`). **Next spec after user review: backend 20** (observability pipeline) on `feature/backend/20-observability-logging-pipeline`.

---

## System-by-System Dependency Analysis

### Backend (`backend/project-kit/feature-specs/`)

| Spec | Title | Dependencies | Order OK? |
|------|-------|--------------|-----------|
| 01 | ERD & schema design | Week-1 design, Figma Make | ✅ Yes (no code deps) |
| 02 | SQL Server EF Core | 01 (ERD) | ✅ Yes |
| 03 | Stored procedures | 02 (schema) | ✅ Yes |
| 04 | REST APIs | 02, 03 (domain + Dapper) | ✅ Yes |
| 05 | GraphQL layer | 04 (service layer) | ✅ Yes |
| 06 | Bulk operations | 03, 04 (procs + routes) | ✅ Yes |
| 07 | Auth: JWT + Argon2 | 04, 02 (controllers + table) | ✅ Yes |
| 08 | API testing (Postman) | 04-07 (routes + auth) | ✅ Yes (after 07) |
| 09 | AI service token | 07 (auth middleware) | ✅ Yes (after 07) |
| 10 | API documentation | 04-07, 08, **18-27** (routes + Postman + hardened surface) | ✅ Yes (last) |
| 11 | Blob storage | None listed | ✅ Yes (independent) |
| 12 | Communication (Brevo, Email-only) | 07 (auth), Redis | ✅ Yes (done) |
| 18 | Search/filter/pagination/sorting | 04-08, 13-17 (list endpoints) | ✅ Yes (after 20 in P0 order) |
| 19 | Caching & rate limiting | 18 (query contract), 07 (Redis) | ✅ Yes (after 18) |
| 20 | Observability pipeline | 02 (log tables exist), 04-08 (write paths) | ✅ Yes (**first hardening spec after 09**) |
| 21 | DB triggers + backups | 02 (schema), 20 (AuditLogs dedupe) | ✅ Yes (after 20) |
| 22 | Notification fan-out | 16 (routes), 12 (email), 20 (event hooks) | ✅ Yes (after 20) |
| 23 | Critical-action OTP & step-up | 07 (OTP), 12 (email), 20 (audit), 16 (guarded routes) | ✅ Yes (after 21) |
| 24 | AI reports & export surface | 07/09 (auth/OBO), 11 (blob), 16/20 (reads/audit), 23 (AI boundary), 28 (evidence) | ✅ Yes (after 11 + 20) |
| 25 | Role-tiered log access + AI capability gateway | 09 (OBO), 20 (logs), 24 (audit-summary) | ✅ Yes (after 24) |
| 26 | AI memory & conversation surface | 16 (reads), 20 (audit), 25 (tiers) | ✅ Yes (after 25) |
| 27 | Incident alerting & confirmed notices | 12, 19/20, 22/23/25 | ✅ Yes (after 26) |
| 28 | Project lifecycle report evidence | 13–18, 20/23 | ✅ Yes (after 11, before 24) |

**Corrected Implementation Order (canonical):**
1. 01 → 02 → 03 → 04 → 05 → 06 → 07 → 08 ✅ (done, unchanged); 12–17 ✅ (done)
2. **09** (AI service token) — implemented; current review hardening stays on its existing branch. Backend 20 is next after review.
3. **20 → 18 → 19 → 22 → 21** (2026-09-10 observability + hardening wave; 20 first because ops/qa/ai read its tables — rationale: `docs/observability/LOGGING-AUDIT-REPORT.md` §3 + ADR-004)
4. **23** (critical-action OTP & step-up — human-only; AI OBO 403)
5. **11** (Blob storage — needs only 01/02/04; also hosts spec-24 report artifacts)
6. **24 → 25 → 26 → 27** (AI superpowers data/ops plane: reports → capability gateway + log tiers → memory/conversations → alerting/broadcasts)
7. 10 (API docs — last, freezes the hardened surface 18–27 for Web/QA)

---

### Web (`web/project-kit/feature-specs/`)

| Spec | Title | Dependencies | Order OK? |
|------|-------|--------------|-----------|
| 01 | React setup Vite | Backend 04-06 (proxy target) | ✅ Yes |
| 02 | Material UI | 01 (scaffold), Week-1 Figma | ✅ Yes |
| 03 | REST integration | 01, Backend 04 + **07 (auth)** | ✅ Yes |
| 04 | GraphQL Apollo | 03, Backend 05 | ✅ Yes |
| 05 | Secure auth & state | Backend 07, Web 03+04 | ✅ Yes |
| 06 | Public shell | 01, 02, 05, Figma screens | ✅ Yes |
| 07 | App shell | 03, 04, 05, Backend 04–06 + 07 (routes + auth) | ✅ Yes (updated) |
| 08 | Kanban interactions | 07, Backend 06 | ✅ Yes |
| 09 | Notifications | 07, Backend 04–06 | ✅ Yes (updated) |
| 10 | Copilot panel | 05, 07, AI 02 | ✅ Yes |
| 11 | Reports/Audit Center | web 10, ai 06/07, backend 24/25/28 | ✅ Before ai 08 |
| 12 | Persistent AI workspace | ai 09, backend 25/26/27 | ✅ Before ai 10–12 |

**Status:** ✅ All dependencies correct. Web specs reference Backend 07 (auth) correctly in specs 03 and 05.

---

### Mobile (`mobile/project-kit/feature-specs/`)

| Spec | Title | Dependencies | Order OK? |
|------|-------|--------------|-----------|
| 01 | Flutter app setup | Flutter 3.19+ toolchain | ✅ Yes |
| 02 | Login & auth screens | 01, Backend 07 (auth) | ✅ Yes |
| 03 | REST API integration | 02 (auth provider) | ✅ Yes |
| 04 | GraphQL integration | 01, 02, Backend 05 | ✅ Yes |
| 05 | Riverpod state mgmt | 03, 04 (data hooks) | ✅ Yes |
| 06 | Responsive UI | 04, 05 (data + state) | ✅ Yes |
| 07 | Notifications | 02-06, Backend 04 | ✅ Yes |

**Status:** ✅ All dependencies correct. Mobile spec 02 correctly references Backend 07 (auth).

---

### AI (`ai/project-kit/feature-specs/`)

| Spec | Title | Dependencies | Order OK? |
|------|-------|--------------|-----------|
| 01 | Trigger setup | Backend 09 (not blocking) | ✅ Yes |
| 02 | Copilot streaming | 01, Backend 05+09, Web 10 | ✅ Yes |
| 03 | Scheduled agents | 02, Backend notifications | ✅ Yes |
| 04 | Propose-before-write | 02, Web 10 | ✅ Yes |
| 05 | Golden transcripts | 01-04 | ✅ Yes |
| 06 | Knowledge agent & system auditor | 02, 05, Backend 16/20/24 | ✅ Yes (P2 wave) |
| 07 | Report generation (PDF + CSV) | 02/03/05, Backend 11/24 | ✅ Yes (P2 wave) |
| 08 | Advanced executor (Level 4) | 02/04/05/06/07, Backend 09/20/23/24 | ✅ Yes (P2 wave) |
| 09 | Agentic BI copilot + memory | 02/05/06/07, Backend 24/25/26 | ✅ Yes (P2 wave) |
| 10 | SuperAdmin ops agent | 05/09, Backend 20/25/26/27 | ✅ Yes (P2 wave) |

**Status:** ✅ All dependencies correct. AI specs correctly reference Backend 09 (service token).

| 06 | Scoped knowledge/auditor | ai 05, backend 24/25/28 | Before web 11 |
| 07 | Evidence template reports | ai 05, backend 24/25/28 | Before web 11 |
| 08 | Approved executor | web 11, backend 09/20/24/25 | After web 11 |
| 09 | BI/threads/manifest | backend 25/26, ai 06/07 | Before web 12 |
| 10 | Ops summaries/notices | backend 27, web 12 | After web 12 |
| 11 | Institutional memory | backend 26/28, web 12 | After ai 10 |
| 12 | Explainable assignments | ai 11, web 12 | After ai 11 |

---

### MCP (`mcp/project-kit/feature-specs/`)

| Spec | Title | Dependencies | Order OK? |
|------|-------|--------------|-----------|
| 01 | MCP server setup | Node 20 | ✅ Yes |
| 02 | Tool roster | 01 | ✅ Yes |
| 03 | Service token | 02, Backend 09+05 | ✅ Yes |
| 04 | Container & deploy | 03, Infra 02-04/06 | ✅ Yes |
| 05 | Contract testing | 01-03 | ✅ Yes |
| 06 | Report & audit tools (v2 roster) | 02, Backend 20/24, ai 06/07 | ✅ Yes (P5 wave) |

**Status:** ✅ All dependencies correct. MCP spec 03 correctly references Backend 09 (service token).

---

### Infra (`infra/project-kit/feature-specs/`)

(7 specs — dependencies checked, all sequential, no cross-system blocking issues found)

**Status:** ✅ All dependencies correct.

---

### QA (`qa/project-kit/feature-specs/`)

(13 specs — dependencies checked, refer to completed backend/web/mobile features, no ordering issues)

**Status:** ✅ All dependencies correct. QA spec 04 reuses **Backend 08's Postman base collection**; QA spec 05 (Newman) executes it in CI (Infra 05); QA spec 06 targets backend code under test (routes 04–06 + auth 07 — the Postman collection is a contract artifact, not code).

---

## Resolution — Actions Taken (all complete in this audit pass)

**Spec IDs physically swapped (branch `fix/backend/swap-specs-07-08`, 2026-09-08):** Backend files `07↔08` were renamed so file numbers match natural implementation order. Auth is now `07-auth-jwt-argon2-redis.md`, Postman is now `08-api-testing-postman.md`. All ~65 references across all 7 systems were updated atomically.

### 1. File renames
- `backend/project-kit/feature-specs/08-auth-jwt-argon2-redis.md` → `07-auth-jwt-argon2-redis.md`
- `backend/project-kit/feature-specs/07-api-testing-postman.md` → `08-api-testing-postman.md`

### 2. Ordering fixed in every state document
- **Backend progress tracker:** Spec 07 = Auth ✅ `Done`; Spec 08 = `Next (depends on 07)`; Next Steps list **08 → 09 → 10 → 11** explicitly.
- **Root progress tracker:** backend row + Next Steps now name **08 (Postman)** as immediate next.
- **README.md:** status line updated ("Next: spec 08 (Postman)").
- **This audit file** records the canonical order permanently (see Backend section above).

### 3. Spec-level cleanliness (contract sync, same pass)
- `backend/…/10-api-documentation.md`: bogus "Backend feature 13" → real reference `PROMPTS/week-02/13-diagram-api-surface.md` (a diagram prompt, not a feature spec).
- `backend/…/11-blob-storage-integration.md`: added missing `## Dependencies` section (needs 01/02/04; **not** gated by 08–10 — parallelizable).
- `infra/…/07-netdata-monitoring.md`: added missing `## Dependencies` section (needs 02 + 06; Phase-1 blocker, not a gate).
- `backend/…/08-api-testing-postman.md`: corrected stale QA-spec numbers (Newman = qa 05, k6 = qa 10, Cypress = qa 09).
- `qa/…/06-unit-integration-xunit.md`: narrowed "Backend 04-08" → "04–06, 07 (code under test)".
- `web/…/07` + `web/…/09`: backend deps clarified to routes + auth (04–06, 07) — applied.

### 4. Progress-tracker verification (post-fix)
- ✅ Backend: Spec 07 (Auth) = ✅ Done; Spec 08 (Postman) = Next; canonical order 08 → 09 → 10 → 11.
- ✅ Web/Mobile/AI/MCP/Infra/QA: all cross-system spec numbers updated — every "Backend 07 (auth)" / "Backend 08 (Postman)" reference is now consistent.

---

## Summary

**Total Feature Specs Audited:** 58 (59 markdown files under `**/feature-specs/`, incl. the root `feature-specs/README.md`)  
**Dependency/Ordering Violations Found & Fixed:** 1 real ordering issue (Backend 07↔08 swap) + 4 secondary reference bugs (backend 10 "feature 13", backend 08 QA-spec numbers, missing Dependencies sections in backend 11 + infra 07)  
**Status:** ✅ **Fixed** — spec files physically renamed, all ~65 references updated system-wide; the CodeRabbit 🟠 Major "Progress tracker dependency order" is closed.

**Next Implementation:**
- Backend: Spec **08** (API testing with Postman).
- Branch: `feature/backend/08-api-testing-postman`.
- Auth Spec 07 is implemented; after 08: 09 (AI token) → 10 (docs) → 11 (blob).

---

**Contract:** This audit ensures no feature is implemented before its dependencies exist. Progress trackers must reflect actual dependency order, not just sequential numbering.

The auth repair also removes the accidentally retained obsolete Spec 07/Postman
and Spec 08/Auth files; the canonical feature files are 07/Auth and 08/Postman.
