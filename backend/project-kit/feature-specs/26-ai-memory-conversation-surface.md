# Backend Feature 26 — Conversation, Preferences and Curated Memory [own-stack]

**Status:** PLANNED. All schema names below are proposals pending ERD approval. Conversation history, semantic memory and explicit preferences remain separate data types. One feature branch: `feature/backend/26-ai-memory-conversation-surface`.

## Type

New backend persistence/read surface; no AI database credentials and no new vector service.

## What This Delivers

Private resumable threads, user-selected defaults, and an authorized corpus of reusable lessons across projects in one workspace. Conversation history is never automatically published into shared memory.

## Dependencies

Backend 09 (restricted delegation), 18 (pagination), 20 (durable jobs/audit), 24 (reports), 25 (capability tiers), 28 (evidence). Ai 09/11 and web 12 are consumers, not implementation prerequisites.

## Context To Read First

Root integration contract, backend data-layer context, ai 09/11/12, `diagrams/erd/ai-planning-amendments.md`. Skills: dotnet-ef-core, contract-sync, Context7, documentation-standards.

## Files Owned

Backend Domain entities, Application memory service/DTOs, Infrastructure repositories/migration, Api memory controllers, xUnit and Postman contracts.

## Setup / Initialization

After ERD approval, migration `AddAiMemory` introduces proposed `ConversationThread`, `ConversationMessage`, `UserPreference`, `KnowledgeLesson` names. Threads carry owner and workspace IDs. Messages carry thread, content, sequence, timestamp, source citations and backend job ID when machine-generated. Preferences are a small key allowlist (report type/window/default workspace), explicitly edited by the user. Lessons carry workspace/project/source IDs, revision/hash, curated summary, tags, provenance, approver, embedding vector JSON/model/dimensions and validity state. Index owner/workspace/thread ordering and lesson source revision. No auto-extraction of private conversations or full logs.

## Routes and permissions (PLANNED)

| Method | Route | Gate |
|---|---|---|
| GET/POST | `/api/ai/conversations` | self; start thread = human only |
| GET/POST | `/api/ai/conversations/{id}/messages` | thread owner; append human message = human only |
| GET | `/api/ai/conversations/{id}/context?q=&limit=` | owner + live workspace membership; AI additionally ReadWorkspace and delegation |
| GET/PUT | `/api/me/preferences` | self; write = human only |
| DELETE | `/api/ai/conversations/{id}` | owner human; AI OBO 403 |
| GET/POST | `/api/workspaces/{id}/memory/lessons` | read = member; curate lesson = Owner/Admin human |
| GET | `/api/workspaces/{id}/memory/search?q=&limit=` | member; AI additionally ReadWorkspace, matching grant and source access |
| DELETE | `/api/workspaces/{id}/memory/lessons/{lessonId}` | Owner/Admin human; invalidates derived vectors |

**No borrowed write scope.** CreateNotification cannot create a conversation, preference or lesson. No generic WriteMemory scope is added. Backend persists the requesting human's turn before enqueue. Model replies/embeddings return through spec 20's durable, HMAC-verified callback inbox bound to an existing backend job, owner, workspace, purpose and source version. The callback may complete that one job only; arbitrary user/thread/lesson IDs, expired jobs and replayed event IDs cannot authorize writes. OBO write routes above return 403.

## Retrieval and privacy

1. Check owner, active membership, scope, role and source permissions before selecting any snippet/vector.
2. Conversation context = bounded chronological turns plus explicit summaries; preference values are defaults, overridden by each request.
3. Shared lessons = human-curated, redacted evidence. Ai 11 computes versioned embeddings via the existing provider; backend stores and ranks a bounded corpus with cosine similarity. Keyword/recency fallback is explicitly labeled lexical retrieval, not semantic memory. Initial corpus cap: 500 active lessons per workspace; over-cap retrieval returns an incomplete flag and is not presented as exhaustive recall.
4. Cross-project means permitted projects in one workspace. Cross-workspace/organization-wide pooling and deep employee behavioral modeling are out of scope. AI 12 uses only inspectable work facts.
5. Revoked/deleted source or membership blocks retrieval immediately; revision changes invalidate/re-embed vectors. Delete thread removes messages/derived private summaries. Shared lessons require separate human curation and source validation.
6. Conversation retention 180 days; preferences until deletion; lessons until invalidated/deleted with source-access checks each retrieval. Audit retains action metadata, not deleted prompt bodies or vectors.

## Separation of Concerns

Backend owns authorization, durable job binding and persistence. AI computes narratives/embeddings; web owns thread and curation UI. No direct SQL, client-chosen OBO identity or memory write through notification/report scopes.

## Docker & Deploy

Existing SQL Server migration and Trigger cloud compute. No vector database, new container or extra provider. Provider keys remain only in ai; source content sent for embedding is minimized and covered by the same role restrictions as normal tool calls.

## Acceptance Criteria

- [ ] Owner can resume a paginated thread; cross-user and revoked-workspace access disclose no content.
- [ ] Every OBO thread/preference/lesson write is 403; valid bound callback persists once, replay has no effect.
- [ ] Semantic fixture retrieves a paraphrased lesson; lexical fallback and truncated corpus are visible.
- [ ] No private conversation or raw log appears in shared lessons by default.
- [ ] Source correction/deletion invalidates derived vectors and future retrieval.
- [ ] Explicit defaults persist; an explicit new request overrides them without profiling the user.

## Verification

`dotnet build`, SQL-enabled `dotnet test`, Postman thread/memory privacy tests, contract-sync; mocked callbacks and vectors, no LLM in CI.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
