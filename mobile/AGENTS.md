# AGENTS.md — Griot Mobile (Flutter 3.19+ / Dart 3)

## Read This First

You are the agent for the **Mobile** system of Griot (bootcamp Week 4). Flutter + Riverpod + graphql_flutter + dio, consuming the SAME backend endpoints as the web app — the mobile surface is a companion, not a separate product.

Stack (exact): Flutter 3.19+ / Dart 3, GraphQL Flutter. Riverpod chosen for state (guide allows Provider/Riverpod). [own-stack]: dio for REST, flutter_secure_storage for refresh tokens.

**Android-only on Parrot:** iOS needs Xcode/macOS — out of scope. `flutter doctor` shows Android green / iOS red.

## Folder shape

```
mobile/
├── pubspec.yaml
└── lib/
    ├── main.dart
    ├── core/
    │   ├── network/    # dio client + interceptors, graphql_client
    │   └── storage/    # flutter_secure_storage wrapper
    └── features/
        ├── auth/         # screens + providers
        ├── dashboard/
        ├── boards/       # board list + tasks
        ├── tasks/        # task detail + status picker
        └── notifications/
```

## Reading Order

1. Root `AGENTS.md` + root `integration-contracts.md` (API surface).
2. `research/week-04-mobile-development.md`.
3. `mobile/project-kit/context/{architecture,state-and-data,api-integration,code-standards}.md`.
4. Current spec (numeric order).

## Required Skills

Root shared skills + `mobile/.agents/skills/` (`flutter-setup`, `riverpod-state`, `graphql-flutter`, `dio-rest`).

## Where This System Sits in the Build Order (canonical: `docs/planning/IMPLEMENTATION-ROADMAP.md`)

**Phase P3 (Week 4 system)** — starts after P1 (web 01–09) + P2 (AI hop) complete; all backend deps (04–08, 07 auth) are already ✅. Own order: **01 → 02 → 03 → 04 → 05 → 06 → 07**. Known cross-phase item: spec 07's CI APK artifact is delivered by infra 05 (P4); emulator + physical-device verification happens here. Entry branch: `feature/mobile/01-flutter-app-setup`. Track state in `mobile/project-kit/context/progress-tracker.md`.

## Verification Gates

- `flutter analyze` clean; `flutter test` green; `integration_test` passes on emulator.
- Verified on emulator + one physical device.
- APK/AAB artifact produced by CI (infra/qa).

## Hard Rules

1. Status change = picker, never drag-drop.
2. Access token in memory; refresh in `flutter_secure_storage`; silent refresh on boot.
3. Parity = feature-complete, not pixel-identical, to web.
4. Mobile never touches Trigger.dev or the MCP server — any future AI/Copilot surface consumes the .NET API exclusively (single API surface; contract: `research/ai-integration.md` §2a).

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**

## Implemented authentication contract (Feature 07)

Use the [auth contract](../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

Before committing or pushing implementation, run `python3 scripts/check-contract-sync.py` from
the repository root. Synchronize the owning spec, dependent specs, planning,
research, docs, contexts, agent instructions, diagram sources and progress notes
in the feature branch. Planned behavior must be labeled and must not count as
implemented acceptance evidence. Run the system verification gates as well.

## Audit synchronization — 2026-09-11

Current implementation remains backend 09 review hardening; next is backend 20 after review. Future planning is not completed implementation. P0: backend 09 → 20 → 18 → 19 → 22 → 21 → 23 → 11 → 28 → 24 → 25 → 26 → 27 → 10. P2: ai 01 → ai 02 → web 10 → ai 03 → ai 04 → ai 05 → ai 06 → ai 07 → web 11 → ai 08 → ai 09 → web 12 → ai 10 → ai 11 → ai 12. Full requirement/review ledger: `docs/planning/AI-SYSTEM-AUDIT-2026-09-11.md`.
Mobile stays at core PM parity; no dedicated AI workspace, report composer or assignment UI is added to its seven specs. Consume permitted notification/report links through .NET only.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
