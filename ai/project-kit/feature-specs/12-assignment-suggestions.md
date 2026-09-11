# AI Feature 12 — Explainable Task and Role Suggestions [own-stack]

**Status:** PLANNED. Branch `feature/ai/12-assignment-suggestions`.

## Type

New read/propose feature; separate from memory retrieval and report generation.

## What This Delivers

A PM (the human operating the project), workspace Owner or Admin can ask who might take a task. Show candidates with cited relevant completed tasks, explicit skills volunteered by the member, current open-task counts and a growth/rotation option. Role suggestions are drafts for human consideration and never automated access changes.

## Dependencies

Ai 05/09/11, backend 16/18/20/25/26 and web 12. No new OBO mutation scope, ranking database or personnel scoring system.

## Context To Read First

Backend task/member RBAC contract, ai 11, ai security skill, `research/ai-integration.md`, Context7 and contract-sync.

## Files Owned

`ai/src/assignment/` candidate explanation and fixtures; ai tool manifest read/proposal entry. Existing web 12 cards and human backend task/member routes are integration consumers.

## Setup / Initialization

Resolve the caller and workspace from trusted execution context. **Bound the candidate-history query before invoking the AI provider**: use a bounded history window (e.g. last 12 months) or server-side aggregates, enforce a maximum row count and prompt-token cap, and apply a request timeout; exhaustive pagination is used only within that bounded query. Reuse backend 26 lesson provenance; do not infer protected traits, health, personality, salary, private messages or hidden performance ratings. Task counts are workload indicators, not estimates of free hours.

## Proposal and confirmation contract

Return `{taskId, workspaceId, sourceVersion, candidates:[{userId, supportingTaskIds, relevantExperience, openTaskCount, caveats}], growthOption?, proposedAssigneeId?}`. Scores alone are not an acceptable explanation. With insufficient evidence, offer a neutral shortlist and say so.

Web displays the exact task, assignee and reasons. A human explicitly confirms; web submits the existing task update as that authenticated human. Backend rechecks current membership, task version and role; stale versions require a refreshed preview. The AI never calls task update or member-management routes. A role change follows the existing Owner/Admin human flow and required step-up, separately from task assignment. Confirmation of a task assignment does not approve a role change.

The evidence subject or an authorized manager can inspect the permitted facts behind an explanation. If evidence is outside their access, exclude it from the ranking rather than leak it. Users can correct declared skills and flag inaccurate evidence. Show when the top matches repeatedly favor the same experienced people; offer a developing member/stretch assignment without forcing it. No automated employment decisions or persistent employee league table.

## Separation of Concerns

AI reads and proposes; web confirms; backend validates and performs the existing human write, recording the human actor plus proposal/source IDs in AuditLogs and ActivityLogs. No member/role permission is delegated to AI.

## Docker & Deploy

Existing AI project, API and web only; no new container, schema migration or credential. Report whether evidence is missing rather than adding a people analytics service.

## Acceptance Criteria

- [ ] Candidate explanations cite the exact permitted history and workload counts.
- [ ] A new/junior member appears as a growth option when appropriate; lack of history is not labeled poor ability.
- [ ] Nothing is assigned until human confirmation; AI direct task/role update remains denied.
- [ ] Cancel, stale task, revoked membership and changed role prevent the write and request a new preview.
- [ ] The candidate/manager can inspect and challenge the evidence within their own access scope.
- [ ] Successful human assignment is attributed to the approving user with the proposal/source references; no opaque score is required to reconstruct it.

## Verification

`npm run lint && npm run typecheck && npm test`; mocked LLM golden transcripts, cross-user privacy fixtures, stale-proposal tests and web confirmation E2E (owned by web/QA).

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
