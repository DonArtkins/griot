# Changelog

All notable changes follow [Keep a Changelog](https://keepachangelog.com/) + [Semantic Versioning](https://semver.org/).

## [0.2.0] - 2026-09-03 (planning phase)

### Added
- Seven-system monorepo kits: `backend/`, `web/`, `mobile/`, `infra/`, `qa/`, `ai/`, `mcp/` — each with `AGENTS.md`, `.agents/skills/`, and `project-kit/` (context + feature-specs + diagrams + examples).
- Root `AGENTS.md` orchestrator + shared `.agents/skills/` (contract-sync, figma-make-erd, git-branch-flow, throttling-prevention).
- `PROMPTS/` week-grouped prompts, incl. the Figma Make ERD **single extensive master prompt (no length limit)**.
- Schema expanded to **16 tables** (13 core + `ApiLogs` + `ErrorLogs` + `AuditLogs`) + 5 enums — observability/audit decided pre-implementation.
- Extensive system-design diagram specs (C4 Level 1/2, API component, sequence ×3, task state machine, deployment, auth matrix, API surface map, AI system context).
- `docs/` suite: architecture, NFR + capacity plan + risk register + runbook + change management, database design, ADRs, SEO/deployment, monitoring.

### Changed
- Root `project-kit` consolidated to cross-system orchestration docs (system-map, stack-contract, integration-contracts, UI docs, progress-tracker).
- Branch model formalized: `feature/<system>/<NN>-<slug>` (group-aware, one spec = one branch).

## [0.1.0] - 2026-09-02

### Added
- Bootcamp research corpus (`research/`) and initial planning kit.

---

v0.1.0 → v0.2.0: planning/dot-dash only. No production code yet.