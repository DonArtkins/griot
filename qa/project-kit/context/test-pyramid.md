# Test Pyramid

| Layer | Tool | Scope | Where |
|---|---|---|---|
| Backend unit | xUnit | services, domain rules, auth rotation (mocked repos) | `dotnet test` |
| Backend integration | xUnit + WebApplicationFactory | auth flows + core CRUD vs real SQL Server container | `dotnet test` |
| Frontend | Jest + RTL | TaskCard, BoardView, modals, state transitions; MSW copilot stub | `npm test` |
| API contract | Newman | Postman collection (backend feature 07); JSON schema assertions | CI job |
| E2E | Cypress | core loop; drag-drop; notifications; stubbed copilot | CI job |
| Perf | k6 [own-stack] | dashboard/login/board; p95 < 500ms | baseline + regression |
| Mobile | flutter test + integration_test | widget + device flows | `flutter test` |
| AI | Vitest golden transcripts | tool-call order (mock LLM); MCP tool JSON contracts | `npm test` (ai/mcp) |
| Accessibility | axe (RTL/Lighthouse) | Public + App shells | Week-6 gate |
| Security | OWASP review | injection, broken auth, sensitive data, CORS, AI scope | logged |
```
