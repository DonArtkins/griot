# Mobile Architecture

Feature-first folders mirror the backend module split. Single `flutter create mobile` app (package `griot_mobile`).

```
lib/
├── core/network/     # dio_client.dart (Rest), graphql_client.dart (GraphQLClient)
├── core/storage/     # secure_storage.dart (flutter_secure_storage wrapper)
├── core/theme/       # theme.dart (from tokens)
├── features/
│   ├── auth/         # login/signup screens + auth provider + refresh flow
│   ├── dashboard/    # workspace/project overview
│   ├── boards/       # board list + board screen + status picker
│   ├── tasks/        # task detail + comments + attachments
│   └── notifications/# list + read/unread
└── main.dart         # ProviderScope + GraphQLProvider + MaterialApp
```

## Platform

- Android only (Parrot has no Xcode). Emulator + one physical device required.
- API base URL via `--dart-define=API_URL=…` (never hardcoded).

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
