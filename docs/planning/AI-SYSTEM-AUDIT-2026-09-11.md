# AI system review ledger — 2026-09-11

Status: review corrections prepared on `feature/backend/09-ai-service-token-and-webhooks`; future features remain PLANNED. This ledger records the supplied CodeRabbit findings against starting commit `bc4a186`. It does not reconstruct or claim completion of an earlier missing audit file.

## Verified existing fixes (no duplicate edit required)

| Finding | Evidence at starting commit |
|---|---|
| Root/web/tracker P0 omits 25–27 | Opening P0 sequences already contain 28 → 24 → 25 → 26 → 27 → 10. Remaining backend tracker next-step drift corrected in this pass. |
| Backend Hard Rule 3 omits delegation | Already requires UTC expiry, grant workspace IDs and narrowed scopes. |
| AI security skill allows only GraphQL | Already names REST or GraphQL with trusted OBO delegation. |
| AI 10 raw logs, redaction, recipient authorization and delivery keys | AI spec already contains these controls; this pass synchronizes and sharpens the backend 27 owner requirements and backend-level acceptance test. |
| AI 12 unbounded history | Already requires bounded window/aggregates, maximum rows/tokens and timeout; exhaustive pagination is confined to that query. |
| Backend 24 tracker omits 28 | Table and report next-step already require 28 lifecycle evidence. |
| Report lacks proposed CreatedAt | Already proposed immutable CreatedAt; migration backfill and schema review input are now explicit. |
| QA backend 20 complete | Already PLANNED; backend 08 completion and folder 14 blocker preserved. |
| MCP nine-tool roster / missing backend 25 | Fixed roster is eight; historical nine-tool note explicitly describes removal. V2 architecture already requires 20/24/25. |
| Diagram MCP counts | Prompts 14/15/17 already use eight tools; sequence still correctly has nine lifelines. |
| Dependency audit inventory | Opening summary already includes backend 23–28, AI 06–12, web 11–12 and MCP 06. |
| SPF unconditional | Section 3 already makes SPF account-dependent and retains verification TXT, DKIM and DMARC. |
| summarize_project calls LLM | Roster already says deterministic permitted backend aggregation; adjacent MCP example has no direct agent endpoint. |
| Risk-register approval bypass | Already requires authorized backend schedules/idempotency, fixed operator incident policy/audience and confirmed notice drafts. |

## Corrections completed in this pass

- Scheduled notification creation was still claimed in ARCHITECTURE section 3.3 and system-flow section 4. Both now state backend 22 is pending; current writes are CreateTask/AddComment. Removed the misleading POST notification-list entry in API context.
- Backend 10 now requires documentation of the full 18–28 surface. Backend next steps include 25–27; roadmap table places 28 before 24 and names its dependency.
- ProjectEvidence now proposes typed run/page/total columns, filtered uniqueness, serialized metadata validation and snapshot-based completeness, including corrections and concurrent uploads. Backend 24, AI 07, web 11 and data/API contexts agree.
- Backend 27 now owns explicit pre-provider snapshot redaction and output validation, actor-bound preview/confirmation, persisted per-delivery status and reconciliation for ambiguous provider outcomes.
- Trigger.dev v3 downgrade rejected as obsolete: commit `7e90c5b` already adopted v4. The [vendor migration notice](https://trigger.dev/docs/migrating-from-v3) shuts v3 down July 1, 2026. CLI/SDK/react-hooks now pin 4.5.16 together; documentation and setup commands reflect that existing v4 decision. No cloud login/init/deploy was performed.
- The referenced audit ledger and ERD proposal were missing. This ledger and [unapproved schema review input](AI-SCHEMA-PROPOSALS-2026-09-11.md) make the actual state explicit; they are not design approval or implementation evidence.

## Approved branch/PR grouping exception

On 2026-09-11, after being asked whether this correction batch could remain on the backend 09 PR as a one-time exception, the user instructed: **"COMMIT AND PUSH TO GITHUB"**. This authorizes committing and pushing the reviewed planning/contract corrections and AI 01 dependency synchronization together on `feature/backend/09-ai-service-token-and-webhooks`, including the already committed planning wave.

The exception is limited to this review batch. Future production implementations remain one feature spec per branch/PR. No schema implementation, deployment, merge, force-push or history rewrite is authorized. The conflicting review suggestion to leave backend 23–28 grouped does not establish a general workflow exception.

## Validation

- `dotnet build --no-restore --nologo -m:1`: passed, 0 warnings/errors.
- SQL-enabled `dotnet test --no-build --no-restore --nologo -m:1`: 108 passed, 0 failed, 0 skipped. Brevo keys blanked; no real email sent.
- Clean `npm ci --ignore-scripts` in a disposable /tmp directory: passed (316 packages); CLI reports 4.5.16; SDK and react-hooks imports pass. Manifest, lock root and resolved versions match. This validates dependency resolution/imports, not cloud deployment.
- AI lint/typecheck/test scripts are not present in the pre-scaffold package; no production AI implementation was added. No Markdown linter is configured. CodeRabbit CLI is not installed.
- `python3 scripts/check-contract-sync.py`: passed (591 files inspected). `git diff --check`: passed. Newly added relative Markdown links resolve; stale roster/P0 hits are explicitly historical or prohibited-tool notes.
- Environment recovery: sandbox blocked the test-runner socket and npm registry access; approved outside-sandbox reruns passed. One batched repository read used the temporary npm directory by mistake; rerunning in the repository succeeded. These were verification-environment issues, not application defects.
- Local SQL Server/PostgreSQL/Redis containers are running. A temporary API on port 5109 returned HTTP 200 `Healthy`; its sandboxed startup could not reach Redis, and the approved outside-sandbox rerun passed. This endpoint is a host liveness check; SQL connectivity was separately exercised by the tests.

No future-feature schema or production code was implemented. Grouping approval is recorded above. Commit/push is authorized after the verification gates; merging and starting the next feature still require user approval.
