# Progress Tracker — Backend / API

## Current State

Week 2. Spec 04 is complete (REST APIs). Spec 05 is next.

| Spec | Title | Status |
|---|---|---|
| 01 | ERD & schema design (Figma/Figma Make) | ✅ Done |
| 02 | SQL Server implementation (EF Core) | ✅ Done |
| 03 | Stored procedures & optimized queries | ✅ Done |
| 04 | REST APIs (.NET 8) | ✅ Done |
| 05 | GraphQL layer (HotChocolate) | Next |
| 06 | Bulk operations & advanced data | Pending |
| 07 | API testing (Postman) | Pending |
| 08 | Auth: JWT + Argon2 + Redis [own-stack] | Pending |
| 09 | AI service token + webhooks [own-stack] | Pending |
| 10 | API documentation | Pending |
| 11 | Blob storage integration | Pending |

## Next Steps

1. Implement spec 05 (GraphQL layer).
2. Implement subsequent specs sequentially.

## Session Notes

- **2026-09-03** — Kit restructured to one spec per bootcamp deliverable; 11 specs created.
- **2026-09-07** — Spec 02 completed: EF Core entities mapped to approved ERD, GriotDbContext configured, and initial migration generated.
- **2026-09-07** — Spec 03 completed: Added `usp_BulkUpdateTaskStatus` and `usp_GetDashboardSummary` stored procedures in `Griot.Infrastructure/Sql/`, added Dapper dependency, and created `TaskRepository` and `DashboardRepository` along with corresponding interfaces and DTOs in `Griot.Application`.
- **2026-09-07** — Spec 04 completed: Scaffolded REST API controllers corresponding to all routes in `api-surface.md`. Stubbed corresponding application services and DTOs. Updated `Program.cs` to wire up DI, DbContext, generic CORS policy, rate limiting, and JWT authentication.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly.
