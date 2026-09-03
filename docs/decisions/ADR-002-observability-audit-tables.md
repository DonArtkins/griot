# ADR-002 — Observability & Audit Tables in the Core Schema (ApiLogs · ErrorLogs · AuditLogs)

**Status:** Accepted  
**Date:** 2026-09-03

## Context

The user's hard requirement: **no coming back later to redesign the schema** ("OOOH I REALISE I NEED AN ERROR LOGS TRACKING TABLE..."). The original ERD had 13 tables but no error-tracking or audit trail. The Lyncxs KB frames audit + error tracking as core production readiness. Without a decision now, we'd add tables mid-implementation and ripple through every dependent artifact.

## Decision

Add **ApiLogs**, **ErrorLogs**, **AuditLogs** to the core schema (ERD → prompts G+H → `data-layer.md` → backend spec 02 → EF) **before** implementation. Now the schema is 16 tables + 5 enums and supports request tracing, error lifecycle, and change history out of the box.

## Options considered

| Option | Pros | Cons |
|---|---|---|
| **Add observability tables now** (chosen) | no schema churn later; audit + errors designed-in; OWASP/observability lines satisfied from day 1 | none worth a change |
| Add later when "needed" | simpler ERD | violates the no-rework requirement; ripples through ERD → migration → specs → types |
| External logging only (no tables) | simpler DB | loses traceability to user/request/entity; worse audit story |

## Consequences

- 16 tables, 5 enums; `ApiLogs`/`ErrorLogs`/`AuditLogs` written by the normal pipeline (middleware + services), append-only.
- Indexes + retention (90-day prune) documented in `docs/database/DATABASE-DESIGN.md`.
- Backend feature spec 02 must map these; MONITORING.md consumes them.

## References
- `PROMPTS/week-02/02-erd-figma-make-master-prompt.md` (single extensive master prompt with the observability tables included)
- `docs/database/DATABASE-DESIGN.md`, Lyncxs WCPSE observability sections.