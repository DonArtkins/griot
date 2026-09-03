# Backend Architecture

## Solution layout

```
backend/
├── Griot.sln
├── global.json                  # SDK 8.0 pin
├── src/
│   ├── Griot.Api/               # Program.cs, Controllers/, GraphQL/, Middleware/, Program.cs
│   ├── Griot.Application/       # AuthService, WorkspaceService, ProjectService, BoardService,
│   │                            #   TaskService, CommentService, NotificationService
│   ├── Griot.Domain/            # Entities/ + Enums/ (TaskStatus, Priority, WorkspaceRole, NotificationType)
│   └── Griot.Infrastructure/    # Persistence/GriotDbContext.cs, Repositories/, Redis/, Migrations/, Sql/
└── tests/Griot.Tests/           # xUnit + WebApplicationFactory
```

## Layer responsibilities (absolute)

| Layer | Owns | Never does |
|---|---|---|
| Griot.Api | HTTP: routing, serialization, auth middleware, CORS, GraphQL | business logic |
| Griot.Application | business rules, orchestration | EF, HTTP, Redis |
| Griot.Infrastructure | EF Core DbContext, repositories, Dapper, Redis, migrations | business rules |
| Griot.Domain | entities + enums | references anything |

## DI & composition root

`Program.cs` wires everything: `AddDbContext<GriotDbContext>`, service registrations, JWT bearer, CORS allow-list, Redis rate-limit middleware, `AddGraphQLServer()…`, health checks (`/health`). Controllers and HotChocolate resolvers both take service interfaces from `Griot.Application`.

## Auth (own-stack)

Argon2 hashing · JWT access (15-min, `sub`/`wid`) · opaque rotated refresh (hashed at rest) · Redis sliding-window rate limit · `GRIOT_SERVICE_TOKEN` → restricted `ai-agent` principal · HMAC `/api/webhooks/trigger`. Full detail: `/.agents/skills/jwt-argon2-auth` + feature spec 08.

## Operational details

- One process serves `/api`, `/graphql`, and `/health`; `ASPNETCORE_URLS=http://+:8080` in containers.
- Structured logging via `ILogger<T>`; every request gets a request id; no `Console.WriteLine`.
- Connection string from `ConnectionStrings__Default` (compose: `Server=gtp-sqlserver,1433;Database=griot;…`).
