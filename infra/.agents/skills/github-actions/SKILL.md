---
name: github-actions
description: "GitHub Actions pipeline for Griot: test-gate jobs (dotnet/web/mobile/ai/mcp + Newman/Cypress) and the main-branch deploy job (Vercel/Railway/Trigger)."
metadata:
  version: "0.1.0"
---

# GitHub Actions Skill

## Pipeline shape

```yaml
jobs:
  test-dotnet:   # SQL Server 2022 service container; dotnet build + test (XPlat coverage)
  test-web:      # node 20; lint/typecheck/test/build
  test-mobile:   # Docker-pinned Flutter; analyze/test + APK artifact
  test-ai:       # node 20; lint/typecheck/test (golden transcripts)
  test-mcp:      # node 20; lint/typecheck/test (contracts)
  newman:        # Postman collection against the deployed API
  cypress:       # E2E with the stubbed copilot
  deploy:        # needs: all; if main; Vercel + Railway + Trigger + APK release
```

## Rules

- CI service containers only — never require a developer's local compose.
- Secrets least-privilege per job; never share tokens across jobs.
- Merge blocked if any job is red.
