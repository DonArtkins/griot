# UI Rules

## Contracts Synchronization Gate

Any implementation change that corrects or changes a contract must be reflected in the same branch across the affected feature spec, dependent future specs, relevant context files, root `AGENTS.md`, and `progress-tracker.md`. Contracts include component names (see `ui-registry.md`), route names, design tokens, and the enum values annotated in Figma.

## Interaction Rules

1. **Two shells, two budgets.** Public shell = spectacle + conversion (GSAP/Lenis, soft shadows, big display type). App shell = speed + clarity (MUI transitions only, `motion.fast`, no decorative animation, no parallax). Never import GSAP into the App shell.
2. **Board drag-drop is web-only.** Move tasks by drag on web with optimistic Apollo cache updates and REST persistence. Mobile uses a status picker — the mobile-idiomatic pattern. No drag-and-drop libraries in the Flutter app.
3. **Propose-before-write for AI.** The Copilot panel streams answers; mutations (create task, change status) return as a tappable approval card. The user approves, then the **app** calls the API itself. The agent never writes directly.
4. **Status colors are the enum.** `TaskStatus` / `Priority` chips use the severity map in `ui-tokens.md`. A status chip color must never diverge from the enum annotation.
5. **Empty, loading, and error states are designed assets.** Every list/board/dashboard defines: skeleton loader, empty illustration with a one-line CTA, and an error state with retry. Never render a bare grey rectangle.
6. **Threads and overlays:** modals use `shadow.2`, dim the backdrop, trap focus, close on Escape + backdrop click. Notifications/toasts stack in the top-right at `motion.fast`.
7. **Accessibility is a gate:** 44×44px min touch targets (`min-touch`), visible focus rings, WCAG AA contrast on all text, axe-clean on the Public shell. GSAP-animated content must not rely on animation to convey meaning.

## Layout Rules

### App shell
- Fixed left rail: workspace switcher → project nav → board list → settings (per `ui-registry.md` `SidebarNav`).
- Content region: page header (title + primary actions) above the surface; boards scroll horizontally within their container; the page body scrolls vertically.
- Copilot panel: collapsible right rail (320 px), never pushes the board out of view unless docked by the user.
- Responsive: 1-col mobile, 2-col tablet, 3-col desktop for card grids (dashboard); board columns stay ≥272 px and scroll horizontally on narrow screens.

### Public shell
- Hero: real product UI (the App shell dashboard) visible above the fold — "show the product, don't describe it."
- Trust signals at decision points (pricing CTA, signup): partner/testimonial/social-proof row.
- Sticky, minimal nav: logo → features → pricing → Sign in (secondary) → Get started (primary).

## State Rules

- **Server data lives in query caches**: Apollo `InMemoryCache` for GraphQL reads; TanStack Query for REST reads/mutations. Feature hooks select from those caches.
- **Zustand holds client-only state only**: filters, modal open/close, drag state, `accessToken` in memory. No server data mirrors into Zustand — a mirrored entity is a contract violation.
- **Optimistic updates** for: column move, status change (web), task create. Rollback on error with a toast, then refetch.
- **Auth refresh flow**: on 401 → call `/auth/refresh` once via the httpOnly-cookie refresh token → retry the original request once. If refresh fails → clear session → redirect to `/login`. No infinite retry loops.

## Component Interaction Standards

1. Use the components in `ui-registry.md` — no ad-hoc re-implementation of `TaskCard` or `BoardColumn`.
2. Component props are typed interfaces exported from `web/src/features/<feature>/types.ts`; GraphQL fragments live next to the component that owns them (Apollo colocation).
3. Each interactive component (TaskCard actions, column menu, notification item) is unit-tested with Jest + RTL: assert behavior, not implementation.
4. Motion hooks: `useGSAP()` (StrictMode-safe) for Public shell; MUI `TransitionGroup`/`Collapse` for App shell lists.
5. Forms: MUI `TextField`/`Select` with Zod-validated schemas; inline server errors render under the field.

## Flutter (mobile) Parity

- The same screens render from the same backend: dashboard/boards/tasks/notifications. Parity means feature-complete, not pixel-identical.
- Status changes = picker; comments = threaded list; notifications = same read/unread model.
- Riverpod providers mirror the Zustand split: server state from `graphql_flutter` cache refetched explicitly; client state (filter, panel visibility) in providers.

## Acceptance Baseline

Every UI feature is done only when: it uses `ui-tokens` + `ui-registry` components, has empty/loading/error states, passes axe where applicable, passes Lighthouse budget (LCP < 2.5 s, CLS < 0.1), and its interaction tests are green.