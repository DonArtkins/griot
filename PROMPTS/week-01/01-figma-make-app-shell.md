# Week 01 · Prompt 01 — Figma Make: App Shell (5 Core Screens) — EXTENSIVE MASTER

**Tool:** Figma Make → **Plan mode**. **When:** Week 1, before any code.
**Research:** `research/week-01-fundamentals-and-system-design.md` §4.2–4.4.

> **No character limit.** This is the single authoritative prompt for the App Shell. Paste it whole; it deliberately enumerates every screen, component, state, and rule so the result needs only light refinement.

---

## 1. Master spec (what this must capture)

### Global system (applies to ALL 5 screens)
- **Brand**: Griot — dark, high-contrast, calm workspace. Saturated accents ONLY for priority/status.
- **Persistent chrome** (identical on every screen):
  - **Left sidebar (rail)**: Griot logo + workspace name/avatar at top; nav Dashboard · Boards · Projects · Team · Notifications; workspace switcher; **Invite** button pinned bottom.
  - **Top bar**: breadcrumb (Workspace / Project / Board), global search with ⌘K hint, notification bell with unread badge, user avatar menu (Profile, Settings, Sign out).
- **Component language** (design once, reuse everywhere): TaskCard, BoardColumn, StatusChip, PriorityChip, MemberChip/AvatarStack, Modal, EmptyState, LoadingSkeleton, Toast.
- **Status color contract** = the enum map: InProgress=blue, InReview=amber, Done=green, Backlog/Todo=neutral. Priority: Low=info, Medium=warning, High=danger-orange, Urgent=danger-red. These MUST match the EF Core enums later (no re-skin).
- Every async surface ships **EmptyState + LoadingSkeleton + ErrorState-with-retry**.

### Screen 1 — Dashboard
- Greeting header: workspace name, member avatar stack, **New Project** primary CTA.
- **Project cards grid** (3-col / 2-col / 1-col responsive): project name + key, progress bar (Done/total), near-deadline pulse, last-activity line, contributor avatars.
- **Right column: Recent activity feed** — `actor → action → entity` lines with relative time + inline status/priority chips.
- **Notifications summary card** — latest 3 unread, "See all".
- Empty: no projects → illustration "Create your first project".

### Screen 2 — Kanban Board
- Header: board title, filter bar (status/priority/assignee chips), **Add Task**.
- **4 columns** (Backlog, In Progress, In Review, Done): colored header accent, title + count badge, scrollable TaskCards, "+ Add task" ghost at column bottom, WIP-limit note.
- **TaskCard**: title (2-line clamp), PriorityChip, due date (red if overdue), assignee avatar, comment-count icon, subtle hover lift.
- Empty column: dashed "Drop tasks here" zone. Loading: skeleton cards.

### Screen 3 — Task Detail (modal over Board)
- Left/main: inline-editable title, rich description, comment thread (avatar, name, time, body, composer), attachments (file chips + upload dropzone).
- Right rail **Properties**: StatusChip dropdown (ONLY legal transitions), PriorityChip dropdown, Assignee search, Due-date picker with overdue alert, Creator + created time, task ref id.
- Footer: Delete (confirm dialog), Copy link, Close. Focus-trapped + keyboard navigable.

### Screen 4 — Team Settings
- Tabs: Members · Invites · General.
- **Members**: avatar, name, email, role badge (Owner/Admin/Member), joined date, row actions (Change role / Remove) permission-gated.
- **Invites**: multi-email composer + role selector + Send; pending-invite rows with status chips (Pending/Accepted/Declined/Expired), resend/cancel.
- **General**: workspace name/slug/avatar edit + danger zone (Delete workspace with type-to-confirm).

### Screen 5 — Notifications
- Header + "Mark all read". Group by Today / This week / Earlier.
- **NotificationItem**: type icon (mention/assignment/due-date/system), unread dot, title, body snippet, relative time, deep-link, hover "Mark read".
- Filter chips: All · Mentions · Assignments · Due dates. Empty: "You're all caught up".

### Cross-cutting
- Empty/loading/error states = designed assets, never grey rectangles.
- A11y: 44px min touch targets, visible focus, WCAG AA, keyboard-navigable board + modal.
- Tokens: all colors/spacing/radius/type from `project-kit/context/ui-tokens.md` naming — no ad-hoc values.

---

## 2. THE PROMPT — paste into Figma Make (no length limit)

```text
Design the complete **App Shell** for **Griot**, a project-management web app for small teams. Dark, high-contrast, calm workspace; saturated accents ONLY for priority and status. Produce exactly 5 screens with identical chrome. Use Plan mode and iterate until all 5 match.

GLOBAL CHROME (identical on all screens):
- Left sidebar rail: Griot logo + workspace name/avatar; nav Dashboard, Boards, Projects, Team, Notifications; workspace switcher; Invite button pinned bottom.
- Top bar: breadcrumb (Workspace / Project / Board), centered global search with ⌘K hint, notification bell with unread badge, user avatar menu (Profile, Settings, Sign out).
- Component language reused everywhere: TaskCard, BoardColumn, StatusChip, PriorityChip, MemberChip/AvatarStack, Modal, EmptyState, LoadingSkeleton, Toast.
- Status color contract (MUST match the backend enum): InProgress = blue, InReview = amber, Done = green, Backlog/Todo = neutral. Priority: Low = info, Medium = warning, High = danger-orange, Urgent = danger-red.
- Every async surface ships EmptyState + LoadingSkeleton + ErrorState-with-retry.

SCREEN 1 — DASHBOARD:
Greeting header (workspace name, member avatar stack, New Project primary button). Project cards grid (3-col desktop / 2-col tablet / 1-col mobile): project name + key, progress bar (Done/total), near-deadline pulse, last-activity line, contributor avatars. Right column: Recent activity feed (actor → action → entity, relative time, inline chips) + Notifications summary card (latest 3 unread, See all). Empty state when no projects: centered illustration Create your first project.

SCREEN 2 — KANBAN BOARD:
Header with board title, filter chips (status/priority/assignee), Add Task. Exactly 4 columns: Backlog, In Progress, In Review, Done — each with colored header accent, title + count badge, scrollable TaskCards, a + Add task ghost button at the column bottom, and a WIP-limit note. TaskCard = title (2-line clamp), PriorityChip, due date (red if overdue), assignee avatar, comment count, subtle hover lift. Empty column = dashed Drop tasks here zone. Loading = skeleton cards.

SCREEN 3 — TASK DETAIL (modal over the board):
Left/main: title (inline editable), rich description (paragraphs + task list), comment thread (avatar, name, time, body + Add comment composer), attachments (file chips with name/size/type + upload dropzone). Right rail Properties: StatusChip dropdown (ONLY legal transitions), PriorityChip dropdown, Assignee search, Due-date picker with overdue alert, Creator + created time, task reference id. Footer: Delete (confirm dialog), Copy link, Close. Keyboard navigable, focus-trapped.

SCREEN 4 — TEAM SETTINGS:
Tabs Members · Invites · General. Members table: avatar, name, email, role badge (Owner/Admin/Member), joined date, row actions (Change role / Remove) gated by permission. Invite-by-email composer (multi-email, role selector, Send invites) + pending invites with status chips (Pending/Accepted/Declined/Expired) and resend/cancel. General: workspace name/slug/avatar edit + danger zone (Delete workspace with type-to-confirm).

SCREEN 5 — NOTIFICATIONS:
Header + Mark all read. Group list Today / This week / Earlier. Each item: type icon (mention/assignment/due-date/system), unread dot, title, body snippet, relative time, deep-link, hover Mark read. Filter chips All · Mentions · Assignments · Due dates. Empty state You're all caught up.

CROSS-CUTTING:
- Empty/loading/error states are designed assets everywhere (never grey rectangles).
- Accessibility: 44px min touch targets, visible focus rings, WCAG AA, keyboard-navigable board + modal.
- All colors/spacing/radius/typography use token-style naming — no raw ad-hoc values.
- Do NOT include: timelines/Gantt, custom fields, guest access, automations, multi-board reports, or mobile layouts.
```

---

## 3. Refine (2–3 targeted rounds)

- "Make all 5 screens share the exact same sidebar + top bar (align them)."
- "Recolor In Progress chips → blue; In Review → amber; Done → green; keep everywhere consistent."
- "Board: make TaskCards identical width/height rhythm across columns."
- "Task modal: keep the properties rail fixed width (~260px); let description/comments flex."

## 4. Done → handoff

- [ ] 5 screens + all cross-cutting states rendered in one Figma Make file
- [ ] Enum color map (InProgress blue / InReview amber / Done green) matches `ui-tokens.md`
- [ ] Empty/loading/error states present on dashboard + board
- [ ] Export screens → turn TaskCard, BoardColumn, Sidebar, Modal, NotificationItem into Figma **components**
- [ ] Extract tokens per `project-kit/context/ui-tokens.md`
- [ ] Complete the screen→entity table in `research/week-01` §4.4 (Week-2 ERD input)
- [ ] Archive screenshots → `project-kit/diagrams/ui/`
