# Progress Tracker — Backend / API

## Current State

Week 2. Spec 03 is complete (stored procedures & optimized queries). Spec 04 (REST APIs on .NET 8) is next on branch `feature/backend/04-rest-apis-dotnet8`.

| Spec | Title | Status |
|---|---|---|
| 01 | ERD & schema design (Figma/Figma Make) | ✅ Done |
| 02 | SQL Server implementation (EF Core) | ✅ Done |
| 03 | Stored procedures & optimized queries | ✅ Done |
| 04 | REST APIs (.NET 8) | Next |
| 05 | GraphQL layer (HotChocolate) | Pending |
| 06 | Bulk operations & advanced data | Pending |
| 07 | API testing (Postman) | Pending |
| 08 | Auth: JWT + Argon2 + Redis [own-stack] | Pending |
| 09 | AI service token + webhooks [own-stack] | Pending |
| 10 | API documentation | Pending |
| 11 | Blob storage integration | Pending |

## Next Steps

1. Implement spec 04 (REST APIs on .NET 8) on branch `feature/backend/04-rest-apis-dotnet8`.
2. Implement subsequent specs sequentially, each feature on its own branch.

## Session Notes

- **2026-09-03** — Kit restructured to one spec per bootcamp deliverable; 11 specs created.
- **2026-09-07** — Spec 02 completed: EF Core entities mapped to approved ERD, GriotDbContext configured, and initial migration generated.
- **2026-09-07** — Spec 03 completed: Added `usp_BulkUpdateTaskStatus` (TVP bulk status update, transactional + SYSUTCDATETIME on UpdatedAt) and `usp_GetDashboardSummary` (one-round-trip dashboard: counts by status, urgent-open count, recent activity head) in `Griot.Infrastructure/Sql/`. Added Dapper dependency and created `TaskRepository` (parameterized, `CommandType.StoredProcedure`, `dbo.IdList` TVP) and `DashboardRepository` (`QueryMultipleAsync`) along with `ITaskRepository`, `IDashboardRepository` and the `DashboardSummaryDto`/`ActivityLogDto` DTOs in `Griot.Application`. Verified against spec 03 acceptance criteria: both procs idempotent (CREATE OR ALTER + `IF TYPE_ID` guard), bulk update atomic (BEGIN TRAN/XACT_ABORT), dashboard one round-trip.  `dotnet build` green.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
