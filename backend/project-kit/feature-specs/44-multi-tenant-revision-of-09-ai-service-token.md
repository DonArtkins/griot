# Feature 44 — Multi-Tenant Revision of Feature 09 (AI Service Token & Webhooks) (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented** (revision of implemented spec 09; the original spec 09 file remains untouched — this revision supersedes it for tenant behavior)

## What This Delivers

The AI OBO boundary resolved **within the org tenant**: `ServiceToken:Delegations:{userId}` gains an `OrganizationId`, the trusted `X-On-Behalf-Of` principal is resolved against that org (global filters + `ITenantContext` apply to every AI read/write), **client-role OBO capabilities** expose progress-only + feedback-only tools, the **default-deny** mapping of `AiAccessFilter`/`AiFieldMiddleware` is unchanged, and the planned HMAC **webhook inbox rows are org-stamped** for the durable replay-safe dispatch (spec 20's planned gate).

## Dependencies

- Implemented spec 09 (service token, OBO resolution, MVC filter + GraphQL middleware ✅)
- Spec 30 (JWT `org` claim), spec 37 (filters), spec 39 (suspend gate: suspended org blocks OBO writes too)
- Spec 25 revision (capability gateway the client-role OBO tools flow through)
- Spec 20 (HMAC inbox / durable dispatch — its planned gate is org-stamped by this revision)

## Context To Read First

- `docs/api/ai-service-token-contract.md` (implemented boundary + this wave's PLANNED section)
- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §1, §6 (client AI boundary)
- Original spec: `backend/project-kit/feature-specs/09-ai-service-token-and-webhooks.md`

## Agent Skills To Use

- `backend/.agents/skills/jwt-argon2-auth/SKILL.md` (token validation)
- `backend/.agents/skills/dotnet-ef-core/SKILL.md`
- Root `.agents/skills/contract-sync/SKILL.md`

## Files Owned

- `Griot.Application/Services/ServiceTokenDelegationService.cs` (delegation gains `OrganizationId`)
- `Griot.Api/Filters/AiAccessFilter.cs` + `AiFieldMiddleware` (org binding + default-deny unchanged)
- HMAC webhook inbox handling (`Griot.Application`/`Griot.Infrastructure`) — org-stamped rows
- `docs/api/ai-service-token-contract.md` PLANNED section update

## Implementation Notes

- Delegation contract extension (PLANNED): `ServiceToken:Delegations:{userId}` gains **`OrganizationId`**; `WorkspaceIds` must all belong to that org — validation fails closed on any mismatch (delegation rejected before OBO resolution).
- `X-On-Behalf-Of` resolves to a real-user **On-Behalf-Of** principal as today (role `ai-on-behalf-of`, exactly the four scopes ReadWorkspace/CreateTask/AddComment/CreateNotification; no deletes, no invites, no member management) — now additionally bound into the **org tenant scope**: every OBO query runs through the spec-37 global filters for the delegation's org.
- OBO user's **org role** shapes capabilities (spec 25 revision): a `Client` OBO exposes only progress-read + feedback-add tools (`client.read_progress` / `client.add_feedback`) — internal task/board data never enters the response; `Admin` OBO sees the org's own data; cross-org capability never advertised.
- Default-deny unchanged: `AiAccessFilter` + `AiFieldMiddleware` reject any unmapped REST/GraphQL operation; raw-log reads stay permanently closed; auth/OTP/deletes/invites/member management untouched. CreateNotification fan-out is org-scoped (recipients resolved within the delegation org — spec 22 revision).
- Suspend interplay: in a `Suspended`/`Offboarding` org, OBO **writes** hit the same 403 `org_suspended` gate as human writes (reads allowed, spec 32 semantics).
- HMAC webhook inbox (spec 20's planned durable dispatch): inbox rows gain **`OrganizationId`** stamped from the bound job; unknown/expired jobs still cannot select a new user/thread/report/recipient; replay window, 401 bad HMAC, 413 oversized, 503 unconfigured — all unchanged from implemented 09.
- The future fifth scope `CreateReport` (spec 24) is capability-gated within the delegation's org — created here as contract-sync only, never implemented ahead of spec 24.
- Delegation/audit changes carry org ids so the spec-20 trail answers "which tenant" for every AI action.

## Separation of Concerns

AI principals never resolve tenants themselves — the backend binds the delegation's `OrganizationId` into `ITenantContext`; authorization (scopes + capabilities) stays in `Griot.Application` policy; `ai/` + `mcp/` remain data-plane consumers of scoped REST/GraphQL only.

## Acceptance Criteria

- [ ] OBO request resolved within the delegation's org: cross-org data returns empty (two-org seeded test)
- [ ] Delegation with mismatched org/workspace ids rejected (fail-closed) before any data access
- [ ] Client-role OBO exposes progress + feedback tools only; internal task data never reaches the OBO response (contract test)
- [ ] Default-deny unchanged: unmapped operations 403; raw-log reads closed for AI
- [ ] HMAC inbox rows carry `OrganizationId`; replay/oversize/unconfigured behaviors unchanged (401/413/503)
- [ ] `dotnet build` + `dotnet test` green (existing OBO suite unbroken)

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.