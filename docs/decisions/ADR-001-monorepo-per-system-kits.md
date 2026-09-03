# ADR-001 — Monorepo with Per-System Kits (seven systems, one repo)

**Status:** Accepted  
**Date:** 2026-09-03  
**Deciders:** Don Artkins (Griot)

## Context

The bootcamp defines five systems (backend, web, mobile, DevOps, QE) plus our [own-stack] AI layer (ai, mcp). We need deep planning artifacts per system, but also a single repo where the root links them (like a web). Each system has its own feature specs numbered from `01`, its own `AGENTS.md`, skills, and kit.

## Decision

One monorepo with a root `AGENTS.md` + root `project-kit/` (system-map, stack-contract, integration-contracts). Each system (`backend/`, `web/`, `mobile/`, `infra/`, `qa/`, `ai/`, `mcp/`) carries its own `AGENTS.md`, `.agents/skills/`, and `project-kit/` (context + feature-specs + diagrams + examples). Branches are group-aware: `feature/<system>/<NN>-<slug>`.

## Options considered

| Option | Pros | Cons |
|---|---|---|
| **Monorepo, per-system kits** (chosen) | atomic cross-system changes; shared root contracts; each system's docs stay small; mirrors Foundrie pattern + FAANG monorepo norm | branch naming must be disciplined |
| All-in-one kit | simplest | bloats; systems aren't separable; violates separation of concerns |
| Multi-repo | clean boundaries | cross-repo contract sync is painful; CI complexity |

## Consequences

- Separation of concerns is physical (folders). Contract sync is a root hard rule.
- Each system's planning is deep enough that implementation is straight-line.
- Root docs (`docs/ARCHITECTURE.md`, planning) link systems.

## References
- `AGENTS.md`, `project-kit/context/system-map.md`, `CONTRIBUTING.md`.