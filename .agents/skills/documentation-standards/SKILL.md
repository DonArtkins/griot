---
name: documentation-standards
description: "Standards for writing Griot documentation (docstrings, ADRs, feature specs, API docs, runbooks): clarity, contract-sync, no AI-slope, changelog discipline, and markdown lint. Use when producing or editing any .md in the repo."
metadata:
  version: "0.1.0"
---

# Documentation Standards Skill

## Core rules (repo-wide)

1. **Docs are contracts.** When a spec/code change happens, update the relevant doc in the same branch (contract-sync). A feature is not done while a doc describes stale behavior.
2. **No AI-slope.** Comments explain *why*, never *what*; no decorative narration, no marketing-speak, no celebratory filler (outside the README taglines).
3. **Actionable over vague.** Every doc that guides a build has explicit commands/paths; every acceptance criterion is binary pass/fail.
4. **ADRs for design decisions.** Options considered + chosen + consequences + references (template `docs/decisions/ADR-000-template.md`).
5. **Changelog.** Every user-visible/behavioral change → `CHANGELOG.md` (Keep a Changelog + SemVer).
6. **Error/fix documentation.** Any error hit and fixed → tested → documented per `docs/planning/CHANGE-MANAGEMENT.md` (never silently diverges from the spec).

## Markdown style

- ATX headings (`#`, `##`) with space; no bare bold headings as headings.
- Tables for structured data (entity fields, routes, env vars, risk registers).
- Fenced code blocks with language tags.
- Full-width `—`/`·` only where already used; keep ASCII fallback in code.
- Links use relative paths within the repo (no absolute `/home/...` paths in committed docs).

## Per-doc conventions

| Doc | Convention |
|---|---|
| `AGENTS.md` (root/system) | Reading order, systems, hard rules, verification gates |
| Feature spec | Type · What This Delivers · Dependencies · Context To Read First · Skills · Files Owned · Setup · Separation of Concerns · Docker & Deploy · Out of Scope · Acceptance Criteria |
| `ARCHITECTURE.md` | one system view, flows, scaling, security, ops |
| `DATABASE-DESIGN.md` | tables/enums/indexes/retention = the schema contract |
| ADR | Status · Date · Context · Decision · Options · Consequences · References |
| Runbook | exact command sequences, revert paths, detection |

## Verify

- `git grep` for stale contract names before commit (contract-sync).
- Markdown renders (no broken relative links); lint if a markdown linter is configured.
- Docs committed on the SAME branch as the code change they describe.