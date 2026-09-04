# Security Policy for Griot

## Supported versions

| Version | Supported |
|---|---|
| main (current) | ✅ |

## Reporting a vulnerability

Please **do not** open a public issue for security bugs. Report privately:

- Email: `info.donartkins.ke@gmail.com` (GTP bootcamp contact)
- Please include: affected endpoint/component, a minimal repro, expected vs actual behavior, and impact.

You'll get an acknowledgment within **48 hours**. We'll confirm, fix, test, and document the fix (see `docs/planning/CHANGE-MANAGEMENT.md`).

## Security posture (what we build toward)

- **Auth**: Argon2 password hashing; 15-min JWT access; opaque rotated refresh tokens (SHA-256 at rest, family revoke on reuse); Redis sliding-window rate limits.
- **AI boundary**: Two distinct AI write paths — both go through the backend with a scoped `GRIOT_SERVICE_TOKEN` (restricted `ai-agent` principal, no deletes/invites):
  1. **Copilot mutations (propose-before-write)**: The Copilot agent _proposes_ mutations (`create_task`, `update_task_status`, `add_comment`) as structured approval cards; the **web app executes the mutation via REST after explicit user approval** — the agent itself never writes. This is the only path for Copilot-initiated state changes (features ai/04 + backend/09; see `ai/project-kit/feature-specs/04-propose-before-write-workflow.md`).
  2. **Deterministic/scheduled writes** (due reminders, sprint digests, stale-board notifications): these bypass the approval step by design and write directly via backend REST. They are workspace-scoped and audit-logged (feature ai/03).
  - **No direct DB access by AI.** No GraphQL mutation path for Copilot writes — REST only, after approval, to preserve the audit trail and `AuditLogs`/`ActivityLogs` rows.
  - Synchronized in: `ai/project-kit/feature-specs/04-propose-before-write-workflow.md`, `backend/project-kit/feature-specs/09-ai-service-token-and-webhooks.md`, `backend/project-kit/context/api-surface.md` (webhook route + REST mutation surface), and diagram prompt `PROMPTS/week-02/14-…ai-system-context`.
- **Secrets**: `.env` git-ignored; `.env.example` is the documented shape; env-injected at runtime (Docker/Railway/Vercel); no keys in code or images.
- **API**: parameterized SQL only (Dapper procs); CORS allow-list; ownership failures return 404 (never disclose existence); query-cost guard on `/graphql`.
- **Web/mobile**: no tokens in `localStorage`; refresh in `httpOnly` cookie (web) / secure storage (mobile); silent-refresh-once discipline.

## OWASP + audit

Weekly-6/7 include an OWASP Top-10 review (logged per item) and every state-changing write is captured in `AuditLogs` + `ActivityLogs`. See `qa/project-kit/` + `docs/observability/MONITORING.md`.

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**