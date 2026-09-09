# Backend Feature Spec 14 — Projects, Boards & Columns

## Goal
Implement the projects → boards → columns hierarchy (list/create/get/update/delete),
scoped to workspace membership, replacing the 501 scaffold.

## Dependencies
- Spec 13 (workspaces) — every action requires workspace membership

## Routes
- `GET|POST /api/workspaces/{id}/projects` · `GET|PUT|DELETE /api/projects/{id}`
- `GET|POST /api/projects/{id}/boards` · `GET /api/boards/{id}`
- `POST /api/boards/{id}/columns` · `PATCH|DELETE /api/columns/{id}`

## Implementation
- `DomainService` (Project/Board/Column repos), `ProjectController`, `BoardController`, `ColumnController`
- Membership check through `ScopedWorkspaceAsync`; Owner/Admin gating on create/delete
- `Order` auto-increments via `ComputeMaxInt` when not supplied

## Acceptance (implemented)
- [x] Projects listed/created under a workspace the caller belongs to
- [x] Boards/columns create with ordering; structural delete cascades via EF
- [x] No 501 across projects/boards/columns
