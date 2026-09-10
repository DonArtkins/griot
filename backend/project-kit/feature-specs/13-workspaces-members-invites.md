# Backend Feature Spec 13 — Workspaces, Members & Invites

## Goal
Implement the workspaces domain end-to-end (list/create/get/update/delete, membership
management, invite-by-email + token accept). Replaces the 501 scaffold with real,
auth-scoped persistence backed by EF Core via `IGenericRepository<T>`.

## Dependencies
- Spec 07 (auth — JWT `sub` principal)
- Spec 02 (schema: Workspaces, WorkspaceMembers, Invites)

## Routes
- `GET /api/workspaces` · `POST /api/workspaces` · `GET|PUT|DELETE /api/workspaces/{id}`
- `GET|POST /api/workspaces/{id}/members` · `PATCH|DELETE /api/workspaces/{id}/members/{userId}`
- `POST /api/workspaces/{id}/invites` · `GET /api/invites/{token}` · `POST /api/invites/{token}/accept`

## Role model
- Owner: delete workspace, remove members, manage all
- Owner + Admin (manage): add member, update role, invite
- Member: read + update workspace

## Implementation
- `IDomainService` + `DomainService` (Workspace/WorkspaceMember/Invite repos)
- `DomainControllerBase` (claim `sub` → Guid; `DomainError` → 400/401/403/404/409)
- `WorkspaceController`, `InviteController` (both delegate to `DomainService`)

## Acceptance (implemented)
- [x] Workspace CRUD + membership scoped by role (401/403/404 paths tested)
- [x] Invite created with token; accept verifies email + marks Accepted
- [x] No `Not implemented yet` remains in workspace/invite controllers
