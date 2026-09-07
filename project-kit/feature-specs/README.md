# Root Feature Specs

Cross-system/runtime specs for the monorepo live **inside the owning system's kit**, not here:

- Repo layout, shared skills, git flow → covered by the root `AGENTS.md` + `/.agents/skills/` (no numbered spec needed).
- Everything else → `<system>/project-kit/feature-specs/`.

| System | Specs |
|---|---|
| backend | 01 ERD → 09 AI service token |
| web | 01 Vite setup → 10 Copilot panel |
| mobile | 01 Flutter setup → 07 responsive UI |
| infra | 01 Vercel → 06 Docker Hub |
| qa | 01 QE fundamentals → 13 UAT & exec report |
| ai | 01 Trigger setup → 05 golden transcripts |
| mcp | 01 MCP setup → 05 contract testing |

Keep this file; it is the index agents use to find a system's specs.


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly.
