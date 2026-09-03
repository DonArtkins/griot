---
name: newman-api
description: "Newman CLI runs of the Postman collection in CI: API contract + regression suite, JSON schema assertions, chained tokens."
metadata:
  version: "0.1.0"
---

# Newman API Skill

## Run

```bash
newman run Postman/Griot.postman_collection.json -e <env> --reporters junit --reporter-junit-export newman.xml
```

## Rules

- Reuse the Week-2 collection (the contract suite) - never a throwaway.
- Light contract checks via `pm.response.to.have.jsonSchema` on core endpoints.
- Runs in CI (qa + infra) and weekly against the deployed system.
