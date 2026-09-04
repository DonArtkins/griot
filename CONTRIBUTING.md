# Contributing to Griot

Thanks for wanting to contribute to **Griot** (GTP 2026 Bootcamp capstone). This repository is a **planning-first, seven-system monorepo** — every system (`backend/`, `web/`, `mobile/`, `infra/`, `qa/`, `ai/`, `mcp/`) has its own `AGENTS.md`, `project-kit/`, and (later) code. The root `AGENTS.md` links them.

## Branch model (group-aware — one feature spec = one branch)

Everything is grouped by system, and each system has feature specs numbered from `01`. Branches follow the **same coding standard**:

```text
feature/<system>/<NN>-<slug>
```

Examples:
- `feature/backend/02-sql-server-efcore`  (a backend feature-02 branch)
- `feature/web/05-secure-auth`
- `feature/mobile/03-rest-api-dio`
- `feature/infra/05-github-actions-cicd`
- `feature/qa/06-xunit`
- `feature/ai/02-copilot-agent`
- `feature/mcp/02-tool-roster`
- `feature/docs-and-planning/...`  (root docs branch)

Rules:
- **One feature spec = one branch = one PR.** Never bundle specs.
- Create the branch from `main`; never commit progress-tracker updates directly to `main`.
- The progress-tracker travels on the feature branch (so merged code always points to next work).
- No code implementation until the planning/design is approved (we're still in docs + diagrams phase).

## Commit message convention

We use [Conventional Commits](https://www.conventionalcommits.org/):

```text
feat(backend): add EF Core InitialCreate migration
fix(web): correct silent-refresh interceptor retry
docs(planning): add capacity plan + NFR
chore(infra): pin Docker base images
```

## Development workflow (per feature)

1. Check `.agents/skills/` + the system's `.agents/skills/` for the relevant `SKILL.md` and follow it.
2. Read the current **feature spec** (`<system>/project-kit/feature-specs/NN-*.md`) + its context.
3. Implement in scope only; write tests; run the system's verification gates.
4. Update contracts (**the owning feature spec**, **all dependent feature specs** that reference it, **docs/** for cross-system contracts, context files, root `AGENTS.md`, progress-tracker) **in the same branch** — never leave a system describing a stale contract (Rule 0 hard gate).
5. Commit + push; open a PR; fix review findings; the human merges.

## Definition of done (a PR is not done until…)

- [ ] All verification gates green (per root `AGENTS.md`)
- [ ] Tests written for new logic; coverage ≥80% on service-layer/auth
- [ ] Contracts synchronized (no stale API/entity/env/doc references)
- [ ] Docs updated if behavior changed (root `docs/` or system kit)
- [ ] The fix to any error found during work is **tested + documented** before shipping (see `docs/planning/CHANGE-MANAGEMENT.md`)

## Code of conduct

See `CODE_OF_CONDUCT.md`. Be kind, be specific, cite sources (Lyncxs KB + research), and prefer small verifiable increments.

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**