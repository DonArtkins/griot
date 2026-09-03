---
name: flutter-testing
description: "Flutter widget + integration tests for the Griot mobile app: widget tests for key screens, integration_test for device flows, Docker-pinned runner in CI."
metadata:
  version: "0.1.0"
---

# Flutter Testing Skill

## Commands

```bash
cd mobile
flutter test                # widget tests
flutter test integration_test  # device flows (emulator/physical)
```

## Must-cover

- Login screen + auth flow (mocked dio).
- Status picker updates + persistence.
- Board/task screens render from a mocked GraphQL client.

## Rules

- Widget tests assert behavior; integration_test covers real flows on a device.
