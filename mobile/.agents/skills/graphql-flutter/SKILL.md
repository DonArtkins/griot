---
name: graphql-flutter
description: "graphql_flutter integration on Griot mobile: same HotChocolate endpoint as web, explicit refetch after mutations, per-feature queries."
metadata:
  version: "0.1.0"
---

# graphql_flutter Skill

## Client

- One `GraphQLClient` (http link) pointed at `API_URL/graphql`, Bearer from the auth provider.
- Wrap the app in a `GraphQLProvider`.

## Rules

- Do NOT assume Apollo-equivalent cache semantics — after mutations, use explicit `refetch`/`client.query`.
- Dashboard + boards read via GraphQL; task detail + comments too.
- Auth token attached via link middleware.
