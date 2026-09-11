# AuditLogs RequestId + incident index amendment

Date: 2026-09-11. Scope authorized by the backend Feature 20 (observability/logging
pipeline) implementation request: make the four log tables correlate by the same
`X-Request-Id` the client saw, and prove ADR-002's incident indexes exist as
columns/schema. This supplements the frozen `griot-erd-v1.0.0.png` export and
ADR-002 (`docs/decisions/ADR-002-observability-audit-tables.md`).

| Field / index | Contract |
|---|---|
| `AuditLogs.RequestId` | Nullable `uniqueidentifier`; the normalized request id of the HTTP request (or auth event) that produced the row. Null only for durable audit writes with no HTTP context. |
| `IX_ApiLogs_CreatedAt` | Standalone non-unique index — spillover-window + "how many users affected" queries scan `ApiLogs` by time alone (spec 18/25 consumers). |
| `IX_AuditLogs_EntityType_EntityId_CreatedAt` | Composite index — "what a mutation changed" recipe (`AuditLogs WHERE EntityType/EntityId ORDER BY CreatedAt DESC`). |
| `IX_ErrorLogs_RequestId` | Non-unique index — "one request, one incident" recipe (`ErrorLogs WHERE RequestId = @req`). |

`ApiLogs.RequestId` (already unique per ERD v1) and `ErrorLogs.RequestId`
(nullable) keep their existing shapes. The request-id middleware normalizes
`X-Request-Id` to a GUID (assigned when absent, replaced when malformed) so
every log row, the `ApiLogs.RequestId` column and the response header carry
the same value.

**PLANNED (future amendment, not in this migration):** `ApiLogs.RunId` +
`AuditLogs.RunId` (nvarchar(64), nullable) linking Trigger.dev run ids —
delivered with the ai-layer ERD work (backend 24/25). The durable-job/outbox
and idempotency schema likewise ships as its own amendment before any
TriggerDevClient caller is wired (backend 09 review gate; webhook stays 503
until then).

Migration `AddObservabilityAuditRequestIdAndIndexes` is additive and nullable:
no entity or enum is renamed, no existing row is rewritten. The original PNG
remains a historical export; read it together with this amendment for the
current observability schema.