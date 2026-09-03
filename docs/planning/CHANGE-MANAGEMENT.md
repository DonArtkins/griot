# Change Management — Griot

> The rule: **everything must be documented.** If an AI agent hits an error and it's fixed, the fix must be **tested, verified working, then documented** — because it's no longer exactly what the spec said. Every deviation from a feature spec, context file, or ADR is recorded.

## The change lifecycle (mandatory)

1. **Encounter** — an error, deviation, or "I need something the spec doesn't cover".
2. **Don't silently patch.** Create/attach a **change note**: what happened, why, what the spec said vs what's needed.
3. **Implement the fix** in a small, testable increment.
4. **Test it** — prove the fix works (unit/integration/manual per the system's gates).
5. **Document it** — record the fix in:
   - the relevant **feature spec** (correct the contract — real names/signatures/enums),
   - any **dependent future specs** (stale refs fixed),
   - **context files** if architecture/data changed,
   - `progress-tracker.md` session notes,
   - optionally a **CHANGELOG** entry or **ADR** if it's a design decision,
   - the root `AGENTS.md` if agent workflow/rules changed.
6. **Contract-sync gate**: before commit, grep for stale references; update in the same branch. Run the skill `.agents/skills/contract-sync/`.
7. **Ship** on the feature branch (one spec = one branch); never directly to `main`.

## Where changes land

| Type of change | Recorded in |
|---|---|
| Bug/error fixed during implementation | feature spec + progress-tracker + CHANGELOG |
| API route/GraphQL type changed | `api-surface.md` + dependent specs + Postman collection |
| Schema/entity/enum changed | ERD (re-approve) + `data-layer.md` + EF specs + web/mobile types + MCP schemas |
| Auth/security behavior changed | auth ADR + SECURITY.md + qa test suite |
| Deployment/infra changed | infra context + DEPLOYMENT.md runbooks |
| New design decision (options considered) | `docs/decisions/ADR-XXX-*.md` |
| AI/MCP tool changed | root `integration-contracts.md` + ai/mcp feature specs |

## Rules

- **No silent drift.** If code differs from the spec, the spec (and this system's planning) is updated to match — in the same PR.
- **Test before document.** The fix ships only after its test passes.
- **Small increments.** No mega-PRs; one spec/one fix per branch.
- **Append-only audit.** `AuditLogs`/`ErrorLogs` are never edited.

## Error/fix log template

```md
### Error: <short title>
- **Where**: <system + file/route>
- **What the spec said**: …
- **What actually happened**: …
- **Root cause**: …
- **Fix applied**: …
- **Test proving fix**: <test name / step>
- **Docs updated**: <files>
- **Date / branch**: …
```

This is the guarantee that "I don't come back later to change the schema" holds — whatever changes, it's captured, tested, and synchronized **before** it becomes code.

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**