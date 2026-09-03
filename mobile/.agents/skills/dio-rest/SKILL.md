---
name: dio-rest
description: "dio REST integration on Griot mobile: auth endpoints, 401→refresh→retry interceptor, uploads, and the secure-storage refresh token (flutter_secure_storage)."
metadata:
  version: "0.1.0"
---

# dio REST Skill

## Client

```dart
// lib/core/network/dio_client.dart
final dio = Dio(BaseOptions(baseUrl: env.apiUrl));
dio.interceptors.add(RefreshInterceptor(authProvider, secureStorage));
```

## Rules

- Same 401→refresh→retry-once pattern as the web Axios client.
- Refresh token stored in `flutter_secure_storage` (Android Keystore-backed) — the mobile equivalent of an httpOnly cookie.
- Access token in memory (Riverpod), cleared on restart.
