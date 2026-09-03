# Week 02 · Diagram 06 — Task Status State Machine

**Master spec + Figma Make paste prompts.** Task status as a finite state machine. Board UI and API validation MUST agree on the same allowed transitions — decide now, draw once.

---

## 1. States & allowed transitions (the contract)

- **Backlog** → Todo · Delete(archived)
- **Todo** → Backlog · InProgress · Done(stretch) · Delete
- **InProgress** → Todo (reopen) · InReview · Done · Blocked→Todo (no Blocked state in v1 — use InReview+pinned note) 
- **InReview** → InProgress (request changes) · Done · Todo (per product decision: yes)
- **Done** → **Todo** (reopen allowed — decided YES) · Archive
- Any state → **Deleted** (soft delete: `IsDeleted` or AuditLog tombstone)

### Legal transition matrix (draw as table in the diagram)

| From \ To | Backlog | Todo | InProgress | InReview | Done | Deleted |
|---|---|---|---|---|---|---|
| Backlog | – | ✅ | ✅ | – | – | ✅ |
| Todo | ✅ | – | ✅ | – | ✅ | ✅ |
| InProgress | – | ✅ | – | ✅ | ✅ | ✅ |
| InReview | – | ✅ | ✅ | – | ✅ | ✅ |
| Done | – | ✅ | – | – | – | ✅ |

> Note: Done → Todo (reopen) is allowed (product decided). InProgress → Done is allowed only via InReview (no direct Done skip) — decided for v1 keep strict.

## 2. The prompt (single, extensive — no length limit)

Paste the full prompt below into Figma Make. It draws the entire state machine + the validation note in one pass.

```text
UML state machine diagram: Griot TaskItem status. States as rounded rects: Backlog (entry, initial state with a filled-dot arrow), Todo, InProgress, InReview, Done, Deleted (gray, dashed border).

COLORS (must match the enum severity map): InProgress = blue, InReview = amber, Done = green, Backlog/Todo = neutral, Deleted = gray.

LEGAL TRANSITIONS (draw each with a label on the edge):
- Backlog → Todo
- Backlog → Deleted
- Todo → Backlog
- Todo → InProgress
- Todo → Done
- Todo → Deleted
- InProgress → Todo (label "reopen")
- InProgress → InReview
- InProgress → Deleted
- InReview → InProgress (label "request changes")
- InReview → Done
- InReview → Deleted
- Done → Todo (label "reopen — product decision: YES")
- Any state → Deleted (soft delete / AuditLog tombstone)

DO NOT draw an InProgress → Done direct edge (strict v1: must pass through InReview).

ANNOTATION (validation note, near the matrix):
"API validation + board UI + mobile picker all enforce EXACTLY this transition set — a move not in this matrix is rejected with 409 by TaskService. 'InProgress→Done' is NOT allowed (must go via InReview). 'Done→Todo' reopen IS allowed. Backend enum TaskStatus, web StatusChip, and mobile picker use the SAME names."

Also draw a small TRANSITION MATRIX table (rows FROM, columns TO):
Backlog→Todo ✅; Todo→Backlog/InProgress/Done ✅; InProgress→Todo/InReview ✅; InReview→InProgress/Done ✅; Done→Todo ✅; all → Deleted ✅.
```

### Refine

- "Remove the direct InProgress→Done edge (strict)."
- "Gray the Deleted state, dashed border."
- "Make Done→Todo edge green-ish and labeled 'reopen allowed'."

---

## Definition of Done

- [ ] All 5 active states + Deleted; every legal transition drawn + labeled
- [ ] Does NOT show illegal direct transitions
- [ ] Backend TaskService + web/mobile UIs all match this matrix
- [ ] Approved → PNG → `project-kit/diagrams/architecture/task-state-machine.png`