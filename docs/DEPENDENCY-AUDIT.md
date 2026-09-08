# Dependency Audit — Feature Spec Implementation Order

**Generated:** 2026-09-08  
**Purpose:** System-wide audit of all feature spec dependencies to ensure correct implementation order and prevent dependency violations.

## Critical Findings

### ✅ **Backend: Specs 07 and 08 — Ordering Violation Found and Fixed by ID Swap**

**Original Finding:** Backend **Feature 07 (API testing — Postman)** depended on **Feature 08 (Auth: JWT + Argon2 + Redis)**, yet 07 was numbered before 08. Following plain numbering would build the Postman collection before its auth endpoints exist.

**Resolution — spec IDs physically swapped (2026-09-08, branch `fix/backend/swap-specs-07-08`):**
- **07 = Auth: JWT + Argon2 + Redis** ← implement FIRST (file: `07-auth-jwt-argon2-redis.md`)
- **08 = API testing (Postman)** ← implement AFTER 07 (file: `08-api-testing-postman.md`)
- All ~65 references across all 7 systems updated atomically in this branch.

**Canonical next-step rule:** Spec 07 (Auth) is ✅ **Done**. Implement **08 (Postman)** next. Next branch: `feature/backend/08-api-testing-postman`.

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
| 10 | API documentation | 04-07, 08 (routes + Postman) | ✅ Yes (after 08) |
| 11 | Blob storage | None listed | ✅ Yes (independent) |

**Corrected Implementation Order (canonical):**
1. 01 → 02 → 03 → 04 → 05 → 06 → 07 ✅ (done, unchanged)
2. **08** (Postman) — next branch: `feature/backend/08-api-testing-postman`
3. 09 (AI service token — needs 07)
4. 10 (API docs — needs 04–07 + 08)
5. 11 (Blob storage — needs only 01/02/04; may run in parallel once 04 exists, not gated behind 08–10)

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

**Status:** ✅ All dependencies correct. AI specs correctly reference Backend 09 (service token).

---

### MCP (`mcp/project-kit/feature-specs/`)

| Spec | Title | Dependencies | Order OK? |
|------|-------|--------------|-----------|
| 01 | MCP server setup | Node 20 | ✅ Yes |
| 02 | Tool roster | 01 | ✅ Yes |
| 03 | Service token | 02, Backend 09+05 | ✅ Yes |
| 04 | Container & deploy | 03, Infra 02-04/06 | ✅ Yes |
| 05 | Contract testing | 01-03 | ✅ Yes |

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
- Backend: Spec **07** (Auth: JWT + Argon2 + Redis).
- Branch: `feature/backend/07-auth-jwt-argon2-redis`.
- After 07: 08 (Postman) → 09 (AI token) → 10 (docs) → 11 (blob, parallelizable).

---

**Contract:** This audit ensures no feature is implemented before its dependencies exist. Progress trackers must reflect actual dependency order, not just sequential numbering.
