# UI Registry

## Contracts Synchronization Gate

Any implementation change that corrects or changes a contract must be reflected in the same branch across the affected feature spec, dependent future specs, relevant context files, root `AGENTS.md`, and `progress-tracker.md`. Contracts include component names, their props, route names, and the entity/enum values they render.

## Purpose

The component inventory for the App shell and Public shell. Every component is first listed here, then built as an MUI-extended React component (`web/`), and mirrored as a Flutter widget where the mobile surface needs it. The list derives from the Week-1 Figma design system components (TaskCard, BoardColumn, Sidebar, Modal, NotificationItem, …).

## Conventions

- All visual components extend MUI (`Box`, `Paper`, `Card`, `ListItem`, …) — they are never built from bare divs.
- Props are typed interfaces in `web/src/features/<feature>/types.ts`; GraphQL fragments colocate with components.
- Components render statuses/priorities through the chip components below so colors follow `ui-tokens.md`.

## App Shell Components

### Layout
| Component | Source | Used by | Notes |
|---|---|---|---|
| `AppShell` | MUI stacked layout | Root `/app/*` | Left rail + header + content + Copilot rail |
| `SidebarNav` | MUI `Drawer`/`List` | App shell | Workspace switcher → projects → boards → settings |
| `TopBar` | MUI `AppBar` | App shell | Search, notifications bell, user menu |
| `CopilotPanel` | MUI `Paper` right rail | Board + Task views | Collapsible; stream + approval cards |
| `PageHeader` | MUI `Stack` | Feature pages | Title + primary/secondary actions |

### Workspace & Projects
| Component | Source | Used by | Notes |
|---|---|---|---|
| `WorkspaceCard` | MUI `Card` | Dashboard | Name, member avatars, project count |
| `ProjectCard` | MUI `Card` | Dashboard | Name/key, progress, last-activity |
| `MemberChip` / `AvatarStack` | MUI `AvatarGroup` | Everywhere | Owner/Admin/Member ring or tooltip |

### Board & Tasks
| Component | Source | Used by | Notes |
|---|---|---|---|
| `BoardColumn` | MUI `Paper` | Board | Header with count, `space.md` padding, min-width 272 px |
| `TaskCard` | MUI `Card` | Board | Title, priority chip, due date, assignee avatar, comment count |
| `TaskModal` | MUI `Dialog` | Board/Task | Full task detail: description, comments, attachments |
| `StatusChip` | MUI `Chip` | TaskCard/TaskModal | Colors from `ui-tokens.md` severity map |
| `PriorityChip` | MUI `Chip` | TaskCard/TaskModal | Severity map |
| `CommentThread` | MUI `List` | TaskModal | Sorted asc; optimistic add |
| `AttachmentList` | MUI `List` | TaskModal | File name, size, download |
| `ColumnMenu` | MUI `Menu` | BoardColumn | Add task, reorder, column settings |

### Notifications & Feed
| Component | Source | Used by | Notes |
|---|---|---|---|
| `NotificationItem` | MUI `ListItem` | Notifications page + bell | Type icon, text, read/unread dot |
| `NotificationBell` | MUI `Badge` + `Popover` | TopBar | Unread count; mark-all-read |
| `ActivityFeedItem` | MUI `ListItem` | Dashboard feed | `actor → action → entity`, relative time |

### Forms & Overlays
| Component | Source | Used by | Notes |
|---|---|---|---|
| `WorkspaceForm`, `ProjectForm`, `TaskForm`, `InviteForm` | MUI `Dialog` + `TextField` + `Select` | Respective modals | Zod-validated |
| `ConfirmDialog` | MUI `Dialog` | Destructive actions | Danger-flagged primary |
| `EmptyState`, `LoadingSkeleton`, `ErrorState` | MUI `Stack`/`Skeleton` | Every list/board | Designed assets per `ui-rules.md` |
| `Toast` | MUI `Snackbar`/`Alert` | App actions | `motion.fast`, top-right stack |

## Public Shell Components

| Component | Source | Used by | Notes |
|---|---|---|---|
| `MarketingNav` | MUI `AppBar` | `/`, `/pricing` | Sticky minimal |
| `HeroSection` | MUI `Container` | `/` | Real product UI in hero; GSAP reveal |
| `FeatureGrid` | MUI `Grid` | `/` | Bento-style capabilities |
| `PricingCard` | MUI `Card` | `/pricing` | 3 tiers, featured tier highlight |
| `AuthShell` | MUI `Container` | `/login`, `/signup` | Split layout; form card |
| `LoginForm`, `SignupForm` | MUI `TextField` + `Button` | Auth routes | Zod + inline errors |

## Flutter Mirror (mobile/)

Only what the mobile surface actually renders: `BoardColumn` → `ColumnHeader + TaskListTile` (with status picker instead of drag), `TaskDetailScreen`, `CommentTile`, `NotificationTile`, `LoginScreen`/`SignupScreen`, `DashboardScreen`. Widgets live in `mobile/lib/features/<feature>/widgets/`.

## When to Add a Component

- New component → add to this registry, build it against tokens, write its interaction test, then use it in pages.
- A component reused in two+ places must be in this registry (never duplicated inline).
- Removing/renaming a component here triggers the contracts sync gate (feature specs + progress-tracker).

## Component Quality Bar

1. Typed props; no `any`.
2. Uses tokens (no raw colors/spacing).
3. Has empty/loading/error handling when it renders async data.
4. Has a Jest+RTL test when it has interaction (click, drag, enter).
5. Accessible: labels, roles, focusable, `min-touch`.