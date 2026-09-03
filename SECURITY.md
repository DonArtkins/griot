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
- **AI boundary**: AI/MCP write only through the backend GraphQL with a scoped `GRIOT_SERVICE_TOKEN` (restricted `ai-agent` principal, no deletes/invites). **No direct DB access by AI.**
- **Secrets**: `.env` git-ignored; `.env.example` is the documented shape; env-injected at runtime (Docker/Railway/Vercel); no keys in code or images.
- **API**: parameterized SQL only (Dapper procs); CORS allow-list; ownership failures return 404 (never disclose existence); query-cost guard on `/graphql`.
- **Web/mobile**: no tokens in `localStorage`; refresh in `httpOnly` cookie (web) / secure storage (mobile); silent-refresh-once discipline.

## OWASP + audit

Weekly-6/7 include an OWASP Top-10 review (logged per item) and every state-changing write is captured in `AuditLogs` + `ActivityLogs`. See `qa/project-kit/` + `docs/observability/MONITORING.md`.

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**