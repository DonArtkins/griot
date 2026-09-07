# Feature 06 - Docker Hub Push + Railway Deploy

## Type

NEW FEATURE (Optional Advanced Deployment per the PDF)

## What This Delivers

The PDF's optional lines "Push image to Docker Hub" and "Deploy to Azure / Railway / Render" realized as: registry push + Railway deployment of backend and MCP, with Render + Azure variants documented.

## Dependencies

- Features 01-05 (image + CI exist).

## Context To Read First

- `research/week-05-deployment-devops.md` sec 6
- `infra/project-kit/context/deployment-targets.md`

## Agent Skills To Use

- `infra/.agents/skills/railway-hosting/SKILL.md`

## Files Owned

- `docs/DEPLOYMENT.md`, `docs/RAILWAY.md`, `docs/RENDER.md`, `docs/AZURE.md`

## Files

CREATE: per-app runbooks + env matrix; Railway backend service with release command `dotnet tool restore && dotnet ef database update`; Railway mcp service; optional Docker Hub push step in CI.

## Setup / Initialization

```bash
railway up --service griot-api
railway variables --service griot-api --set 'ConnectionStrings__Default=...'  # etc.
# release command set in Railway service settings, NOT in CI
```

## Implementation Notes

- Railway release command runs migrations against production SQL.
- Render fallback + Azure App Service variant (az webapp up) documented but not defaulted.

## Separation of Concerns

- Host choices documented; app code host-agnostic.

## Docker & Deploy

- Completes the Week-5 deploy matrix.

## Out of Scope

Kubernetes, autoscaling (v2).

## Acceptance Criteria

- [ ] Backend live on Railway; REST + GraphQL + health responding; migrations by release command
- [ ] MCP live on Railway (Streamable HTTP)
- [ ] Runbooks complete (Railway primary, Render fallback, Azure variant)


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly.
