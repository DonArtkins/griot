# PROMPTS — Week-Grouped Generation Prompts

Everything you paste into an AI tool so it does a week's deliverable for you: Figma Make (UI/design), Figma Make (diagrams, ERD), Figma AI, and agent prompts (Cline/Claude) that turn a design into code.

## How to use

1. Open the folder for the week you're working on (`week-01/` … `week-07/`).
2. Each file is self-contained: **Context** (what we're building), **Prompt** (paste-ready), **Refinement** (how to iterate), **Done** (what "finished" looks like), and where the output lands in `diagrams/`.
3. Prompts reference the research files and the project-kit context — read those first so the tool gets coherent input.
4. After generation, export/archive outputs into `diagrams/**` so the code agents use the same artifacts.

## Index

| Week | File | Tool | Output |
|---|---|---|---|
| 1 | `week-01/01-figma-make-app-shell.md` | Figma Make (Plan mode) | 5 core App-shell screens → Figma design system |
| 1 | `week-01/02-figma-make-public-shell.md` | Figma Make (single-shot) | Landing / pricing / login → Public shell |
| 2 | `week-02/01-database-schema-erd-figma-make.md` | Figma Make | ERD master spec + full walkthrough |
| 2 | `week-02/02-erd-figma-make-master-prompt.md` | **Figma Make (today)** | **Single extensive master prompt → full 16-table ERD** |
| 2 | `week-02/03-rest-api-surface.md` | Agent (Cline/Claude) | REST + GraphQL endpoint map from the approved ERD |
| 3 | `week-03/01-web-app-implementation.md` | Agent | Vite + React + MUI app from the UI registry |
| 4 | `week-04/01-mobile-app-implementation.md` | Agent | Flutter app from the mobile mirror |
| 5 | `week-05/01-docker-compose-prod.md` | Agent | Dockerfile/compose/host runbooks |
| 6 | `week-06/01-test-harness-setup.md` | Agent | Test suites + CI gate wiring |
| 7 | `week-07/01-final-qe-and-reporting.md` | Agent + AI tools | Manual/UAT/exec summary artifacts |

> Rule: a prompt is only written **after** its research file says the week is ready. If a file is missing for a week, the week's research (in `research/`) is the draft.

## Naming convention

`<week-NN>/<NN>-<short-slug>-<tool>.md` — numbered in the order the week does the work.

---

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**