# Environments

| Environment | Runs | Purpose |
|---|---|---|
| Local | `docker compose up` + local runners | Dev iteration; first run of every suite |
| CI | GitHub Actions with SQL Server 2022 service container | Hermetic gates; blocks merges |
| Deployed | Railway + Vercel (staging = prod previews) | Weekly manual+UAT; Newman+Cypress; k6 regression |

Rules:
- Tests never require a developer's local compose in CI.
- Manual + UAT + perf run against the deployed system (env var mismatch, CORS, cold starts are exactly what to catch).

## Implemented authentication contract (Feature 07)

Use the [auth contract](../../../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
