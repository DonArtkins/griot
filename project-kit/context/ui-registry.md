# UI Registry (shared component inventory)

Every visual component is listed here first, then built in `web/` (MUI), mirrored in `mobile/` (Flutter) where the mobile surface needs it, and registered before use. Derived from the Week-1 Figma components.

## App shell

Layout: `AppShell`, `SidebarNav`, `TopBar`, `CopilotPanel`, `PageHeader`. Workspace/projects: `WorkspaceCard`, `ProjectCard`, `MemberChip`/`AvatarStack`. Board & tasks: `BoardColumn`, `TaskCard`, `TaskModal`, `StatusChip`, `PriorityChip`, `CommentThread`, `AttachmentList`, `ColumnMenu`. Notifications & feed: `NotificationItem`, `NotificationBell`, `ActivityFeedItem`. Forms & overlays: `WorkspaceForm`, `ProjectForm`, `TaskForm`, `InviteForm`, `ConfirmDialog`, `EmptyState`, `LoadingSkeleton`, `ErrorState`, `Toast`.

## Public shell

`MarketingNav`, `HeroSection`, `FeatureGrid`, `PricingCard`, `AuthShell`, `LoginForm`, `SignupForm`.

## Flutter mirror (mobile)

`ColumnHeader` + `TaskListTile` (status picker), `TaskDetailScreen`, `CommentTile`, `NotificationTile`, `LoginScreen`/`SignupScreen`, `DashboardScreen`.

## Rules

- New component -> register here -> build against tokens -> interaction test -> use in pages.
- Reused components must be in this registry; no duplicate inlining.
- Removing/renaming a component triggers the contract-sync gate.
- Quality bar: typed props, tokens only, async states, Jest/RTL (or Flutter widget) test when interactive, accessible (labels/roles/focus/touch targets).

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
