# AI Feature 07 — Evidence-Based Template Reports (PDF and CSV) [own-stack]

**Status:** PLANNED. Templates are grounded in the four local DOCX samples; backend 24 owns eligibility, jobs and storage.

## Type

New rendering/aggregation pipeline; not a freeform document-writing agent.

## What This Delivers

CAB deployment requests, QA/test, post-deployment, regression and sprint/status reports using the registry in backend 24. Preserve the sample metadata, numbered sections, findings/results tables, summary badges, limitations and human sign-off area. See `research/reports/README.md` for exact sample-to-template mappings. Role/access reminders use backend 27 notices, not this renderer.

## Dependencies

Ai 01/02/03/05; backend 09/11/18/20/22/24/25/28. Web 11 consumes the outputs after this spec; mcp 06 calls backend generation later. Neither is an implementation prerequisite.

## Context To Read First

`research/reports/README.md`, backend 24/28, ai architecture/security skill, Context7 and contract-sync.

## Files Owned

`ai/src/reports/` template registry, `aggregate.ts`, `csv.ts`, `pdf.ts`, pipeline and golden fixtures. No backend/web/MCP code in this implementation branch.

## Setup / Initialization

Use exact versions through ai 01's lockfile. Review and pin the PDF/CSV libraries when this spec starts; prefer existing dependencies, selectable text and deterministic layouts. Build anonymized fixtures from sample section structures and counts. Do not upload the source DOCX documents or copy real sample reviewers into generated reports.

## Pipeline

1. Consume the backend's persisted generation job with user/workspace, requested type/formats, evidence IDs, source snapshot and output binding. Check the live manifest/eligibility again.
2. Exhaust all REST/GraphQL pages beyond the 1,000-item cap using stable ordering and the snapshot/cutoff policy in backend 24. Frozen test-run case IDs and baseline mappings are required for regression. Source changes invalidate/restart the job or mark it incomplete; budget exhaustion is not a successful partial report.
3. Deterministic typed code computes totals/severity/trends. ESS fixture = 5 checks, 4 pass, 1 critical; Finsights = 5 findings, 3 critical, 1 high, 1 medium; regression fixture = 3 cases, 3 pass. Unknown/unclassified and not-run are preserved. Raw telemetry is optional corroboration with its retention/drop limits disclosed, never the sole proof of successful QA.
4. Optional model prose receives only permitted facts/aggregates and citations. Separate observations from hypotheses. Approval, signature, rollback success and test execution cannot be invented by the model.
5. Render A4 PDF with brand tokens, accessible selectable text, page numbers and source/window/coverage notes. CSV is RFC 4180 UTF-8 with deterministic column order/escaping. Prefix untrusted text with a single quote when its first significant character is =, +, -, @ after whitespace/control normalization. Preserve typed numeric values, including legitimate negative numbers. Test commas, quotes, newlines and each dangerous prefix.
6. Upload requested PDF/CSV through the bound backend report-artifact route (CreateReport), never direct blob credentials or arbitrary URLs. Each file ≤20 MB. Send only bounded metadata/summary in the 64 KiB completion callback; large datasets remain artifacts/evidence pages.
7. Backend validates completion, persists once and notifies the requester. Read/list/download/delete actions do not send completion notifications. Scheduled reports use a stored authorized schedule identity, current membership and idempotency key.

## Separation of Concerns

Backend owns data, authorization, eligibility and job/result binding; AI owns template rendering and narrative; web owns human evidence and previews; MCP calls backend POST reports and never imports this renderer or bypasses orchestration.

## Docker & Deploy

Existing Trigger cloud project and provider; artifacts go through backend 11/24. No public share links, DOCX export, custom template designer, new chart service or additional vector store.

## Acceptance Criteria

- [ ] Each requested template matches its sections and required metadata; AI cannot fabricate CAB/QE sign-off.
- [ ] 1,205-row fixture totals include every page; changing sources and incomplete runs cannot silently produce a complete report.
- [ ] CSV is deterministic and safe for all formula prefixes/whitespace variants while numeric fields remain numeric.
- [ ] PDF preserves tables, page breaks and selectable text; both artifacts stay under the cap.
- [ ] Missing deployment/baseline/test evidence blocks the relevant report type server-side.
- [ ] Low-tier jobs never receive raw logs or counts of inaccessible records; saved artifacts retain the source data tier.
- [ ] Retry/partial upload creates one report and one completion notification; unknown counts remain explicitly unknown.

## Verification

`npm run lint && npm run typecheck && npm test`; mocked model/vector calls, deterministic CSV fixtures, PDF text/metadata checks and visual sample review. Backend download/eligibility tests remain owned by backend 24.

## Test-run input completeness (planned backend 28 contract)

Consume only backend-validated complete runs from the source manifest. Typed RunId/Page/TotalPages/TotalResults columns must cover all declared pages and exactly the declared result count with unique case IDs. Do not reconstruct completeness from ContentJson or treat an incomplete replacement as completed evidence. Missing evidence remains an explicit eligibility failure in backend 24.

## Multi-Tenant Update (2026-09-11 — PLANNED)

- Reports are per-org: generation jobs carry `organizationId` from backend 24 (org-stamped job/eligibility); every aggregate computes within the active organization only — no cross-tenant rollups.
- Add the **client progress report variant** for `Client` recipients: percent-complete, milestones, activity digest — no internal board internals; gated by the capability gateway (backend 25 bump) and the ai 13 client boundary.
- Saved artifacts retain the org stamp alongside the source data tier, so per-tenant log/report reads (backend 25, mcp 07) stay possible.
- Report jobs under a suspended org are rejected (`403 org_suspended`) before any model spend.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
