---
name: k6-perf
description: "k6 load/performance testing for Griot: dashboard query, login, board read. Baseline (Week 6) vs regression (Week 7). p95 < 500ms target."
metadata:
  version: "0.1.0"
---

# k6 Performance Skill

## Scripts

`k6/dashboard-load.js`, `k6/login.js`, `k6/board.js` - threshold p95 < 500ms on API responses.

```js
import http from "k6/http";
import { check } from "k6";
export default function () {
  const res = http.get(`${__ENV.API_URL}/graphql`, { headers: { Authorization: `Bearer ${__ENV.TOKEN}` } });
  check(res, { "200": (r) => r.status === 200, "p95<500ms": (r) => r.timings.duration < 500 });
}
```

## Rules

- Baseline recorded Week 6 against deployed Railway; regression Week 7.
- Feed results into the exec summary.
