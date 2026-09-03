# Week 4 — Mobile Development (Flutter 3.19+ & Dart 3)

> Guide's official Week 4 goals: login & auth screens; REST API integration; **GraphQL endpoint integration**; advanced state management (**Provider / Riverpod**); fully responsive cross-device UI.
> Stack: **exact** — Flutter 3.19+/Dart 3 with **GraphQL Flutter** (`graphql_flutter`). The guide lists Provider/Riverpod; we choose **Riverpod** (same rule as master doc §2: guide allows either, pick the closer conceptual match).

---

## 1. Why mobile stays Flutter (zero substitution)

Flutter is the guide's explicit Week 4 stack and Week 6's test list includes "Flutter widget & integration tests" cohort-wide — swapping to React Native would mean opting out of instructed, graded material. So this is the one fully new-skill surface that *remains* new; it's the price of the programme's breadth and it's already a lightly-known skill.

Also unchanged: **state management via Riverpod** (`[own-stack]-aligned pick`), `dio` for REST, `flutter_secure_storage` for the refresh token (mobile equivalent of an httpOnly cookie — Android Keystore-backed).

## 2. Setup

```bash
flutter create griot_mobile
cd griot_mobile
flutter pub add flutter_riverpod graphql_flutter dio flutter_secure_storage
```

## 3. Feature-first folder (mirrors backend module split)

```
lib/
├── core/network/     # dio client + interceptors, graphql_client setup
├── core/storage/     # flutter_secure_storage wrapper
├── features/
│   ├── auth/         # screens + Riverpod providers + login/refresh via dio
│   ├── dashboard/
│   ├── boards/        # board list + tasks
│   ├── tasks/         # task detail + status picker
│   └── notifications/
└── main.dart
```

## 4. Decisions
- **GraphQL**: `graphql_flutter` pointed at the same HotChocolate endpoint as the web app. Verify cache-refresh behavior after mutations (don't assume Apollo-equivalent); use explicit refetch where needed.
- **REST**: `dio` with an interceptor that mirrors the Axios one (refresh-on-401 then retry).
- **Auth**: access token in-memory Riverpod state (cleared on restart); refresh token in `flutter_secure_storage`; silent refresh on boot.
- **Status change = picker, not drag-drop** — the mobile-idiomatic pattern while web gets drag (Week 3).

## 5. Real constraint: Android-only on Parrot
iOS builds require Xcode on macOS — there is no Linux path (same category of constraint as the guide's Visual Studio on Windows). Expect `flutter doctor` to show Android green / iOS rows red. Target Android emulator + a physical device; iOS out of scope unless a Mac appears.

## 6. Definition of Done — Week 4
- [ ] Login/signup screens working against the Week-2 REST endpoint
- [ ] Dashboard + boards via GraphQL `graphql_flutter`
- [ ] Task detail + status picker + notifications
- [ ] Riverpod providers for auth + board/task/UI state
- [ ] Access token in memory, refresh in secure storage, silent refresh on boot
- [ ] `flutter doctor` clean for Android, verified on emulator + one physical device

---
**Engineering Excellence. Production Mindset. Professional Impact.**