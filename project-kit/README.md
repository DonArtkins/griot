# Project Kit — Griot (modeled on the Foundrie AI kit)

The live implementation kit: what to build, in what order, against which contract. The bootcamp research corpus (`research/`) decides the stack; this kit decides the code.

## Contents

```
project-kit/
├── README.md            ← this index
├── context/             ← 12 context files (read in the AGENTS.md order)
│   ├── project-overview.md
│   ├── architecture-context.md
│   ├── build-plan.md
│   ├── code-standards.md
│   ├── library-docs.md
│   ├── ui-context.md
│   ├── ui-tokens.md
│   ├── ui-rules.md
│   ├── ui-registry.md
│   ├── ai-workflow-rules.md
│   ├── test-validation-plan.md
│   └── progress-tracker.md
├── feature-specs/       ← 10 ordered implementation specs (01–10)
├── examples/            ← reference examples / inspiration assets
└── diagrams/            ← approved Figma/FigJam artifacts (ERD, architecture, UI)
```

## Reading order

Root `AGENTS.md` → research week files → context files (per `AGENTS.md` §Mandatory Reading Order) → the current feature spec → the governing diagram in `diagrams/`.

## Feature-spec conventions (Griot-specific)

Unlike generic kits, every Griot feature spec carries three mandatory sections:
1. **Setup / Initialization** — the exact scaffold/init commands for the app it creates.
2. **Separation of Concerns** — the folder/architecture boundaries that app must respect.
3. **Docker & Deploy** — the container story and the deploy target for *that* app.

Specs are implemented strictly in numeric order; a spec is done only when its tests/build/CI gates are green and its contracts are synchronized.

## Status

Weeks 1–2 in progress — see `context/progress-tracker.md`.

---

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**