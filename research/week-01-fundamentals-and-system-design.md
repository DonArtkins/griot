# Week 1 — Fundamentals & System Design (Project Griot)
> Guide's official Week 1 goals: complete all required HackerRank challenges + set up a production-grade development environment.
> Bumped for this workflow (unchanged since the rewrite): full system design + UI/UX in Figma Make formalized into a Figma design system, and an **EF Core schema draft** derived from it (Week 2 will implement it in SQL Server).

---

## 1. Why design is front-loaded this week

The guide sequences schema design inside Week 2 because for most cohort members Week 1 is the bottleneck — getting .NET/SQL Server running is real work. That's still true here (this machine runs the exact same .NET 8 + SQL Server 2022 stack the guide requires), but **downloading a stack does not tell you what to build** — and building against a shifting design means rework in Weeks 2–4 that is far more expensive than rework in Figma. Doing full system design + UI/UX now means Week 2's EF Core schema is a *transcription* of a validated design instead of a guess that gets revised twice. This also satisfies the guide's Week 2 line item "Database schema design in Figma" a week early.

## 2. HackerRank — kept exactly as specified
- **SQL (Basic & Intermediate)** — SQL practice feeds directly into SQL Server work in Week 2 regardless of server.
- **Problem Solving in C#** — note: since the bootcamp's backend is C#/.NET, this is no longer "abstract algorithm practice"; it is direct onboarding for the capstone. Take it seriously as a skill on-ramp, 30–45 min/day, not a single sitting.

## 3. Environment Setup for This Week

Everything needed for Week 1 (the rest of the stack is in the master doc §6 and each week pulls what it needs):

- [ ] .NET 8 SDK (`dotnet --version` → 8.0.x) 
- [ ] VS Code with the **C# Dev Kit** extension (`.NET SDK` runs on Linux — no Visual Studio needed)
- [ ] SQL Server 2022 container (`gtp-sqlserver`) accepting connections via DBeaver
- [ ] Redis container running for the auth/rate-limiting store added in Week 2
- [ ] Docker Engine (real; `docker --version` must NOT show "podman")
- [ ] Node.js 20 LTS via `nvm` (personal 24.x unchanged — master doc §6 shows the non-conflicting setup)
- [ ] Flutter `3.19+` SDK + Android toolchain (final check, `flutter doctor`)
- [ ] Figma account (Figma Make accessible), Jira workspace, GitHub SSH key, Postman

Full commands: `gtp-2026-prep.md` §6. Nothing backend-framework-specific needs installing yet past SQL Server/Redis containers — Week 2 adds the .NET project.

---

## 4. System Design & UI/UX — Using Figma Make

### 4.1. Scope Griot v1 *before* prompting
Same guard as before: lock scope first or Figma Make will happily generate a beautiful UI for a feature set too large to ship in 7 weeks.

**In scope for v1:**
- One workspace per user (multi-workspace = v2)
- Projects → Boards → Columns (Backlog / In Progress / In Review / Done) → Tasks
- Task: title, description, assignee, priority, due date, status, comment thread, attachments
- Team membership with roles (owner/admin/member), invites
- Activity feed (who did what, when)
- Notifications (mention, assignment, due-date reminder)

**Deferred (do not let Figma Make wander here):** timelines/Gantt, custom fields, guest access, automations, multi-board reports, mobile (Flutter app in Week 4 is a companion/secondary surface).

### 4.2 Two Figma Make sessions, not one (kept)

1. **App Shell session** (the product): 5 core screens — Dashboard, Kanban Board, Task Detail, Team Settings, Notifications. Use Plan mode: it is the multi-screen, stateful surface where steering intent first saves regeneration.
2. **Public Shell session** (the Awwwards face): landing, pricing, login — single-shot candidate; spectacle/design-cred here (Week 3 minimum).

Prompt guard rails both sessions: keep each surface's own goal (speed/clarity vs. spectacle); reject widgets that aren't in §4.1 scope.

### 4.3 From Figma Make → formal design system

Budget 2–3 refinement rounds per surface; the model output is a "rough clay model," not the final UI. Then in Figma proper, before any code:

- Turn the app-shell screens into **components** (TaskCard, BoardColumn, Sidebar, Modal, NotificationItem…)
- Extract real **tokens**: color ramp, type scale, spacing rhythm, radii — these feed two targets in Weeks 2–3:
  - the **MUI theme** in the web app (colors/typography/shape → `createTheme`), *not* a separate Tailwind config.
  - Figma annotation of status colors so the EF Core `TaskStatus`/`Priority` enums match the visual model.

### 4.4 From Figma to the EF Core schema draft

The data model falls out of the five screens instead of being invented separately:

| Screen | Entities implied |
|---|---|
| Dashboard | `Workspace`, `Project`, `ActivityLog` (feed) |
| Kanban board | `Board`, `Column`, `TaskItem` (status, priority, dueDate, assigneeId) |
| Task detail | `TaskItem` + `Comment`, `Attachment` |
| Team settings | `User`, `WorkspaceMember` (role enum), `Invite` |
| Notifications | `Notification` (type, read state, target ref) |

This table is Week 2's input for the **EF Core schema** (`GriotDbContext`); nothing in Week 2 should add an entity with no corresponding screen. Deliverable this week: a schema sketch (entities, keys, relations) attached here — Week 2 transcribes it into `OnModelCreating`.

---

## 5. Decisions & Rationale (Week 1 summary)

- **Design before code** — Week 2 code is translation, not invention.
- **Two Figma sessions** — opposite design goals shouldn't share a generation.
- **Plan mode only for the App shell** — complex/stateful; Public is closer to single-shot.
- **HackerRank kept exactly** — and the C# track now doubles as .NET onboarding.
- **Schema derived from screens, not parallel-invented** — keeps Week 2 honest.
- **Full stack stays bootcamp-exact** — this week installs the guide's stack (SQL Server 2022 + PostgreSQL 16 + Redis), not a personal substitute.

---

## 6. Risks & Watch-outs

- **Scope creep inside Figma Make itself** — keep the §4.1 scope list open in another tab and reject anything not on it (no timeline view, no custom fields).
- **Treating the first generation as final** — forces at least one formalization pass in Figma before Week 1 closes.
- **HackerRank being crowded out by the design work** — time-box it daily.
- **Over-speccing the schema table in §4.4** — if it's not on a screen, it's not in the schema sketch. Week 2 adds the SQL implementation details, not new tables.

## 7. Definition of Done — Week 1

- [ ] HackerRank: SQL (Basic & Intermediate) + Problem Solving in C# complete
- [ ] Dev environment per master doc §6 verified: .NET 8, Node 20, VSCodium + C#, Docker Engine real, `gtp-sqlserver` container reachable (DBeaver), PostgreSQL 16 + Redis containers, Flutter doctor (Android row green)
- [ ] App Shell: 5 core screens generated in Figma Make, refined ≥2 rounds
- [ ] Public Shell: landing/pricing/login generated, refined ≥1 round
- [ ] Both surfaces formalized into one Figma file with components + color/type/spacing tokens
- [ ] Entity/screen mapping table (§4.4) completed — the input Week 2 transcribes into the EF Core schema
- [ ] Jira workspace live, Week 1 tasks logged and closed out

---
**Engineering Excellence. Production Mindset. Professional Impact. 🚀**