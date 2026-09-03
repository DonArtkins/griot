---
name: riverpod-state
description: "Riverpod state management on Griot mobile: server state from graphql_flutter/dio with explicit refetch, client-only UI state in providers, auth token in memory. Guide allows Provider/Riverpod; we chose Riverpod."
metadata:
  version: "0.1.0"
---

# Riverpod State Skill

## Providers

- `authProvider` — accessToken + user (memory; cleared on restart).
- Feature providers mirror the Zustand split on web: server state refetched explicitly; UI state (filters, panel visibility) in providers.
- Use `ProviderScope` at the root.

## Rules

- No server data duplication into providers that already have a cache/refetch path.
- Status change = picker, not drag-drop (mobile idiom).
