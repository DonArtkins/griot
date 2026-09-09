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
| Auth | Custom JWT (15-min access, rotated opaque refresh, Argon2, Redis sliding-window rate limit, Brevo Email OTP 2FA) | backend |
| Perf/load testing | k6 | qa |
| AI layer | Trigger.dev v3 Level-4 Autonomous Agents (reasoning loops, human-in-loop) + Griot MCP server + in-app Copilot | ai, mcp, web |
| System Reports | SQL Server stored procedures + Trigger.dev scheduled digests + AI ad-hoc generation | backend, ai, web |
| Contract testing | Postman collection → Newman | qa |

**Rule:** no substitution of a bootcamp-mandated item without updating this table and the owning system's architecture context in the same branch.

## Implemented authentication contract (Feature 07)

Use the [auth contract](../../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
