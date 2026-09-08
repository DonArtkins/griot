---
name: jwt-argon2-auth
description: "Owned auth stack: Argon2 password hashing, 15-minute JWT access tokens, opaque rotated refresh tokens (hashed at rest), and Redis sliding-window rate limiting."
metadata:
  version: "0.1.0"
---

# JWT + Argon2 Auth Skill

## Hashing

Argon2 via `Konscious.Security.Cryptography`, per-user salt. Never plaintext/SHA/MD5.

## Tokens

- Access: JWT, 15-min TTL, claims `sub` + `email` + `jti`, env-signed; reject on invalid `iss`/`aud`.
  Workspace context is selected per-request and checked against membership; no global `wid` claim is issued.
- Refresh: opaque 256-bit; store SHA-256 hash in `RefreshTokens`; **rotate on use**; **revoke the family on reuse** (replay must fail).

## Rate limiting

Redis sliding window on `/api/auth/login`; query-cost guard on `/graphql`. CORS allow-list only.

## Service token

`GRIOT_SERVICE_TOKEN` → restricted `ai-agent` principal (ReadWorkspace, CreateTask, AddComment, CreateNotification — no deletes/invites). `/api/webhooks/trigger` verifies HMAC `X-Trigger-Signature`.

## Verify

- `Refresh_Cannot_Be_Replayed` test passes; hammered login returns 429 from Redis limit.
