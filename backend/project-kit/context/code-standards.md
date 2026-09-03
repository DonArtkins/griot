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
