# Feature 04 — REST APIs using .NET 8

## Type

NEW FEATURE

## What This Delivers

The bootcamp deliverable **"REST APIs using .NET 8"**: the `Griot.Api` controllers over the `Griot.Application` service layer, for every module in `api-surface.md` — thin controllers, DTO responses, validation, auth middleware wiring.

## Dependencies

- Feature 02 (domain + infrastructure + DbContext).
- Feature 03 (Dapper repos for the two procs).

## Context To Read First

- `backend/project-kit/context/{architecture,api-surface,code-standards}.md`
- `research/week-02-backend-api-development.md` §5

## Agent Skills To Use

- `backend/.agents/skills/sql-server-2022/SKILL.md` (connection/cxn patterns)
- `backend/.agents/skills/hotchocolate-graphql/SKILL.md` (auth middleware shared)

## Files Owned

- `backend/src/Griot.Api/Controllers/**`
- `backend/src/Griot.Application/**` (service classes + DTOs)
- `backend/src/Griot.Api/Program.cs` (DI + middleware wiring)

## Files

CREATE: `Griot.Application` services + DTOs (workspace, project, board, task, comment, notification, auth).
CREATE: Controllers: `AuthController`, `WorkspaceController`, `ProjectController`, `BoardController`, `TaskController`, `CommentController`, `NotificationController`.
CREATE: `Program.cs` wiring: DbContext, services, JWT bearer, CORS, rate-limit middleware, health.
RUN: `dotnet build`; run against compose; smoke via curl.

## Setup / Initialization

```bash
cd backend
dotnet add src/Griot.Api reference src/Griot.Application
dotnet add src/Griot.Application reference src/Griot.Domain src/Griot.Infrastructure
# env: ConnectionStrings__Default, JWT__*, Redis__Connection, Cors__AllowedOrigins
dotnet run --project src/Griot.Api
```

## Implementation Notes

- Controllers call services; map entities → DTOs; return `ActionResult<T>`; 404 on ownership miss (never disclose existence).
- Validation via data annotations + `[ApiController]`.
- Use the repositories (EF + Dapper procs) through interfaces — no SQL in services.
- Structured logging `ILogger<T>`; request-id middleware.

## Separation of Concerns

- Controllers: HTTP only. Services: business rules. Repos: persistence. DTOs are the API contract for all clients (web/mobile/AI/MCP).

## Docker & Deploy

- Compose `api` service (infra spec 03) runs this image; `/health` answers 200.
- Deployed to Railway (infra spec 05); migrations are a release command.

## Out of Scope

GraphQL (feature 05), auth flows (feature 08), Postman collection (feature 07).

## Future Modifications

- Features 05/06/07/08 extend these controllers.

## Acceptance Criteria

- [ ] All routes in `api-surface.md` respond correctly against the compose stack
- [ ] Controllers contain no business logic
- [ ] `dotnet build` + smoke tests green; contracts synced in `api-surface.md`


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly.
