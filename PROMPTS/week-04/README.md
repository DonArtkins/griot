# Week 04 — Mobile Prompts

**Week goal (from the roadmap):** login & auth screens; REST API integration; GraphQL endpoint integration; advanced state management (Provider/Riverpod); fully responsive cross-device UI.

## Status

Not started — write the prompts when Feature 05 (web) is stable. Draft on the agent prompt below:

> Build **Feature 06** (`feature-specs/06-mobile-app-flutter.md`). `flutter create mobile`, add `flutter_riverpod graphql_flutter dio flutter_secure_storage`, implement auth (login/signup vs the .NET REST API, refresh-in-secure-storage), dashboard + boards via GraphQL, task detail + status picker + notifications mirroring the web feature split. Follow the "separation of concerns" and "Docker & Deploy" sections (Android APK through the Docker-pinned CI build). `flutter analyze` + `flutter test` green before "done."