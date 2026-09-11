# Integration Contracts — Mobile (Flutter 3.19+ / Dart 3)

Mobile consumes the same .NET API surface as web. Auth contract: `docs/api/auth-contract.md`. AI orchestration boundary: `research/ai-integration.md` §2a (mobile never touches Trigger.dev or MCP).

## Multi-Tenant contract (PLANNED — 2026-09-11 wave)

Canonical contract: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md`. Everything below is PLANNED (2026-09-11 wave) — not implemented acceptance evidence.

- **JWT v2 access token (owner: backend spec 30, HS256, 15 min):** adds claims `name` (`Users.DisplayName`), `org` (active `OrganizationId`), `role` (effective role in the active org: `super_admin`, `Owner`/`Admin`, `ProjectManager`, `Member`, `Client`, `custom:{roleId}`), `perms` (space-separated permission keys from the fixed catalogue, e.g. `org.read`, `project.manage`, `client.feedback.write`).
- **Refresh token stays opaque — deliberately NOT a JWT.** 64-hex random bytes, SHA-256 at rest, rotation + family revoke (unchanged). Mobile never parses it; it lives only in `flutter_secure_storage`. jwt.io decoding it blank is correct behavior.
- **`POST /api/auth/select-organization`** switches the active organization and **re-issues the token pair** (new access + rotated refresh). Mobile must atomically replace both tokens, cancel in-flight requests, invalidate provider state, and refetch per-org data.
- **Org-scoped routes:** the server resolves tenant context from the token `org` claim — never from a caller-supplied header or body. Mobile sends only the Bearer token; cross-tenant isolation (EF Core global query filters) is server-side. Suspended org → writes rejected with **`403 org_suspended`**.
- **Client portal routes (backend spec 34):** client-scoped progress reads (`ClientProjectViewDto`: percent-complete, milestones, activity digest), `ClientFeedback` create/list (`ClientFeedbackDto`: `Kind` = Comment/Suggestion/EditRequest, `Status` = New/Acknowledged/Resolved/Rejected, `PmResponse`). Board internals are absent from the client-authorized surface.
- **Handoff + maintenance routes (backend spec 35):** `ProjectHandoffs` checklist/status, `HandoffDocuments` (BlobKey via backend 11), client acceptance, maintenance/post-deployment support requests (project status `Maintenance`/`PostDeploymentSupport`).
- **Notifications** are org-scoped (backend 22 bump); fan-out keys partition per org. AI triage summaries (ai 13) arrive only through backend notifications — mobile never calls Trigger.dev.
- **Secure-storage note:** access token in memory; refresh token in `flutter_secure_storage` exactly as implemented today — the multi-tenant wave changes token *contents* (claims), not token *storage*.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.