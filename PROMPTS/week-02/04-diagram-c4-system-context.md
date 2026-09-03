# Week 02 · Diagram 01 — C4 System Context (Level 1)

**Master spec + Figma Make paste prompts.** This diagram answers *"what are we even building, and who/what touches it?"* — one page, no internals. Show it to any stakeholder.

---

## 1. What this diagram must capture

**One system box: `Griot`** (project-management web app + AI copilot). Around it, exactly these external actors/systems:

| Actor / external | Type | Interaction | Direction |
|---|---|---|---|
| Workspace Owner | Human | creates workspace, invites members, full admin | → Griot via Web/Mobile |
| Team Member | Human | views boards, moves tasks, comments, receives notifications | → Griot via Web/Mobile |
| External AI client (Claude Desktop / Cursor / Cline) | External system | connects via MCP tools (`get_board`, `create_task`, …) | → Griot (MCP) |
| Email provider (SMTP / transactional) | External system | invite emails, due-date reminders, digest delivery | Griot → provider |
| Vercel | External host | serves the web app (Public + App shells) on the public internet | hosts web |
| Railway | External host | runs the API container, MCP container, SQL Server, Postgres, Redis; public API origin | hosts backend + MCP |
| Trigger.dev | External host | runs scheduled AI agents + Copilot background runs | hosts ai |
| Postman / Newman | External tool | the contract-testing client for the API | → Griot API |
| GitHub Actions | External CI | builds + tests everything, deploys on merge | triggers deploys |

**Rules for this level:** no internal boxes (that's Level 2), no database names, no protocols on every line (that's Level 2). One box labeled "Griot — Project Management & AI Copilot". Keep the human actors as stick figures, external systems as plain boxes on the outside ring.

## 2. Figma Make prompts (each ≤2000 chars, run in order on ONE canvas)

### PROMPT A — Seed the canvas

```
C4 System Context diagram Level1 for Griot, a PM web app + AI copilot. Center box labeled "Griot — Project Management & AI Copilot" (dark fill, white text). Around it place exactly these, no internals, one page:
Humans (stick figures): Workspace Owner (creates workspace, invites team, full admin); Team Member (boards, tasks, comments, notifications).
External systems (plain boxes): Vercel (hosts web app, public internet); Railway (hosts API + MCP + SQL Server + Postgres + Redis); Trigger.dev (scheduled AI agents + Copilot runs); GitHub Actions (CI/CD, deploys on merge); Postman/Newman (API contract testing); Email provider (invites, reminders, digests); External AI client (Claude/Cursor/Cline via MCP tools).
```

### PROMPT B — Add the arrows

```
Add labeled arrows to the Griot C4 L1 diagram:
Owner + Member -> Griot (HTTPS, via Web+Mobile)
Griot -> Email provider (SMTP: invites, reminders, digest)
External AI client -> Griot (MCP: get_board, create_task, update_task_status, add_comment, summarize_project)
Postman/Newman -> Griot (REST+GraphQL, contract tests)
Vercel -> Griot (hosts web, serves Public+App shells)
Railway -> Griot (hosts API+MCP+databases)
Trigger.dev -> Griot (scheduled agents, Copilot)
GitHub Actions -> Vercel/Railway/Trigger.dev (deploy trigger, dashed)
Style: humans as stick figures left, external systems right/bottom, one big Griot box center-top. No internals. Readable at 100% zoom.
```

### Fix snippets

- "Move the Griot box to center; widen it."
- "Rename the center box to 'Griot — Project Management & AI Copilot'."
- "Add thin arrow Griot -> Email provider labeled SMTP invites+digests."
- "Make GitHub Actions arrow dashed (indirect deploy trigger)."

---

## 3. Definition of Done (this diagram)

- [ ] Exactly one internal box (Griot) with zero internals
- [ ] All 7 external systems + 2 human actors present with correct labels
- [ ] Arrows show interaction direction + protocol where meaningful
- [ ] Approved → PNG → `project-kit/diagrams/architecture/c4-system-context.png`