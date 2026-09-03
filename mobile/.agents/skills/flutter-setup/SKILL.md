---
name: flutter-setup
description: "Scaffold and configure the Griot Flutter mobile app: flutter create, pub packages, Android toolchain, and the CI Docker-pinned build."
metadata:
  version: "0.1.0"
---

# Flutter Setup Skill

## Scaffold

```bash
flutter create mobile   # from repo root (app name griot_mobile)
cd mobile
flutter pub add flutter_riverpod graphql_flutter dio flutter_secure_storage
flutter pub add --dev integration_test sdk:flutter
```

## Android-only constraint (Parrot)

- No Xcode on Linux → iOS out of scope. `flutter doctor` must show Android green.
- Verified on emulator + one physical device.

## CI build

- Use a Docker-pinned Flutter image (`ghcr.io/cirruslabs/flutter:3.19.x`) for `flutter test` + APK/AAB release builds.

## Rules

- `flutter analyze` clean; feature-first folders (`lib/features/…`).
