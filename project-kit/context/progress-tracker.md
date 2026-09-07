# Progress Tracker (root)

## Current State

Bootcamp Week 2 — **Planning / system-design phase** (no code yet). Diagrams + docs being finalized in Figma Make before implementation starts.

| System | Kit | Status |
|---|---|---|
| backend | backend/project-kit | 10 feature specs; awaiting approved ERD |
| web | web/project-kit | 10 feature specs; awaiting backend |
| mobile | mobile/project-kit | 7 feature specs; waiting |
| infra | infra/project-kit | 6 feature specs; waiting |
| qa | qa/project-kit | 13 feature specs; waiting |
| ai | ai/project-kit | 5 feature specs; waiting |
| mcp | mcp/project-kit | 5 feature specs; waiting |

Root docs: `docs/ARCHITECTURE.md`, `docs/database/DATABASE-DESIGN.md`, `docs/planning/*` (NFR, capacity, risk, runbook, change-management), `docs/seo/`, `docs/deployment/`, `docs/observability/`, `docs/api/` + ADRs + LICENSE/CONTRIBUTING/SECURITY/CODE_OF_CONDUCT/CHANGELOG + `inspo/`. 12 diagram specs written in `PROMPTS/week-02/`.

## Next Steps

1. Generate + approve the **12 diagrams** in Figma Make from `PROMPTS/week-02/04…14` + the ERD (prompts A–H in `02-…-sequence`).
2. Export PNGs → `project-kit/diagrams/{erd,architecture}/` → update the ledger.
3. Fill `inspo/` with desired UI screenshots (web + mobile visual contract).
4. Then implementation (backend spec 01 → 10) — planning must be complete + approved first.

## Session Notes

- **2026-09-03** — Rebuilt repo as seven-system monorepo per the PDF + Foundrie pattern: per-app AGENTS.md, `.agents/skills/`, per-app project kits, one feature spec per bootcamp deliverable. Root kit consolidated to system-map / stack-contract / integration-contracts.
- **2026-09-03 (2)** — Planning deep-dive: schema expanded to **16 tables / 5 enums** (added ApiLogs, ErrorLogs, AuditLogs so no schema rework later). Wrote 12 system-design diagram specs (C4×2, API component, sequence×3, state, deployment, auth matrix, API surface, AI context) with **extensive single-prompt figures (no character limit)**. Added `docs/` production suite (architecture, database, NFR, capacity/scaling, risk register, runbook, change-management, SEO, deployment, monitoring, API docs) + ADRs + LICENSE (MIT) + CONTRIBUTING/SECURITY/CODE_OF_CONDUCT/CHANGELOG + `inspo/`. Added Context7 + DOCX/PDF MCP skills. Bumped root AGENTS hard rules.
- **2026-09-03 (3)** — Removed all ≤2000-char prompt caps by user request. ERD is now one extensive master prompt (`02-erd-figma-make-master-prompt.md`, full 16 tables / 5 enums / 19 edges / 13 indexes). All 11 diagram figure files promoted to single extensive prompts (no length limit); kept tiny "Refine" follow-ups.
- **2026-09-07** — **Master design system synthesized from `inspo/` (all 12)**. Deliverables: `docs/design/MASTER-DESIGN-SYSTEM.md` (catalog, ranking, tokens + provenance, component rules, rejections); `project-kit/context/ui-tokens.md` rewritten as a concrete token contract (v2 — **light canvas `#F7F8FA` + white cards supersedes the dark-workspace placeholder**, 12/12 inspos are light; dark is now a derived variant); theme files created at the spec-owned paths `web/src/theme.ts` (MUI v6 `createTheme`, chrome-ink CTA, severity maps exported) and `mobile/lib/core/theme/theme.dart` (ThemeData + `GriotColors`/`GriotRadii` extensions, header tint, radius-24 cards); contract-synced web design-system.md, web skill 0.2.0, web specs 02, web architecture/diagrams README, both public-shell prompts (dark→light), created the missing `mobile/project-kit/context/design-system.md`, cataloged `inspo/README.md`. Next: MUI/Flutter font bundling in scaffold specs (feature 01s); `StatusChip`/`PriorityChip` etc. build on these primitives.
