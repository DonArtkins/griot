# AGENTS.md — Griot Backend / API (ASP.NET Core 8)

## Read This First

You are the agent for the **Backend / API system** of Griot. This system is the sole owner of data and business logic. Everything else (web, mobile, AI, MCP) talks to this system and nothing else touches SQL Server.

Stack (exact per `project-kit/context/stack-contract.md`): .NET 8, ASP.NET Core Web API, EF Core 8, Dapper 2.x, HotChocolate GraphQL 14+, SQL Server 2022 (primary), PostgreSQL 16 (secondary), Redis 7. Auth is `[own-stack]`: custom JWT + Argon2 + rotated refresh tokens + Brevo Email OTP 2FA. Reports [own-stack]: RBAC-scoped scheduled & ad-hoc.

## Solution Layout (Separation of Concerns)

```
backend/
├── Griot.sln
├── global.json                  # pins SDK 8.0
├── src/
│   ├── Griot.Api/               # hosting only: Program.cs, controllers, GraphQL, middleware
│   ├── Griot.Application/       # services only: business rules, orchestration
│   ├── Griot.Domain/            # entities + enums; ZERO references
│   └── Griot.Infrastructure/    # GriotDbContext, EF repos, Dapper repos, Redis, migrations, Sql/
└── tests/Griot.Tests/           # xUnit + WebApplicationFactory
```

**Rules:** controllers/resolvers are thin (no business logic). `Griot.Application` has no EF/HTTP/Redis. `Griot.Infrastructure` is persistence only. `Griot.Domain` depends on nothing.

## Reading Order

1. Root `AGENTS.md` + `project-kit/context/{system-map,stack-contract,integration-contracts}.md`.
2. `research/week-02-backend-api-development.md` (+ `research/gtp-2026-prep.md` §6.5).
3. `backend/project-kit/context/architecture.md` → `data-layer.md` → `api-surface.md` → `code-standards.md`.
4. The approved ERD at `diagrams/erd/` (entity/enum names are a contract).
5. The current feature spec (one at a time, numeric order).

## Required Skills

Check `/.agents/skills/` (contract-sync, figma-make-erd, git-branch-flow, throttling-prevention) and `backend/.agents/skills/` (dotnet-ef-core, sql-server-2022, dapper-stored-procs, hotchocolate-graphql, jwt-argon2-auth) and follow the relevant `SKILL.md` exactly.

## Verification Gates

- `dotnet build` clean; `dotnet test` green (xUnit, incl. refresh-rotation replay + bulk-update atomicity).
- `docker compose up` runs api + sqlserver + postgres + redis; `/health` answers.
- Postman collection runs end-to-end (Newman in qa/infra CI).
- No hardcoded secrets; `.env.example` current; contracts synchronized.

## Hard Rules

1. Schema changes go through the ERD first (figma-make-erd skill), then a migration.
2. Both REST and GraphQL share `Griot.Application` services — zero drift allowed.
3. `GRIOT_SERVICE_TOKEN` resolves to the restricted `ai-agent` principal; `/api/webhooks/trigger` verifies HMAC.
4. Every raw SQL / Dapper call is parameterized; stored procs are `usp_` prefixed and idempotent.
5. Auth details (Argon2, 15-min JWT, refresh rotation with revocation-on-reuse) match spec 07 exactly.

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**

## Implemented authentication contract (Feature 07)

Use the [auth contract](../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Email-OTP 2FA implemented: `POST /api/auth/otp/request` (202; `email_verify` auto-sent on register( + `POST /api/auth/otp/verify` (200/401/429;; sets `Users.EmailVerified`; branded Brevo template per purpose. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

Before committing or pushing implementation, run `python3 scripts/check-contract-sync.py` from
the repository root. Synchronize the owning spec, dependent specs, planning,
research, docs, contexts, agent instructions, diagram sources and progress notes
in the feature branch. Planned behavior must be labeled and must not count as
implemented acceptance evidence. Run the system verification gates as well.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
