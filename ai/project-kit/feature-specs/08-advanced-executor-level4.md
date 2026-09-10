# AI Feature Spec 08 — Advanced Executor (Level-4 Planning Loop) [own-stack]

**Status:** PLANNED — turns the Copilot from a single-tool caller into a planning-loop executor: everything the logged-in user can do (through the OBO principal) plus the audited superpowers of ai 06/07. Research: `research/ai-features-research.md` §3.

## What This Delivers

Multi-step, human-approved execution: "close the 4 overdue tasks, notify their assignees, and write me a summary report" becomes ONE visible plan, approved once, executed step-by-step through the API, with an audit trail per step and a mini-report at the end.

## The loop (research §3.2)

PLAN → HUMAN GATE → ACT → OBSERVE → summarize-as-report.

- **PLAN** — the agent proposes a full ordered plan (diff-style cards), never executes before approval.
- **HUMAN GATE** — one approve/edit/reject on the whole plan (web 10 renders the approval cards).
- **ACT** — executes approved steps via the existing scoped tool roster (the 4 OBO scopes today; + `CreateReport` with backend 24).
- **OBSERVE** — validates every tool result against a typed zod schema before the next step (research §3.3.4); failures halt and escalate to the user.
- **Summary** — ends with a mini-report (`ai_action_summary`, research §2.3 Type C / §3.4): every autonomous action concludes with a report row, giving the audit trail for free.

## Guardrails (research §3.3 — non-negotiable)

1. Destructive/bulk steps require full-plan approval — no per-step surprise writes; notifications are listed in the plan too.
2. Idempotency — `idempotencyKey` per request; re-runs never double-fire mutations or notifications.
3. Least privilege — the OBO grant is never loosened for planning convenience; new capabilities arrive as new scoped tools (`CreateReport` via backend 24), never a quiet scope bump.
4. Prompt-injection — task/board text is neutralized before planning (a task titled "ignore prior instructions…" is data, research §3.3.5).
5. Confidence thresholds — ambiguous asks trigger clarifying questions instead of best-guess plans.
6. Observability — every planning step, `runId`, and outcome is logged (backend 20); "why did the AI do X" is answerable.

## Tool roster (unchanged today; +1 planned)

- **Today:** `ReadWorkspace` / `CreateTask` / `AddComment` / `CreateNotification` (spec 09) — REST/GraphQL write-back via the service token.
- **Planned:** `CreateReport` (backend 24) for report/audit rows ONLY. NEVER on any roadmap: OTP/auth/MFA/password/account tools — spec 23 step-up is human-only, and AI OBO callers are 403 on every guarded delete (they never reach `RequireStepUp`).
- Knowledge + report tools from ai 06/07 are injected as scoped, typed tools under the same boundaries.

## Dependencies

- ai 02 (copilot) · ai 04 (propose-before-write) · ai 05 (golden transcripts + budgets) · ai 06 (knowledge/auditor) · ai 07 (reports) · backend 09 (OBO) · backend 20 (observability) · backend 23 (human-only step-up boundary) · backend 24 (`CreateReport`) · web 10 (approval cards) · web 11 (report center).

## Acceptance Criteria (all PENDING)

- [ ] Multi-step instruction → one visible plan → approve → executes in order (golden transcript, mocked LLM)
- [ ] Same `idempotencyKey` resend → zero double mutations (integration test)
- [ ] Injection-titled task does not alter the approved plan (golden fixture)
- [ ] Ambiguous instruction → clarifying question, no execution
- [ ] Every executed step produces ApiLogs/AuditLogs rows with `runId` (validated after backend 20)
- [ ] Roster contract test: no OTP/auth/delete/invite tools exist

## Verification

`npm run lint && npm run typecheck && npm test` (mocked LLM); golden transcripts incl. multi-step plans; MCP contract tests.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
