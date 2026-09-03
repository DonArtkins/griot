# Week 5 — Deployment & DevOps (Docker + Compose v2 + Vercel + GitHub Actions)

> Guide's official Week 5 goals: **Frontend → Vercel** (Vite framework preset), **backend Dockerization** (production Dockerfile, build, run), **Docker Compose — the PDF literally names `docker-compose.yml (API + SQL Server)`**, optional: Docker Hub push, Azure / Railway / Render, **GitHub Actions CI/CD**.

---

## 1. Decisions & Rationale

1. **Backend = Docker multi-stage .NET 8 image** (`mcr.microsoft.com/dotnet/sdk:8.0` build → `aspnet:8.0` runtime). Keep the image free of dev tools and secrets (env-injected at runtime).
2. **Compose v2 = the guide's exact pairing**: `api` + **SQL Server 2022** + Postgres (16, secondary) + Redis (own-stack). Running this locally each week means deployment day is never the first time it's exercised.
3. **Vercel for the frontend**, Vite preset — the guide's 5-step flow: push to GitHub → import → select Vite preset → env vars → deploy & verify. (Week-3's MUI/React/Vite app is a first-class Vite preset citizen.)
4. **GitHub Actions** gates every PR (build, tests, coverage) and deploys on `main` — the Week-7 "CI/CD-integrated automated testing" outcome built early.
5. **Host: Railway** (Docker-native, one-click SQL Server/Postgres/Redis add-ons) with Render as fallback and an **Azure App Service** variant documented — the PDF lists Azure / Railway / Render; any satisfies the stack.

## 2. Backend Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/Griot.Api/Griot.Api.csproj
RUN dotnet publish src/Griot.Api/Griot.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Griot.Api.dll"]
```

## 3. docker-compose.yml (the guide's exact "API + SQL Server")

```yaml
services:
  api:
    build: .
    ports: ["8080:8080"]
    environment:
      ConnectionStrings__Default: "Server=gtp-sqlserver,1433;Database=griot;User Id=sa;Password=${GTP_SA_PASSWORD};TrustServerCertificate=True"
    depends_on: [sqlserver, redis]
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment: { ACCEPT_EULA: "Y", MSSQL_SA_PASSWORD: "${GTP_SA_PASSWORD}" }
    ports: ["14333:1433"]
    volumes: [mssql_data:/var/opt/mssql]
  postgres:
    image: postgres:16-alpine
    ports: ["5433:5432"]
    volumes: [pg_data:/var/lib/postgresql/data]
  redis:
    image: redis:7-alpine
    ports: ["6380:6379"]
volumes:
  mssql_data:
  pg_data:
```

## 4. GitHub Actions (test gate → deploy)

```yaml
# .github/workflows/ci-cd.yml
name: CI/CD
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2022-latest
        env: { ACCEPT_EULA: "Y", MSSQL_SA_PASSWORD: "Griot_Test_2026!" }
        ports: ["1433:1433"]
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: 8.0.x }
      - run: dotnet restore src/Griot.Api/Griot.Api.csproj
      - run: dotnet build src/Griot.Api/Griot.Api.csproj --no-restore -c Release
      - run: dotnet test tests/Griot.Tests/Griot.Tests.csproj --no-build -c Release --collect:"XPlat Code Coverage"
  deploy:
    needs: test
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: 8.0.x }
      - run: dotnet publish src/Griot.Api/Griot.Api.csproj -c Release -o publish
      - name: Deploy to Railway
        run: railway up --service griot-api
        env: { RAILWAY_TOKEN: ${{ secrets.RAILWAY_TOKEN }} }
```

> EF migrations run as a **release command** (`dotnet ef database update`) in Railway — never assumed from local `migrate dev`.

## 5. Frontend deploy (Vercel, guide's exact steps)
1. Push `web/` to GitHub → 2. Import into Vercel → 3. **select Vite framework preset** → 4. env vars (`VITE_API_URL` → deployed API base) → 5. Deploy & verify live URL. Vercel auto-detects Vite; assets served from `dist`.

## 6. Host matrix (PDF allows Azure / Railway / Render)
| Path | When |
|---|---|
| **Railway** (primary) | Docker-native, auto-injects `ConnectionStrings__Default`/secrets; free-tier friendly |
| Render (fallback) | if Railway limits bite mid-week |
| Azure App Service (optional stretch) | `az webapp up` + `azd`; containerized .NET 8 — documented but not default |

### 6.5 Deploying the AI layer (see `ai-integration.md`)

- **`ai/`** (Trigger.dev v3) → Trigger-hosted or self-hosted on Railway; scheduled + agent runs are durable and observable from the Trigger dashboard. Env: `GRIOT_SERVICE_TOKEN`, `ANTHROPIC_API_KEY`/`OPENAI_API_KEY`, `GRIOT_API_URL`.
- **`mcp/`** (Griot MCP server) → Docker image, run in **Streamable HTTP** mode on Railway/Render (one extra service in the compose file); stdio mode stays for local Claude/Cursor use.
- Both are added to the same GitHub Actions pipeline (own lockfiles; `npm run lint && npm run typecheck && npm test` gate).

## 7. Definition of Done — Week 5
- [ ] Backend image builds and runs locally (`docker run` → health OK)
- [ ] Compose starts api + sqlserver + postgres + redis in one command
- [ ] Frontend live on Vercel (Vite preset) talking to the deployed API
- [ ] Backend live on Railway, REST + GraphQL responding
- [ ] GitHub Actions: tests on every PR; `main` merge deploys automatically
- [ ] `.env.example` matches local, Railway and Vercel variable sets

---
**Engineering Excellence. Production Mindset. Professional Impact.**