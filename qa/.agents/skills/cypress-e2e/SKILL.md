---
name: cypress-e2e
description: "Cypress E2E for the Griot web app: core-loop suite (signup -> project -> tasks -> drag to Done) and Copilot flows with an MSW-stubbed copilot (no LLM in CI)."
metadata:
  version: "0.1.0"
---

# Cypress E2E Skill

## Suites

- Core loop: signup -> create project -> add 3 tasks -> move one to Done.
- Board drag-drop; comments; notifications read/unread.
- Copilot panel with an MSW stub (no real LLM latency/cost).

## Rules

- Run against the deployed API (or CI API service).
- Keep selectors via data-testid; no UI-text coupling.
- Runs in CI parallel to Newman.
