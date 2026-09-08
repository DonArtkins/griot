# Test Pyramid

| Layer | Tool | Scope | Where |
|---|---|---|---|
| Backend unit | xUnit | services, domain rules, auth rotation (mocked repos) | `dotnet test` |
| Backend integration | xUnit + WebApplicationFactory | auth flows + core CRUD vs real SQL Server container | `dotnet test` |
| Frontend | Jest + RTL | TaskCard, BoardView, modals, state transitions; MSW copilot stub | `npm test` |
| API contract | Newman | Postman collection (backend feature 08); JSON schema assertions | CI job |
| E2E | Cypress | core loop; drag-drop; notifications; stubbed copilot | CI job |
| Perf | k6 [own-stack] | dashboard/login/board; p95 < 500ms | baseline + regression |
| Mobile | flutter test + integration_test | widget + device flows | `flutter test` |
| AI | Vitest golden transcripts | tool-call order (mock LLM); MCP tool JSON contracts | `npm test` (ai/mcp) |
| Accessibility | axe (RTL/Lighthouse) | Public + App shells | Week-6 gate |
| Security | OWASP review | injection, broken auth, sensitive data, CORS, AI scope | logged |
```

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
