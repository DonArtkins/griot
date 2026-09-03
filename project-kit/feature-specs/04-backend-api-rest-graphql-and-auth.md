# Feature 04 — Backend API: REST + GraphQL + Auth (ASP.NET Core 8)

## Type

NEW FEATURE

## What This Delivers

The production backend: `Griot.Api` hosting REST controllers **and** HotChocolate GraphQL in one process, the `Griot.Application` service layer (six modules), owned auth (Argon2 + JWT + rotated refresh tokens + Redis rate limits), the `ai-agent` service-token principal, and the Postman collection that becomes the Newman contract suite in Feature 08/10.

## Dependencies

- Feature 03 (schema, DbContext, repos, procs).
- Feature 02 (Redis container for auth support).

## Context To Read First

- `context/architecture-context.md` (API Surface, Auth, AI Boundary)
- `context/library-docs.md` (.NET section)
- `research/week-02-backend-api-development.md`

## Files Owned

- `backend/src/Griot.Api/**`
- `backend/src/Griot.Application/**`
- `backend/src/Griot.Infrastructure/Repositories/**`
- `Postman/Griot.postman_collection.json` + `Postman/gtp-2026.postman_environment.json`

## Files

CREATE: `Griot.Api/Program.cs` — DI wiring, `AddDbContext`, JWT auth, CORS allow-list, Redis rate-limit middleware, `/health`, `AddGraphQLServer()…AddDataLoader()`.
CREATE: Controllers — `AuthController`, `WorkspaceController`, `ProjectController`, `BoardController`, `TaskController`, `CommentController`, `NotificationController`.
CREATE: `Griot.Api/GraphQL/` — `GriotQuery`, `GriotMutation`, DataLoaders for assignees/comments.
CREATE: `Griot.Application` services — `AuthService`, `WorkspaceService`, `ProjectService`, `BoardService`, `TaskService`, `CommentService`, `NotificationService` (interfaces defined in `Griot.Domain`).
CREATE: `Griot.Infrastructure` auth pieces — refresh-token repository, Redis rate-limiter, Argon2 hasher, `ai-agent` principal resolver.
CREATE: Postman collection (REST + GraphQL folders, env-chained baseUrl/tokens).
RUN: `dotnet build`; run against compose stack.

## Setup / Initialization

```bash
cd backend
dotnet add src/Griot.Api package HotChocolate.AspNetCore HotChocolate.AspNetCore.Authorization
dotnet add src/Griot.Api package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/Griot.Infrastructure package Konscious.Security.Cryptography
# .env values consumed by the API:
#   ConnectionStrings__Default, JWT__SigningKey, JWT__Issuer, JWT__Audience,
#   Redis__Connection, GRIOT_SERVICE_TOKEN, Cors__AllowedOrigins
dotnet run --project src/Griot.Api           # http://localhost:PORT/api & /graphql & /health
```

## Separation of Concerns

- Controllers/resolvers: **thin** — parse request → call service → map DTO → return. Zero business logic.
- Services: orchestrate repos + domain rules; no EF, no HTTP, no Redis calls inline.
- Repositories: EF for CRUD (95%), Dapper for the two stored-proc hot paths (5%). Both behind interfaces.
- Auth: `AuthService` owns hashing/rotation; the auth middleware only verifies tokens; the `ai-agent` principal is a policy resolution — one concern each.
- REST and GraphQL share the same service layer (drift-proof): a change in one surface is a change in both by construction.

## Docker & Deploy

- Add `api` service to `docker-compose.yml`: multi-stage build (sdk:8.0 → aspnet:8.0), `EXPOSE 8080`, env-injected, `depends_on: [sqlserver, redis]`.
- Health check: `GET /health` used by compose + production.
- Deploy target: Railway Docker deployment (Feature 09); `ASPNETCORE_URLS=http://+:8080`.
- Postman collection doubles as the Newman CI contract suite.

## Auth Behavior (owned, auditable)

- Register/login → Argon2 verify → JWT access (15-min, claims `sub`/`wid`) + opaque refresh (hashed at rest, single-use rotation).
- Reusing a rotated refresh token revokes the whole family (replay must fail — Week-6 OWASP case).
- Redis sliding window on `/api/auth/login`; query-cost guard on `/graphql`.
- `GRIOT_SERVICE_TOKEN` → `ai-agent` workspace member with reduced role; HMAC verify on `/api/webhooks/trigger`.

## Out of Scope

- Real-time presence, file upload storage backend (attachment upload endpoint only; storage wiring deferred unless time allows), admin routes, multi-workspace expansion.

## Acceptance Criteria

- [ ] `dotnet build` + `dotnet test` green in the Week-6 harness
- [ ] All six modules + auth live over REST and GraphQL against the real SQL Server container
- [ ] Refresh-rotation replay test passes (second reuse → 401)
- [ ] Rate limits active; `GRIOT_SERVICE_TOKEN` resolves to the scoped `ai-agent` principal
- [ ] Postman collection runs end-to-end; compose `api` service healthy; `.env.example` current

## Future Modifications

- Feature 05/06 build clients against these endpoints; Feature 07 consumes GraphQL with the service token; Feature 08/09 deploy it.