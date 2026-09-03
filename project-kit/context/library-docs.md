# Library Docs

## Contracts Synchronization Gate

Any implementation change that corrects or changes a contract must be reflected across the affected feature spec, dependent future specs, relevant context files, root `AGENTS.md`, and `progress-tracker.md` in the same branch. Contracts include EF Core entities/relations and enum values, REST route signatures, GraphQL type/query/mutation names, auth token claims/endpoints, `GRIOT_SERVICE_TOKEN` behavior, env variables, Docker/Compose service names and ports, storage paths, generated file structure, package versions, and file ownership.

## Rules of Use

- Bootcamp-mandated libraries are `exact`; personal-choice libraries are marked `[own-stack]`. Never swap a mandated library silently.
- Pin versions: `.NET` via `Directory.Packages.props`/`global.json`; Node via lockfiles + `.nvmrc` → 20.
- Check current official docs before implementing a feature that depends on a library's moving parts (migrations CLI, GraphQL conventions, Provider APIs). Record version-specific findings here or in the relevant feature spec.

## .NET Backend

### ASP.NET Core 8 Web API (`exact`)
- One process hosts REST controllers + HotChocolate middleware. Minimal API is not used — controllers keep the tutorial/graded surface explicit.
- `Program.cs` wires: DI for `Griot.Application` services, `AddDbContext<GriotDbContext>`, `AddGraphQLServer()…`, JWT auth scheme, CORS allow-list, rate-limit middleware, health checks (`/health` for Docker/CI).

### Entity Framework Core 8 (`exact`)
- Code-first. `GriotDbContext` in `Griot.Infrastructure`; entities in `Griot.Domain`.
- Conventions: keep entities flat in v1; store enums as `varchar` via `HasConversion<string>()` so SQL reads are readable.
- `Guid` keys (`Guid.NewGuid()`); `SYSUTCDATETIME()` intercepted in `SaveChangesAsync` for `CreatedAt`/`UpdatedAt`.
- Migrations: `dotnet ef migrations add <Name>` and `dotnet ef database update` via the repo tool-manifest (`dotnet new tool-manifest` + `dotnet tool install dotnet-ef`), never a global `dotnet-ef`.

### Dapper 2.x (`exact`)
- Only for the documented hot paths: `usp_BulkUpdateTaskStatus` (table-valued parameter) and `usp_GetDashboardSummary`.
- Call via `IDbConnection` from `Griot.Infrastructure.Repositories`; every parameter is forced (`commandType: CommandType.StoredProcedure`).
- No Dapper data in `Griot.Application`'s service contracts unless the DTO is one of the two documented procs.

### HotChocolate GraphQL 14+ (`exact`)
- `QueryType`/`MutationType` in `Griot.Api/GraphQL/`; resolvers delegate to services.
- `DataLoader<,>` for assignees and comments (batch loading); `AddFiltering()` / `AddSorting()` on list types.
- Auth: the query-cost guard middleware protects `/graphql`; resolver policies use the same JWT principal.
- Code-first keeps C# types as the contract (shared with `Griot.Domain`/DTO assembly).

### Auth (`[own-stack]`)
- `Konscious.Security.Cryptography` for Argon2; `System.IdentityModel.Tokens.Jwt` for JWT issuance/validation.
## Web Frontend

### React 18 + Vite 5 (`exact`)
- `npm create vite@latest griot-web -- --template react-ts`; `.nvmrc` → 20.
- No Next.js. Vercel detects the Vite framework preset automatically (Week 5).

### Material UI v6 (`exact`)
- Themed via `createTheme` from `ui-tokens.md` (colors, typography, shape). Glyphs: `@mui/icons-material`.
- Components used from `ui-registry.md`; no raw `<div>` styling for interactive surfaces.

### Apollo Client (`exact`)
- One client pointed at `{VITE_API_URL}/graphql`; `InMemoryCache` with `typePolicies` (e.g. `Task.order` merge: false) to survive board reorders.

### Axios + TanStack Query v5 (`exact`)
- One Axios instance with request (attach Bearer) + response (401 → silent refresh → retry once) interceptors.
- `QueryClient` with `staleTime: 30_000`; feature hooks (`useBoardQuery`, `useMoveTask`) live in each feature folder.

### Zustand (`[own-stack]`)
- Client-only state: filters, modal open/close, drag state, `accessToken` in memory. Never server data.

### React Router (`[own-stack]`)
- `public.tsx` (`/`, `/pricing`, `/login`, `/signup`) and `protected.tsx` (`/app/*`) behind `RequireAuth`.

### GSAP + Lenis (`[own-stack]`)
- Public shell only; lazy-loaded (`dynamic import`); StrictMode-safe via `useGSAP()` from `@gsap/react`.

## Mobile

### Flutter 3.19+ / Dart 3 (`exact`)
- `flutter create griot_mobile`; package set: `flutter_riverpod graphql_flutter dio flutter_secure_storage`.
- Riverpod (guide allows Provider/Riverpod; Riverpod is the closer match to Zustand's explicit-store model).

### graphql_flutter (`exact`)
- Same HotChocolate endpoint as web; verify cache-refresh behavior after mutations — use explicit `refetch` where needed instead of assuming Apollo semantics.

## AI Layer

### Trigger.dev v3 (`[own-stack]`)
- `ai/` npm package (Node 20, own lockfile). Agents defined in TS/`agents.json`; scheduled tasks are durable and idempotent.
- Web copilot streams via `@trigger.dev/react-hooks` `RealtimeProvider`/`useRealtimeRun`; NEVER wrap `triggerAndWait`/`batchTriggerAndWait` in `Promise.all`.

### MCP (`[own-stack]`)
- `mcp/` npm package with `@modelcontextprotocol/sdk` + `zod`.
- Each tool is a pure function `(graphqlClient, input) → output`; serve stdio (local) or Streamable HTTP (Docker/Railway).

### Redis (`[own-stack]` choice; stack-open)
- Rate limiting (sliding window), session/refresh metadata, AI token budgets. Container `gtp-redis:7-alpine` on 6380.

## Testing Libraries

| Layer | Library | Notes |
|---|---|---|
| Backend | xUnit + WebApplicationFactory | `IClassFixture<WebApplicationFactory<Program>>` against the real SQL Server container |
| Web | Jest + React Testing Library | `setupTests.ts`; MSW for the copilot stub |
| API contract | Newman | Reuses `Griot.postman_collection.json` headlessly |
| E2E | Cypress | Core-loop suite; stubbed copilot |
| Perf | k6 (`[own-stack]`) | JS scripts in `k6/` |
| Mobile | `flutter test` + `integration_test` | Widget + integration |
| AI | Vitest golden transcripts | Mocked LLM client injected at construction |