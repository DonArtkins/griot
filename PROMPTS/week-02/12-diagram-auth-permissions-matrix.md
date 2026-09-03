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

## 2. Figma Make prompt

```
Build a permission matrix TABLE for Griot (not a diagram). 4 columns: Owner, Admin, Member, ai-agent. Rows:
View workspace/board/tasks: Y Y Y Y(ReadWorkspace)
Create project/board: Y Y Y N
Edit task: Y Y Y(assigned or workspace) Y(CreateTask/UpdateStatus)
Move task across columns: Y Y Y N(propose-only via Copilot)
Delete task: Y Y N N
Add comment: Y Y Y Y(AddComment)
Invite members: Y Y N N
Remove member: Y N N N
Change roles: Y N N N
Delete workspace: Y N N N
Manage notifications: Y Y Y Y(CreateNotification)
View audit/error logs: Y Y(own) N N
Use Copilot: Y Y Y —
MCP external access: Y Y Y N
Style: green Y cells, red N cells; add footer "denied = 404 not 403 (never disclose existence); Admin != Owner for destructive ops". Keep one page.
```

### Fix snippets

- "Change 'Edit task' row: Member = workspace-wide edit allowed, mark with a footnote."
- "Add a third column unicode for ai-agent service scope."

---

## Definition of Done

- [ ] Matrix matches `backend/project-kit/context/api-surface.md` + auth design
- [ ] Owner/Admin/Member/ai-agent all present
- [ ] 404-not-403 + propose-only notes included
- [ ] Approved → PNG → `project-kit/diagrams/architecture/auth-permissions-matrix.png`