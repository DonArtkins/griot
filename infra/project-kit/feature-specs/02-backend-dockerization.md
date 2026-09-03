# Feature 02 - Backend Dockerization

## Type

NEW FEATURE

## What This Delivers

The bootcamp deliverable "Create production-ready Dockerfile, build image, run container" for the .NET 8 backend.

## Dependencies

- Backend features 04-05 (server exists).

## Context To Read First

- `research/week-05-deployment-devops.md` sec 2

## Agent Skills To Use

- `infra/.agents/skills/docker-compose/SKILL.md`

## Files Owned

- `backend/Dockerfile`

## Files

CREATE: `backend/Dockerfile` - multi-stage: sdk 8.0 build, aspnet 8.0 runtime, `ASPNETCORE_URLS=http://+:8080`, `EXPOSE 8080`, `ENTRYPOINT ["dotnet","Griot.Api.dll"]`.

RUN: `docker build -t griot-api .` then `docker run --rm -p 8080:8080 --env-file ../.env griot-api`.

## Implementation Notes

- Image free of dev tools and secrets (env-injected at runtime).
- Health check `/health`.

## Separation of Concerns

- Dockerfile ships with the app it packages; owned by infra lifecycle; no app-logic changes to containerize.

## Docker & Deploy

- This image is what Railway runs (feature 06).

## Out of Scope

Compose orchestration (03), registry push (06).

## Acceptance Criteria

- [ ] Image builds; container runs; `/health` 200
- [ ] No secrets in the image layers
