# AI Feature 11 — Institutional Memory and Pattern Detection [own-stack]

**Status:** PLANNED. Branch `feature/ai/11-institutional-memory-pattern-detection`.

## Type

New feature, separate from conversation persistence and report rendering.

## What This Delivers

At task creation or on request, show relevant prior work, known mistakes and reusable patterns across projects in the same authorized workspace. Each suggestion cites task/report/lesson IDs and includes the evidence, not an opaque confidence claim. Recall is measured and fallible; never promise guaranteed recall or that a report is automatically ground truth.

## Dependencies

Backend 20 (auditable sources), 25 (capabilities/data tiers), 26 (curated lessons and retrieval), 28 (deployment/test evidence), ai 05 (evals), 06/07 (knowledge/reports), 09 (conversation integration), web 12 (review cards). Build after ai 10, before ai 12. PR/pre-commit/editor integrations are deferred: no repository connector or code-ingestion contract exists today.

## Context To Read First

`research/ai-integration.md`, `docs/planning/AI-SYSTEM-AUDIT-2026-09-11.md`, backend 26, ai security skill, Context7, contract-sync.

## Files Owned

`ai/src/memory/` (fingerprints, matching, provenance), `ai/src/tasks/task-memory-check.ts`, fixtures and golden transcripts. Persistence belongs to backend 26; UI belongs to web 12.

## Setup / Initialization

Start with explicit lessons curated from completed tasks, resolution notes, reports and approved engineering documentation. Each lesson records workspace/project/source IDs, source revision/hash, pattern tags, evidence window and observed outcome. Human approval creates/updates a lesson through backend 26. Backend-bound jobs compute embeddings using the existing AI provider; model/version/dimensions accompany every vector. Backend 26 stores vectors with the lesson in SQL Server and performs bounded cosine retrieval; no vector database or raw-log replication service. Keyword matching is the fallback and must be labeled as such.

## Matching and evidence

- Candidate reads are filtered by workspace, caller role and live source permissions BEFORE retrieval. Do not embed passwords, OTPs, raw request bodies, stack traces, private conversations or indiscriminate full AuditLogs/ErrorLogs rows into shared memory.
- Deduplication: exact content hash/source revision prevents duplicate ingestion. Similarity means related work, not proof that two tasks are interchangeable. Show reusable source links and differences.
- Conservative threshold and a maximum of three proactive suggestions per task; dismiss/snooze and record useful/not-useful feedback. Calibrate on labeled relevant/irrelevant fixtures before rollout; do not alert on every task.
- Cost estimates require linked resolved incidents and measured timestamps. Show sample count, time window, median/range and whether duration is elapsed time or recorded engineering effort. `FixedAt - CreatedAt` is elapsed resolution time, not paid work hours. API latency alone cannot establish time-to-fix or money saved. With insufficient comparable evidence, say unknown.
- Suggested fixes are explanatory diffs/snippets tied to cited patterns. Only a human can apply code/schema changes. No auto-commit, automatic patch application, deployment or repository write tool.
- Updating/deleting a source invalidates derived snippets/vectors; removed membership prevents retrieval immediately. Store provenance so a reviewer can inspect every suggestion and correction.

## Separation of Concerns

AI proposes matches and explanatory patches; backend authorizes retrieval, persists approved lessons and records request/run attribution; web renders evidence and accept/dismiss actions. Acceptance of a suggestion is not authorization to commit code.

## Docker & Deploy

Existing Trigger cloud project and AI provider only. Batch embeddings with the existing cost budget. If the authorized corpus exceeds the bounded retrieval limit (initially 500 lessons per workspace), disclose incomplete retrieval and require an evaluated scaling change; never silently scan only the newest rows.

## Acceptance Criteria

- [ ] A seeded OTP-expiry lesson is found for related task wording with source citations; an unrelated task produces no proactive warning.
- [ ] Two projects in one workspace can reuse a lesson; another workspace or revoked membership receives no snippet, score or existence hint.
- [ ] Duplicate source ingestion is idempotent; changed/deleted sources invalidate old matches.
- [ ] Cost output identifies sample count and elapsed-time basis; missing data yields unknown, not invented hours or severity.
- [ ] Proposed diff is reviewable; accept records the actor but never writes to a repository or schema.
- [ ] Keyword fallback is explicit; model/dimension mismatch triggers re-embedding, not mixed-vector scoring.
- [ ] Golden evals measure precision and missed matches; acceptance threshold is documented from fixtures before enabling proactive notifications.

## Verification

`npm run lint && npm run typecheck && npm test`; mocked embedding/LLM fixtures, cross-workspace retrieval and source-invalidation contract tests. No model calls in CI.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
