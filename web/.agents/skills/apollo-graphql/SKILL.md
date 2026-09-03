---
name: apollo-graphql
description: "Apollo Client integration for Griot's GraphQL reads: single client, InMemoryCache typePolicies, colocated fragments, refetch discipline."
metadata:
  version: "0.1.0"
---

# Apollo GraphQL Skill

## Client

```ts
// src/lib/apolloClient.ts
export const apolloClient = new ApolloClient({
  link: new HttpLink({ uri: import.meta.env.VITE_API_URL + "/graphql", headers: () => ({ authorization: Bearer… }) }),
  cache: new InMemoryCache({ typePolicies: { Task: { fields: { order: { merge: false } } } } }),
});
```

## Rules

- GraphQL reads: dashboard, boards, task detail. Mutations that Apollo handles (board/task moves) use optimistic updates.
- Fragments colocate with components (`BoardTaskCard.fragment.ts`).
- `typePolicies` prevent reorder merge bugs.
- Server data never enters Zustand; select from the Apollo cache.

## Verify

- Board reorder keeps cache sane; no duplicate key warnings in console.
