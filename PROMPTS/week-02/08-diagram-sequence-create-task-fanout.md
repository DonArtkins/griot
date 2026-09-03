# Week 02 · Diagram 05B — Sequence: Create Task → Notification Fan-out

**Master spec + Figma Make paste prompts.** The other critical flow: **create task → ActivityLog write → Notification write → delivery to the assignee** (poll/websocket/next-load decision drawn here, not coded blind).

---

## 1. Lifelines

1. **Client** (web board or mobile)
2. **TaskController / GriotMutation** (REST or GraphQL — same service)
3. **TaskService** (Griot.Application)
4. **SQL Server** (TaskItems, ActivityLogs, Notifications, AuditLogs)
5. **NotificationHub / client subscriber** (web realtime)

## 2. The flow (happy)

```
Client → TaskController/GriotMutation: createTask { title, columnId, assigneeId?, priority?, dueDate? }
→ TaskService: CreateTaskAsync(dto)
TaskService → SQL Server: BEGIN TX
TaskService → SQL Server: INSERT TaskItems (Position = max + 1 in column)
TaskService → SQL Server: INSERT ActivityLogs (actor, action=Created, entity=Task, payload)
TaskService → SQL Server: INSERT Notifications (assignee if assigned; type=Assignment, targetRef=task)
TaskService → SQL Server: INSERT AuditLogs (before=null, after=snapshot)
TaskService → SQL Server: COMMIT TX
TaskService → NotificationHub: PushNotification(userId=assignee, payload)
TaskService → Client: 201 { task }
NotificationHub → assignee client (if subscribed): realtime event
Assignee client (if not subscribed): next board load / notification poll fetches it
```

## 3. The fan-out decision (draw as a note)

- **Web (app shell)**: NotificationHub over the Trigger realtime/websocket channel — instant badge.
- **Mobile**: notification list refreshes on focus + pull-to-refresh; no always-on websocket in v1 (battery).
- **Fallback**: next board load pulls unread counts regardless. No email push in v1 (that's the AI scheduled digest).

## 4. Figma Make prompts (≤2000 chars)

### PROMPT A

```
UML sequence diagram: Griot create-task fan-out. Lifelines L→R: Client (web/mobile), TaskController/GriotMutation, TaskService (Griot.Application), SQL Server (TaskItems+ActivityLogs+Notifications+AuditLogs), NotificationHub (web realtime).
Flow: Client→Controller createTask {title,columnId,assigneeId?,priority?,dueDate?}; Controller→TaskService CreateTaskAsync; TaskService→SQL Server BEGIN TX; INSERT TaskItems (Position=max+1); INSERT ActivityLogs (Created, payload); INSERT Notifications (Assignment, targetRef taskId) if assignee; INSERT AuditLogs (before null, after snapshot); COMMIT; TaskService→NotificationHub PushNotification(assignee); Controller→Client 201 {task}; NotificationHub→assignee client realtime event (if subscribed); note: mobile + unsubscribed clients see it on next load/poll.
Show the TX boundary spanning the 4 INSERTs with a labeled bracket 'transaction (atomic)'.
```

### PROMPT B — fan-out decision note

```
Add an annotation box to the Griot create-task sequence: "Fan-out decision: web app shell = realtime push over NotificationHub; mobile = refresh on focus + pull-to-refresh (no always-on socket, battery); fallback = next board load pulls unread counts; no email in v1 (AI digest does that)." Connect it to the NotificationHub lifeline with a dashed note line.
```

### Fix snippets

- "Move the transaction bracket to cover exactly the 5 DB inserts + commit."
- "Add a parallel alt: subscribed vs not-subscribed delivery."

---

## Definition of Done

- [ ] Full create-task flow with atomic TX boundary
- [ ] ActivityLog + Notification + AuditLog inserts drawn
- [ ] Fan-out decision (realtime vs poll vs next-load) annotated
- [ ] Approved → PNG → `project-kit/diagrams/architecture/sequence-create-task-fanout.png`