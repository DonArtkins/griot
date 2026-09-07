---
name: contract-sync
description: "Cross-system contract synchronization gate. Run before any commit/push/PR when an implementation changes or corrects a project contract (schema, routes, GraphQL types, env vars, ports, tokens, enums, Docker services)."
metadata:
  version: "0.1.0"
---

# Contract Synchronization Gate

Any implementation change that corrects or changes a contract must be reflected in the same branch across:
1. The owning system's current feature spec (correct field names, real signatures, final enums).
2. Every future spec in that system AND every dependent system's specs (fix stale references).
3. Relevant context files: `project-kit/context/*`, `<system>/project-kit/context/*`.
4. Root `AGENTS.md` when agent workflow or system boundaries change.
5. `progress-tracker.md` of every affected system (session notes on what changed and why).

## Contract Types

- Backend: EF Core entities/relations, enum values, REST route signatures, GraphQL type/query/mutation names, auth token claims/endpoints, `GRIOT_SERVICE_TOKEN` behavior, Docker/Compose service names and ports.
- Web/Mobile: route names, theme tokens, query cache shapes, auth store/cookie behavior.
- Infra: compose service names/ports, container registry names, CI job names, env var matrix.
- QA: suite names, coverage thresholds, test environment expectations.
- AI/MCP: tool names, tool input/output JSON contract, agent/task ids, realtime stream shapes.

## Procedure

1. Before starting a feature, list every contract it touches.
2. Implement.
3. Diff against every file that references those contracts; update stale mentions in the same branch.
4. Verify with grep: search the repo for the old contract name/signature and confirm zero stale hits.
5. Commit the contract updates together with the feature — never on `main` separately.

A feature is not ready for review while later specs or context still describe stale contracts.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
