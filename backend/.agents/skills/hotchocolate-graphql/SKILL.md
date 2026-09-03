---
name: hotchocolate-graphql
description: "HotChocolate GraphQL 14+ code-first layer beside the REST controllers: QueryType/MutationType, DataLoaders, filtering/sorting, query-cost guard. Resolvers delegate to Griot.Application."
metadata:
  version: "0.1.0"
---

# HotChocolate GraphQL Skill

## Setup

```bash
dotnet add src/Griot.Api package HotChocolate.AspNetCore HotChocolate.AspNetCore.Authorization
```

`Program.cs`: `AddGraphQLServer().AddQueryType<GriotQuery>().AddMutationType<GriotMutation>().AddDataLoader().AddFiltering().AddSorting()`; map `/graphql`.

## Rules

- Code-first: C# types are the contract (shared with Domain/DTOs). No SDL-first.
- Resolvers delegate to services — zero business logic in resolvers.
- `DataLoader<,>` for assignees + comments (N+1 prevention).
- Same JWT principal as REST; query-cost guard on `/graphql`.
- Type names mirror the ERD entity names exactly (contract sync across systems).

## Verify

- Schema at `/graphql?sdl`; dashboard/board queries show no N+1 in tests/logs.
