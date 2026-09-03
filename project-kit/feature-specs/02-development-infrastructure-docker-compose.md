# Feature 02 — Development Infrastructure: Docker + Compose Data Stack

## Type

NEW FEATURE

## What This Delivers

The real Docker Engine + Compose v2 development stack on Parrot: SQL Server 2022 (primary), PostgreSQL 16 (secondary), and Redis 7 (runtime support) running as `gtp-*` containers on isolated host ports, plus the `api` compose service wired with `depends_on`. After this feature, `docker compose up -d` reproduces the bootcamp data layer locally and every later feature has a real database to run against.

## Dependencies

- Feature 01 (repo skeleton).
- Docker Engine installed per `research/gtp-2026-prep.md` §6.3 (verify: `docker --version` must NOT show "podman").

## Context To Read First

- `context/architecture-context.md` (Database Architecture)
- `context/code-standards.md` (Isolation contract)
- `research/gtp-2026-prep.md` §6.2–6.4, §8

## Files Owned

- `docker-compose.yml`
- `.env` (local, git-ignored) + `.env.example`

## Files

CREATE: `docker-compose.yml` — services `sqlserver` (image `mcr.microsoft.com/mssql/server:2022-latest`, host port 14333, volume `mssql_data`), `postgres` (`postgres:16-alpine`, 5433, `pg_data`), `redis` (`redis:7-alpine`, 6380), and `api` placeholder for Feature 04.
CREATE: `.env` from `.env.example` with dev-only `GTP_SA_PASSWORD` / `GTP_PG_PASSWORD` defaults.
RUN: `docker compose up -d`

## Setup / Initialization

```bash
cd <repo-root>
cp .env.example .env
docker compose up -d
# verify each daemon (names gtp-* to avoid personal-stack collisions):
docker exec gtp-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$GTP_SA_PASSWORD" -C -Q "SELECT @@VERSION"
docker exec gtp-redis redis-cli ping            # PONG
psql -h localhost -p 5433 -U postgres -c "SELECT version();"
```

DBeaver connects to `localhost:14333` for SQL Server, `localhost:5433` for Postgres. Ports never collide with personal 1433/5432/6379.

## Separation of Concerns

- `docker-compose.yml` owns the *development* wiring only: data daemons + API service declaration.
- Secrets live in `.env` (git-ignored); the compose file reads variables, never hardcodes passwords.
- Data persists in named volumes (`mssql_data`, `pg_data`) — no host-path bind mounts for databases.
- Production images/Dockerfiles belong to Feature 04 (backend) and Feature 09 (deployment); compose here stays development parity.

## Docker & Deploy

- Local: `docker compose up -d` → full data stack + (later) API.
- Deploy parity is maintained by running the backend in Compose (`api` service) as it is built in Feature 04 — this is exactly the "deploy day is never the first time it's exercised" rule.
- The SQL Server image *is* SQL Server 2022 (not an emulation) — the Week-2 deliverables run against this container.

## Out of Scope

- Production compose, secrets management, image registries, Kubernetes.

## Acceptance Criteria

- [ ] All three containers healthy (`docker compose ps`); `SELECT @@VERSION` + `PING` + `psql SELECT` succeed
- [ ] `docker --version` reports real Docker Engine (containerd), not the Podman shim
- [ ] `.env` git-ignored; `.env.example` committed and current
- [ ] Host ports 14333/5433/6380 reachable from dev tooling

## Future Modifications

- Feature 04 adds the `api` service (image build + depends_on + connection string).
- Feature 09 reuses this compose file for the Railway deployment narrative.