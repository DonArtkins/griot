# Progress Tracker — Mobile (Flutter 3.19+ / Dart 3)

## Current State

**Phase P3** in `docs/planning/IMPLEMENTATION-ROADMAP.md`. Kit written (7 specs). **Not started.** Every mobile dependency is already-satisfied backend work (04–08, 07 auth ✅) — the system waits only for its roadmap slot after **P1 (web) + P2 (AI hop) complete**, because one spec runs at a time and web is the Week-3 deliverable.

| Spec | Title | Status | Blocked by |
|---|---|---|---|
| 01 | Flutter app setup + theme | Pending (P3 entry point) | Flutter 3.19+ toolchain |
| 02 | Login & auth screens | Pending | m 01, backend 07 ✅ |
| 03 | REST API integration (dio) | Pending | m 02 |
| 04 | GraphQL integration | Pending | m 01–02, backend 05 ✅ |
| 05 | Advanced state (Riverpod) | Pending | m 03–04 |
| 06 | Responsive cross-device UI | Pending | m 04–05 |
| 07 | Notifications + device verification | Pending | m 02–06, backend 04 ✅; CI APK artifact arrives with infra 05 (P4) |
| 08 | Organization switching & role navigation | 📋 Spec written (PLANNED) — multi-tenant wave | m 02–06, backend 29/30/32–33, web 13–16 |
| 09 | Client portal & handoff (mobile parity) | 📋 Spec written (PLANNED) — multi-tenant wave | m 02–08, backend 34/35/22, web 15/16 |

## Roadmap Order (canonical, from IMPLEMENTATION-ROADMAP.md P3)

`mobile 01 → 02 → 03 → 04 → 05 → 06 → 07`

**Why mobile after web/AI-hop:** bootcamp Week 4 follows Week 3; mobile is a companion surface (parity with web, not pixel-identical), so implementing it after the web patterns exist lets it copy proven data-layer/auth wiring instead of inventing it. **Known cross-phase item:** spec 07's "CI APK artifact" acceptance point is fulfilled by infra 05's Flutter job (P4) — emulator + physical-device verification happens in P3.

## Next Steps

1. Do not start until P2 (web 10 + ai 03–05) is complete.
2. Then branch `feature/mobile/01-flutter-app-setup` and implement spec 01 only.
3. Verification gates: `flutter analyze` clean; `flutter test` green; verified on Android emulator + one physical device.

## Session Notes

- **2026-09-03** — Mobile kit created (AGENTS, skills, contexts, 7 specs).
- **2026-09-07** — Theme scaffolded ahead of spec 01: `lib/core/theme/theme.dart` (ThemeData + `GriotColors`/`GriotRadii` extensions) from the inspo-synthesized master design system.
- **2026-09-10** — Orchestration boundary ratified: mobile never touches Trigger.dev or MCP — all data through the .NET API (`research/ai-integration.md` §2a).
- **2026-09-10 (2)** — Tracker created during the cross-system audit (this system previously had none). Phase P3 position recorded above.

- **2026-09-11** — **Multi-Tenant Migration Wave (PLANNED):** specs 01–07 each gained a "Multi-Tenant Update (2026-09-11 — PLANNED)" section (JWT v2 claims `name`/`org`/`role`/`perms`, org switch via `POST /api/auth/select-organization`, `403 org_suspended`, org-scoped notifications, client-portal layouts; refresh tokens stay opaque in `flutter_secure_storage` — NOT JWTs). New specs **08** (organization switching & role navigation) and **09** (client portal & handoff mobile parity) written as PLANNED — no production code, no status changes to existing specs. Contract: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md`; mobile multi-tenant contract mirrored in `mobile/project-kit/context/integration-contracts.md`.

## Audit synchronization — 2026-09-11

Current implementation remains backend 09 review hardening; next is backend 20 after review. Future planning is not completed implementation. P0: backend 09 → 20 → 18 → 19 → 22 → 21 → 23 → 11 → 28 → 24 → 25 → 26 → 27 → 10. P2: ai 01 → ai 02 → web 10 → ai 03 → ai 04 → ai 05 → ai 06 → ai 07 → web 11 → ai 08 → ai 09 → web 12 → ai 10 → ai 11 → ai 12. Full requirement/review ledger: `docs/planning/AI-SYSTEM-AUDIT-2026-09-11.md`.
Mobile stays at core PM parity; no dedicated AI workspace, report composer or assignment UI is added to its seven specs. Consume permitted notification/report links through .NET only.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
