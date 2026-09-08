---
name: git-branch-flow
description: "Standard feature-branch workflow for Griot. One feature spec = one branch = one PR. The progress tracker travels on the branch."
metadata:
  version: "0.1.0"
---

# Git Branch Flow

## Process (per feature spec)

1. Pull latest `main`: `git checkout main && git pull`.
2. Create branch from the current system's repo root: `git checkout -b feature/NN-slug`.
3. Read only the current numbered spec + its required context. Present a concrete plan and wait for explicit user approval.
4. Implement within spec scope. Write tests for the core logic + critical paths.
5. Run the system's verification gates (see root `AGENTS.md`).
6. Update the owning system's `project-kit/context/progress-tracker.md` **on the branch**: move current spec to Completed, clear In Progress, point Current Goal / Next Up at the next spec, add a session note.
7. Commit implementation + tracker together, push the branch.
8. Optionally run review (CodeRabbit or equivalent) and fix findings.
9. The user merges to `main` manually; sync before the next spec.

## Rules

- Never commit a progress-tracker update directly to `main`.
- Never batch specs or skip ahead.
- Root-level changes (repo layout, shared skills) use the same flow against the root tracker.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
