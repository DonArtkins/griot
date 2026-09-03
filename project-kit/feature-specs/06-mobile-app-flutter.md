# Feature 06 — Mobile App (Flutter + Riverpod + graphql_flutter)

## Type

NEW FEATURE

## What This Delivers

The Android companion app: login/signup, dashboard, boards, task detail + status picker, and notifications — consuming the same HotChocolate GraphQL endpoint and REST surface as the web app. Riverpod state, dio REST with refresh-on-401, secure refresh-token storage.

## Dependencies

- Feature 04 (backend live; the same endpoints serve mobile).
- Flutter 3.19+/Dart 3 + Android toolchain (`research/gtp-2026-prep.md` §6.7, `flutter doctor` Android row green).
- iOS out of scope (no Xcode on Parrot) — Android emulator + physical device only.

## Context To Read First

- `context/library-docs.md` (mobile section)
- `research/week-04-mobile-development.md`

## Files Owned

- `mobile/**` (own package)

## Files

CREATE: `mobile/` Flutter app — `lib/core/network/{dio_client,graphql_client}.dart`, `lib/core/storage/secure_storage.dart`.
CREATE: `lib/features/{auth,dashboard,boards,tasks,notifications}/` — screens + Riverpod providers.
CREATE: `mobile/test/` widget + integration tests; `integration_test/` for device runs.
MODIFY: `mobile/pubspec.yaml`, `analysis_options.yaml`.

## Setup / Initialization

```bash
cd <repo-root>
flutter create mobile   # or `flutter create griot_mobile` then rename folder to mobile
cd mobile
flutter pub add flutter_riverpod graphql_flutter dio flutter_secure_storage
flutter pub add --dev integration_test sdk:flutter
flutter run -d emulator-5554   # API base configured via --dart-define=API_URL=…
```

## Separation of Concerns

- `core/network/` — one dio client + interceptor (401 → refresh → retry once) and one GraphQL client pointed at the same endpoint as web.
- `core/storage/` — `flutter_secure_storage` wrapper (Android Keystore-backed) — the mobile equivalent of an httpOnly cookie.
- `features/*` — screens + providers per module, mirroring the backend module split; no cross-feature imports.
- State rule: access token in memory (Riverpod), refresh token in secure storage, silent refresh on boot. Server data refetched explicitly after mutations (graphql_flutter cache is not assumed Apollo-equivalent).

## Docker & Deploy

- **Local**: `flutter run` on the Android emulator; no container.
- **CI**: a Docker-pinned Flutter image (`ghcr.io/cirruslabs/flutter:3.19.x`) builds `flutter test` + the release APK/AAB in GitHub Actions (Feature 08).
- **Release**: APK/AAB artifact uploaded from CI; distribution to Play Console (or direct APK link) — deploy day ships a verifiable, reproducible build.

## Out of Scope

- iOS build pipeline (Xcode/macOS), drag-drop board, offline-first sync.

## Acceptance Criteria

- [ ] Login/signup works against the deployed API; silent refresh on boot
- [ ] Dashboard + boards via GraphQL; task detail + status picker + notifications
- [ ] `flutter analyze` clean; widget tests green; `integration_test` passes on emulator
- [ ] Verified on emulator + one physical Android device
- [ ] APK artifact produced by CI

## Future Modifications

- Feature 10 adds the Flutter widget/integration suites to the Week-6 gate.