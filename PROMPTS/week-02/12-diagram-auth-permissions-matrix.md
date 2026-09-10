# Week 02 · Diagram 08 — Auth/Permissions Matrix

> Backend 09 contract: Bearer `GRIOT_SERVICE_TOKEN` plus `X-On-Behalf-Of` resolves a real-user OBO principal (`ai-on-behalf-of`), never a synthetic member. Exactly four scope claims are issued: ReadWorkspace/CreateTask/AddComment/CreateNotification. AI OBO bulk status, deletes, invites and member management are denied; ActivityLog persistence is planned for backend 20. See `docs/api/ai-service-token-contract.md`. **2026-09-11 (PLANNED, spec 23):** critical actions (login 2FA, forgot/reset password, delete account, guarded ops) require a step-up email OTP — human-only; AI never appears in the step-up column. A FIFTH OBO scope `CreateReport` (spec 24) is planned for report/audit tools.

**Table + Figma Make prompts.** Rows = roles (Owner/Admin/Member); columns = actions. Catches "wait, can a Member delete someone else's task?" before it's a bug report.

---

## 1. The matrix (the contract)

| Action | Owner | Admin | Member | ai-on-behalf-of (service) |
|---|---|---|---|---|
| View workspace/board/tasks | ✅ | ✅ | ✅ | ✅ (ReadWorkspace) |
| Create project / board | ✅ | ✅ | ✅ | ❌ |
| Edit task — status/assignee (MCP-tool path) | ✅ | ✅ | ✅ (assigned OR workspace) | ✅ via existing task API and real-user RBAC; no separate status scope. Bulk status is denied to AI OBO callers. |
| Edit task — all fields (in-app Copilot path) | ✅ | ✅ | ✅ | ⚠️ **proposal-only** — agent proposes, web app executes as the authenticated user after explicit approval; agent never writes directly |
| Move task across columns | ✅ | ✅ | ✅ | ❌ (propose-only via in-app Copilot) |
| Delete task | ✅ | ✅ | ❌ | ❌ |
| Add comment | ✅ | ✅ | ✅ | ✅ (AddComment via MCP-tool path); in-app Copilot = propose-only |
| Invite members | ✅ | ✅ | ❌ | ❌ |
| Remove member | ✅ | ❌ | ❌ | ❌ |
| Change roles | ✅ | ❌ | ❌ | ❌ |
| Delete workspace | ✅ | ❌ | ❌ | ❌ |
| Manage notifications | ✅ | ✅ | ✅ | ✅ (CreateNotification via MCP-tool path) |
| View audit/error logs | ✅ | ✅ (own errors only) | ❌ | ❌ |
| Use in-app Copilot | ✅ | ✅ | ✅ | — (ai-on-behalf-of IS the Copilot — not a user of it) |
| Invoke MCP tools (external client) | ✅ (workspace scope) | ✅ (workspace scope) | ✅ (workspace scope) | ❌ (ai-on-behalf-of is the MCP executor, not a caller — it does not invoke itself) |

> **Two ai-on-behalf-of execution paths — resolved:**
> - **MCP-tool path**: an external AI client (Claude Desktop, VS Code Copilot, etc.) calls the MCP server with a user's workspace token. The MCP server calls the API using `GRIOT_SERVICE_TOKEN` + `X-On-Behalf-Of` → `ai-on-behalf-of` principal. The **authorized executor** is the MCP server acting on behalf of the external client. Routes: `PATCH /api/tasks/{id}` (status/assignee only), GraphQL `updateTask`, `addComment`.
> - **In-app Copilot path**: Trigger.dev agent (inside the app) generates a proposal. The web app renders the diff; the **user** presses "Apply" and the **web app** calls the API as the authenticated user. The agent never touches the API directly for write operations in this path.
> - **ai-on-behalf-of MCP access**: ai-on-behalf-of is the server-side executor, not a client of the MCP server. Cell = ❌ (not applicable).

Legend: ❌ = denied; AI OBO scope guards return 403 before domain lookup. Ownership/existence checks follow the domain API contract. ⚠️ = propose-only (agent cannot execute directly). Admin ≠ Owner for destructive ops. ai-on-behalf-of is restricted via `GRIOT_SERVICE_TOKEN`.

## 2. The prompt (single, extensive — no length limit)

Paste the full prompt below into Figma Make. It draws the full permission matrix in one pass.

```text
Build a permission matrix TABLE for Griot (not a diagram). 4 columns: Owner | Admin | Member | ai-on-behalf-of (service). Rows (Y = green cell, N = red cell, P = yellow "propose-only" cell):

View workspace/board/tasks — Y | Y | Y | Y (ReadWorkspace)
Create project/board — Y | Y | Y | N
Edit task status/assignee (MCP-tool path) — Y | Y | Y (assigned OR workspace) | Y via existing task API and real-user RBAC; send GRIOT_SERVICE_TOKEN + X-On-Behalf-Of. No separate status scope; bulk status is denied.
Edit task all fields (in-app Copilot path) — Y | Y | Y | P (propose-only: agent proposes diff, web app executes as the authenticated user after explicit approval — agent never writes directly)
Move task across columns — Y | Y | Y | P (propose-only via in-app Copilot)
Delete task — Y | Y | N | N
Add comment — Y | Y | Y | Y via MCP-tool path (AddComment); in-app Copilot = propose-only
Invite members — Y | Y | N | N
Remove member — Y | N | N | N
Change roles — Y | N | N | N
Delete workspace — Y | N | N | N
Manage notifications — Y | Y | Y | Y (CreateNotification via MCP-tool path)
View audit/error logs — Y | Y (own errors only) | N | N
Use in-app Copilot — Y | Y | Y | — (ai-on-behalf-of is the Copilot, not a user of it)
Invoke MCP tools (external client) — Y (workspace scope) | Y (workspace scope) | Y (workspace scope) | N (ai-on-behalf-of is the MCP executor, not a caller)

FOOTER (must be present):
"AI OBO scope guards return 403 before domain lookup; ownership/existence checks follow the domain API contract. Admin ≠ Owner for destructive ops (remove member, role changes, delete workspace, delete task). ai-on-behalf-of is the restricted service principal via GRIOT_SERVICE_TOKEN — no deletes, no invites, workspace-scoped. MCP-tool path: external client → MCP server → API (GRIOT_SERVICE_TOKEN). In-app Copilot path: proposal-only — web app executes on user approval."

Style: green Y cells, red N cells, yellow P cells (propose-only), clear column headers, ai-on-behalf-of column with dashed border, one page.
STYLE: light canvas (#F7F8FA), white boxes with 1px hairlines, token-named fills only (per docs/design/MASTER-DESIGN-SYSTEM.md), readable at 100% zoom, one page.
```

### Refine

- "Change 'Edit task' row: Member = workspace-wide edit, add a footnote."
- "Make the ai-on-behalf-of column visually distinct (dashed border)."

---

## Definition of Done

- [ ] Matrix matches `backend/project-kit/context/api-surface.md` + auth design
- [ ] Owner/Admin/Member/ai-on-behalf-of all present with split rows for MCP-tool path vs in-app Copilot path
- [ ] ai-on-behalf-of column uses dashed border; propose-only cells are yellow (P), not green
- [ ] MCP external tool access row: ai-on-behalf-of = ❌ (executor, not caller) — no conflict with other rows
- [ ] Two-path annotation present: MCP-tool path (executor route + token) vs in-app Copilot path (propose-only, user executes)
- [ ] 404-not-403 + propose-only + GRIOT_SERVICE_TOKEN scope notes in footer
- [ ] Approved → PNG → `diagrams/architecture/auth-permissions-matrix.png`
