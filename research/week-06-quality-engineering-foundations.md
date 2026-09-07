# Week 6 — Quality Engineering Foundations (Project Griot)

> Guide's official Week 6 goals: QE fundamentals (SDLC/STLC, testing types, KPIs) → manual testing excellence → test management in Jira → **API testing mastery (Postman, env vars, assertions, Newman, contract testing)** → unit & integration testing (**xUnit/NUnit for .NET, Jest + RTL, Flutter widget/integration**) → automated UI testing (**Selenium/Cypress**) → non-functional (performance, OWASP, accessibility) → DevOps/TDD with **≥80% coverage**.
> Stack: exact. **xUnit** (choosing NUnit's sibling), **Jest + RTL**, **Cypress** (Selenium remains an alternative), Newman, k6 `[own-stack]`.

---

## 1. Decisions & Rationale

- **xUnit for the .NET backend** — the guide names xUnit/NUnit; xUnit chosen, run via `dotnet test`.
- **Jest + React Testing Library for the frontend** — guide line kept verbatim.
- **Cypress for UI/E2E** — guide says "Selenium / Cypress"; Cypress for DX and CI friendliness. A minimal Selenium suite stays as a documented option if juries ask.
- **Postman + Newman** — the Week-2 collection is the contract suite now; Newman runs it in CI (same collection, no throwaway).
- **Flutter widget & integration tests** — the Week-4 app gets `flutter test` + `integration_test`.
- **Performance → k6** `[own-stack]`: JS-scripted load checks against the deployed API (dashboard query, login, board).
- **OWASP Top-10 basics** now that auth is owned code: token storage, refresh rotation, rate limiting, password hashing, CORS — all auditable surfaces (payoff of the own-stack auth decision).
- **Coverage gate ≥80%** enforced in CI (line coverage), with the hard rule from before: service-layer + auth first, presentation second.

## 2. Backend: xUnit (unit) + WebApplicationFactory (integration)

```csharp
// tests/Griot.Tests/TaskServiceTests.cs
public class TaskServiceTests
{
    [Fact]
    public async Task BulkUpdate_Rotates_Statuses_Atomically()
    {
        // unit: mock the repository; assert single $transaction / ExecuteAsync call
    }
}

public class AuthApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Refresh_Cannot_Be_Replayed()
    {
        var client = _factory.CreateClient();     // against the real SQL Server container
        // login → capture refresh → reuse twice → second call must be 401
    }
}
```

## 3. Frontend: Jest + RTL
Component tests for TaskCard, BoardView, modal state transitions; mocks for Apollo/React Query hooks. Snapshot use is minimal (assert behavior).

## 4. Manual + Jira (guide items)
Test cases & scenarios, exploratory sessions, defect severity/priority, traceability matrix (requirement ↔ test ↔ defect) — all in Jira, screenshots attached.

## 5. API testing with Newman
```bash
newman run Griot.postman_collection.json -e sabahisha-env.json --reporters junit --reporter-junit-export newman.xml
```
Add as a **CI job parallel to the Cypress step** (same pattern as the old draft, now against the .NET API). Light contract checks (`pm.response.to.have.jsonSchema`) on the core endpoints.

## 6. Performance (k6) + security (OWASP)

```javascript
// k6/dashboard-load.js
import http from "k6/http";
import { check } from "k6";

export default function () {
  const res = http.get(`${__ENV.API_URL}/graphql`, { headers: { Authorization: `Bearer ${__ENV.TOKEN}` } });
  check(res, { "200": (r) => r.status === 200, "p95<500ms": (r) => r.timings.duration < 500 });
}
```

OWASP passes to perform on the .NET app:
- Injection — verify every Dapper/raw-SQL call is parameterized
- Broken auth — replay of rotated refresh tokens must fail (covered by §2)
- Sensitive data — no tokens in localStorage; cookie flags `HttpOnly; Secure; SameSite`
- CORS — API only serves the Vercel origin in prod

### 6.5 AI & MCP testing (own-stack — see `ai-integration.md`)

- **Golden-transcript tests** in `ai/`: fixed conversations against a **mocked LLM client** → assert exact tool-call order (no network in CI).
- **MCP tool contract tests** in `mcp/`: each tool unit-tested as `(graphqlClient, input) → output`; JSON contract asserted; plus a manual MCP Inspector smoke run.
- **OWASP for the AI surface**: the `ai-agent` principal is scoped (no deletes/invites), prompt-injection reviewed (user text as data), tool-call authorization verified per workspace.
- **E2E**: Cypress chat-panel flows with an MSW-stubbed copilot — no real LLM latency/cost in CI.

## 7. Accessibility
axe-core (through RTL or Lighthouse). An Awwwards-calibre Public Shell that fails a basic a11y audit undercuts the programme's "production mindset" goal.

## 8. Definition of Done — Week 6
- [ ] xUnit unit + integration suites green; refresh-rotation race covered
- [ ] Jest + RTL suites green for key App-shell components
- [ ] Cypress core-loop suite green in CI
- [ ] Newman collection green in CI (this is the contract suite)
- [ ] k6 baseline numbers recorded against the deployed Railway API
- [ ] OWASP review logged (findings or "no issue found" per item)
- [ ] Accessibility (axe) pass on the deployed Public Shell
- [ ] Coverage gate ≥80% on service-layer/auth specifically (vet on aggregate flat rate)
- [ ] AI layer: golden-transcript + MCP tool tests green; Cypress uses the stubbed copilot; `ai-agent` role scoped

---
**Engineering Excellence. Production Mindset. Professional Impact.**