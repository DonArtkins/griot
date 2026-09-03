# Project Kit — Griot (Orchestrator Kit)

The root kit coordinates the **seven systems**. Each system owns a full kit of its own in `<system>/project-kit/` (its AGENTS entry point, context files, feature specs, diagrams, examples). This root kit holds only what spans systems.

## Contents

```
project-kit/
├── README.md
├── context/
│   ├── system-map.md            ← the 7 systems + how they communicate
│   ├── stack-contract.md        ← the PDF stack table + [own-stack] markers
│   ├── integration-contracts.md ← ports, env vars, API/GraphQL cross-system contracts
│   └── progress-tracker.md      ← root-level tracker
├── diagrams/                    ← approved cross-system Figma Make/Figma artifacts (ERD, system map)
└── examples/                    ← reference assets
```

## Where the real kits live

| System | Kit root |
|---|---|
| Backend / API | `backend/project-kit/` |
| Web | `web/project-kit/` |
| Mobile | `mobile/project-kit/` |
| DevOps / Infra | `infra/project-kit/` |
| Quality Engineering | `qa/project-kit/` |
| AI agents | `ai/project-kit/` |
| MCP server | `mcp/project-kit/` |

## Reading order

Root `AGENTS.md` → `research/` → root context (system-map, stack-contract, integration-contracts) → the system's `AGENTS.md` → its context → its feature specs → the governing diagram.

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**
