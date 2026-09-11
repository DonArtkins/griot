# System Flow — End to End (Griot)

> The request-level walkthrough of the entire system: how a click in the browser becomes a row in SQL Server, how the AI copilot gets an answer, how an external AI client reads a board. Companion to `docs/ARCHITECTURE.md`.

---

## 1. Web: board read (the most common path)

```
[Browser] User opens /app/board/123
  1. Vercel serves the SPA (static) + env VITE_API_URL.
  2. Apollo Client sends: POST {VITE_API_URL}/graphql { board(id:123){ columns{ tasks{...} } } }  + Bearer access JWT (memory).
  3. Backend: JWT middleware verifies (iss/aud/sig) → principal {sub, email, jti}.
  4. HotChocolate resolves board → GraphQL resolver → BoardService → repos → EF Core.
  5. DataLoader batches assignee + comment fetches (N+1 safe).
  6. ApiLogs row written (requestId, duration); response JSON.
  7. Apollo updates InMemoryCache → components render; Zustand only holds client UI state.
```

Latency budget: p95 < 500 ms. Monitored by `ApiLogs.DurationMs`; k6 in CI.

## 2. Web: create task (write with audit)

```
[Browser] TaskModal → POST /api/tasks  { title, columnId, ... }  (Axios + TanStack)
  → TaskController → TaskService.CreateTaskAsync
  → [TX] INSERT TaskItems (Position=max+1) + INSERT ActivityLogs + INSERT Notifications(assignee) + INSERT AuditLogs(after)
  → commit → 201 {task}
  → NotificationHub → assignee's web client (realtime badge) if subscribed
  → TanStack invalidates board query
```

Atomic TX; all-or-nothing. Fan-out decision: web realtime; mobile next-load (see diagram 05B).

## 3. AI Copilot (read + propose-before-write)

```
[Web] Copilot "what's blocked?"
  → Trigger realtime WS → ai/ griotCopilot agent
  → tool call get_board(boardId) → POST /graphql (GRIOT_SERVICE_TOKEN + X-On-Behalf-Of → real-user OBO principal)
  → SQL Server → results → agent (LLM reasoning) → streamed answer to web
  → if a write is needed: agent returns PROPOSAL → web renders approval card
  → user approves → WEB calls POST /api/tasks (normal REST path) → caches update
  → every tool call → ActivityLogs (workspaceId, tool, payloadHash, runId)
```

AI NEVER writes to SQL Server directly; the human/app performs writes.

## 4. Scheduled AI digest

```
PLANNED: Trigger cron (daily) → backend-stored authorized schedule → sprintDigest → permitted GraphQL ActivityLogs reads. Notification creation remains unavailable until backend 22 ships its authorized fan-out route; CreateNotification is reserved today. Backend 20 must supply durable recovery and activity writers first.
```

## 5. External AI client (MCP)

```
Claude/Cursor/Cline → mcp/ (stdio local or Streamable HTTP) → tool get_board / create_task / ...
  → POST /graphql (GRIOT_SERVICE_TOKEN + X-On-Behalf-Of → real-user OBO principal)
  → SQL Server → JSON content → client
```

## 6. Mobile

```
Flutter app → same REST + GraphQL; access token in memory; refresh in flutter_secure_storage; 401 → refresh → retry once.
Reads via graphql_flutter (explicit refetch after writes); writes via dio.
```

## 7. Failure paths (decided in RISK-REGISTER)

- Redis down → rate limiting fails **closed** (deny login bursts) — documented in auth ADR.
- Refresh replay → whole family revoked, 401, client must re-login.
- Migration on deploy → runs as Railway release command; failure blocks promote.
- Board N+1 → prevented by DataLoader + indexes; caught by k6 + ApiLogs p95.

---

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**

## Implemented authentication contract (Feature 07)

Use the [auth contract](../api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

## Scheduled AI identity (planned consumers)

Scheduled jobs read via GraphQL and write via the implemented REST routes only — `POST /api/boards/{id}/tasks` (CreateTask) and `POST /api/tasks/{id}/comments` (AddComment) — using Bearer GRIOT_SERVICE_TOKEN plus X-On-Behalf-Of from the backend-stored authorized schedule. A live backend user/workspace/scope/expiry delegation and current membership are required; a cron task cannot pick an arbitrary user. CreateNotification is a reserved scope today, but no notification-create REST route or GraphQL mutation exists yet — its write support is deferred to backend 22. Durable job recovery/callback processing is a backend 20 prerequisite.
