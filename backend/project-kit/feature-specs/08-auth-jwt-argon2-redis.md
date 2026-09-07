# Feature 08 — Auth: JWT + Argon2 + Redis (own-stack)

## Type

NEW FEATURE (`[own-stack]` — the guide is silent on auth; the mechanism is owned and auditable)

## What This Delivers

The bootcamp's un-specified auth surface, implemented in-house: Argon2 password hashing, JWT access tokens (15-min, `sub`/`wid`), opaque rotated refresh tokens (SHA-256 at rest, family revoke on reuse), and Redis sliding-window rate limiting.

## Dependencies

- Feature 04 (controllers exist).
- Feature 02 (`RefreshTokens` table in the migration).

## Context To Read First

- `backend/project-kit/context/{architecture,api-surface}.md` (auth sections)
- `research/week-02-backend-api-development.md` §6

## Agent Skills To Use

- `backend/.agents/skills/jwt-argon2-auth/SKILL.md`

## Files Owned

- `backend/src/Griot.Application/AuthService.cs` + auth DTOs
- `backend/src/Griot.Infrastructure/Redis/*` (rate limiter, session store)
- `backend/src/Griot.Api/Middleware/*` (auth middleware, rate-limit middleware)

## Files

CREATE: `AuthService` — register (Argon2 hash), login (verify + issue), refresh (rotate), logout (revoke).
CREATE: Redis rate limiter (sliding window on login; query-cost guard for GraphQL).
CREATE: auth middleware + principal factory (`sub`, `wid` claims).
MODIFY: `Program.cs` — AddAuthentication(JwtBearer), CORS allow-list.
RUN: `dotnet build`; integration smoke of register→login→refresh→reuse-rejected.

## Setup / Initialization

```bash
dotnet add src/Griot.Api package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/Griot.Infrastructure package Konscious.Security.Cryptography
# env: JWT__SigningKey (>=256-bit), JWT__Issuer, JWT__Audience, Redis__Connection
```

## Implementation Notes

- Refresh rotation: on every refresh call, revoke the old token row, hash+insert the new one, link via `ReplacedByTokenId`.
- Reuse of a rotated token → revoke the whole family (qa spec 05 test).
- Rate limit: Redis fixed/sliding window returns 429 with `Retry-After`.
- CORS: allow-list only (Vercel origin prod, localhost dev).

## Separation of Concerns

- `AuthService` owns hashing/token policy. Middleware only validates. Redis wrapper is an infrastructure concern. No auth logic in controllers.

## Docker & Deploy

- Redis is already in compose (infra spec 03). No new container. `GRIOT_SERVICE_TOKEN` for AI arrives in feature 09.

## Out of Scope

OAuth/SSO, 2FA, session revocation UI (v2).

## Future Modifications

- Feature 09 adds the AI service-token principal; qa 05/08 test this surface.

## Acceptance Criteria

- [ ] register/login/refresh/logout work; replay of a rotated refresh returns 401 and revokes the family
- [ ] Rate limit returns 429 under hammering
- [ ] JWT claims `sub`/`wid` correct; CORS restricts to allow-list


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
