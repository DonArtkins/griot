# AI Workflow Rules

## Contracts Synchronization Gate

Any implementation change that corrects or changes a contract must be reflected in the same branch across the affected feature spec, dependent future specs, relevant context files, root `AGENTS.md`, and `progress-tracker.md`. Contracts include EF Core entities/relations and enum values, REST route signatures, GraphQL type/query/mutation names, auth token claims and endpoints, `GRIOT_SERVICE_TOKEN` behavior, env variables, Docker/Compose service names and ports, storage paths, generated file structure, package versions, and file ownership.

## Approach

Build Griot incrementally from the feature specs, in numeric order, one spec at a time. Context files define what to build and why; feature specs define the implementation order and include Setup/Initialization, Separation of Concerns, and Docker & Deploy. Agents must not jump from a vague goal to code. Roadmap phase labels are organizational only — never permission to batch specs.

## Mandatory Startup Routine

1. Read `AGENTS.md`.
2. Read `research/gtp-2026-prep.md` (stack contract + isolation rules).
3. Read `research/week-0X-*.md` (the week(s) the current feature belongs to) and `research/ai-integration.md` when the AI surface is in scope.
4. Read all context files in order: project-overview, architecture-context, build-plan, code-standards, library-docs, ui-context, ui-tokens, ui-rules, ui-registry, test-validation-plan, progress-tracker.
5. Read the current feature spec.
6. Check `project-kit/diagrams/**` for the governing diagram (ERD before schema work, architecture before infra work, wireframes before UI work).
7. For schema changes: review the **approved ERD** and keep entity names/enums synchronized.
8. For UI changes: check `ui-tokens.md`/`ui-registry.md`/`ui-rules.md` before writing components.

## Planning Gate

- Never jump from a vague goal directly to implementation.
- Present a concrete plan before implementation-impacting work and wait for explicit user approval.
- If the user requests revisions, update the plan and present it again before executing.
- Apply the gate to: new apps init, schema migrations, API surface changes, Docker/Compose changes, deployment changes, and feature-spec work.

## Research-Driven Decisions

- Every stack decision traces to the bootcamp PDF (status `exact`) or carries `[own-stack]` with a written rationale.
- When current library docs conflict with a context file, research the correct behavior, update the context file, then implement.
- Before committing a framework/library/package version, verify current stable versions against official sources (the bootcamp pins the majors: .NET 8, EF Core 8, Dapper 2.x, HotChocolate 14+, React 18, Vite 5, MUI 6, Flutter 3.19+).

## Scoping Rules

- One feature spec at a time. Do not open the next spec until the current one is done (build+tests+CI green).
- Build exactly what the current spec requires. Do not prebuild future behavior.
- Keep UI, API, background task, database, and AI-provider work separated unless the spec explicitly combines them.
- Never initialize another app's scaffold from a spec that doesn't own it (setup commands live in the creating spec).

## Separation of Concerns Rules

- Controllers/resolvers: no business logic.
- `Griot.Application`: no EF, no HTTP, no Redis calls — services orchestrate repositories + domain rules.
- `Griot.Infrastructure`: persistence only; Dapper and EF coexist behind repository interfaces.
- `Griot.Domain`: entities/enums only — zero references.
- `web/` + `mobile/`: presentation only; API access through typed clients; no direct DB.
- `ai/` + `mcp/`: no DB credentials ever; only GraphQL via `GRIOT_SERVICE_TOKEN`.

## Verification Gates (before "done")

- **Backend**: `dotnet build` + `dotnet test` (xUnit incl. refresh-rotation replay) green; Compose local stack health check OK.
- **Web/Node**: `npm run lint && npm run typecheck && npm test && npm run build` green (each project's own lockfile).
- **Mobile**: `flutter analyze` + `flutter test` green.
- **CI**: Newman + Cypress jobs green; coverage ≥80% on service-layer/auth.
- **Docs**: contracts synchronized (feature specs + context + AGENTS.md + progress-tracker in one branch).

## AI-Layer Build Rules

- The Copilot/agent/MCP code is tested with mocked LLM clients (golden transcripts); CI never pays LLM latency/cost.
- Mutation tool calls in the Copilot are proposed → approved in the UI → executed by the app, not by the agent.
- Service token `GRIOT_SERVICE_TOKEN` is the only credential an agent holds; LLM keys live in `ai/.env` only.
- Every AI build updates the audit contract (tool call logging with `workspaceId`, `tool`, `payloadHash`, `runId`).

## Progress Tracking

- Update `progress-tracker.md` at the end of every session: what changed, what's next, open questions, and any contract deltas.
- Keep the `Definition of Done` checklists in the research week files in sync with what ships.