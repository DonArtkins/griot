# Stack Contract (from `GTP 2026 BOOTCAMP EDITION.pdf` §3)

Status codes: **exact** = bootcamp-mandated, use as-is. **[own-stack]** = the guide is silent; personal choice, always flagged.

## Backend (§3.1)

| Item | Status | Griot use | Owner kit |
|---|---|---|---|
| .NET 8 (LTS) | exact | Web API host, `net8.0`, C# 12 | backend |
| ASP.NET Core Web API | exact | REST controllers + HotChocolate in one process | backend |
| Entity Framework Core 8 | exact | Code-first migrations to SQL Server | backend |
| Dapper 2.x | exact | Stored-proc/raw-SQL hot paths | backend |
| HotChocolate GraphQL 14+ | exact | `/graphql` beside `/api` | backend |
| SQL Server 2022 | exact | Primary DB (official Linux container, port 14333) | backend |
| PostgreSQL 16 | exact | Secondary/test store (port 5433) | backend |

## Frontend (§3.2)

| Item | Status | Griot use | Owner kit |
|---|---|---|---|
| React 18.3+ | exact | `react@18.3` via Vite template | web |
| Vite 5+ | exact | Build/dev server; Vercel Vite preset | web/infra |
| Material UI v6 | exact | Re-themed from figma tokens (`createTheme`) | web |
| Apollo Client | exact | GraphQL reads | web |
| Axios | exact | REST calls | web |
| TanStack Query (React Query 5) | exact | REST server-state cache | web |
| Client state, router, motion | [own-stack] | Zustand, React Router, GSAP+Lenis | web |

## Mobile (§3.3)

| Item | Status | Griot use | Owner kit |
|---|---|---|---|
| Flutter 3.19+ / Dart 3 | exact | Android companion app | mobile |
| GraphQL Flutter | exact | `graphql_flutter` → same endpoint | mobile |
| Provider / Riverpod | Riverpod chosen | guide allows either | mobile |
| REST (dio), secure storage | [own-stack] | dio + flutter_secure_storage | mobile |

## DevOps (§3.4)

| Item | Status | Griot use | Owner kit |
|---|---|---|---|
| Docker 26+ | exact | real Engine on Parrot (not the Podman shim) | infra |
| Docker Compose v2 | exact | local + prod parity topology | infra |
| Vercel | exact | frontend hosting (Vite preset) | infra |
| GitHub Actions | exact | PR gates + deploy | infra |
| Azure / Railway / Render | Railway primary, Render fallback, Azure variant | PDF offers all three | infra |

## Own-stack fills (guide is silent)

| Area | Choice | Owner kit |
|---|---|---|
| Communication | Brevo transactional Email only (single verified sender `Brevo:FromEmail` + per-call reply-to; 2026-09-11: `Brevo:Senders:*` profile map removed) | backend |
| Auth | Custom JWT (15-min access, rotated opaque refresh, Argon2, Redis sliding-window rate limit, Brevo Email OTP 2FA — critical-action OTP/step-up on login/forgot/reset/delete/guarded ops spec 23 PLANNED) | backend |
| Perf/load testing | k6 | qa |
| AI layer | Trigger.dev v4 (CLI/SDK/react-hooks pinned to 4.5.16; Node 20 toolchain) Level-4 Autonomous Agents (reasoning loops, human-in-loop) + Griot MCP server + in-app Copilot — orchestrated by the .NET backend only; Trigger.dev = compute adapter, never a data owner (`research/ai-integration.md` §2a). Superpowers wave (PLANNED): knowledge agent + system auditor (ai 06), report generation PDF/CSV (ai 07), advanced executor (ai 08) — all behind `CreateReport` scope (backend 24), never loosening the OBO grant, never touching auth/OTP | ai, mcp, web |
| System Reports | SQL Server stored procedures + Trigger.dev scheduled digests + AI ad-hoc generation — surface = backend 24 (Report rows + PDF/CSV artifacts + audit-summary) | backend, ai, web |
| Contract testing | Postman collection → Newman | qa |

**Rule:** no substitution of a bootcamp-mandated item without updating this table and the owning system's architecture context in the same branch.

## Implemented authentication contract (Feature 07)

Use the [auth contract](../../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

## Multi-Tenant Migration Wave — 2026-09-11 [own-stack] (PLANNED)

Tenancy model decision for this wave: **Pool model** (shared SQL Server 2022 schema, `OrganizationId` per tenant row, EF Core global query filters + `ITenantContext` from the JWT `org` claim; SQL Server has no RLS, so the repository guard + integration tests are part of the contract). Canonical: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md`; planned ERD amendment: `diagrams/erd/multi-tenant-amendment.md`. No stack substitution: .NET 8 / EF Core 8 / SQL Server 2022 / Redis / Brevo / HotChocolate all carry the tenant dimension without new infrastructure. New env (PLANNED): `JWT__Key` ≥64 chars, `SUPERADMIN__EMAIL`, `SUPERADMIN__PASSWORD`, `Organizations:RetentionDays`.

## Trigger.dev version correction — 2026-09-11

[own-stack] Preserve the v4 adoption from commit `7e90c5b`; the previous v3 descriptions were stale. The [vendor migration notice](https://trigger.dev/docs/migrating-from-v3) records the v3 cloud shutdown on July 1, 2026. AI 01 pins CLI/SDK/react-hooks together at 4.5.16. No task or cloud deployment is claimed by this dependency correction.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
