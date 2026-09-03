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

## 2. Figma Make prompts

### PROMPT A

```
UML state machine diagram: Griot TaskItem status. States as rounded rects: Backlog, Todo, InProgress, InReview, Done, Deleted (gray, dashed border). Transitions (label each): Backlog→Todo; Backlog? none to InProgress (must go through Todo); Todo→Backlog; Todo→InProgress; InProgress→Todo (reopen); InProgress→InReview; InReview→InProgress (request changes); InReview→Done; Done→Todo (reopen, label 'product decision: yes'); any state → Deleted (soft delete). Add entry pseudo-node arrow into Backlog (initial state). Color: InProgress blue, InReview amber, Done green (matches enum severity map).
```

### PROMPT B — validation note

```
Add a note to the Griot state machine: "API validation + board UI both enforce EXACTLY this transition set — a move not in the matrix is rejected 409 by TaskService. 'InProgress→Done' is NOT allowed (must pass InReview) for v1; 'Done→Todo' reopen IS allowed. Sync: backend enum TaskStatus + web StatusChip + mobile picker use the same names."
```

### Fix snippets

- "Remove the direct InProgress→Done edge (strict)."
- "Add / enforce back-edge InReview→InProgress labeled 'request changes'."
- "Gray the Deleted state, dashed border."

---

## Definition of Done

- [ ] All 5 active states + Deleted; every legal transition drawn + labeled
- [ ] Does NOT show illegal direct transitions
- [ ] Backend TaskService + web/mobile UIs all match this matrix
- [ ] Approved → PNG → `project-kit/diagrams/architecture/task-state-machine.png`