# AI Feature Spec 06 — Copilot Knowledge Agent & System Auditor [own-stack]

**Status:** PLANNED — the first of the AI superpowers wave (research: `research/ai-integration.md` §1–§2, `research/ai-features-research.md` §2–§3). Consumes backend specs 16 (reads), 20 (logs), 24 (reports/audit-summary); surfaces in web 10/11.

## What This Delivers

Two superpowers on top of the Copilot:

1. **Knowledge QA — "ask anything about my system".** Answers are grounded in real API responses (never model memory), cite their sources, and render as cards/tables in web 10/11: "what's blocked this week?", "velocity for the last 3 sprints?", "who is overloaded?", "summarize the audit trail for project X".
2. **System auditor — "audit the entire system".** Periodic or on-demand audits: 2FA coverage, stale invites, orphaned tasks, error-flooded stacks (`ErrorLogs`), unusual bulk actions (`AuditLogs`), data hygiene. Output is a typed audit report (backend 24 `Report` row) or a chat summary with severity chips.

## Boundaries (non-negotiable)

- **Read-only data plane.** The auditor uses ONLY read scopes; it never mutates, deletes, invites, or manages members.
- **Never touches auth/OTP.** No tool reads or acts on OTP challenges, MFA settings, password resets, or account deletion (research §3.6 — the same human-only boundary as backend spec 23). The AI OBO principal is 403 on all of it.
- **RBAC respected.** A member auditor sees exactly what that member can read; Owner-only checks return "insufficient permissions" instead of bypassing. No cross-workspace leakage.
- Every call: `GRIOT_SERVICE_TOKEN` + `X-On-Behalf-Of: {real User.Id}` (spec 09).

## Capability → data-source map (candidate v1)

| "Ask anything" class | Data source (backend) |
|---|---|
| Board/task state, blocks, overdue | `GET /api/boards/{id}/tasks`, `GET /api/tasks/{id}` |
| Velocity / workload / cycle time | dashboard + task history (spec 16) |
| Who changed what / trace | `GET /api/logs/audit` (Owner) |
| Error health | `GET /api/logs/errors` (Owner/Admin — spec 20) |
| Security posture (2FA coverage, stale invites) | workspaces/members/invites reads (spec 13) — aggregate counts only |

## Dependencies

- ai 01 (Trigger.dev setup) · ai 02 (Copilot streaming) · ai 05 (golden transcripts + budgets) · backend 16 (dashboard/log reads) · backend 20 (persisted logs) · backend 24 (audit-summary + Report rows) · web 10/11.

## Implementation notes (PLANNED)

- New agent `systemAuditor` (Trigger.dev task) + a `knowledgeTool` in the `ai/` tool roster; typed zod schemas on every tool input/output; trust boundary — validate tool output before it feeds the next step (research §3.3.4).
- Periodic audits run on the scheduler (ai 03 pattern); results persisted as Report rows via backend 24 (OBO `CreateReport` once shipped; otherwise propose-before-write through web).
- Prompt-injection hardening: board/task text is data, never instructions (research §3.3.5).
- Token budget + Redis cost caps per ai 05; observability: every audit run writes `runId` + outcome (backend 20).

## Acceptance Criteria (all PENDING)

- [ ] "Ask anything" answers cite the API rows they were computed from (golden transcripts)
- [ ] Auditor flags seeded failures: unverified-2FA users, stale invites, error-flooded stacks, bulk-action spikes
- [ ] Owner-only data → "insufficient permissions" for a member auditor; zero cross-workspace leakage
- [ ] OTP/auth tools absent from the tool roster (roster contract test)
- [ ] Audit run persists a Report row (backend 24) or an approval card (pre-24)

## Verification

`npm run lint && npm run typecheck && npm test` (mocked LLM, no network in CI) — golden transcripts, roster contract tests, MCP contract tests.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.