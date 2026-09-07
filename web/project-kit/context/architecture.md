# Web Architecture

## Two shells (layout from the Week-1 design conversation; visual contract now `docs/design/MASTER-DESIGN-SYSTEM.md` + `project-kit/context/ui-tokens.md`)

- **Public**: `/`, `/pricing`, `/login`, `/signup`. React Router `public.tsx`; GSAP/Lenis lazy-loaded. Real product UI in the hero; trust signals at decision points.
- **App**: `/app/*` behind `RequireAuth`. `protected.tsx`. Sidebar nav (workspace → projects → boards → settings), top bar (search, notifications bell, user menu), collapsible Copilot rail (320px).

## Folder shape

```
web/src/
├── main.tsx            # Providers: Theme, Apollo, QueryClient, Router, (RealtimeProvider for Copilot)
├── theme.ts
├── lib/                # apolloClient, apiClient (Axios), queryClient
├── stores/             # authStore (Zustand)
├── routes/             # public.tsx, protected.tsx, RequireAuth.tsx
└── features/
    ├── dashboard/      # page + WorkspaceCard/ProjectCard + ActivityFeed widgets
    ├── board/          # BoardColumn, TaskCard, TaskModal, drag-drop
    ├── taskDetail/     # TaskModal deep-dive, CommentThread, AttachmentList
    ├── settings/       # TeamSettings, InviteForm, MemberList
    ├── notifications/  # NotificationBell, NotificationsPage
    └── copilot/        # CopilotPanel (stream + approval cards)
```

## Rules

- Routes: Public lazy imports; App behind auth guard.
- Layout: fixed left rail + header; only content scrolls (came from Week-1 Figma conversation).
- Responsive: 1/2/3-col grids for dashboards; board columns scroll horizontally on narrow screens.
