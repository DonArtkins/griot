---
name: auth-and-zustand
description: "Web auth + client-state split: JWT access token in memory (Zustand), refresh token in an httpOnly cookie, silent refresh on boot, and the Zustand=client-only-state rule."
metadata:
  version: "0.1.0"
---

# Auth + Zustand Skill

## Auth contract

- Access token: memory only (Zustand). Refresh token: `httpOnly; Secure; SameSite=Lax` cookie. No `localStorage` tokens (XSS surface — OWASP line).
- Silent refresh on boot + in the axiointerceptor (401 → refresh → retry once).

## Zustand scope (absolute)

Client-only state ONLY: filters, modal open/close, drag state, `accessToken`, UI preferences. Server data lives in Apollo/TanStack caches — mirroring it into Zustand is a contract violation.

## Auth routes

- Public: `/login`, `/signup`. App routes behind `RequireAuth` redirect.

## Rules

- Refresh failure clears the session and redirects to `/login`.
- No double-dispatch loops in the interceptor (use `_retried` guard).
