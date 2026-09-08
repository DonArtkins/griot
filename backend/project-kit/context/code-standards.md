# Backend Code Standards

## C# / .NET

- `net8.0`, C# 12, nullable enabled, `ImplicitUsings` enabled.
- `Griot.Domain`: zero references; enums only + entities.
- Services depend on interfaces (domain); constructor injection; async `Task<T>`; `ConfigureAwait(false)` in libraries.
- Controllers return `ActionResult<T>` with DTOs — entities never leave the API un-mapped.
- Structured logging `ILogger<T>`; no `Console.WriteLine`.

## EF Core

- Code-first mapping in `OnModelCreating`; migrations committed; never reverse-engineer.
- Enums as strings; Guid keys; soft-delete only where the ERD says so.

## Dapper / T-SQL

- Only the two documented procs. Parameterized always. `usp_` prefix, `NOCOUNT ON`.
- SQL files owned by `Griot.Infrastructure/Sql/`; applied idempotently.

## Auth & security

- Argon2 for passwords; JWT 15-min; refresh hashed + rotated + family revoke on reuse.
- Redis sliding-window rate limit on login; query-cost guard on `/graphql`.
- CORS allow-list: Vercel origin prod, localhost dev.
- Service token → `ai-agent` principal (reduced role). HMAC on `/api/webhooks/trigger`.
- Prompt-injection: user text is data, never instructions.

## Verification (before "done")

- `dotnet build` + `dotnet test` green.
- Local compose stack healthy; `/health` answers.
- Postman collection updates committed together with any API change.

## Implemented authentication contract (Feature 07)

Use the [auth contract](../../../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
