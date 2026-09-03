# Week 02 · Diagram 08 — Auth/Permissions Matrix

**Table + Figma Make prompts.** Rows = roles (Owner/Admin/Member); columns = actions. Catches "wait, can a Member delete someone else's task?" before it's a bug report.

---

## 1. The matrix (the contract)

| Action | Owner | Admin | Member | ai-agent (service) |
|---|---|---|---|---|
| View workspace/board/tasks | ✅ | ✅ | ✅ | ✅ (ReadWorkspace) |
| Create project / board | ✅ | ✅ | ✅ | ❌ |
| Edit task (title/desc/status/assignee) | ✅ | ✅ | ✅ (assigned OR workspace) | ✅ (CreateTask/UpdateStatus) |
| Move task across columns | ✅ | ✅ | ✅ | ❌ (propose-only via Copilot) |
| Delete task | ✅ | ✅ | ❌ | ❌ |
| Add comment | ✅ | ✅ | ✅ | ✅ (AddComment) |
| Invite members | ✅ | ✅ | ❌ | ❌ |
| Remove member | ✅ | ❌ | ❌ | ❌ |
| Change roles | ✅ | ❌ | ❌ | ❌ |
| Delete workspace | ✅ | ❌ | ❌ | ❌ |
| Manage notifications | ✅ | ✅ | ✅ | ✅ (CreateNotification) |
| View audit/error logs | ✅ | ✅ (own errors) | ❌ | ❌ |
| Use Copilot | ✅ | ✅ | ✅ | — |
| MCP external tool access | ✅ (workspace scope) | ✅ | ✅ | ✅ |

Legend: ❌ = hard denied (404 not 403 for ownership — never disclose existence). Admin ≠ Owner for destructive ops. ai-agent is restricted via `GRIOT_SERVICE_TOKEN`.

## 2. The prompt (single, extensive — no length limit)

Paste the full prompt below into Figma Make. It draws the full permission matrix in one pass.

```text
Build a permission matrix TABLE for Griot (not a diagram). 4 columns: Owner | Admin | Member | ai-agent (service). Rows (Y = green cell, N = red cell):

View workspace/board/tasks — Y | Y | Y | Y (ReadWorkspace)
Create project/board — Y | Y | Y | N
Edit task (title/desc/status/assignee) — Y | Y | Y (assigned OR workspace) | Y (CreateTask/UpdateStatus)
Move task across columns — Y | Y | Y | N (propose-only via Copilot)
Delete task — Y | Y | N | N
Add comment — Y | Y | Y | Y (AddComment)
Invite members — Y | Y | N | N
Remove member — Y | N | N | N
Change roles — Y | N | N | N
Delete workspace — Y | N | N | N
Manage notifications — Y | Y | Y | Y (CreateNotification)
View audit/error logs — Y | Y (own errors) | N | N
Use Copilot — Y | Y | Y | —
MCP external tool access — Y (workspace scope) | Y | Y | N

FOOTER (must be present):
"Denied = 404 not 403 (never disclose existence). Admin ≠ Owner for destructive ops (remove member, role changes, delete workspace, delete task). ai-agent is the restricted service principal via GRIOT_SERVICE_TOKEN — no deletes, no invites, workspace-scoped."

Style: green Y cells, red N cells, clear column headers, one page.
```

### Refine

- "Change 'Edit task' row: Member = workspace-wide edit, add a footnote."
- "Make the ai-agent column visually distinct (dashed border)."

---

## Definition of Done

- [ ] Matrix matches `backend/project-kit/context/api-surface.md` + auth design
- [ ] Owner/Admin/Member/ai-agent all present
- [ ] 404-not-403 + propose-only notes included
- [ ] Approved → PNG → `project-kit/diagrams/architecture/auth-permissions-matrix.png`