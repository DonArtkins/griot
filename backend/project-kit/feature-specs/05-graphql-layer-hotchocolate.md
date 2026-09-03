# Feature 05 — GraphQL Layer via HotChocolate

## Type

NEW FEATURE

## What This Delivers

The bootcamp deliverable **"GraphQL layer via HotChocolate"**: the code-first HotChocolate schema at `/graphql` — `GriotQuery` / `GriotMutation`, filtering/sorting on list fields, `DataLoader` batch loading (assignees, comments), and the query-cost guard.

## Dependencies

- Feature 04 (service layer exists).

## Context To Read First

- `backend/project-kit/context/api-surface.md` (GraphQL section)
- `research/week-02-backend-api-development.md` §5

## Agent Skills To Use

- `backend/.agents/skills/hotchocolate-graphql/SKILL.md`

## Files Owned

- `backend/src/Griot.Api/GraphQL/**`
- `backend/src/Griot.Api/Program.cs` (AddGraphQLServer wiring)

## Files

CREATE: `GriotQuery`, `GriotMutation`, resolvers, DataLoaders, filter/sort config.
MODIFY: `Program.cs` — `.AddGraphQLServer().AddQueryType<GriotQuery>().AddMutationType<GriotMutation>().AddDataLoader().AddFiltering().AddSorting()` + map `/graphql`; query-cost guard middleware.
RUN: `dotnet build`; verify `/graphql?sdl`.

## Setup / Initialization

```bash
dotnet add src/Griot.Api package HotChocolate.AspNetCore HotChocolate.AspNetCore.Authorization
```

## Implementation Notes

- Code-first: C# types are the contract (shared with Domain/DTOs).
- Resolvers delegate to services — zero business logic.
- `[DataLoader]` for `Task.assignee` and `Task.comments` (N+1 prevention).
- Type names = ERD entity names exactly (contract sync with web/mobile/AI/MCP).
- Auth: same JWT principal as REST.

## Separation of Concerns

- GraphQL types map 1:1 to domain/DTO shapes; resolvers are thin adapters over `Griot.Application`.

## Docker & Deploy

- Served by the same process/image as REST (one container). No separate deploy.

## Out of Scope

SDL-first schema, subscriptions (v2), persisted-queries cache.

## Future Modifications

- AI (ai spec 03) and MCP (mcp spec 02) read/write through this surface with the service token.

## Acceptance Criteria

- [ ] Schema live at `/graphql?sdl`; queries/mutations match `api-surface.md`
- [ ] Dashboard/board queries show no N+1
- [ ] Query-cost guard active
