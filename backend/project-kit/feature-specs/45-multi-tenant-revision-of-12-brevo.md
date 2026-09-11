# Feature 45 — Multi-Tenant Revision of Feature 12 (Communication Channels — Brevo, Email Only) (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented** (revision of implemented spec 12; the original spec 12 file remains untouched — this revision supersedes it for tenant behavior)

## What This Delivers

The tenant email additions over the implemented Email-only contract: **per-company sender metadata stays on the single dashboard-verified sender** (`Brevo:FromEmail` — no per-tenant sender identities; domain verification per company is explicitly out of scope), plus **org invite and lifecycle emails** (onboard invite, suspend notice, offboard/export notice to the company owner + SuperAdmin) — while the OTP/register email flows are unchanged.

## Dependencies

- Implemented spec 12 (`IEmailService`, `BrevoEmailService`, branded templates, single verified sender ✅)
- Specs 32/33 (lifecycle events that trigger the new emails), spec 29/46 (org invite chain)
- Spec 22 revision (fan-out routing for the lifecycle notices)

## Context To Read First

- `docs/communication/COMMUNICATION-GUIDE.md` §7b (why per-company senders fail — a domain YOU own must be verified; `griot.vercel.app` cannot be authenticated)
- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §5 (lifecycle)
- Original spec: `backend/project-kit/feature-specs/12-communication-channels-brevo.md`

## Agent Skills To Use

- Root `.agents/skills/contract-sync/SKILL.md`
- (Brevo usage unchanged from spec 12 — `IEmailService` best-effort contract)

## Files Owned

- `Griot.Application/Services/OrganizationEmailComposer.cs` (lifecycle email purposes)
- `Griot.Infrastructure.Email/BrandedEmailTemplate.cs` (new tenant template bodies)
- `docs/communication/COMMUNICATION-GUIDE.md` (tenant purposes section)

## Implementation Notes

- **Single verified sender retained:** every tenant email sends from the one dashboard-verified `Brevo:FromEmail`/`BREVO_FROM_EMAIL`; `OrganizationPlan`/company branding never changes the sender, reply-to stays optional per-call (default none). Per-company SMTP/dkim/domain auth is a documented non-goal until a company owns + verifies its own domain.
- New email purposes (PLANNED), each with a branded template in the spec-12 shell: `organization_invite` (company onboarding invite via `OrganizationInvites`, specs 32/46), `organization_suspended` (notice to the company Owner), `reactivated` (optional owner notice), `offboard_started` (owner + SuperAdmin), `data_exported` (export-bundle blob link, spec 33), `purged` (final confirmation record).
- Recipients resolved **within the org**: owner email from `Organizations.OwnerId` → `Users.Email`; SuperAdmin recipients from the platform audience (spec 27's fixed audience) — never from client-supplied addresses.
- **OTP unchanged:** `email_verify` auto-send on register, `/api/auth/otp/request` (3/15min/email Redis gate, 502 surfaced failure), `login_2fa`, `password_reset` templates and gates all untouched by this revision.
- Best-effort semantics preserved: lifecycle email failure never fails the API request; delivery failure → `ErrorLogs` row (spec 20) — the only surfaced-failure path in the system remains `/api/auth/otp/request` (502).
- Rate/budget protection: the API global limiter (100/min/caller) + per-event fan-out cap (25 recipients, spec 22) keep Brevo's 300/day free cap unreachable from app code; lifecycle emails are one-per-event (no digests this wave).
- `X-Griot-Notification-Id` / request-id tracing carried into the new purposes for spec-20 correlation; audit rows carry `OrganizationId`.
- Register-time "New user registered" admin notice (`Brevo:ContactToEmail`) unchanged; no new optional-contacts surface added.

## Separation of Concerns

Email composition + recipients: `Griot.Application`; transport + templates: `Griot.Infrastructure.Email` (existing `IEmailService`). Lifecycle **orchestration** (which event fires which email) is specs 32/33; this revision only supplies the composition and keeps the sender/best-effort contract.

## Acceptance Criteria

- [ ] Organization invite email sends from the single verified sender with the branded `organization_invite` template; accept links the invite token flow
- [ ] Suspend/offboard/export/purge events produce exactly one email per event to the resolved in-org recipients (idempotent per event id)
- [ ] OTP/register email flows unchanged (existing spec-12 tests still green)
- [ ] Brevo failure during a lifecycle email → request still succeeds; `ErrorLogs` row with org id exists
- [ ] No per-company sender identity is introduced (sender constant asserted in tests)
- [ ] `dotnet build` + `dotnet test` green

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.