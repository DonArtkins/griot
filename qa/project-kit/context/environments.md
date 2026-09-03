# Environments

| Environment | Runs | Purpose |
|---|---|---|
| Local | `docker compose up` + local runners | Dev iteration; first run of every suite |
| CI | GitHub Actions with SQL Server 2022 service container | Hermetic gates; blocks merges |
| Deployed | Railway + Vercel (staging = prod previews) | Weekly manual+UAT; Newman+Cypress; k6 regression |

Rules:
- Tests never require a developer's local compose in CI.
- Manual + UAT + perf run against the deployed system (env var mismatch, CORS, cold starts are exactly what to catch).
