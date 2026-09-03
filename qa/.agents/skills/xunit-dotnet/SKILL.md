---
name: xunit-dotnet
description: "xUnit unit + integration testing for the Griot backend: WebApplicationFactory against the real SQL Server container, refresh-rotation replay, bulk-atomicity, coverage collection."
metadata:
  version: "0.1.0"
---

# xUnit (.NET) Skill

## Harness

```bash
cd backend
dotnet new xunit -o tests/Griot.Tests
dotnet add tests/Griot.Tests reference src/Griot.Api
dotnet add tests/Griot.Tests package Microsoft.AspNetCore.Mvc.Testing
dotnet test --collect:"XPlat Code Coverage"
```

## Must-cover

- `Refresh_Cannot_Be_Replayed` (reuse rotated refresh -> 401 + family revoke).
- `BulkUpdate_Rotates_Statuses_Atomically` (all-or-nothing).
- Service-layer unit tests with mocked repos; N+1 regression checks on hot queries.
- Rate limit 429; CORS enforcement.

## Rules

- Service-layer + auth coverage first; presentation second. Gate >=80% service-layer/auth.
