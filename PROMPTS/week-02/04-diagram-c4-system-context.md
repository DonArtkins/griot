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

## 2. The prompt (single, extensive — no length limit)

Paste the full prompt below into Figma Make (Plan mode first). It intentionally includes every actor, every arrow, and every label — no abbreviation.

```text
C4 System Context diagram Level 1 for Griot, a project-management web app + AI copilot. Center box labeled "Griot — Project Management & AI Copilot" (dark fill, white text). Around it place exactly these, no internals, one page:

HUMANS (stick figures, left side):
- Workspace Owner (creates workspace, invites team, full admin)
- Team Member (boards, tasks, comments, notifications)

EXTERNAL SYSTEMS (plain boxes, right/bottom):
- Vercel (hosts web app, public internet)
- Railway (hosts API + MCP + SQL Server + Postgres + Redis)
- Trigger.dev (scheduled AI agents + Copilot background runs)
- GitHub Actions (CI/CD, builds + deploys on git push)
- Postman/Newman (API contract testing)
- Email provider (SMTP: invites, reminders, digests)
- External AI clients (Claude Desktop / Cursor / Cline via MCP tools)

ARROWS (label each):
- Workspace Owner → Griot (HTTPS, via Web + Mobile)
- Team Member → Griot (HTTPS, via Web + Mobile)
- External AI clients → Griot (MCP: get_board, create_task, update_task_status, add_comment, summarize_project)
- Postman/Newman → Griot (REST + GraphQL, contract tests)
- Griot → Email provider (SMTP: invites, reminders, weekly digest)
- Vercel → Griot (hosts web, serves Public + App shells)
- Railway → Griot (hosts API + MCP + databases)
- Trigger.dev → Griot (scheduled agents, Copilot streaming)
- GitHub Actions → Vercel / Railway / Trigger.dev (deploy trigger, dashed arrows)

STYLE: humans as stick figures on the left, external systems around the right/bottom, one big Griot box center-top. Protocol labels on every arrow. Everything readable at 100% zoom. One page, no internals.
```

### Refine

- "Move the Griot box to center; widen it."
- "Rename the center box to 'Griot — Project Management & AI Copilot'."
- "Make the GitHub Actions arrows dashed (indirect deploy trigger)."

---

## 3. Definition of Done (this diagram)

- [ ] Exactly one internal box (Griot) with zero internals
- [ ] All 7 external systems + 2 human actors present with correct labels
- [ ] Arrows show interaction direction + protocol where meaningful
- [ ] Approved → PNG → `project-kit/diagrams/architecture/c4-system-context.png`