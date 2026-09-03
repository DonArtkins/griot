# Docs — Project Griot (production & planning)

Everything a reviewer, an agent, or a future maintainer needs that isn't in the per-system `project-kit`s. The root `docs/` is the **overall system** documentation; each system's kit covers that system.

## Index

### Production / app-level files
| File | Purpose |
|---|---|
| `README.md` (root) | Project landing page |
| `LICENSE` | MIT (see `docs/decision-record-license.md`) |
| `CONTRIBUTING.md` | Contribution + branch rules |
| `SECURITY.md` | Vulnerability reporting + posture |
| `CODE_OF_CONDUCT.md` | Community guidelines |
| `CHANGELOG.md` | Semantic changelog |

### Architecture & planning
| File | Purpose |
|---|---|
| `docs/ARCHITECTURE.md` | **The** overall architecture doc (systems, flows, scaling, security, operations) |
| `docs/architecture/system-flow.md` | The end-to-end request flow (web→api→db; ai→api; mcp→api) |
| `docs/database/DATABASE-DESIGN.md` | Full DB schema (16 tables / 5 enums) + conventions + retention |
| `docs/decisions/` | ADRs — one per major decision (template + 001, 002…) |
| `docs/planning/NFR.md` | Non-functional requirements (load, latency, availability, retention) |
| `docs/planning/CAPACITY-PLAN.md` | Capacity + scaling ("how many users + why + how to improve") |
| `docs/planning/RISK-REGISTER.md` | Failure modes → blast radius → mitigation → detection |
| `docs/planning/RUNBOOK-ROLLBACK.md` | One-page deploy/rollback + monitoring |
| `docs/planning/CHANGE-MANAGEMENT.md` | Every error/fix tested + documented; no silent drift |

### SEO & deployment
| File | Purpose |
|---|---|
| `docs/seo/SEO-DEPLOYMENT.md` | Google registration, sitemap, robots, llms.txt, OG, Lighthouse |
| `docs/deployment/DEPLOYMENT.md` | Per-app deploy runbooks (Vercel/Railway/Trigger/APK) |
| `docs/observability/MONITORING.md` | Health, uptime, logs, alerts, error-tracking |

### API
| File | Purpose |
|---|---|
| `docs/api/README.md` | Postman-first API docs (collection = source of truth) |

## Conventions

- **Docs are contracts.** When a spec/code change happens, update the relevant doc in the same branch (contract-sync gate).
- **ADRs** live in `docs/decisions/`, numbered `ADR-001-…` onward.
- The root `docs/` links to system kits; a system's kit does not duplicate root docs.
- **inspo/**: reference UI screenshots (web + mobile build's visual contract).

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**