# Progress Tracker (root)

## Current State

Bootcamp Week 2 — **Implementation phase**. Backend implementation is underway.

| System | Kit | Status |
|---|---|---|
| backend | backend/project-kit | Specs 01–06 done; **Spec 08 (Auth) next** — 07 (Postman) depends on it |
| web | web/project-kit | 10 feature specs; awaiting backend |
| mobile | mobile/project-kit | 7 feature specs; waiting |
| infra | infra/project-kit | 6 feature specs; waiting |
| qa | qa/project-kit | 13 feature specs; waiting |
| ai | ai/project-kit | 5 feature specs; waiting |
| mcp | mcp/project-kit | 5 feature specs; waiting |

Root docs: `docs/ARCHITECTURE.md`, `docs/database/DATABASE-DESIGN.md`, `docs/planning/*` (NFR, capacity, risk, runbook, change-management), `docs/seo/`, `docs/deployment/`, `docs/observability/`, `docs/api/` + ADRs + LICENSE/CONTRIBUTING/SECURITY/CODE_OF_CONDUCT/CHANGELOG + `inspo/`. 12 diagram specs written in `PROMPTS/week-02/`.

## Next Steps

1. Fill `inspo/` with desired UI screenshots (web + mobile visual contract).
2. Continue backend implementation in **dependency order** (canonical list in `docs/DEPENDENCY-AUDIT.md`): 01 → 02 → 03 → 04 → 05 → 06 ✅ → **08 (Auth)** → **07 (Postman)** → 09 (AI service token) → 10 (API docs) → 11 (Blob storage — only needs 01/02/04, can run in parallel once 04 exists).

## Session Notes
- **2026-09-08** — Root README runbook added (`docs/runbook-commands` branch): "How to run & build (command reference)" with exact folder-per-command for Docker compose (shared `~/sababisha/infra/docker-compose.yml`, `infra-`-prefixed container names, ports 14333/5433/6380), backend run/build/watch (`dotnet watch run --project src/Griot.Api` → :5064), EF Core migrations + stored-proc apply commands, web + mobile target commands (marked 🚧 not scaffolded yet; includes `flutter build apk --release` output path), AI/MCP, and an end-to-end smoke check. Status section updated to reflect backend through spec 04.
- **2026-09-08** — ✅ **System-wide dependency-order audit completed** (`docs/DEPENDENCY-AUDIT.md`). Audited all 58 feature specs across the 7 systems; the only ordering violation was **backend 07 (Postman) depending on backend 08 (Auth)**. Canonical backend order is now 06 → **08 → 07** → 09 → 10 → 11, fixed in every tracker + README + the audit file. Also fixed: stale root status ("spec 04 / spec 05 next"), README status line, bogus "feature 13" reference in backend spec 10, wrong QA-spec numbers in backend spec 07, missing `## Dependencies` in backend 11 + infra 07. **Spec IDs deliberately NOT renumbered** (07 = Postman, 08 = Auth): 30+ cross-system references resolve "08 = auth", so IDs stay stable and ordering is enforced via trackers — the CodeRabbit 🟠 Major (dependency order) → ✅ Fixed item.
- **2026-09-07 (3)** — **Features 3–4 completed; Feature  ǀ 5 marked Next (backend)**. Spec 03 (stored procedures `usp_BulkUpdateTaskStatus` / `usp_GetDashboardSummary` + Dapper repositories) and Spec 04 (REST controllers / services / DTOs scaffolded) are implemented on `feature/backend/03-stored-procedures-and-optimized-queries` and `feature/backend/04-rest-apis-dotnet8` (merged into `main` via PRs #10–#11;. Backend tracker marks specs 01–04 ✅ Done and **spec 05 (GraphQL layer — HotChocolate) as Next — not started, awaiting explicit go-ahead**.
- **2026-09-07 (2)** — Integrated new features from `@research/ai-features-research.md`: OTP 2FA (Resend email), System Reports (SQL Server stored procs + AI scheduled/ad-hoc), and AI Copilot upgrade to Level 4 Autonomous Agent (reasoning loop + human gate). Updated all planning files, contracts, and AGENTS.md across the monorepo.

- **2026-09-03** — Rebuilt repo as seven-system monorepo per the PDF + Foundrie pattern: per-app AGENTS.md, `.agents/skills/`, per-app project kits, one feature spec per bootcamp deliverable. Root kit consolidated to system-map / stack-contract / integration-contracts.
- **2026-09-03 (2)** — Planning deep-dive: schema expanded to **16 tables / 5 enums** (added ApiLogs, ErrorLogs, AuditLogs so no schema rework later). Wrote 12 system-design diagram specs (C4×2, API component, sequence×3, state, deployment, auth matrix, API surface, AI context) with **extensive single-prompt figures (no character limit)**. Added `docs/` production suite (architecture, database, NFR, capacity/scaling, risk register, runbook, change-management, SEO, deployment, monitoring, API docs) + ADRs + LICENSE (MIT) + CONTRIBUTING/SECURITY/CODE_OF_CONDUCT/CHANGELOG + `inspo/`. Added Context7 + DOCX/PDF MCP skills. Bumped root AGENTS hard rules.
- **2026-09-03 (3)** — Removed all ≤2000-char prompt caps by user request. ERD is now one extensive master prompt (`02-erd-figma-make-master-prompt.md`, full 16 tables / 5 enums / 19 edges / 13 indexes). All 11 diagram figure files promoted to single extensive prompts (no length limit); kept tiny "Refine" follow-ups.
- **2026-09-07** — **Master design system synthesized from `inspo/` (all 12)**. Deliverables: `docs/design/MASTER-DESIGN-SYSTEM.md` (catalog, ranking, tokens + provenance, component rules, rejections); `project-kit/context/ui-tokens.md` rewritten as a concrete token contract (v2 — **light canvas `#F7F8FA` + white cards supersedes the dark-workspace placeholder**, 12/12 inspos are light; dark is now a derived variant); theme files created at the spec-owned paths `web/src/theme.ts` (MUI v6 `createTheme`, chrome-ink CTA, severity maps exported) and `mobile/lib/core/theme/theme.dart` (ThemeData + `GriotColors`/`GriotRadii` extensions, header tint, radius-24 cards); contract-synced web design-system.md, web skill 0.2.0, web specs 02, web architecture/diagrams README, both public-shell prompts (dark→light), created the missing `mobile/project-kit/context/design-system.md`, cataloged `inspo/README.md`. Next: MUI/Flutter font bundling in scaffold specs (feature 01s); `StatusChip`/`PriorityChip` etc. build on these primitives.


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
