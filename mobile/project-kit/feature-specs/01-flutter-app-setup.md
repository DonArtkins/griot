# Feature 01 — Flutter App Setup

## Type

NEW FEATURE

## What This Delivers

The Flutter app scaffold (`mobile/`) with the package set, feature-first folders, theme skeleton, and Android toolchain verified.

## Dependencies

- Flutter 3.19+ + Android toolchain (`flutter doctor` Android green).

## Context To Read First

- `mobile/AGENTS.md` + `mobile/project-kit/context/{architecture,design-system}.md`
- `research/week-04-mobile-development.md` §2

## Agent Skills To Use

- `mobile/.agents/skills/flutter-setup/SKILL.md`

## Setup / Initialization

```bash
flutter create mobile   # package griot_mobile
cd mobile
flutter pub add flutter_riverpod graphql_flutter dio flutter_secure_storage
flutter pub add --dev integration_test sdk:flutter
```

## Implementation Notes

- Create `lib/core/{network,storage,theme}`, `lib/features/{auth,dashboard,boards,tasks,notifications}`.
- `core/theme/theme.dart` stub from tokens; `core/network` client stubs.

## Separation of Concerns

- Core = transport/storage/theme; features = screens + providers; no cross-feature imports.

## Docker & Deploy

- Local run on emulator. CI builds use a Docker-pinned Flutter image (infra).

## Out of Scope

Auth flows (feature 02), data wiring (03/04).

## Acceptance Criteria

- [ ] `flutter analyze` clean; app runs on the emulator
- [ ] Feature-first folders in place


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
