# AI Feature 14 — Handoff User-Manual Generator [own-stack]

**Status:** PLANNED — generates the client user manual from project history during close-out. Backend owners: 35 (`ProjectHandoffs`/`HandoffDocuments`) / 20 (spec-20 outbox, durable write-back) / 24 (`CreateReport` artifacts) / 11 (blob storage). UI owners: web 16 / mobile 09.

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented**

## What This Delivers

The generator that turns a project's history — boards/columns, task history, deliverables, environments and the `ProjectHandoffs` checklist — into a structured **client user manual**, written to `HandoffDocuments` (`Kind = Manual`, `GeneratedByAi = true`) via backend 35 **through the spec 20 outbox** (durable, replay-safe, job-bound). The manual is idempotent (re-runs produce one document per handoff revision) and **human-reviewable before submit**: the PM reviews/edits the draft before it becomes part of the handoff the client sees.

## Dependencies

- ai 02/03/05 (scaffolding, scheduling, transcripts), ai 07 (artifact rendering pipeline via backend 24 `CreateReport`).
- Backend 09 ✅ (OBO), 20 (outbox — hard prerequisite for the write-back path), 24 (report jobs/artifacts), 11 (blob), 29 (tenancy), 35 (handoff + `HandoffDocuments`).
- Consumers (not prerequisites): web 16, mobile 09.

## Context To Read First

- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` (§7 handoff, client offboarding, maintenance)
- backend 35 spec + backend 20 outbox contract
- `ai/project-kit/context/architecture.md` + `research/ai-integration.md` §2a

## Agent Skills To Use

- `ai/.agents/skills/trigger-dev-tasks/SKILL.md`
- `ai/.agents/skills/ai-agent-security/SKILL.md`
- Context7 + contract-sync before any implementation branch.

## Files Owned

- `ai/src/handoff/` (manual generator: history aggregation, section assembly, checklist mapping), golden fixtures.
- No backend/web/MCP code in this implementation branch.

## Implementation Notes

- Inputs are authorized, bounded reads only: boards/columns structure, task aggregates (counts, statuses), the handoff `ChecklistJson`, linked deliverables/evidence (backend 28 where applicable). Every section cites its source rows; unknowns are disclosed.
- Structure mirrors the checklist: overview, what was delivered, how to use (per board/column workflow), environments/credentials placeholders (the generator NEVER invents or exposes real credentials — credential rows are PM-entered, `Kind = Credential`, never model-generated), known limitations, support/maintenance expectations.
- Write-back path: generation result goes through the spec 20 outbox (durable, job-bound, replay-safe) → backend 35 persists `HandoffDocuments` once with the blob key (backend 11). No direct SQL, no webhook with oversized payloads (≤64 KiB callbacks).
- Idempotency: keyed per (handoffId, checklist revision); a retry/rerun updates or replaces the AI draft deterministically without duplicating rows.
- Human review: the draft lands in web 16 as reviewable/editable; only PM submit attaches the manual to the client handoff. Rejection returns to draft with reviewer notes.
- Org-stamped: the job carries `OrganizationId`; the manual contains only that org's project data; client-visible content excludes internal-only metadata (assignee performance, cost, internal QA evidence tiers).

## Separation of Concerns

- AI assembles the draft narrative from authorized history; backend 35 owns handoff state, persistence and client visibility; backend 24/11 own artifact jobs and blob storage; backend 20 owns the durable outbox; web 16 / mobile 09 own review + client-facing handoff view. AI never submits the handoff or sets `Completed`.

## Docker & Deploy

- Existing Trigger cloud project; artifacts via backend 11/24. No new container, vector store or direct delivery secret in AI.

## Out of Scope

- Credential generation/management (human-only, PM-entered). Client acceptance flow UI (web 16/mobile 09). Maintenance-phase triage (ai 15). PDF/CSV report templates (ai 07).

## Acceptance Criteria

- [ ] A seeded project produces a manual whose sections map to the checklist with cited sources; missing evidence is disclosed as unknown
- [ ] Write-back is idempotent: retry/rerun produces one AI draft per handoff revision, no duplicate `HandoffDocuments` rows
- [ ] Generation rides the spec 20 outbox (durable, replay-safe); direct SQL or oversized-callback paths are absent
- [ ] The manual never contains model-invented credentials or internal-only data; a client-visible review check confirms internal metadata is excluded
- [ ] Human review is mandatory: unsubmitted drafts are not visible to the client; PM submit is the only client-publish path (AI submit denied)
- [ ] Cross-tenant fixture: org B's project data never appears in org A's manual

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.