# Diagrams — Figma Make / FigJam Reference Artifacts

Everything generated in Figma Make / FigJam that governs or documents Griot's implementation. **Code agents read these before writing code** (per `AGENTS.md` + `ai-workflow-rules`).

## Folders

| Folder | Holds | Governs |
|---|---|---|
| `erd/` | Entity-relationship diagram (Week-2 deliverable) | Backend spec 02 schema; every entity/enum name is a contract here |
| `architecture/` | C4 context/container, API component, sequences, state, deployment, auth matrix, AI context | `ARCHITECTURE.md`, infra, backend, web, ai, mcp |
| `ui/` | App shell / Public shell screens, wireframes, tokens | `ui-context.md`, `ui-tokens.md`, `ui-rules.md`, `ui-registry.md` + Features 05/06 |
| `assets/` | Raw exports, PNG/JPG/source files referenced by the above | Naming + versioning ledger |

## Naming convention

`<artifact>-v<major.minor.patch>.png`. Bump version on every approved change; never overwrite an approved artifact.

## Ledger

| File | Source spec (PROMPTS/) | Status | Notes |
|---|---|---|---|
| `erd/griot-erd-v1.0.0.png` | `week-02/01-…` + `02-…-sequence` | ✅ Approved | **16 tables / 5 enums**; contract for backend spec 02 |
| `erd/griot-erd-v2.0.0.png` | spec 29 multi-tenant amendment | ✅ Approved (operator, 2026-09-11) | **v2.0.0 — multi-tenant wave**: org tables + `OrganizationId` stamps; org FKs NO ACTION; contract for backend spec 29 |
| `erd/griot-erd2-v2.0.0.png` | spec 29 multi-tenant amendment | ✅ Approved (operator, 2026-09-11) | **v2.0.0 continuation sheet** — org tables + tenant columns detail |
| `erd/griot-erd3-v2.0.0.png` | spec 29 multi-tenant amendment | ✅ Approved (operator, 2026-09-11) | **v2.0.0 continuation sheet** — third sheet of the spec-29 multi-tenant amendment export set |
| `architecture/c4-system-context-v2.0.0.png` | spec 29 wave v2 refresh | ✅ Approved (operator, 2026-09-11) | multi-tenant system context (SuperAdmin / Company / Client actors) |
| `architecture/griot-api-v2.0.0.png` | spec 29 wave v2 refresh | ✅ Approved (operator, 2026-09-11) | API internals v2 with tenancy |
| `architecture/c4-system-context.png` | `week-02/04-…c4-system-context` | ✅ Approved | Level 1 |
| `architecture/c4-container.png` | `week-02/05-…c4-container` | ✅ Approved | Level 2 — maps to compose/Railway |
| `architecture/api-component.png` | `week-02/06-…api-component` | ✅ Approved | L3 API internals |
| `architecture/sequence-login-refresh.png` | `week-02/07-…` | ✅ Approved | auth + replay race |
| `architecture/sequence-create-task-fanout.png` | `week-02/08-…` | ✅ Approved | fan-out decision |
| `architecture/sequence-bulk-status.png` | `week-02/09-…` | ✅ Approved | TX boundary |
| `architecture/task-state-machine.png` | `week-02/10-…` | Awaiting generation | legal transitions |
| `architecture/deployment-production.png` | `week-02/11-…` | Awaiting generation | network boundaries |
| `architecture/auth-permissions-matrix.png` | `week-02/12-…` | Awaiting generation | Owner/Admin/Member/ai-on-behalf-of (real-user OBO) |
| `architecture/api-surface-map.png` | `week-02/13-…` | Awaiting generation | every route + GraphQL |
| `architecture/ai-system-context.png` | `week-02/14-…` | Awaiting generation | web→ai→mcp→backend |

## Rules

1. Only **approved** diagrams enter here (approval = human sign-off in Figma Make + this ledger updated).
2. A schema change: update the ERD first, re-approve, **then** update backend spec 02 + context + all dependent specs (contracts sync).
3. Screenshots from Week-1 Figma Make live in `ui/`; keep one versioned export per approval round.
4. The ledger is the index — if a file is not listed, it is not an approved artifact.
