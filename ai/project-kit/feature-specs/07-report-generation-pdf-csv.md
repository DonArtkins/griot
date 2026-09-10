# AI Feature Spec 07 — Report Generation Agent (PDF + CSV) [own-stack]

**Status:** PLANNED — the "generate extensive reports from database data (PDF + CSV)" superpower (research: `research/ai-features-research.md` §2). Backend data/artifact side: backend spec 24; UI: web 11; external clients: mcp 06.

## What This Delivers

The Copilot produces **typed, brand-styled reports from live database data** in two forms:

- **CSV** — always (RFC 4180, UTF-8) for spreadsheets and follow-up analysis.
- **PDF** — A4, brand design tokens, accessible selectable-text layer, executives/share-ready.

Report types (`Report.Type`): `sprint_digest` · `task_summary` · `velocity` · `workload` · `ai_action_summary` · `custom`. Runs ad-hoc (copilot request) AND scheduled (ai 03 schedules — `sprintDigest`/`dueReminders` persist as reports, not only notifications).

## The pipeline (numbers never come from the LLM)

```
User asks, or schedule fires
  → plan data fetches through the OBO read scope (existing REST/GraphQL)
  → deterministic aggregation in Node (sum/group/count/trend — typed, unit-tested)
  → optional LLM narrative section (template prompts, per-report tone)
  → CSV writer (RFC 4180) + PDF builder (A4, brand tokens, text layer)
  → upload artifact to blob (backend 11, server-to-server)
  → create Report row via backend 24 (OBO CreateReport once shipped; otherwise
    propose-before-write: web 11 shows the draft, user approves, web creates the row)
  → notify requester (spec 22) with a deep link to /download
```

## Guardrails

- **RBAC:** report data = exactly what the OBO user can read; "excluded X rows due to permissions" is stated explicitly, never silent.
- **Prompt-injection:** board/task text is data; the narrative section summarizes only the typed aggregates (research §3.3.5).
- **Cost:** Redis token budget per ai 05; long aggregations stream to disk and only the digest is tokenized (never the whole dataset in LLM context).
- **No expansion of the write grant:** report-row creation rides `CreateReport` (spec 24) — a new scoped capability, not a loosened grant.

## Dependencies

- ai 02 (copilot surface) · ai 03 (schedules) · ai 05 (budgets/transcripts) · backend 11 (blob) · backend 16 (reads) · backend 24 (Report rows/artifacts/download) · backend 22 (notification) · web 11.

## Implementation notes (PLANNED)

- `ai/src/reports/` library: `aggregate.ts` (typed per type), `csv.ts`, `pdf.ts` (brand tokens; text layer; page numbers), `pipeline.ts` (registry keyed by `Report.Type`).
- Golden transcripts: frozen inputs → expected CSV bytes + PDF metadata + Report payload; LLM narrative mocked (no network in CI).
- MCP exposure: `generate_report` (mcp 06) reuses the same `pipeline.ts` — one implementation, two surfaces.

## Acceptance Criteria (all PENDING)

- [ ] Each report type produces byte-identical CSV for identical inputs (golden fixtures)
- [ ] PDF renders A4 with brand tokens + selectable text; artifact ≤ 20 MB (spec 11 Phase-1 limit)
- [ ] RBAC exclusion line present when scope-limited; cross-workspace fetch returns 404
- [ ] Row creation goes through backend 24 (OBO `CreateReport`) or an approval card pre-24
- [ ] Scheduled digests persist Report rows (not only notifications)
- [ ] No OTP/auth/delete tool in the whole pipeline (roster contract test)

## Verification

`npm run lint && npm run typecheck && npm test` (mocked LLM); golden transcripts; MCP contract tests; Newman download-route run (post-24).

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.