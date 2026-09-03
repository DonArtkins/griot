# Diagrams — Figma / FigJam Reference Artifacts

Everything generated in Figma / FigJam that governs or documents Griot's implementation. **Code agents read these before writing code** (per `ai-workflow-rules.md`).

## Folders

| Folder | Holds | Governs |
|---|---|---|
| `erd/` | Entity-relationship diagrams (Week 2 deliverable) | Feature 03 schema; every entity/enum name is a contract here |
| `architecture/` | System context, container/C4, DFD, deployment diagrams | `architecture-context.md`; infra + deployment specs |
| `ui/` | App shell / Public shell screens, wireframes, tokens | `ui-context.md`, `ui-tokens.md`, `ui-rules.md`, `ui-registry.md` + Features 05/06 |
| `assets/` | Raw exports, PNG/JPG/source files referenced by the above | Naming + versioning ledger |

## Naming convention

`<artifact>-v<major.minor.patch>.png` — e.g. `griot-erd-v1.0.0.png`. Bump the version on every approved change; never overwrite an approved artifact.

## Ledger

| File | Version | Date | Source | Status | Notes |
|---|---|---|---|---|---|
| *(pending)* `griot-erd-v1.0.0.png` | v1.0.0 | Week 2 | FigJam (`PROMPTS/week-02/01-database-schema-erd-figma.md`) | Awaiting generation | 13 tables + 4 enums; contract for Feature 03 |

## Rules

1. Only **approved** diagrams enter here (approval = human sign-off in FigJam + this ledger updated).
2. A schema change means: update the ERD first, re-approve, **then** update Feature 03 + context + progress-tracker (contracts sync gate).
3. Screenshots from Week-1 Figma Make live in `ui/`; keep one versioned export per approval round.
4. The ledger is the index — if a file is not listed, it is not an approved artifact.