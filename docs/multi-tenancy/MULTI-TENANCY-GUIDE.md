# Griot Multi-Tenancy Guide — Companies, Lifecycle, Roles & Client Support [own-stack]

**Created:** 2026-09-11 · **Status:** PLANNING WAVE (no production code yet)
**Source of research:** `research/LYNCXS-MULTI-TENANT-SYSTEMS-ENGINEERING.md` (v1.0.0)
**Owner:** backend spec 29 (foundation) — dependents: backend 30–51, all other systems' bumped specs
**Contract-sync:** every cross-system surface in this file is mirrored in `project-kit/context/integration-contracts.md`, `docs/api/auth-contract.md`, and each owning spec.

This document is the single canonical contract for converting Griot from a single-tenant project-management app into a **multi-tenant platform**: the operator (you) onboards **Companies (Organizations)** as tenants, each company gets an **Admin (owner of the company)**, and the rest of the existing hierarchy (workspaces → projects → boards → tasks) hangs *below* the company. Everything here is **PLANNED** — it becomes implemented acceptance evidence only when its owning spec ships on its own feature branch.

## 1. Tenancy model — Pool (research §2, §12.1)

**Decision (ADR-005 to be written at implementation):** **Pool model** — one SQL Server 2022 database, one schema, every tenant-owned row carries `OrganizationId`. Per the research volume, Pool is the correct default for standard B2B SaaS with many small-to-mid tenants. Bridge/Silo (schema-per-tenant / database-per-tenant) and Hybrid promotion are explicitly **deferred**; revisit only if an enterprise contract demands physical isolation.

- SQL Server has no Postgres RLS. The isolation boundary is therefore **three layers that survive an application bug**:
  1. `ITenantContext` resolved per request by `TenantResolutionMiddleware` (from the JWT `org` claim — never from a caller-supplied header/body) and exposed via `IHttpContextAccessor`.
  2. **EF Core global query filters** (`HasQueryFilter(e => e.OrganizationId == tenantContext.OrganizationId)`) on every tenant-owned entity, so even a forgotten `Where` cannot cross tenants; writes assert `OrganizationId` matches the context or throw.
  3. **Repository guard** in `IGenericRepository` save paths + integration tests that prove cross-tenant reads return empty and writes are rejected (qa spec 14).
- Cache keys, rate-limit partitions, notification fan-out jobs and outbox rows all carry the tenant id (`cache:org:{orgId}:…`, `ratelimit:org:{orgId}:…`) — see backend 19/22 bumps.
- `ApiLogs`/`ErrorLogs`/`AuditLogs`/`ActivityLogs` gain `OrganizationId` (nullable for platform-level/system events) so per-tenant log reads are possible (feeds backend 25 role-tiered log access and mcp 07).

## 2. Canonical domain model (ERD amendment v2 — PLANNED, Figma Make approval required before schema code)

New/changed entities (names are contract once the ERD amendment is approved — full column/index list: `diagrams/erd/multi-tenant-amendment.md`):

| Entity | Purpose | Key fields |
|---|---|---|
| `Organizations` | The tenant (a Company) | `Id`, `Name`, `Slug` (unique), `OwnerId` (the company Admin), `Status` (`OrganizationStatus`: `Active`, `Suspended`, `Offboarding`, `Archived`), `Plan` (`OrganizationPlan`: `Free`, `Pro`, `Enterprise` — display/billing metadata only, no payments this wave), `CreatedAt`, `UpdatedAt`, `OffboardedAt?` |
| `OrganizationMembers` | A user's membership in a company | `OrganizationId`, `UserId`, `Role` (`OrganizationRole`), `CustomRoleId?` (FK `Roles`, only when `Role == Custom`), `Status` (`Invited`/`Active`/`Suspended`), `JoinedAt` |
| `Roles` | Custom roles created **inside one company** by its Admin/PM | `OrganizationId`, `Name`, `IsSystem` (bool), `Permissions` (comma-separated permission keys) |
| `OrganizationInvites` | Company-scoped invite chain (workspace invites remain) | `OrganizationId`, `Email`, `Role`, `CustomRoleId?`, `Token`, `ExpiresAt`, `AcceptedAt?` |
| `ProjectClients` | A client (user w/ `Client` role) attached to a project | `ProjectId`, `UserId`, `OrganizationId`, `AccessLevel` (`ProgressOnly` default), `AddedAt`, `RemovedAt?` |
| `ClientFeedback` | Client comments/suggestions routed to the PM | `ProjectId`, `AuthorUserId`, `Body`, `Kind` (`Comment`/`Suggestion`/`EditRequest`), `Status` (`New`/`Acknowledged`/`Resolved`/`Rejected`), `PmResponse?`, `CreatedAt` |
| `ProjectHandoffs` | The handoff record per project | `ProjectId`, `Status` (`NotStarted`/`InProgress`/`AwaitingClientAcceptance`/`Completed`), `ChecklistJson`, `InitiatedBy`, `CompletedAt?` |
| `HandoffDocuments` | Files/artifacts handed to the client | `HandoffId`, `Title`, `BlobKey` (backend 11), `Kind` (`Manual`/`Credential`/`Design`/`Report`/`Other`), `GeneratedByAi` (bool), `UploadedBy`, `CreatedAt` |
| `OrganizationLifecycleEvents` | Onboarding/offboarding/suspension audit trail | `OrganizationId`, `Kind` (`Onboarded`/`Suspended`/`Reactivated`/`OffboardStarted`/`DataExported`/`Offboarded`/`Purged`), `ActorUserId`, `PayloadJson`, `CreatedAt` |

Enums: `OrganizationStatus`, `OrganizationPlan`, `OrganizationRole` (`Owner`, `Admin`, `ProjectManager`, `Member`, `Client`, `Custom`), `PlatformRole` (`User`, `SuperAdmin`), `ClientFeedbackKind`, `ClientFeedbackStatus`, `HandoffStatus`, `HandoffDocumentKind`, `LifecycleEventKind`. `ProjectStatus` gains `Handoff`, `Maintenance`, `PostDeploymentSupport` values (append-only; existing ordinals unchanged).

**Tenant column rollout:** `Workspaces`, `Projects`, `Boards`, `Columns`, `TaskItems`, `Comments`, `Attachments`, `Invites`, `Notifications`, and the four observability tables all gain `OrganizationId` (boards/columns store their project's org id). `Users` and `RefreshTokens` stay **global** (a user may belong to many companies; sessions survive org switching).


## 3. Role model — layered RBAC (research §6.1–6.5)

Three layers, resolved in precedence order (highest wins):

1. **Platform role** — `Users.PlatformRole` (`User` default, `SuperAdmin`). SuperAdmin is **the operator (owner of Griot itself)**: manages all companies, sees the platform console, is the only actor who can onboard/suspend/offboard companies. Bootstrapped idempotently from `SUPERADMIN__EMAIL` env on startup.
2. **Organization role** — `OrganizationMembers.Role`. System-seeded per company: **`Owner`/`Admin` (the company owner — can view ALL projects under that company only, manage members, create custom roles), `ProjectManager` (manages only their assigned projects), `Member`**. Admin/PM can **create additional custom roles** in their own company (`Roles`; `Role == Custom` + `CustomRoleId`), composing only the fixed permission catalogue (no platform or lifecycle powers, ever).
3. **Project/client scope** — a ProjectManager's authority is bounded to projects where they are assigned; **`Client`** org role sees only `ProjectClients`-attached projects in the client-portal view (read-only progress + feedback), enforced server-side and mirrored by the AI capability gateway (backend 25 bump, ai 13).

**Permission catalogue (fixed keys, used by the `perms` JWT claim):** `org.read`, `org.settings.manage`, `org.members.manage`, `org.roles.manage`, `org.projects.view_all`, `project.manage`, `task.manage`, `comment.write`, `client.manage`, `client.feedback.read`, `client.feedback.respond`, `report.generate`, `log.read_tier` (tier rules owned by backend 25). Custom roles compose these keys only.

| Actor | Sees |
|---|---|
| SuperAdmin (platform) | everything — all companies, platform console, lifecycle |
| Company Admin (`Owner`/`Admin`) | ALL projects inside their company only |
| ProjectManager | only their assigned projects (full manage) |
| Member | workspaces/projects they are members of |
| Client | only their attached projects' progress view + feedback |

## 4. JWT v2 — token contract (owner: backend spec 30)

- **Access token (JWT, HS256, 15 min — lifetimes unchanged):** adds claims to the existing `sub`, `email`, `jti`, `iss=Griot`, `aud=GriotClients`:
  - `name` — `Users.DisplayName`
  - `org` — the **active** `OrganizationId` (selected via `POST /api/auth/select-organization`; re-issued on switch). Absent only for platform-only sessions.
  - `role` — effective role in the active org (`super_admin` when `PlatformRole == SuperAdmin`, else the `OrganizationRole`; `custom:{roleId}` for custom roles)
  - `perms` — space-separated permission keys for the active role (empty for pure `Member`)
- **Refresh token stays opaque — deliberately.** 64-hex random bytes, SHA-256 at rest, rotation + family revoke (unchanged). It is *not* a JWT and must never become one: putting user data into a long-lived token violates the research volume §5 (opaque rotating refresh tokens) and OWASP guidance; jwt.io showing it blank is **correct behavior**, not a defect. Verification: the access token decodes **and verifies** at jwt.io when the real `Jwt:Key` is pasted — `a-string-secret-at-least-256-bits-long` is jwt.io's *placeholder*, which is why pasting it fails verification.
- **Signing key policy:** `JWT__Key` must be ≥ 64 chars (512 bits) in production, generated via a CSPRNG, stored in the platform secret store (Railway env), never committed (`appsettings.Local.json` is git-ignored); rotation runbook documented in spec 30. Dev keys already exceed 256 bits.
- SuperAdmin bootstrap, org-switch endpoint and the full route table live in `docs/api/auth-contract.md` (PLANNED section added in this wave).

## 5. Company lifecycle — onboarding → manage → offboard (owners: backend 32/33)

**Onboard (SuperAdmin):** `POST /api/organizations` (SuperAdmin; self-serve signup is a later option) → creates `Organizations` row + owner user (invite-by-email flow reusing Brevo branded templates) + seeds the system roles + a default workspace + `OrganizationLifecycleEvents(Onboarded)`. Bulk provisioning (SCIM 2.0) is deferred per research §12.4 until an enterprise customer demands it.

**Manage:** suspend (`POST /api/organizations/{id}/suspend` — auth kept, writes rejected with 403 `org_suspended`), reactivate, ownership transfer, plan/metadata edits. The Company Admin manages everything *inside* their company only.

**Offboard (SuperAdmin):** `POST /api/organizations/{id}/offboard` → 1) export bundle (JSON of all org data; artifacts via blob backend 11) → 2) `Status = Offboarding`, 30-day retention window (`Organizations:RetentionDays`, default 30) → 3) purge/anonymize (`Purged` lifecycle event; personal data removed, legal/financial records retained per research §8.3). Every step is an audit event; SuperAdmin notifications use backend 27 broadcasts.

## 6. Client support surface (owners: backend 34, web 15, ai 13)

The most important product axis: **client satisfaction**. A project's client is onboarded (`ProjectClients`, `Client` role, scoped invite), sees a **different progress view** (percent-complete, milestones, recent activity digest — no internal board internals beyond the granted level), can **comment and suggest edits** (`ClientFeedback` → routed to the assigned ProjectManager via notification + AI-assisted triage summary), and the AI (ai 13) answers client questions **only** from client-scoped data (capability gateway extension of backend 25).

## 7. Handoff, client offboarding, maintenance phase (owners: backend 35, web 16, ai 14)

Project close-out: `ProjectHandoffs` checklist (deliverables, credentials, environments, docs) → **AI generates the client user manual** into `HandoffDocuments` (`Kind = Manual`, `GeneratedByAi = true`) → client accepts → **client offboarding** (access narrowed to portal-read-only + maintenance requests) → project moves to `Maintenance`/`PostDeploymentSupport` where the client can raise support requests and the PM (or AI triage) responds. **Client data is retained** after offboarding so maintenance stays possible; only full *company* offboarding purges.

## 8. Payments — explicitly out of scope this wave

Research §7 is recorded but deferred: `OrganizationPlan` is display metadata only; no Stripe/subscriptions in this wave (a future wave re-enters through `docs/planning/IMPLEMENTATION-ROADMAP.md`).

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
