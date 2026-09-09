# Griot — Postman Collection & Environment (Backend Feature 08)

The official Postman contract suite for Griot. Reused headlessly as
**Newman** in CI by the qa system (qa spec 05: `newman-contract-testing`).

## What's inside

| File | Purpose |
|---|---|
| `Griot.postman_collection.json` | Contract collection with two top-level folders: **REST** (every route from `backend/project-kit/context/api-surface.md`) and **GraphQL** (queries + mutations from `api-surface.md` GraphQL section). |
| `gtp-2026.postman_environment.json` | Chained environment: `baseUrl`, `graphqlUrl`, `accessToken`, `refreshToken`, `userId`, workspace/project/board/task IDs, and test credentials. |

Tokens (`accessToken`, `refreshToken`) and entity IDs are written into
the environment **automatically** by the `Tests` tab of each request that
produces them — no manual copy-paste needed to run the whole collection.

## Quick start (local)

### 1. Import into Postman

1. Open Postman → **Import** → select both JSON files from this folder.
2. Top-right environment dropdown → pick **"GTP 2026 — Griot Local Dev"**.
3. Verify `baseUrl` matches your running backend (default `http://localhost:5064`).

### 2. Start the backend + backing services

From the repo root (shared Sababisha compose first, one-time):

```bash
docker compose -f ~/sababisha/infra/docker-compose.yml up -d
```

From `backend/` run the API:

```bash
cd backend
dotnet run --project src/Griot.Api
```

Expect the backend to listen on `http://localhost:5064` (HTTP profile) and
`https://localhost:7198` (HTTPS). The container profile uses port `8080`.

### 3. Run the collection, folder-by-folder in order

Required order so tokens and IDs chain:

| Step | Folder / request | What it does |
|---|---|---|
| 0 | `REST / Public / Health` | Confirms `/health` is reachable. No auth needed. |
| 1 | `REST / Auth / Register` | Creates the test user; writes `accessToken`, `refreshToken`, `userId` to the env. |
| 2 | `REST / Auth / Login` | Re-authenticates; rotates tokens; confirms 429 lockout works on repeated calls. |
| 3 | `REST / Workspaces` through `REST / Webhooks` | Iterates every route in `api-surface.md`; scaffold routes return `501 Not Implemented` until their features ship. |
| 4 | `GraphQL / Queries` + `GraphQL / Mutations` | Same `accessToken` reused; `boardId`-scoped tasks; assertions on JSON shape and time. |

### 4. Expected status codes per implementation maturity

| Surface | Implemented | Scaffold (pending specs 09–11) |
|---|---|---|
| **Auth** (`/api/auth/*`) | `200 / 201 / 202 / 204` | — |
| **Health** (`/health`) | `200 Healthy` | — |
| **REST modules** (workspaces → webhooks) | — | `501 Not Implemented` |
| **GraphQL queries** (11) | `401 AUTH_NOT_AUTHENTICATED` without token; `200 OK` + payload with valid token for resolvers where data exists | — |
| **GraphQL mutations** (11) | Same auth rules | — |

The collection assertions accept the current status (auth passes, scaffold
passes 501, GraphQL passes 200/401 correctly). As each feature spec lands,
the corresponding request's assertions tighten from `pm.response.to.be.scaffold`
to real schema checks.

## Assertions (Tests tab) used

| Kind | Applied to |
|---|---|
| Status code matches | Every request |
| `X-Request-Id` header present | Every REST request (contract: `api-surface.md` §Conventions) |
| `pm.response.to.have.jsonSchema` for key shapes | Register, Login, Refresh response bodies; GraphQL task/board payloads |
| Response time < 500 ms | Dashboard summary (latency budget in `api-surface.md`) |
| Env variable write (chain) | Register → `accessToken`/`refreshToken`/`userId`; subsequent entity-creating requests → their IDs |
| 429 + `Retry-After` present | Login (after 10 calls, Redis enforces the lockout) |

## Reusing as Newman in CI (qa system)

The qa spec 05 wires this into a GitHub Actions workflow. Run locally the
same way CI does:

```bash
# Install Newman (one-time, if you don't have it globally)
npm install -g newman

# From backend/ directory
newman run Postman/Griot.postman_collection.json \
  -e Postman/gtp-2026.postman_environment.json \
  --reporters cli,junit --reporter-junit-export newman-report.xml
```

Coverage notes:
- **No LLM in CI.** The collection is deterministic; seed credentials are in
  the environment.
- **Scaffold 501s are not CI failures.** Assertions match implementation
  maturity. When a route graduates to real, update its Tests tab alongside
  the feature PR and contract-sync the change.

## Updating the collection (contract-sync gate)

The collection is a contract artifact owned by backend spec 08. Any change
to routes, shapes, status codes, GraphQL fields, or entity names must:

1. Edit the collection *and* `api-surface.md` in the same PR.
2. Run `python3 scripts/check-contract-sync.py` from repo root (exit 0 required).
3. Run Newman locally to confirm the collection still imports and runs.
4. Add a session note to `backend/project-kit/context/progress-tracker.md`
   describing the contract drift corrected.

Never update the collection alone — it derives from `api-surface.md`, not the
other way around.

## Troubleshooting

| Symptom | Fix |
|---|---|
| Register returns `500` "ConnectionStrings:Default is not configured" | Create `backend/src/Griot.Api/appsettings.Local.json` with a dev `ConnectionStrings:Default` → `Server=localhost,14333;Database=Griot;User Id=sa;Password=SababishaDev2026!;TrustServerCertificate=True` (git-ignored). |
| Redis connection refused on Login | `docker compose -f ~/sababisha/infra/docker-compose.yml up -d sababisha-redis`. Default Redis host port is `6380`. |
| Tokens not chaining to later requests | Top-right env dropdown must be the one you wrote to. Postman doesn't persist edits to the imported env unless you explicitly save. |
| GraphQL returns `AUTH_NOT_AUTHENTICATED` | Run the Register or Login REST request first so `accessToken` is in the env. The GraphQL folder reuses the same Bearer `{{accessToken}}` header. |
