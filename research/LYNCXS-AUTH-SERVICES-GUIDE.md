# LYNCXS INDUSTRIES AUTH SERVICES INTEGRATION GUIDE
## Don Artkins · Lyncxs Industries · Nakuru, Kenya
### Authentication for Every App, API, Tool, and Agent You Build

**Version**: 2.0.0\
**Date**: June 2026\
**Stack**: Next.js · Python FastAPI · React Native · Flutter · LynxJS · Rust Axum · Go Gateway\
**Primary**: Clerk · Auth0 · Supabase Auth · Auth.js v5 · Stack Auth · Kinde · Better Auth · WorkOS

---

## TABLE OF CONTENTS

### VOLUME I — Auth Services & JWT (Sections 1–19)

1. [Free Tier Comparison — Pick Your Starting Point](#1-free-tier-comparison--pick-your-starting-point)
2. [Why JWT Is Hard — The 12 Ways It Goes Wrong](#2-why-jwt-is-hard--the-12-ways-it-goes-wrong)
3. [JWT Deep Dive — When You Must Implement It Yourself](#3-jwt-deep-dive--when-you-must-implement-it-yourself)
4. [Clerk — The Lyncxs Industries Default](#4-clerk--the-artkins-default)
5. [Auth0 — Enterprise Grade](#5-auth0--enterprise-grade)
6. [Supabase Auth — Database-Integrated Auth](#6-supabase-auth--database-integrated-auth)
7. [Auth.js (NextAuth v5) — Open Source, Framework-Native](#7-authjs-nextauth-v5--open-source-framework-native)
8. [Stack Auth — Open-Source Clerk Alternative](#8-stack-auth--open-source-clerk-alternative)
9. [Kinde — Simplest Launch Alternative](#9-kinde--simplest-launch-alternative)
10. [Better Auth — TypeScript-Native Modern Library](#10-better-auth--typescript-native-modern-library)
11. [WorkOS — Enterprise SSO/SAML](#11-workos--enterprise-ssosamل)
12. [Platform-Specific Setup Guides](#12-platform-specific-setup-guides)
13. [Backend JWT Verification in Every Language](#13-backend-jwt-verification-in-every-language)
14. [Social OAuth2 Providers Map](#14-social-oauth2-providers-map)
15. [MFA and Advanced Security](#15-mfa-and-advanced-security)
16. [Multi-Tenant / Organization Auth](#16-multi-tenant--organization-auth)
17. [Session Management and Token Strategy](#17-session-management-and-token-strategy)
18. [Production Checklist](#18-production-checklist)
19. [Environment Variables Reference](#19-environment-variables-reference)

### VOLUME II — Identity Engineering (Sections 20–30)

20. [Auth by Platform — Complete Coverage](#20-auth-by-platform--complete-coverage)
    - 20.1 Web Applications
    - 20.2 Mobile Apps (Android & iOS Deep Dive)
    - 20.3 API Authentication (Keys, HMAC, mTLS, SigV4)
    - 20.4 Internal Microservices (mTLS, SPIFFE/SPIRE)
    - 20.5 CLI Tools (Device Flow, PAT, SSH)
    - 20.6 Desktop Applications (OS Credential Managers)
    - 20.7 AI Agents & MCP
    - 20.8 Payment Systems & M-Pesa / Daraja API
    - 20.9 Enterprise Systems (SAML Deep Dive)
    - 20.10 IoT & Embedded Devices
    - 20.11 Browser Extensions
    - 20.12 WebSockets & Real-Time
21. [Authorization Models — Full Spectrum](#21-authorization-models--full-spectrum)
    - 21.1 RBAC
    - 21.2 ABAC
    - 21.3 PBAC / Open Policy Agent (OPA)
    - 21.4 ReBAC — Google Zanzibar Model
    - 21.5 Capability-Based Security
    - 21.6 Scope-Based Permissions (OAuth Scopes)
    - 21.7 Row-Level Security (Supabase RLS)
    - 21.8 Multi-Tenant Authorization Patterns
    - 21.9 Authorization in Next.js Middleware
22. [Passkeys, WebAuthn & FIDO2](#22-passkeys-webauthn--fido2)
23. [Token Systems Beyond JWT](#23-token-systems-beyond-jwt)
    - 23.1 PASETO (Platform-Agnostic Security Tokens)
    - 23.2 Opaque Tokens
    - 23.3 Macaroons
    - 23.4 Proof-of-Possession Tokens
24. [Password & Credential Storage](#24-password--credential-storage)
25. [Identity Federation — Complete Reference](#25-identity-federation--complete-reference)
    - 25.1 OAuth 2.1
    - 25.2 OpenID Connect (OIDC)
    - 25.3 SAML 2.0
    - 25.4 SCIM (User Provisioning)
    - 25.5 LDAP & Active Directory
26. [PKI, mTLS & Certificate Management](#26-pki-mtls--certificate-management)
27. [Secret Management](#27-secret-management)
28. [Zero Trust Architecture](#28-zero-trust-architecture)
29. [AI Agent Identity — Expanded](#29-ai-agent-identity--expanded)
30. [Full Identity Architecture Decision Matrix](#30-full-identity-architecture-decision-matrix)

---

## 1. FREE TIER COMPARISON — PICK YOUR STARTING POINT

### 1.1 The Honest Free Tier Map (June 2026)

| Service | Free MAU | Social OAuth | MFA | Orgs | Forever? | Self-Host? |
|---|---|---|---|---|---|---|
| **Clerk** | 10,000 | Unlimited | ✅ | ✅ (1 free org) | ✅ Yes | ❌ |
| **Auth0** | 7,500 | Social + 3 enterprise | ✅ | ❌ | ✅ Yes | ❌ (paid) |
| **Supabase Auth** | 50,000 | OAuth2 providers | ✅ | ❌ | ✅ Yes | ✅ |
| **WorkOS AuthKit** | 1,000,000 | Social + email | ✅ | ❌ | ✅ Yes | ❌ |
| **Kinde** | 10,500 | Unlimited social | ✅ | ✅ (3 free orgs) | ✅ Yes | ❌ |
| **Stack Auth** | Unlimited | All | ✅ | ✅ | ✅ Self-host | ✅ |
| **Better Auth** | Unlimited | All | ✅ | ✅ | ✅ Self-host | ✅ |
| **Auth.js v5** | Unlimited | 70+ adapters | ❌ built-in | ❌ | ✅ Self-host | ✅ |
| **Firebase Auth** | 10K phone/mo | Google, social | ✅ | ❌ | ✅ Yes | ❌ |
| **Keycloak** | Unlimited | All | ✅ | ✅ | ✅ Self-host | ✅ |

> **WorkOS note**: The 1M MAU free tier is for AuthKit (social + email auth only). Enterprise SSO/SAML is a separate paid product. This makes WorkOS the highest-ceiling free option for consumer apps — use it when you expect scale early.

> **Supabase note**: Auth is included free only when using Supabase as your database. Standalone it is not available.

### 1.2 Lyncxs Industries Default Stack

```
NEW PROJECT, NEXT.JS STACK:
  Primary:       Clerk          → best DX, polished components, generous free tier

SUPABASE IS ALREADY YOUR DB:
  Primary:       Supabase Auth  → why add another service?

SELF-HOSTED / OPEN SOURCE REQUIRED:
  Primary:       Auth.js v5     → most mature, 70+ providers, framework-native
  Alternative:   Better Auth    → TypeScript-native, plugin-based

ENTERPRISE B2B SAAS (SSO/SAML from day 1):
  Primary:       WorkOS         → SSO is their core product; AuthKit for consumer auth
  Alternative:   Auth0          → full platform, better developer experience

HIGH MAU CONSUMER APP (>10K users, stay free):
  Primary:       WorkOS AuthKit → 1M MAU free
  Alternative:   Supabase Auth  → 50K MAU free

MULTI-COUNTRY, GDPR-SENSITIVE:
  Primary:       Better Auth    → self-hosted, full control of data residency
  Alternative:   Keycloak       → enterprise self-hosted, EU data residency
```

### 1.3 Paid Tier Quick Reference (When You Outgrow Free)

| Service | First Paid Tier | Per-MAU Overage | Notes |
|---|---|---|---|
| Clerk | $25/month (10K MAU) | $0.02/MAU | Includes orgs, MFA, webhooks |
| Auth0 | $23/month (1K MAU) | Variable | Cheaper at lower user counts |
| Supabase | $25/month (100K MAU) | ~$0.00325/MAU | Bundled with DB, storage, etc. |
| WorkOS | Free AuthKit + $149/connection SSO | Per SSO connection | SSO is expensive but enterprise needs it |
| Kinde | $25/month (unlimited MAU, remove branding) | — | Fixed price, no MAU overage |
| Better Auth / Stack Auth | Self-host costs only | — | Your infra, your cost |

---

## 2. WHY JWT IS HARD — THE 12 WAYS IT GOES WRONG

This is the section that explains why you want a managed auth provider. JWT implementation looks simple — `jwt.sign()` and `jwt.verify()`. That first 80% takes an afternoon. The remaining 20% is where production systems get compromised.

### 2.1 The 12 Failure Modes

#### #1 — Algorithm Choice: HS256 vs RS256

```
HS256 (HMAC-SHA256): ONE shared secret signs AND verifies.
  → Problem: Every service that verifies must hold the same secret.
  → If one service is compromised, attackers can forge any token.
  → Fine for single-server apps. Wrong for distributed systems.

RS256 (RSA-SHA256): Private key signs. Public key verifies.
  → Auth server holds private key (secret). Never shared.
  → Every service verifies against public key (safe to distribute).
  → Standard for distributed architectures (Clerk, Auth0, Supabase all use this).
  → Your Python FastAPI, Rust Axum, and Go services verify without needing any secret.
```

#### #2 — The alg:none Attack

An attacker modifies the JWT header to `"alg": "none"` and removes the signature. Some libraries (especially older ones) will accept this, treating an unsigned token as valid.

```javascript
// VULNERABLE: library accepts any algorithm including 'none'
jwt.verify(token, secret); // Bad — alg not pinned

// SAFE: explicitly whitelist allowed algorithms
jwt.verify(token, secret, { algorithms: ['RS256'] }); // Only RS256 accepted
```

#### #3 — Key Confusion Attack (RS256 vs HS256 mix)

If your library accepts both algorithms: an attacker signs a token using HS256 with the RSA *public key* as the HMAC secret. Since public keys are public, the attacker knows the key. If your verifier isn't pinning the algorithm, it may use the RS256 public key as an HS256 secret and succeed.

#### #4 — Token Storage on the Client

```
localStorage:
  ❌ Readable by any JavaScript — XSS risk.
  ❌ If your site has a single XSS vulnerability, all tokens are stolen.

Memory (React state / JS variable):
  ✅ Not accessible to XSS attacks.
  ❌ Lost on page refresh — user has to re-login every tab refresh.

httpOnly Cookie (SameSite=Strict):
  ✅ Not readable by JavaScript — XSS safe.
  ⚠️ CSRF risk — mitigate with SameSite=Strict + CSRF token on mutations.
  ✅ Survives page refresh.
  ✅ THIS IS THE CORRECT APPROACH for web apps.
```

#### #5 — No Token Invalidation on Logout

JWT is stateless. Once issued, it is valid until `exp`. If a user logs out, their token is still valid until expiry. An attacker who stole the token before logout can use it until it expires.

```
Solution options:
  1. Very short expiry (15 minutes) — limits damage window.
  2. Blocklist in Redis — check every token against Redis on each request.
  3. Token version in DB — increment user's token_version on logout;
     validate token's version claim against DB.

Without one of these: logout doesn't truly log out.
```

#### #6 — Long-Lived Access Tokens

```javascript
// This is wrong. Common in tutorials.
jwt.sign({ userId }, secret, { expiresIn: '7d' });

// If this token is leaked (XSS, log file, screenshot):
// → Attacker has 7 days of unrestricted access.
// → You cannot revoke it without a blocklist.

// Correct: short-lived access + long-lived refresh
Access token:  15 minutes (expiresIn: '15m')
Refresh token: 7–30 days  (expiresIn: '30d')
// Only the short access token is sent on every API call.
// The refresh token is used once to rotate the access token.
```

#### #7 — Refresh Token Theft and No Rotation

If a refresh token is stolen, the attacker gets indefinite access. Refresh token rotation solves this:

```
1. User presents refresh token → server issues NEW access + NEW refresh token.
2. Old refresh token is immediately invalidated.
3. If attacker uses the OLD refresh token after rotation:
   → Detect reuse → ENTIRE token family is invalidated → user re-logs in.
   → Attacker loses access even if they stole the token.

This is called "refresh token family" tracking.
```

#### #8 — Missing JTI (JWT ID)

Without a unique `jti` claim in every token, you cannot:
- Blacklist a specific token on logout
- Detect refresh token reuse (you need to track which JTIs have been consumed)
- Audit which specific token was used for a sensitive action

```typescript
// Every token must include a unique JTI
jwt.sign({
  sub: userId,
  jti: crypto.randomUUID(), // unique per token
  // ... other claims
}, privateKey, { algorithm: 'RS256' });
```

#### #9 — Wrong Validation Order

JWT validation must happen in a specific order. Skipping any step is a vulnerability:

```
CORRECT ORDER:
  1. Parse token structure (header.payload.signature)
  2. Verify signature (reject if invalid — stop here if bad)
  3. Verify algorithm matches expected (reject 'none' or unexpected alg)
  4. Verify 'exp' (reject if expired)
  5. Verify 'nbf' if present (not before — token not yet valid)
  6. Verify 'iss' (issuer — is this from your auth server?)
  7. Verify 'aud' (audience — is this token meant for this service?)
  8. Check JTI blocklist (is this specific token revoked?)
  9. Extract claims — only now can you trust the payload.

COMMON MISTAKE: Decoding payload first to get 'exp', checking if expired,
THEN verifying signature. An attacker can modify 'exp' in an unsigned token
and your code trusts it before signature verification.
```

#### #10 — Clock Skew in Distributed Systems

In a distributed system, servers have slightly different clocks. A token issued at `iat: 1700000000` might arrive at a server whose clock reads `1699999970` — 30 seconds behind. Without a clock tolerance, the token appears to be issued "in the future" and is rejected.

```typescript
// Allow 30 seconds of clock skew
jwt.verify(token, publicKey, {
  algorithms: ['RS256'],
  clockTolerance: 30, // seconds
});
```

#### #11 — Leaking Tokens in Logs

```javascript
// ❌ Never log Authorization headers
console.log(`Request from: ${req.headers.authorization}`);
// Logs the entire "Bearer eyJhbGciOiJSUzI1NiJ9..." → token in plaintext in your log files.

// ✅ Log only the extracted user ID (sub claim)
const payload = await verifyToken(token);
logger.info({ userId: payload.sub, action: 'api_call' });
```

This is more common than you think. Logs are often less secured than databases.

#### #12 — No PKCE for Public Clients

Mobile apps (React Native, Flutter) and browser SPAs cannot keep a client secret. They use the OAuth2 Authorization Code flow with PKCE (Proof Key for Code Exchange). Without PKCE, authorization codes can be intercepted by a malicious app on the same device.

```
PKCE flow:
  1. App generates code_verifier (random string, 43–128 chars)
  2. App computes code_challenge = base64url(sha256(code_verifier))
  3. App sends code_challenge in the auth request
  4. Auth server stores code_challenge
  5. App receives authorization code
  6. App exchanges code + code_verifier for tokens
  7. Auth server verifies sha256(code_verifier) == stored code_challenge
  8. If verification fails → reject. Code is worthless without code_verifier.

All managed auth providers handle PKCE automatically.
Without PKCE on mobile: your OAuth flow is vulnerable to code interception.
```

### 2.2 What Managed Auth Actually Gives You

Every managed provider (Clerk, Auth0, Supabase) handles all 12 of these correctly by default:

- RS256 with JWKS endpoint
- Algorithm pinning
- HttpOnly cookie session management (or SDK-managed secure storage on mobile)
- Short-lived tokens with automatic rotation
- Token revocation on logout
- JTI tracking
- Correct validation order in their SDKs
- Clock skew tolerance
- PKCE for all public client flows

The argument for rolling your own JWT: none, for most applications. The argument for managed auth: ship in hours instead of weeks, production-correct from day one.

---

## 3. JWT DEEP DIVE — WHEN YOU MUST IMPLEMENT IT YOURSELF

Use this section if: you're building internal tooling, an API-only service with no UI, or you have a compliance reason to not use managed auth. For every other case: use Clerk or Supabase Auth and skip to Section 4.

### 3.1 Key Generation

```bash
# Generate RS256 key pair — do this ONCE, store in secrets manager
openssl genrsa -out private.pem 2048
openssl rsa -in private.pem -pubout -out public.pem

# For environment variable storage (escape newlines)
awk 'NF {sub(/\r/, ""); printf "%s\\n",$0;}' private.pem
awk 'NF {sub(/\r/, ""); printf "%s\\n",$0;}' public.pem
```

### 3.2 Full TypeScript Implementation (Production-Correct)

```typescript
// lib/auth/jwt.ts
// npm install jsonwebtoken ioredis
// npm install -D @types/jsonwebtoken

import jwt from 'jsonwebtoken';
import crypto from 'crypto';
import { Redis } from 'ioredis';

const redis = new Redis(process.env.REDIS_URL!);

// Unescape newlines from environment variable storage
const PRIVATE_KEY = process.env.JWT_PRIVATE_KEY!.replace(/\\n/g, '\n');
const PUBLIC_KEY = process.env.JWT_PUBLIC_KEY!.replace(/\\n/g, '\n');
const ISSUER = process.env.APP_URL!;
const AUDIENCE = 'artkins-api';

// ─── TOKEN ISSUANCE ─────────────────────────────────────────────────

export interface TokenClaims {
  sub: string;       // User ID
  email: string;
  roles: string[];
  orgId?: string;    // For multi-tenant
}

export function issueAccessToken(claims: TokenClaims): string {
  return jwt.sign(
    {
      ...claims,
      jti: crypto.randomUUID(),   // Unique token ID (for revocation)
      iss: ISSUER,
      aud: AUDIENCE,
    },
    PRIVATE_KEY,
    {
      algorithm: 'RS256',
      expiresIn: '15m',           // NEVER longer — steal window is 15 minutes max
    }
  );
}

export function issueRefreshToken(
  userId: string,
  familyId: string            // Group tokens into families for theft detection
): string {
  return jwt.sign(
    {
      sub: userId,
      jti: crypto.randomUUID(),
      familyId,
      iss: ISSUER,
      aud: `${AUDIENCE}:refresh`,
    },
    PRIVATE_KEY,
    {
      algorithm: 'RS256',
      expiresIn: '30d',
    }
  );
}

// ─── TOKEN VERIFICATION ──────────────────────────────────────────────

export async function verifyAccessToken(token: string): Promise<jwt.JwtPayload> {
  // Step 1: Verify signature + standard claims. Throws on failure.
  const payload = jwt.verify(token, PUBLIC_KEY, {
    algorithms: ['RS256'],      // Pin algorithm — never allow 'none'
    issuer: ISSUER,
    audience: AUDIENCE,
    clockTolerance: 30,         // Allow 30s clock skew between servers
  }) as jwt.JwtPayload;

  // Step 2: Check JTI blocklist (revoked on logout)
  const isRevoked = await redis.get(`blacklist:jti:${payload.jti}`);
  if (isRevoked) {
    throw new Error('Token revoked');
  }

  return payload;
}

// ─── REFRESH TOKEN ROTATION ──────────────────────────────────────────

export async function rotateRefreshToken(oldRefreshToken: string): Promise<{
  accessToken: string;
  refreshToken: string;
}> {
  let payload: jwt.JwtPayload;

  try {
    payload = jwt.verify(oldRefreshToken, PUBLIC_KEY, {
      algorithms: ['RS256'],
      issuer: ISSUER,
      audience: `${AUDIENCE}:refresh`,
      clockTolerance: 30,
    }) as jwt.JwtPayload;
  } catch {
    throw new Error('Invalid or expired refresh token');
  }

  const { sub: userId, jti, familyId } = payload;

  // Check if entire token family is invalidated (from a prior theft event)
  const familyInvalid = await redis.get(`rt:family:invalid:${familyId}`);
  if (familyInvalid) {
    throw new Error('Session revoked due to security event. Please log in again.');
  }

  // Check if THIS specific refresh token was already consumed
  const alreadyUsed = await redis.get(`rt:used:${jti}`);
  if (alreadyUsed) {
    // ⚠️ THEFT DETECTED — someone used an already-consumed refresh token
    // Invalidate the ENTIRE token family immediately
    const familyTTL = 86400 * 30; // 30 days
    await redis.setex(`rt:family:invalid:${familyId}`, familyTTL, '1');
    throw new Error('Refresh token reuse detected. All sessions have been revoked.');
  }

  // Mark old refresh token as consumed (keep entry until its natural expiry)
  const remainingTTL = Math.max((payload.exp! - Math.floor(Date.now() / 1000)), 1);
  await redis.setex(`rt:used:${jti}`, remainingTTL, '1');

  // Issue fresh token pair
  const user = await getUserById(userId!); // Your DB query
  const newAccessToken = issueAccessToken({
    sub: userId!,
    email: user.email,
    roles: user.roles,
    orgId: user.orgId,
  });
  const newRefreshToken = issueRefreshToken(userId!, familyId); // Same family

  return { accessToken: newAccessToken, refreshToken: newRefreshToken };
}

// ─── LOGOUT (REVOCATION) ────────────────────────────────────────────

export async function revokeToken(tokenOrPayload: string | jwt.JwtPayload): Promise<void> {
  let payload: jwt.JwtPayload;

  if (typeof tokenOrPayload === 'string') {
    // Decode without verifying — we just need the exp/jti to store in blocklist
    payload = jwt.decode(tokenOrPayload) as jwt.JwtPayload;
  } else {
    payload = tokenOrPayload;
  }

  if (!payload?.jti || !payload?.exp) return;

  const ttl = Math.max(payload.exp - Math.floor(Date.now() / 1000), 1);
  await redis.setex(`blacklist:jti:${payload.jti}`, ttl, '1');
}

// ─── NEXT.JS MIDDLEWARE USAGE ────────────────────────────────────────

// middleware.ts
import { NextRequest, NextResponse } from 'next/server';
import { verifyAccessToken } from '@/lib/auth/jwt';

export async function middleware(req: NextRequest) {
  const token = req.cookies.get('access_token')?.value;

  if (!token) {
    return NextResponse.redirect(new URL('/login', req.url));
  }

  try {
    const payload = await verifyAccessToken(token);
    // Forward user info to API routes via header (never the raw token)
    const response = NextResponse.next();
    response.headers.set('X-User-Id', payload.sub!);
    response.headers.set('X-User-Roles', JSON.stringify(payload.roles));
    return response;
  } catch {
    // Try to refresh — see /api/auth/refresh/route.ts
    return NextResponse.redirect(new URL('/api/auth/refresh', req.url));
  }
}

// ─── COOKIE CONFIGURATION (SECURE BY DEFAULT) ───────────────────────

export function setAuthCookies(
  response: NextResponse,
  accessToken: string,
  refreshToken: string
) {
  response.cookies.set('access_token', accessToken, {
    httpOnly: true,         // Not readable by JavaScript — XSS safe
    secure: true,           // HTTPS only — never over HTTP
    sameSite: 'strict',     // CSRF protection — only sent on same-origin requests
    maxAge: 15 * 60,        // 15 minutes (matches access token expiry)
    path: '/',
  });

  response.cookies.set('refresh_token', refreshToken, {
    httpOnly: true,
    secure: true,
    sameSite: 'strict',
    maxAge: 30 * 24 * 60 * 60, // 30 days
    path: '/api/auth/refresh',  // Scoped to refresh endpoint ONLY
  });
}
```

### 3.3 DIY Session Architecture (Alternative to JWT)

```typescript
// When JWT complexity is overkill: server-side sessions with Redis
// Classic battle-tested pattern used by Rails, Django, Laravel

// lib/auth/session.ts
import { Redis } from 'ioredis';
import crypto from 'crypto';

const redis = new Redis(process.env.REDIS_URL!);
const SESSION_TTL = 60 * 60 * 24 * 7; // 7 days

export async function createSession(userId: string): Promise<string> {
  const sessionId = crypto.randomBytes(32).toString('hex'); // 256-bit random ID
  await redis.setex(
    `session:${sessionId}`,
    SESSION_TTL,
    JSON.stringify({ userId, createdAt: Date.now() })
  );
  return sessionId;
}

export async function getSession(sessionId: string): Promise<{ userId: string } | null> {
  const data = await redis.get(`session:${sessionId}`);
  if (!data) return null;
  // Slide expiry on activity (optional)
  await redis.expire(`session:${sessionId}`, SESSION_TTL);
  return JSON.parse(data);
}

export async function destroySession(sessionId: string): Promise<void> {
  await redis.del(`session:${sessionId}`);
}

// Cookie: session ID (opaque — attacker gets nothing useful even if stolen)
// The session itself is stored server-side in Redis
// Logout is instant: just delete the Redis key
// No token rotation logic needed
// Tradeoff: requires Redis on every authenticated request (latency)
//           vs JWT: verify locally, no DB/Redis lookup (except blocklist)
```

---

## 4. CLERK — THE Lyncxs Industries DEFAULT

**Why it is the default**: Best-in-class developer experience for Next.js. Prebuilt `<SignIn>`, `<SignUp>`, `<UserButton>` components that look polished out of the box. Organizations/multi-tenancy built in. Webhooks for DB sync. The free tier of 10,000 MAU covers you until you have product-market fit.

**Free tier**: 10,000 MAU · Unlimited social providers · MFA · 1 free organization · Forever · No credit card

### 4.1 Setup Steps

```
1. clerk.com → Sign up → Create Application
2. Name your app → choose providers (Email, Google, GitHub, etc.)
3. Dashboard → API Keys → copy NEXT_PUBLIC_CLERK_PUBLISHABLE_KEY + CLERK_SECRET_KEY
4. npm install @clerk/nextjs
5. Add keys to .env.local
6. Add middleware.ts (see below)
7. Wrap layout.tsx in <ClerkProvider>
8. Done — auth UI available via <SignIn />, <SignUp />, <UserButton />
```

### 4.2 Next.js App Router — Full Setup

```typescript
// middleware.ts — root of project
import { clerkMiddleware, createRouteMatcher } from '@clerk/nextjs/server';

const isPublicRoute = createRouteMatcher([
  '/',                    // Landing page
  '/pricing',
  '/blog(.*)',
  '/api/webhooks(.*)',    // Clerk webhooks — must be public
  '/api/payments/(.*)',   // Payment callbacks (Daraja, etc.) — must be public
  '/sign-in(.*)',
  '/sign-up(.*)',
]);

export default clerkMiddleware(async (auth, req) => {
  if (!isPublicRoute(req)) {
    await auth.protect(); // Redirect unauthenticated users to sign-in
  }
});

export const config = {
  matcher: [
    '/((?!_next|[^?]*\\.(?:html?|css|js(?!on)|jpe?g|webp|png|gif|svg|ttf|woff2?|ico|csv|docx?|xlsx?|zip|webmanifest)).*)',
    '/(api|trpc)(.*)',
  ],
};
```

```typescript
// app/layout.tsx
import { ClerkProvider } from '@clerk/nextjs';

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <ClerkProvider>
      <html lang="en">
        <body>{children}</body>
      </html>
    </ClerkProvider>
  );
}
```

```typescript
// app/(auth)/sign-in/[[...sign-in]]/page.tsx
import { SignIn } from '@clerk/nextjs';

export default function SignInPage() {
  return (
    <div className="flex min-h-screen items-center justify-center">
      <SignIn
        appearance={{
          elements: {
            formButtonPrimary: 'bg-black text-white hover:bg-gray-800',
            card: 'shadow-xl border border-gray-100',
          },
        }}
      />
    </div>
  );
}
```

```typescript
// Server Component — get user ID and claims
import { auth, currentUser } from '@clerk/nextjs/server';

// Lightweight — gets session claims only (no network call)
export async function ServerPage() {
  const { userId, orgId, sessionClaims } = await auth();
  if (!userId) return null;

  return <div>User ID: {userId}</div>;
}

// Heavyweight — fetches full user object (makes a network call to Clerk)
export async function ProfilePage() {
  const user = await currentUser();
  return (
    <div>
      <img src={user?.imageUrl} alt={user?.fullName ?? ''} />
      <p>{user?.emailAddresses[0].emailAddress}</p>
    </div>
  );
}
```

```typescript
// Client Component — useAuth and useUser hooks
'use client';
import { useAuth, useUser, UserButton } from '@clerk/nextjs';

export function Navbar() {
  const { isSignedIn, isLoaded } = useAuth();
  const { user } = useUser();

  if (!isLoaded) return <NavbarSkeleton />;

  return (
    <nav className="flex items-center justify-between p-4">
      <Logo />
      {isSignedIn ? (
        <div className="flex items-center gap-4">
          <span className="text-sm text-gray-600">
            {user?.firstName}
          </span>
          <UserButton afterSignOutUrl="/" />
        </div>
      ) : (
        <SignInButton />
      )}
    </nav>
  );
}
```

```typescript
// Server Actions with auth
'use server';
import { auth } from '@clerk/nextjs/server';

export async function createProject(formData: FormData) {
  const { userId } = await auth();
  if (!userId) throw new Error('Unauthorized');

  const name = formData.get('name') as string;
  const project = await db.project.create({
    data: { name, userId },
  });
  return project;
}
```

```typescript
// Protected API Route
import { auth } from '@clerk/nextjs/server';
import { NextRequest, NextResponse } from 'next/server';

export async function GET(req: NextRequest) {
  const { userId } = await auth();
  if (!userId) {
    return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
  }

  const data = await db.project.findMany({ where: { userId } });
  return NextResponse.json(data);
}
```

### 4.3 Clerk Webhooks — Syncing Users to Your Database (Critical)

This is the pattern for every Clerk project. Your database needs a copy of user data. Clerk webhooks keep it in sync.

```typescript
// app/api/webhooks/clerk/route.ts
// npm install svix

import { NextRequest, NextResponse } from 'next/server';
import { Webhook } from 'svix';
import { WebhookEvent } from '@clerk/nextjs/server';

export async function POST(req: NextRequest) {
  const WEBHOOK_SECRET = process.env.CLERK_WEBHOOK_SECRET;
  if (!WEBHOOK_SECRET) {
    throw new Error('CLERK_WEBHOOK_SECRET not set');
  }

  const svix_id = req.headers.get('svix-id');
  const svix_timestamp = req.headers.get('svix-timestamp');
  const svix_signature = req.headers.get('svix-signature');

  if (!svix_id || !svix_timestamp || !svix_signature) {
    return NextResponse.json({ error: 'Missing svix headers' }, { status: 400 });
  }

  const body = await req.text();
  const wh = new Webhook(WEBHOOK_SECRET);
  let event: WebhookEvent;

  try {
    event = wh.verify(body, {
      'svix-id': svix_id,
      'svix-timestamp': svix_timestamp,
      'svix-signature': svix_signature,
    }) as WebhookEvent;
  } catch {
    return NextResponse.json({ error: 'Invalid webhook signature' }, { status: 400 });
  }

  switch (event.type) {
    case 'user.created': {
      const { id, email_addresses, first_name, last_name, image_url } = event.data;
      await db.user.create({
        data: {
          clerkId: id,
          email: email_addresses[0].email_address,
          name: `${first_name ?? ''} ${last_name ?? ''}`.trim(),
          avatarUrl: image_url,
        },
      });
      break;
    }
    case 'user.updated': {
      const { id, email_addresses, first_name, last_name, image_url } = event.data;
      await db.user.update({
        where: { clerkId: id },
        data: {
          email: email_addresses[0].email_address,
          name: `${first_name ?? ''} ${last_name ?? ''}`.trim(),
          avatarUrl: image_url,
        },
      });
      break;
    }
    case 'user.deleted': {
      if (event.data.id) {
        await db.user.update({
          where: { clerkId: event.data.id },
          data: { deletedAt: new Date() },
        });
      }
      break;
    }
    case 'organizationMembership.created': {
      const { organization, public_user_data } = event.data;
      await db.orgMembership.upsert({
        where: {
          userId_orgId: {
            userId: public_user_data.user_id,
            orgId: organization.id,
          },
        },
        create: {
          userId: public_user_data.user_id,
          orgId: organization.id,
          role: event.data.role,
        },
        update: { role: event.data.role },
      });
      break;
    }
  }

  return NextResponse.json({ received: true });
}

// Setup:
// 1. Clerk Dashboard → Webhooks → Add Endpoint
// 2. URL: https://yourdomain.com/api/webhooks/clerk
// 3. Events: user.created, user.updated, user.deleted, session.created,
//            organizationMembership.created, organizationMembership.deleted
// 4. Copy signing secret → CLERK_WEBHOOK_SECRET env var
```

### 4.4 Clerk JWT Verification in Python FastAPI

```python
# auth/clerk.py
# pip install PyJWT cryptography httpx

import os
import jwt
from jwt import PyJWKClient
from fastapi import HTTPException, Depends
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer

CLERK_JWKS_URL = os.environ["CLERK_JWKS_URL"]

jwks_client = PyJWKClient(
    CLERK_JWKS_URL,
    cache_keys=True,
    cache_jwk_set=True,
    lifespan=3600,
)

security = HTTPBearer()

async def get_current_user(
    credentials: HTTPAuthorizationCredentials = Depends(security)
) -> dict:
    token = credentials.credentials

    try:
        signing_key = jwks_client.get_signing_key_from_jwt(token)
        payload = jwt.decode(
            token,
            signing_key.key,
            algorithms=["RS256"],
            options={
                "verify_aud": False,
                "require": ["exp", "sub"],
            },
            leeway=30,
        )
    except jwt.ExpiredSignatureError:
        raise HTTPException(status_code=401, detail="Token expired")
    except jwt.InvalidSignatureError:
        raise HTTPException(status_code=401, detail="Invalid token signature")
    except jwt.PyJWKClientError:
        raise HTTPException(status_code=401, detail="Could not fetch signing keys")
    except Exception:
        raise HTTPException(status_code=401, detail="Invalid token")

    return payload

# Usage in FastAPI routes
from fastapi import FastAPI

app = FastAPI()

@app.get("/api/projects")
async def list_projects(user: dict = Depends(get_current_user)):
    user_id = user["sub"]
    projects = await db.project.find_many(where={"clerkId": user_id})
    return projects

@app.get("/api/me")
async def get_me(user: dict = Depends(get_current_user)):
    return {
        "userId": user["sub"],
        "email": user.get("email"),
        "orgId": user.get("org_id"),
    }
```

```python
# For multi-tenant: verify orgId from JWT
async def require_org_member(user: dict = Depends(get_current_user)) -> dict:
    if not user.get("org_id"):
        raise HTTPException(
            status_code=403,
            detail="Organization membership required"
        )
    return user

@app.get("/api/org/members")
async def get_org_members(user: dict = Depends(require_org_member)):
    org_id = user["org_id"]
    # ...
```

### 4.5 Clerk in React Native (Expo)

```typescript
// npm install @clerk/clerk-expo expo-secure-store expo-linking expo-web-browser

// app/_layout.tsx
import { ClerkProvider, ClerkLoaded } from '@clerk/clerk-expo';
import * as SecureStore from 'expo-secure-store';
import { Slot } from 'expo-router';

const tokenCache = {
  async getToken(key: string) {
    return SecureStore.getItemAsync(key);
  },
  async saveToken(key: string, value: string) {
    await SecureStore.setItemAsync(key, value, {
      keychainAccessible: SecureStore.WHEN_UNLOCKED,
    });
  },
  async clearToken(key: string) {
    await SecureStore.deleteItemAsync(key);
  },
};

export default function RootLayout() {
  return (
    <ClerkProvider
      tokenCache={tokenCache}
      publishableKey={process.env.EXPO_PUBLIC_CLERK_PUBLISHABLE_KEY!}
    >
      <ClerkLoaded>
        <Slot />
      </ClerkLoaded>
    </ClerkProvider>
  );
}
```

```typescript
// app/(auth)/sign-in.tsx
import { useSignIn, useOAuth } from '@clerk/clerk-expo';
import { useCallback } from 'react';
import * as WebBrowser from 'expo-web-browser';
import * as Linking from 'expo-linking';

WebBrowser.maybeCompleteAuthSession();

export default function SignInScreen() {
  const { signIn, setActive, isLoaded } = useSignIn();

  const handleEmailSignIn = useCallback(async (email: string, password: string) => {
    if (!isLoaded) return;
    try {
      const result = await signIn.create({ identifier: email, password });
      if (result.status === 'complete') {
        await setActive({ session: result.createdSessionId });
      }
    } catch (err: any) {
      console.error('Sign in failed:', err.errors?.[0]?.message);
    }
  }, [isLoaded, signIn, setActive]);

  const { startOAuthFlow: googleSignIn } = useOAuth({ strategy: 'oauth_google' });

  const handleGoogleSignIn = useCallback(async () => {
    try {
      const { createdSessionId, setActive: setOAuthActive } = await googleSignIn({
        redirectUrl: Linking.createURL('/oauth-native-callback'),
      });
      if (createdSessionId) {
        await setOAuthActive!({ session: createdSessionId });
      }
    } catch (err) {
      console.error('Google sign in failed:', err);
    }
  }, [googleSignIn]);

  return (/* ... your UI ... */);
}
```

```typescript
// hooks/useAuthenticatedFetch.ts
import { useAuth } from '@clerk/clerk-expo';

export function useAuthenticatedFetch() {
  const { getToken } = useAuth();

  const fetchWithAuth = async (url: string, options?: RequestInit) => {
    const token = await getToken();
    return fetch(`${process.env.EXPO_PUBLIC_API_URL}${url}`, {
      ...options,
      headers: {
        ...options?.headers,
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    });
  };

  return fetchWithAuth;
}
```

### 4.6 Clerk in Flutter (REST API Approach)

Clerk has no official Flutter SDK. Use the REST API directly.

```dart
// services/clerk_auth_service.dart
// pub add dio flutter_secure_storage url_launcher

import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class ClerkAuthService {
  final Dio _dio;
  final FlutterSecureStorage _storage;

  ClerkAuthService({
    required String publishableKey,
    required String frontendApi,
  })  : _storage = const FlutterSecureStorage(),
        _dio = Dio(BaseOptions(
          baseUrl: 'https://$frontendApi',
          headers: {'Authorization': 'Bearer $publishableKey'},
        ));

  Future<Map<String, dynamic>> signInWithEmail({
    required String email,
    required String password,
  }) async {
    final signInRes = await _dio.post('/v1/client/sign_ins', data: {
      'identifier': email,
      'password': password,
      'strategy': 'password',
    });

    final sessionId = signInRes.data['response']['created_session_id'];
    final token = signInRes.data['client']['sessions']
        .firstWhere((s) => s['id'] == sessionId)['last_active_token']['jwt'];

    await _storage.write(key: 'clerk_jwt', value: token);
    await _storage.write(key: 'clerk_session_id', value: sessionId);

    return signInRes.data;
  }

  Future<String?> getSessionToken() async {
    return _storage.read(key: 'clerk_jwt');
  }

  Future<void> signOut(String sessionId) async {
    await _dio.delete('/v1/client/sessions/$sessionId');
    await _storage.deleteAll();
  }
}
```

### 4.7 Clerk Custom Session Claims (Adding Data to JWT)

```json
// Clerk Dashboard → Sessions → Edit → Customize session token
{
  "userId": "{{user.id}}",
  "email": "{{user.primary_email_address}}",
  "firstName": "{{user.first_name}}",
  "orgId": "{{org.id}}",
  "orgRole": "{{org.role}}",
  "metadata": "{{user.public_metadata}}"
}
```

### 4.8 Clerk ENV Variables

```bash
# Next.js
NEXT_PUBLIC_CLERK_PUBLISHABLE_KEY=pk_test_xxxxxxxxxxxxxxxxxxxx
CLERK_SECRET_KEY=sk_test_xxxxxxxxxxxxxxxxxxxx
NEXT_PUBLIC_CLERK_SIGN_IN_URL=/sign-in
NEXT_PUBLIC_CLERK_SIGN_UP_URL=/sign-up
NEXT_PUBLIC_CLERK_SIGN_IN_FALLBACK_REDIRECT_URL=/dashboard
NEXT_PUBLIC_CLERK_SIGN_UP_FALLBACK_REDIRECT_URL=/onboarding

# Webhook
CLERK_WEBHOOK_SECRET=whsec_xxxxxxxxxxxxxxxxxxxxxxxxxxxx

# Backend (FastAPI, Axum, Go)
CLERK_JWKS_URL=https://your-app.clerk.accounts.dev/.well-known/jwks.json

# React Native / Expo
EXPO_PUBLIC_CLERK_PUBLISHABLE_KEY=pk_test_xxxxxxxxxxxxxxxxxxxx
EXPO_PUBLIC_API_URL=https://your-api.artkins.dev
```

---

## 5. AUTH0 — ENTERPRISE GRADE

**When to use Auth0**: Client-facing enterprise products that require SAML SSO, custom domains, fine-grained authorization, multi-tenancy with B2B, or audit log compliance requirements.

**Free tier**: 7,500 MAU · Social connections · MFA · No CC required · Forever

### 5.1 Next.js App Router — Auth0

```typescript
// npm install @auth0/nextjs-auth0

// auth0.ts
import { Auth0Client } from '@auth0/nextjs-auth0/server';

export const auth0 = new Auth0Client({
  domain: process.env.AUTH0_DOMAIN!,
  clientId: process.env.AUTH0_CLIENT_ID!,
  clientSecret: process.env.AUTH0_CLIENT_SECRET!,
  appBaseUrl: process.env.APP_URL!,
  secret: process.env.AUTH0_SECRET!,
});

// middleware.ts
import { auth0 } from '@/auth0';
import { NextRequest } from 'next/server';

export async function middleware(req: NextRequest) {
  return auth0.middleware(req);
}

// Server component
import { auth0 } from '@/auth0';

export async function Dashboard() {
  const session = await auth0.getSession();
  if (!session) return null;
  const { user } = session;
  return <div>Hello, {user.name}</div>;
}

// API Route
import { auth0 } from '@/auth0';

export async function GET() {
  const session = await auth0.getSession();
  if (!session) {
    return Response.json({ error: 'Unauthorized' }, { status: 401 });
  }
  const { user, tokenSet } = session;
  return Response.json({ userId: user.sub, email: user.email });
}
```

### 5.2 Python FastAPI — Auth0 JWT Verification

```python
# auth/auth0.py
# pip install PyJWT cryptography

import os
from jwt import PyJWKClient
import jwt
from fastapi import HTTPException, Depends
from fastapi.security import HTTPBearer

AUTH0_DOMAIN = os.environ["AUTH0_DOMAIN"]
AUTH0_AUDIENCE = os.environ["AUTH0_AUDIENCE"]

jwks_client = PyJWKClient(
    f"https://{AUTH0_DOMAIN}/.well-known/jwks.json",
    cache_keys=True,
    cache_jwk_set=True,
    lifespan=3600,
)

security = HTTPBearer()

async def require_auth(credentials = Depends(security)) -> dict:
    token = credentials.credentials
    try:
        signing_key = jwks_client.get_signing_key_from_jwt(token)
        payload = jwt.decode(
            token,
            signing_key.key,
            algorithms=["RS256"],
            audience=AUTH0_AUDIENCE,
            issuer=f"https://{AUTH0_DOMAIN}/",
            leeway=30,
        )
    except jwt.ExpiredSignatureError:
        raise HTTPException(status_code=401, detail="Token expired")
    except Exception:
        raise HTTPException(status_code=401, detail="Invalid token")
    return payload

def require_permission(permission: str):
    async def check(user: dict = Depends(require_auth)):
        permissions = user.get("permissions", [])
        if permission not in permissions:
            raise HTTPException(status_code=403, detail=f"Missing permission: {permission}")
        return user
    return check

@app.delete("/api/projects/{project_id}")
async def delete_project(
    project_id: str,
    user = Depends(require_permission("delete:projects"))
):
    pass
```

### 5.3 Auth0 ENV Variables

```bash
AUTH0_DOMAIN=your-app.us.auth0.com
AUTH0_CLIENT_ID=your_client_id
AUTH0_CLIENT_SECRET=your_client_secret
AUTH0_SECRET=your_32_char_random_secret  # openssl rand -hex 32
APP_URL=https://yourdomain.com

# Backend
AUTH0_DOMAIN=your-app.us.auth0.com
AUTH0_AUDIENCE=https://api.artkins.dev
```

---

## 6. SUPABASE AUTH — DATABASE-INTEGRATED AUTH

**When to use it**: When you're already using Supabase as your database. Auth is bundled — adding a separate auth provider is unnecessary complexity. Supabase Auth has Row Level Security (RLS) integration that is difficult to replicate with external auth providers.

**Free tier**: 50,000 MAU · OAuth providers · MFA · Forever (with Supabase free tier)

**Key advantage**: RLS policies can reference `auth.uid()` directly. Authentication is built into the database layer.

### 6.1 Next.js App Router — Supabase Auth

```typescript
// npm install @supabase/supabase-js @supabase/ssr

// lib/supabase/server.ts
import { createServerClient } from '@supabase/ssr';
import { cookies } from 'next/headers';

export function createSupabaseServerClient() {
  const cookieStore = cookies();
  return createServerClient(
    process.env.NEXT_PUBLIC_SUPABASE_URL!,
    process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY!,
    {
      cookies: {
        getAll() { return cookieStore.getAll(); },
        setAll(cookiesToSet) {
          cookiesToSet.forEach(({ name, value, options }) => {
            cookieStore.set(name, value, options);
          });
        },
      },
    }
  );
}

// lib/supabase/client.ts
import { createBrowserClient } from '@supabase/ssr';

export function createSupabaseBrowserClient() {
  return createBrowserClient(
    process.env.NEXT_PUBLIC_SUPABASE_URL!,
    process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY!
  );
}

// middleware.ts
import { createServerClient } from '@supabase/ssr';
import { NextRequest, NextResponse } from 'next/server';

export async function middleware(req: NextRequest) {
  let response = NextResponse.next({ request: req });

  const supabase = createServerClient(
    process.env.NEXT_PUBLIC_SUPABASE_URL!,
    process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY!,
    {
      cookies: {
        getAll() { return req.cookies.getAll(); },
        setAll(cookiesToSet) {
          cookiesToSet.forEach(({ name, value, options }) => {
            req.cookies.set(name, value);
            response.cookies.set(name, value, options);
          });
        },
      },
    }
  );

  const { data: { user } } = await supabase.auth.getUser();

  if (!user && req.nextUrl.pathname.startsWith('/dashboard')) {
    return NextResponse.redirect(new URL('/login', req.url));
  }

  return response;
}
```

```typescript
// app/dashboard/page.tsx — Server Component with RLS
import { createSupabaseServerClient } from '@/lib/supabase/server';
import { redirect } from 'next/navigation';

export default async function DashboardPage() {
  const supabase = createSupabaseServerClient();
  const { data: { user } } = await supabase.auth.getUser();

  if (!user) redirect('/login');

  // RLS is active — only returns rows where user_id = auth.uid()
  const { data: projects } = await supabase
    .from('projects')
    .select('*')
    .order('created_at', { ascending: false });

  return <ProjectList projects={projects ?? []} />;
}
```

```typescript
// app/login/page.tsx
'use client';
import { createSupabaseBrowserClient } from '@/lib/supabase/client';

export default function LoginPage() {
  const supabase = createSupabaseBrowserClient();

  const signInWithGoogle = async () => {
    await supabase.auth.signInWithOAuth({
      provider: 'google',
      options: { redirectTo: `${location.origin}/auth/callback` },
    });
  };

  const signInWithEmail = async (email: string, password: string) => {
    const { error } = await supabase.auth.signInWithPassword({ email, password });
    if (error) console.error(error.message);
  };

  const signUpWithEmail = async (email: string, password: string) => {
    const { error } = await supabase.auth.signUp({
      email,
      password,
      options: { emailRedirectTo: `${location.origin}/auth/callback` },
    });
  };

  const signInWithMagicLink = async (email: string) => {
    await supabase.auth.signInWithOtp({
      email,
      options: { emailRedirectTo: `${location.origin}/auth/callback` },
    });
  };

  return (/* ... your UI ... */);
}
```

```typescript
// app/auth/callback/route.ts — Required for OAuth and magic link flows
import { createSupabaseServerClient } from '@/lib/supabase/server';
import { NextRequest, NextResponse } from 'next/server';

export async function GET(req: NextRequest) {
  const { searchParams } = new URL(req.url);
  const code = searchParams.get('code');
  const next = searchParams.get('next') ?? '/dashboard';

  if (code) {
    const supabase = createSupabaseServerClient();
    const { error } = await supabase.auth.exchangeCodeForSession(code);
    if (!error) {
      return NextResponse.redirect(new URL(next, req.url));
    }
  }

  return NextResponse.redirect(new URL('/error', req.url));
}
```

### 6.2 Supabase Row Level Security (The Killer Feature)

```sql
-- Enable RLS on your table
ALTER TABLE projects ENABLE ROW LEVEL SECURITY;

-- Users can only see their own projects
CREATE POLICY "Users see own projects"
  ON projects FOR SELECT
  USING (user_id = auth.uid());

-- Users can only insert their own rows
CREATE POLICY "Users insert own projects"
  ON projects FOR INSERT
  WITH CHECK (user_id = auth.uid());

-- Users can only update/delete their own projects
CREATE POLICY "Users manage own projects"
  ON projects FOR UPDATE USING (user_id = auth.uid());
CREATE POLICY "Users delete own projects"
  ON projects FOR DELETE USING (user_id = auth.uid());

-- Org-level policy (multi-tenant)
CREATE POLICY "Org members see org projects"
  ON projects FOR SELECT
  USING (
    org_id IN (
      SELECT org_id FROM org_members
      WHERE user_id = auth.uid()
    )
  );
```

```python
# Supabase Auth JWT verification in Python FastAPI
import os
import jwt
from jwt import PyJWKClient
from fastapi import HTTPException, Depends
from fastapi.security import HTTPBearer

SUPABASE_URL = os.environ["SUPABASE_URL"]
SUPABASE_JWKS_URL = f"{SUPABASE_URL}/auth/v1/.well-known/jwks.json"

jwks_client = PyJWKClient(SUPABASE_JWKS_URL, cache_keys=True, cache_jwk_set=True)
security = HTTPBearer()

async def get_supabase_user(credentials = Depends(security)) -> dict:
    token = credentials.credentials
    try:
        signing_key = jwks_client.get_signing_key_from_jwt(token)
        payload = jwt.decode(
            token,
            signing_key.key,
            algorithms=["RS256"],
            options={"verify_aud": False},
            leeway=30,
        )
    except Exception:
        raise HTTPException(status_code=401, detail="Invalid token")
    return payload
```

### 6.3 Supabase Auth ENV Variables

```bash
NEXT_PUBLIC_SUPABASE_URL=https://xxxx.supabase.co
NEXT_PUBLIC_SUPABASE_ANON_KEY=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

# Backend (service role — NEVER expose to frontend)
SUPABASE_SERVICE_ROLE_KEY=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
SUPABASE_URL=https://xxxx.supabase.co
SUPABASE_JWT_SECRET=your_jwt_secret   # Supabase → Settings → API
```

---

## 7. AUTH.JS (NEXTAUTH v5) — OPEN SOURCE, FRAMEWORK-NATIVE

**When to use it**: When you want full control, self-hosted, no vendor lock-in. Auth.js supports 70+ providers and works across Next.js, SvelteKit, Express, and Astro.

**Free tier**: Unlimited — open source. Host it yourself.

### 7.1 Next.js App Router — Auth.js v5

```typescript
// npm install next-auth@beta @auth/prisma-adapter

// auth.ts
import NextAuth from 'next-auth';
import { PrismaAdapter } from '@auth/prisma-adapter';
import Google from 'next-auth/providers/google';
import GitHub from 'next-auth/providers/github';
import Resend from 'next-auth/providers/resend';
import Credentials from 'next-auth/providers/credentials';
import { db } from '@/lib/db';
import bcrypt from 'bcryptjs';
import { z } from 'zod';

export const { handlers, auth, signIn, signOut } = NextAuth({
  adapter: PrismaAdapter(db),

  providers: [
    Google({
      clientId: process.env.GOOGLE_CLIENT_ID,
      clientSecret: process.env.GOOGLE_CLIENT_SECRET,
    }),
    GitHub({
      clientId: process.env.GITHUB_ID,
      clientSecret: process.env.GITHUB_SECRET,
    }),
    Resend({
      apiKey: process.env.RESEND_API_KEY,
      from: 'auth@artkins.dev',
    }),
    Credentials({
      name: 'credentials',
      credentials: {
        email: { label: 'Email', type: 'email' },
        password: { label: 'Password', type: 'password' },
      },
      async authorize(credentials) {
        const schema = z.object({
          email: z.string().email(),
          password: z.string().min(8),
        });
        const parsed = schema.safeParse(credentials);
        if (!parsed.success) return null;

        const user = await db.user.findUnique({
          where: { email: parsed.data.email },
        });
        if (!user?.hashedPassword) return null;

        const valid = await bcrypt.compare(parsed.data.password, user.hashedPassword);
        if (!valid) return null;

        return user;
      },
    }),
  ],

  session: {
    strategy: 'jwt',
    maxAge: 30 * 24 * 60 * 60, // 30 days
  },

  callbacks: {
    async jwt({ token, user, trigger, session }) {
      if (user) {
        token.userId = user.id;
        token.role = user.role ?? 'user';
      }
      if (trigger === 'update' && session) {
        token.role = session.role;
      }
      return token;
    },
    async session({ session, token }) {
      if (token) {
        session.user.id = token.userId as string;
        session.user.role = token.role as string;
      }
      return session;
    },
  },

  pages: {
    signIn: '/sign-in',
    signOut: '/sign-out',
    error: '/auth/error',
    verifyRequest: '/auth/verify',
  },
});

// types/next-auth.d.ts
declare module 'next-auth' {
  interface Session {
    user: {
      id: string;
      role: string;
      email: string;
      name?: string;
      image?: string;
    };
  }
}
```

```typescript
// app/api/auth/[...nextauth]/route.ts
import { handlers } from '@/auth';
export const { GET, POST } = handlers;

// middleware.ts
import { auth } from '@/auth';
import { NextResponse } from 'next/server';

export default auth((req) => {
  const isLoggedIn = !!req.auth;
  const isProtected = req.nextUrl.pathname.startsWith('/dashboard');
  if (isProtected && !isLoggedIn) {
    return NextResponse.redirect(new URL('/sign-in', req.url));
  }
});

// Server component
import { auth } from '@/auth';

export default async function Page() {
  const session = await auth();
  if (!session?.user) redirect('/sign-in');
  return <div>Hello {session.user.name}</div>;
}

// Server actions
import { signIn, signOut } from '@/auth';

export async function signInWithGoogle() {
  await signIn('google', { redirectTo: '/dashboard' });
}

export async function handleSignOut() {
  await signOut({ redirectTo: '/' });
}
```

```prisma
// prisma/schema.prisma — Auth.js required tables
model Account {
  id                String  @id @default(cuid())
  userId            String
  type              String
  provider          String
  providerAccountId String
  refresh_token     String? @db.Text
  access_token      String? @db.Text
  expires_at        Int?
  token_type        String?
  scope             String?
  id_token          String? @db.Text
  session_state     String?
  user              User    @relation(fields: [userId], references: [id], onDelete: Cascade)

  @@unique([provider, providerAccountId])
}

model Session {
  id           String   @id @default(cuid())
  sessionToken String   @unique
  userId       String
  expires      DateTime
  user         User     @relation(fields: [userId], references: [id], onDelete: Cascade)
}

model User {
  id             String    @id @default(cuid())
  name           String?
  email          String?   @unique
  emailVerified  DateTime?
  image          String?
  hashedPassword String?
  role           String    @default("user")
  accounts       Account[]
  sessions       Session[]
  createdAt      DateTime  @default(now())
}

model VerificationToken {
  identifier String
  token      String   @unique
  expires    DateTime
  @@unique([identifier, token])
}
```

### 7.2 Auth.js ENV Variables

```bash
AUTH_SECRET=your_32_char_random_secret  # openssl rand -base64 32
AUTH_URL=https://yourdomain.com

GOOGLE_CLIENT_ID=xxxx.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=GOCSPX-xxxx
GITHUB_ID=Iv1.xxxx
GITHUB_SECRET=xxxx

DATABASE_URL=postgresql://...
```

---

## 8. STACK AUTH — OPEN-SOURCE CLERK ALTERNATIVE

**When to use it**: When you want Clerk-level DX (prebuilt React components, organization support) with full self-hosting capability. Same component API surface as Clerk, but you own the data.

**Free tier**: Unlimited on self-hosted · Open source (MIT)

### 8.1 Next.js App Router — Stack Auth

```typescript
// npm install @stackframe/stack

// stack.ts
import 'server-only';
import { StackServerApp } from '@stackframe/stack';

export const stackServerApp = new StackServerApp({
  tokenStore: 'nextjs-cookie',
});

// middleware.ts
import { stackServerApp } from '@/stack';
export const { middleware } = stackServerApp;

// app/handler/[...stack]/page.tsx
import { StackHandler } from '@stackframe/stack';
import { stackServerApp } from '@/stack';

export default function Handler(props: any) {
  return <StackHandler app={stackServerApp} {...props} />;
}

// Server component
import { stackServerApp } from '@/stack';

export default async function Page() {
  const user = await stackServerApp.getUser({ or: 'redirect' });
  return <div>Hello {user.displayName}</div>;
}

// Client component
'use client';
import { useUser } from '@stackframe/stack';

export function Profile() {
  const user = useUser({ or: 'redirect' });
  return <div>{user.primaryEmail}</div>;
}
```

### 8.2 Stack Auth ENV Variables

```bash
NEXT_PUBLIC_STACK_PROJECT_ID=your_project_id
NEXT_PUBLIC_STACK_PUBLISHABLE_CLIENT_KEY=pck_xxxx
STACK_SECRET_SERVER_KEY=ssk_xxxx
NEXT_PUBLIC_STACK_URL=https://api.stack-auth.com
```

---

## 9. KINDE — SIMPLEST LAUNCH ALTERNATIVE

**When to use it**: You want Clerk-level simplicity but more free organizations (3 vs Clerk's 1). Setup is under 10 minutes.

**Free tier**: 10,500 MAU · Unlimited social · MFA · 3 free organizations · No CC · Forever

### 9.1 Next.js App Router — Kinde

```typescript
// npm install @kinde-oss/kinde-auth-nextjs

// middleware.ts
import { withAuth } from '@kinde-oss/kinde-auth-nextjs/middleware';
export default withAuth;

export const config = {
  matcher: ['/dashboard/:path*', '/api/protected/:path*'],
};

// app/api/auth/[kindeAuth]/route.ts
import { handleAuth } from '@kinde-oss/kinde-auth-nextjs/server';
export const GET = handleAuth();

// Server component
import { getKindeServerSession } from '@kinde-oss/kinde-auth-nextjs/server';

export default async function Page() {
  const { getUser, isAuthenticated } = getKindeServerSession();
  const user = await getUser();
  const authed = await isAuthenticated();

  if (!authed) redirect('/api/auth/login');

  return <div>Hello {user?.given_name}</div>;
}

// Client component
'use client';
import { useKindeBrowserClient } from '@kinde-oss/kinde-auth-nextjs';
import { LoginLink, LogoutLink } from '@kinde-oss/kinde-auth-nextjs/components';

export function Navbar() {
  const { user, isAuthenticated, isLoading } = useKindeBrowserClient();
  if (isLoading) return <Spinner />;

  return isAuthenticated ? (
    <div>
      <span>{user?.email}</span>
      <LogoutLink>Sign Out</LogoutLink>
    </div>
  ) : (
    <LoginLink>Sign In</LoginLink>
  );
}
```

### 9.2 Kinde ENV Variables

```bash
KINDE_CLIENT_ID=your_kinde_client_id
KINDE_CLIENT_SECRET=your_kinde_client_secret
KINDE_ISSUER_URL=https://your-app.kinde.com
KINDE_SITE_URL=https://yourdomain.com
KINDE_POST_LOGOUT_REDIRECT_URL=https://yourdomain.com
KINDE_POST_LOGIN_REDIRECT_URL=https://yourdomain.com/dashboard
```

---

## 10. BETTER AUTH — TYPESCRIPT-NATIVE MODERN LIBRARY

**When to use it**: Self-hosted auth library with a modern TypeScript API, plugin-based architecture, and no vendor dependency.

**Free tier**: Unlimited — self-hosted open source · MIT license

### 10.1 Next.js App Router — Better Auth

```typescript
// npm install better-auth

// lib/auth.ts
import { betterAuth } from 'better-auth';
import { prismaAdapter } from 'better-auth/adapters/prisma';
import { nextCookies } from 'better-auth/next-js';
import { organization } from 'better-auth/plugins';
import { twoFactor } from 'better-auth/plugins/two-factor';
import { db } from './db';

export const auth = betterAuth({
  database: prismaAdapter(db, { provider: 'postgresql' }),

  plugins: [
    nextCookies(),
    organization(),
    twoFactor({ issuer: 'Lyncxs Industries' }),
  ],

  emailAndPassword: {
    enabled: true,
    requireEmailVerification: true,
    sendResetPassword: async ({ user, url }) => {
      await resend.emails.send({
        from: 'auth@artkins.dev',
        to: user.email,
        subject: 'Reset your password',
        react: <PasswordResetEmail resetUrl={url} />,
      });
    },
  },

  socialProviders: {
    google: {
      clientId: process.env.GOOGLE_CLIENT_ID!,
      clientSecret: process.env.GOOGLE_CLIENT_SECRET!,
    },
    github: {
      clientId: process.env.GITHUB_ID!,
      clientSecret: process.env.GITHUB_SECRET!,
    },
  },

  session: {
    expiresIn: 60 * 60 * 24 * 7,
    updateAge: 60 * 60 * 24,
    cookieCache: { enabled: true, maxAge: 60 * 5 },
  },

  trustedOrigins: [process.env.APP_URL!],
});

export type Session = typeof auth.$Infer.Session;
export type User = typeof auth.$Infer.Session.user;
```

```typescript
// app/api/auth/[...all]/route.ts
import { auth } from '@/lib/auth';
import { toNextJsHandler } from 'better-auth/next-js';
export const { GET, POST } = toNextJsHandler(auth.handler);

// Server component
import { auth } from '@/lib/auth';
import { headers } from 'next/headers';

export default async function Page() {
  const session = await auth.api.getSession({ headers: await headers() });
  if (!session) redirect('/sign-in');
  return <div>Hello {session.user.name}</div>;
}
```

```typescript
// lib/auth-client.ts
import { createAuthClient } from 'better-auth/react';
import { organizationClient } from 'better-auth/client/plugins';
import { twoFactorClient } from 'better-auth/client/plugins';

export const authClient = createAuthClient({
  baseURL: process.env.NEXT_PUBLIC_APP_URL!,
  plugins: [organizationClient(), twoFactorClient()],
});

export const { signIn, signOut, signUp, useSession, useActiveOrganization } = authClient;
```

### 10.2 Better Auth ENV Variables

```bash
BETTER_AUTH_SECRET=your_32_char_random_secret   # openssl rand -hex 32
BETTER_AUTH_URL=https://yourdomain.com
DATABASE_URL=postgresql://...
GOOGLE_CLIENT_ID=xxxx.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=GOCSPX-xxxx
```

---

## 11. WORKOS — ENTERPRISE SSO/SAML

**When to use it**: B2B SaaS where your enterprise clients need SSO (Okta, Microsoft Entra ID, Google Workspace).

**Free tier**: AuthKit — 1,000,000 MAU forever · SSO — paid ($149/connection/month)

### 11.1 Next.js App Router — WorkOS AuthKit

```typescript
// npm install @workos-inc/authkit-nextjs

// middleware.ts
import { authkitMiddleware } from '@workos-inc/authkit-nextjs';

export default authkitMiddleware({
  middlewareAuth: {
    enabled: true,
    unauthenticatedPaths: ['/', '/pricing', '/blog(.*)'],
  },
});

// Server component
import { withAuth } from '@workos-inc/authkit-nextjs';

export default withAuth(async function Page({ user }) {
  return <div>Hello {user.firstName}</div>;
});

// Get sign in URL
import { getSignInUrl } from '@workos-inc/authkit-nextjs';

export async function SignInButton() {
  const signInUrl = await getSignInUrl();
  return <a href={signInUrl}>Sign In</a>;
}
```

### 11.2 WorkOS ENV Variables

```bash
WORKOS_API_KEY=sk_live_xxxx
WORKOS_CLIENT_ID=client_xxxx
NEXT_PUBLIC_WORKOS_REDIRECT_URI=https://yourdomain.com/callback
WORKOS_COOKIE_PASSWORD=your_32_char_password  # openssl rand -base64 32
```

---

## 12. PLATFORM-SPECIFIC SETUP GUIDES

### 12.1 Next.js App Router — Recommended File Structure

```
/app/
  (auth)/
    sign-in/
      [[...sign-in]]/page.tsx     ← Clerk: catch-all for hosted pages
    sign-up/
      [[...sign-up]]/page.tsx
  (app)/                          ← Protected routes group
    dashboard/page.tsx
    settings/page.tsx
  api/
    auth/
      [...nextauth]/route.ts      ← Auth.js handler
      [kindeAuth]/route.ts        ← Kinde handler
      [...all]/route.ts           ← Better Auth handler
    webhooks/
      clerk/route.ts              ← Clerk webhook handler

/lib/
  auth/
    index.ts
    jwt.ts                        ← DIY JWT (section 3)
    session.ts                    ← Server-side sessions

/middleware.ts                    ← Auth middleware (root level, critical)
```

### 12.2 Python FastAPI — Auth Module Structure

```
/app/
  auth/
    __init__.py        ← export get_current_user, require_role, etc.
    clerk.py           ← Clerk JWT verification
    supabase.py        ← Supabase JWT verification
    auth0.py           ← Auth0 JWT verification
    jwt.py             ← DIY JWT (only if needed)
    models.py          ← TokenPayload Pydantic model
  routers/
    auth.py            ← /api/auth/* routes (if needed)
  middleware/
    cors.py            ← CORS config (must allow Authorization header)
```

```python
# app/auth/models.py
from pydantic import BaseModel

class TokenPayload(BaseModel):
    sub: str
    email: str | None = None
    org_id: str | None = None
    roles: list[str] = []
    exp: int | None = None
```

### 12.3 React Native (Expo) — Auth Module Structure

```
/app/
  (auth)/
    sign-in.tsx
    sign-up.tsx
    forgot-password.tsx
  (app)/              ← Protected tabs/screens
    _layout.tsx       ← Check auth state here

/hooks/
  useAuth.ts
  useProtectedFetch.ts

/services/
  auth/
    clerk.ts
    supabase.ts
```

```typescript
// app/(app)/_layout.tsx — Protect all app screens
import { useAuth } from '@clerk/clerk-expo';
import { Redirect, Stack } from 'expo-router';

export default function AppLayout() {
  const { isSignedIn, isLoaded } = useAuth();

  if (!isLoaded) return null;
  if (!isSignedIn) return <Redirect href="/sign-in" />;

  return <Stack />;
}
```

### 12.4 Flutter — Auth Pattern

```dart
// lib/services/auth_service.dart
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

abstract class AuthService {
  Future<AuthUser?> signInWithEmail(String email, String password);
  Future<AuthUser?> signInWithGoogle();
  Future<void> signOut();
  Future<String?> getAccessToken();
}

// lib/utils/secure_storage.dart
class SecureTokenStorage {
  static const _storage = FlutterSecureStorage(
    aOptions: AndroidOptions(encryptedSharedPreferences: true),
    iOptions: IOSOptions(
      accessibility: KeychainAccessibility.first_unlock_this_device,
    ),
  );

  static Future<void> saveToken(String key, String value) =>
    _storage.write(key: key, value: value);

  static Future<String?> getToken(String key) =>
    _storage.read(key: key);

  static Future<void> clearAll() => _storage.deleteAll();
}
```

### 12.5 LynxJS — Auth via Backend API

LynxJS has no native auth SDK. All auth goes through your backend:

```typescript
// services/auth.ts in LynxJS app
const API_BASE = process.env.API_BASE_URL;

export async function signIn(email: string, password: string): Promise<{ token: string }> {
  const res = await fetch(`${API_BASE}/api/auth/sign-in`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  });

  if (!res.ok) throw new Error('Sign in failed');
  return res.json();
}

// Store the returned token and use it in all subsequent API calls
// as Authorization: Bearer <token>
```

### 12.6 Rust Axum — JWT Auth Middleware

```rust
// src/middleware/auth.rs
// cargo add jsonwebtoken tower axum-extra

use axum::{
    extract::{Request, State},
    http::{StatusCode, HeaderMap},
    middleware::Next,
    response::Response,
    Json,
};
use jsonwebtoken::{decode, decode_header, DecodingKey, Validation, Algorithm};
use serde::{Deserialize, Serialize};
use std::sync::Arc;
use tokio::sync::RwLock;
use serde_json::Value;

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct Claims {
    pub sub: String,
    pub email: Option<String>,
    pub org_id: Option<String>,
    pub exp: usize,
    pub iat: usize,
    pub jti: Option<String>,
}

#[derive(Clone)]
pub struct JwksState {
    pub keys: Arc<RwLock<Vec<Jwk>>>,
    pub jwks_url: String,
}

#[derive(Debug, Deserialize, Clone)]
pub struct Jwk {
    pub kty: String,
    pub kid: Option<String>,
    pub n: Option<String>,
    pub e: Option<String>,
}

#[derive(Deserialize)]
struct JwksResponse {
    keys: Vec<Jwk>,
}

impl JwksState {
    pub async fn new(jwks_url: String) -> anyhow::Result<Self> {
        let state = Self {
            keys: Arc::new(RwLock::new(Vec::new())),
            jwks_url,
        };
        state.refresh().await?;
        Ok(state)
    }

    pub async fn refresh(&self) -> anyhow::Result<()> {
        let response = reqwest::get(&self.jwks_url)
            .await?
            .json::<JwksResponse>()
            .await?;
        let mut keys = self.keys.write().await;
        *keys = response.keys;
        Ok(())
    }
}

pub async fn auth_middleware(
    State(jwks): State<Arc<JwksState>>,
    mut req: Request,
    next: Next,
) -> Result<Response, StatusCode> {
    let token = extract_bearer_token(req.headers())
        .ok_or(StatusCode::UNAUTHORIZED)?;

    let claims = verify_jwt(&token, &jwks)
        .await
        .map_err(|_| StatusCode::UNAUTHORIZED)?;

    req.extensions_mut().insert(claims);

    Ok(next.run(req).await)
}

async fn verify_jwt(token: &str, jwks: &JwksState) -> anyhow::Result<Claims> {
    let header = decode_header(token)?;

    let keys = jwks.keys.read().await;
    let jwk = keys.iter().find(|k| {
        header.kid.as_deref() == k.kid.as_deref() || header.kid.is_none()
    }).ok_or_else(|| anyhow::anyhow!("No matching key found"))?;

    let n = jwk.n.as_deref().ok_or_else(|| anyhow::anyhow!("Missing RSA n"))?;
    let e = jwk.e.as_deref().ok_or_else(|| anyhow::anyhow!("Missing RSA e"))?;
    let decoding_key = DecodingKey::from_rsa_components(n, e)?;

    let mut validation = Validation::new(Algorithm::RS256);
    validation.set_issuer(&[&std::env::var("JWT_ISSUER")?]);
    validation.leeway = 30;

    let token_data = decode::<Claims>(token, &decoding_key, &validation)?;
    Ok(token_data.claims)
}

fn extract_bearer_token(headers: &HeaderMap) -> Option<String> {
    headers
        .get("authorization")
        .and_then(|v| v.to_str().ok())
        .and_then(|v| v.strip_prefix("Bearer "))
        .map(|v| v.to_string())
}

// main.rs
#[tokio::main]
async fn main() {
    let jwks = Arc::new(
        JwksState::new(std::env::var("CLERK_JWKS_URL").unwrap())
            .await
            .expect("Failed to fetch JWKS")
    );

    // Refresh JWKS every hour
    let jwks_clone = jwks.clone();
    tokio::spawn(async move {
        let mut interval = tokio::time::interval(std::time::Duration::from_secs(3600));
        loop {
            interval.tick().await;
            let _ = jwks_clone.refresh().await;
        }
    });

    let app = Router::new()
        .route("/api/projects", get(list_projects))
        .route_layer(axum::middleware::from_fn_with_state(jwks.clone(), auth_middleware))
        .with_state(jwks);

    axum::serve(
        tokio::net::TcpListener::bind("0.0.0.0:8080").await.unwrap(),
        app
    ).await.unwrap();
}

async fn list_projects(Extension(claims): Extension<Claims>) -> impl IntoResponse {
    let user_id = claims.sub;
    Json(vec![] as Vec<Value>)
}
```

---

## 13. BACKEND JWT VERIFICATION IN EVERY LANGUAGE

The pattern is provider-agnostic. Every modern auth provider uses RS256 with a JWKS endpoint.

### 13.1 JWKS URLs by Provider

```
Provider          JWKS Endpoint
──────────────────────────────────────────────────────────────────────────────
Clerk             https://<frontend-api>.clerk.accounts.dev/.well-known/jwks.json
Auth0             https://<your-domain>.us.auth0.com/.well-known/jwks.json
Supabase          https://<project-ref>.supabase.co/auth/v1/.well-known/jwks.json
Kinde             https://<your-domain>.kinde.com/.well-known/jwks.json
WorkOS            https://api.workos.com/auth/jwks/<client-id>
Stack Auth        https://api.stack-auth.com/.well-known/jwks.json
Better Auth       https://yourdomain.com/api/auth/jwks  (self-hosted)
Auth.js           Uses NEXTAUTH_SECRET (HS256 default — use JWT_PRIVATE_KEY for RS256)
```

### 13.2 Python FastAPI — Universal JWKS Verifier

```python
# auth/universal.py — Works with any OIDC/JWT provider

import os
import logging
from functools import lru_cache
from jwt import PyJWKClient
import jwt
from fastapi import HTTPException, Depends
from fastapi.security import HTTPBearer

logger = logging.getLogger(__name__)

@lru_cache(maxsize=1)
def get_jwks_client() -> PyJWKClient:
    jwks_url = os.environ["AUTH_JWKS_URL"]
    return PyJWKClient(
        jwks_url,
        cache_keys=True,
        cache_jwk_set=True,
        lifespan=3600,
    )

security = HTTPBearer(auto_error=False)

async def get_current_user(credentials = Depends(security)) -> dict:
    if not credentials:
        raise HTTPException(status_code=401, detail="Not authenticated")

    token = credentials.credentials

    try:
        client = get_jwks_client()
        signing_key = client.get_signing_key_from_jwt(token)

        payload = jwt.decode(
            token,
            signing_key.key,
            algorithms=["RS256"],
            options={
                "verify_aud": bool(os.environ.get("AUTH_AUDIENCE")),
                "require": ["exp", "sub"],
            },
            audience=os.environ.get("AUTH_AUDIENCE"),
            issuer=os.environ.get("AUTH_ISSUER"),
            leeway=30,
        )

        return payload

    except jwt.ExpiredSignatureError:
        raise HTTPException(status_code=401, detail="Token expired")
    except jwt.InvalidSignatureError:
        logger.warning("Invalid JWT signature attempt")
        raise HTTPException(status_code=401, detail="Invalid token signature")
    except jwt.InvalidTokenError as e:
        logger.warning(f"JWT validation failed: {e}")
        raise HTTPException(status_code=401, detail="Invalid token")
    except Exception as e:
        logger.error(f"Auth error: {e}")
        raise HTTPException(status_code=500, detail="Authentication error")

def require_role(*roles: str):
    async def check(user: dict = Depends(get_current_user)) -> dict:
        user_roles = user.get("roles") or user.get("permissions", [])
        if not any(r in user_roles for r in roles):
            raise HTTPException(
                status_code=403,
                detail=f"Required role: {' or '.join(roles)}"
            )
        return user
    return check
```

### 13.3 Go — JWT Verification

```go
// auth/jwt.go
// go get github.com/golang-jwt/jwt/v5
// go get github.com/lestrrat-go/jwx/v2

package auth

import (
    "context"
    "net/http"
    "strings"
    "sync"
    "time"

    "github.com/golang-jwt/jwt/v5"
    "github.com/lestrrat-go/jwx/v2/jwk"
)

type Claims struct {
    Sub    string   `json:"sub"`
    Email  string   `json:"email"`
    OrgID  string   `json:"org_id"`
    Roles  []string `json:"roles"`
    jwt.RegisteredClaims
}

type JwksCache struct {
    mu          sync.RWMutex
    set         jwk.Set
    lastFetched time.Time
    url         string
}

func NewJwksCache(url string) *JwksCache {
    return &JwksCache{url: url}
}

func (c *JwksCache) GetKey(ctx context.Context, kid string) (interface{}, error) {
    c.mu.RLock()
    stale := time.Since(c.lastFetched) > time.Hour
    c.mu.RUnlock()

    if stale {
        c.mu.Lock()
        defer c.mu.Unlock()
        if time.Since(c.lastFetched) > time.Hour {
            set, err := jwk.Fetch(ctx, c.url)
            if err != nil {
                return nil, err
            }
            c.set = set
            c.lastFetched = time.Now()
        }
    } else {
        c.mu.RLock()
        defer c.mu.RUnlock()
    }

    key, found := c.set.LookupKeyID(kid)
    if !found {
        return nil, jwt.ErrTokenSignatureInvalid
    }

    var rawKey interface{}
    if err := key.Raw(&rawKey); err != nil {
        return nil, err
    }
    return rawKey, nil
}

func AuthMiddleware(jwks *JwksCache) gin.HandlerFunc {
    return func(c *gin.Context) {
        authHeader := c.GetHeader("Authorization")
        if authHeader == "" || !strings.HasPrefix(authHeader, "Bearer ") {
            c.AbortWithStatusJSON(http.StatusUnauthorized, gin.H{"error": "missing token"})
            return
        }

        tokenStr := strings.TrimPrefix(authHeader, "Bearer ")

        token, err := jwt.ParseWithClaims(
            tokenStr,
            &Claims{},
            func(token *jwt.Token) (interface{}, error) {
                if _, ok := token.Method.(*jwt.SigningMethodRSA); !ok {
                    return nil, jwt.ErrTokenSignatureInvalid
                }
                kid, _ := token.Header["kid"].(string)
                return jwks.GetKey(c.Request.Context(), kid)
            },
            jwt.WithLeeway(30*time.Second),
            jwt.WithIssuer(os.Getenv("AUTH_ISSUER")),
            jwt.WithValidMethods([]string{"RS256"}),
        )

        if err != nil || !token.Valid {
            c.AbortWithStatusJSON(http.StatusUnauthorized, gin.H{"error": "invalid token"})
            return
        }

        claims, ok := token.Claims.(*Claims)
        if !ok {
            c.AbortWithStatusJSON(http.StatusUnauthorized, gin.H{"error": "invalid claims"})
            return
        }

        c.Set("userId", claims.Sub)
        c.Set("email", claims.Email)
        c.Set("orgId", claims.OrgID)
        c.Set("roles", claims.Roles)

        c.Next()
    }
}
```

---

## 14. SOCIAL OAUTH2 PROVIDERS MAP

### 14.1 Provider Setup Quick Reference

```
PROVIDER       CONSOLE URL                        Lyncxs Industries USE CASE
──────────────────────────────────────────────────────────────────────────────
Google         console.cloud.google.com           Primary social — universal
GitHub         github.com/settings/developers     Developer tools (RUWA, Foundrie)
Microsoft      portal.azure.com                   Enterprise clients
LinkedIn       developer.linkedin.com             B2B, professional products
Discord        discord.com/developers             Community/gaming tools
Twitter/X      developer.twitter.com              Social products
Apple          developer.apple.com                iOS (mandatory if app is on App Store)
```

### 14.2 OAuth2 Redirect URIs by Platform

```
Platform           Redirect URI format
──────────────────────────────────────────────────────────────────────────────
Next.js (Clerk)    https://yourdomain.com/api/auth/callback/google
Next.js (Auth.js)  https://yourdomain.com/api/auth/callback/google
Supabase           https://xxxx.supabase.co/auth/v1/callback
React Native       myapp://oauth-native-callback  (deep link)
Flutter            com.artkins.myapp://oauth-callback  (custom scheme)
Localhost          http://localhost:3000/api/auth/callback/google
```

### 14.3 Apple Sign In (Required for iOS App Store)

If your app has social login and targets iOS, Apple Sign In is **mandatory** — Apple will reject your App Store submission without it.

```
Apple requires additional setup:
  1. Apple Developer account ($99/year)
  2. Enable Sign in with Apple in your App ID
  3. Create a Service ID (for web) or use App ID directly (for native)
  4. Generate private key (.p8 file) for JWT signing
  5. Configure Key ID, Team ID, and Client ID in your auth provider
```

---

## 15. MFA AND ADVANCED SECURITY

### 15.1 MFA Support by Provider

| Provider | TOTP | SMS | Email OTP | WebAuthn/Passkey | Hardware Key |
|---|---|---|---|---|---|
| Clerk | ✅ | ✅ | ✅ | ✅ | ✅ |
| Auth0 | ✅ | ✅ | ✅ | ✅ | ✅ (paid) |
| Supabase | ✅ | ❌ | ✅ (OTP) | ❌ (roadmap) | ❌ |
| Auth.js | ❌ | ❌ | ❌ (DIY) | ❌ (DIY) | ❌ |
| Better Auth | ✅ | ❌ | ✅ | ✅ (plugin) | ❌ |
| Kinde | ✅ | ✅ | ✅ | ✅ | ❌ |
| WorkOS | ✅ | ✅ | ✅ | ✅ | ❌ |

### 15.2 TOTP (Authenticator App) — Clerk Example

```typescript
'use client';
import { useUser } from '@clerk/nextjs';

export function MFASettings() {
  const { user } = useUser();

  const enableTOTP = async () => {
    const totp = await user?.createTOTP();
    // totp.qrCode → show to user as QR code image
    // totp.secret → manual entry fallback
    // After user enters verification code:
    await user?.verifyTOTP({ code: '123456' });
  };

  return (
    <div>
      <p>TOTP: {user?.totpEnabled ? 'Enabled' : 'Disabled'}</p>
      <button onClick={enableTOTP}>Enable Authenticator App</button>
    </div>
  );
}
```

### 15.3 Passkeys (WebAuthn) — Clerk

```typescript
'use client';
import { useUser } from '@clerk/nextjs';

export function PasskeySettings() {
  const { user } = useUser();

  const registerPasskey = async () => {
    await user?.createPasskey();
    // Clerk handles WebAuthn's complexity
  };

  return (
    <button onClick={registerPasskey}>
      Add Passkey (Face ID / Fingerprint)
    </button>
  );
}
```

---

## 16. MULTI-TENANT / ORGANIZATION AUTH

### 16.1 Organization Auth by Provider

| Provider | Built-in Orgs | Org Roles | RBAC | Best For |
|---|---|---|---|---|
| Clerk | ✅ Native | Custom roles | ✅ | B2B SaaS, teams |
| Auth0 | ✅ (Organizations) | Member/Admin | ✅ | Enterprise B2B |
| WorkOS | ✅ Native | ✅ | ✅ SSO per org | Enterprise |
| Kinde | ✅ Native | Custom roles | ✅ | Teams |
| Better Auth | ✅ Plugin | Custom roles | ✅ | Self-hosted |
| Supabase | ❌ DIY | DIY | DIY | If using Supabase DB |
| Auth.js | ❌ DIY | DIY | DIY | Advanced use |

### 16.2 Clerk Organizations — Full Implementation

```typescript
// Server: get org context
import { auth } from '@clerk/nextjs/server';

export async function getOrgContext() {
  const { userId, orgId, orgRole, orgSlug } = await auth();
  return { userId, orgId, orgRole, orgSlug };
}

export async function requireOrgAdmin() {
  const { userId, orgId, orgRole } = await auth();
  if (!userId) throw new Error('Not authenticated');
  if (!orgId) throw new Error('No organization selected');
  if (orgRole !== 'org:admin') throw new Error('Admin required');
  return { userId, orgId };
}

// API Route with org context
export async function GET() {
  const { userId, orgId } = await auth();
  if (!userId || !orgId) {
    return Response.json({ error: 'Unauthorized' }, { status: 401 });
  }

  const projects = await db.project.findMany({ where: { orgId } });
  return Response.json(projects);
}
```

```typescript
// Client: org switcher
'use client';
import { OrganizationSwitcher, useOrganization } from '@clerk/nextjs';

export function OrgNav() {
  const { organization } = useOrganization();

  return (
    <div>
      <OrganizationSwitcher
        hidePersonal={true}
        afterSelectOrganizationUrl="/dashboard"
        afterCreateOrganizationUrl="/onboarding"
      />
      {organization && (
        <span className="text-sm text-gray-500">
          {organization.membersCount} members
        </span>
      )}
    </div>
  );
}
```

```typescript
// Invite members to organization
'use server';
import { clerkClient, auth } from '@clerk/nextjs/server';

export async function inviteOrgMember(email: string, role: 'org:admin' | 'org:member') {
  const { orgId } = await auth();
  if (!orgId) throw new Error('No organization');

  const client = await clerkClient();
  await client.organizations.createOrganizationInvitation({
    organizationId: orgId,
    emailAddress: email,
    role,
    redirectUrl: `${process.env.APP_URL}/accept-invite`,
  });
}
```

### 16.3 Multi-Tenant DB Pattern (Works with Any Auth Provider)

```prisma
// prisma/schema.prisma
model Organization {
  id         String      @id @default(cuid())
  clerkOrgId String      @unique
  name       String
  slug       String      @unique
  plan       String      @default("free")
  members    OrgMember[]
  projects   Project[]
  createdAt  DateTime    @default(now())
}

model OrgMember {
  id     String       @id @default(cuid())
  orgId  String
  userId String
  role   String       @default("member")
  org    Organization @relation(fields: [orgId], references: [id])

  @@unique([orgId, userId])
}

model Project {
  id    String       @id @default(cuid())
  orgId String
  name  String
  org   Organization @relation(fields: [orgId], references: [id])
}
```

```typescript
// lib/db/org.ts
export async function getOrgProjects(orgId: string) {
  return db.project.findMany({ where: { orgId } });
}

export async function requireOrgMember(
  userId: string,
  orgId: string,
  minRole: 'viewer' | 'member' | 'admin' = 'member'
): Promise<void> {
  const membership = await db.orgMember.findUnique({
    where: { orgId_userId: { orgId, userId } },
  });

  if (!membership) {
    throw new Error('Not a member of this organization');
  }

  const ROLE_HIERARCHY = { viewer: 0, member: 1, admin: 2 };
  if (ROLE_HIERARCHY[membership.role as keyof typeof ROLE_HIERARCHY] < ROLE_HIERARCHY[minRole]) {
    throw new Error(`Requires ${minRole} role`);
  }
}
```

---

## 17. SESSION MANAGEMENT AND TOKEN STRATEGY

### 17.1 Access Token + Refresh Token Flow (Standard)

```
INITIAL SIGN IN:
  User → Provider login UI
  Provider → access token (15min) + refresh token (30 days)
  Store: access token in memory or short httpOnly cookie
         refresh token in httpOnly cookie (path: /api/auth/refresh)

AUTHENTICATED REQUEST:
  Client sends: Authorization: Bearer <access_token>
  Backend verifies access token locally (no DB call if not blacklisted)
  Fast path: O(1) for valid tokens within expiry window

ACCESS TOKEN EXPIRES (15min):
  Browser detects 401 response
  Client silently calls POST /api/auth/refresh (sends refresh cookie)
  Server rotates: invalidates old refresh token, issues new pair
  Client retries original request with new access token
  User notices nothing

LOGOUT:
  Client calls POST /api/auth/logout
  Server adds access token JTI to Redis blocklist (TTL = remaining access token lifetime)
  Server deletes refresh token from DB / invalidates refresh token
  Server clears all auth cookies
  User is logged out

SUSPICIOUS ACTIVITY:
  Old refresh token presented after rotation (theft detection)
  Server invalidates entire token family
  User must re-authenticate
  Security event logged
```

### 17.2 Token Storage Decision Chart

```
APP TYPE              STORAGE               NOTES
──────────────────────────────────────────────────────────────────────────────
Next.js SSR           httpOnly cookie       Automatic with Clerk/Auth.js/Supabase
React SPA             httpOnly cookie       Set via API route, not localStorage
React Native (Expo)   expo-secure-store     Encrypted, device-locked storage
Flutter               flutter_secure_storage Keychain (iOS) / Keystore (Android)
CLI tool (RUWA)       OS keychain           Via keyring crate in Rust or keyring npm
Server-to-server      Env var / secrets     No user session needed — use API keys
```

### 17.3 The Cookie Configuration Reference

```typescript
// Access token cookie
response.cookies.set('session', token, {
  httpOnly: true,         // Not accessible to JS — prevents XSS token theft
  secure: true,           // HTTPS only
  sameSite: 'lax',        // 'strict' breaks OAuth redirects; 'lax' is recommended
  maxAge: 60 * 60 * 24 * 7,
  path: '/',
});

// Refresh token cookie — narrower scope
response.cookies.set('refresh_token', refreshToken, {
  httpOnly: true,
  secure: true,
  sameSite: 'lax',
  maxAge: 60 * 60 * 24 * 30,
  path: '/api/auth/refresh',  // ONLY sent to refresh endpoint
});

// SameSite values:
// 'strict'  → safest from CSRF; breaks OAuth redirects from external sites
// 'lax'     → balances security and functionality; recommended
// 'none'    → allows cross-site sending; requires secure: true; avoid unless needed
```

---

## 18. PRODUCTION CHECKLIST

### Authentication Setup

```
PROVIDER CONFIGURATION
  [ ] Auth provider account created (Clerk / Auth0 / Supabase)
  [ ] Production credentials set (NOT development keys)
  [ ] Allowed origins configured in auth provider dashboard
  [ ] Redirect URIs registered for all environments (staging, prod)
  [ ] Custom domain configured (auth.artkins.dev) — removes branding
  [ ] Webhook endpoint registered + secret stored in ENV

SOCIAL PROVIDERS
  [ ] All OAuth apps created in respective developer consoles
  [ ] OAuth credentials are PRODUCTION credentials (not test credentials)
  [ ] Redirect URIs include production domain (and staging if separate)
  [ ] Apple Sign In configured if any iOS App Store submission planned

JWT / TOKEN CONFIGURATION
  [ ] Access tokens: 15-minute expiry maximum
  [ ] Refresh tokens: 7–30 day expiry with rotation enabled
  [ ] Algorithm: RS256 (never HS256 for distributed systems, never 'none')
  [ ] httpOnly, Secure, SameSite=Lax on all auth cookies
  [ ] Token blocklist implemented (Redis) for logout invalidation
  [ ] JTI included in every access token
  [ ] Clock skew tolerance: 30 seconds on all verifiers
  [ ] Tokens NEVER logged — only sub (user ID) logged
```

### Backend Security

```
API ENDPOINTS
  [ ] Authorization header extracted server-side only
  [ ] JWKS URL cached — not fetched on every request
  [ ] Algorithm pinned to RS256 — reject 'none' and HS256
  [ ] Issuer (iss) validated on every token
  [ ] Audience (aud) validated if set
  [ ] Rate limiting on all auth endpoints (/sign-in, /sign-up, /refresh)
  [ ] Account lockout after N failed login attempts

CORS
  [ ] CORS allows Authorization header
  [ ] Allowed origins: only your domains (no wildcards in production)
  [ ] Credentials: true if using cookies

DATABASE
  [ ] Users table has clerkId (or provider ID) as unique field
  [ ] Webhook handler is idempotent (can process same event twice safely)
  [ ] user.deleted webhook: soft delete, never hard delete
  [ ] Foreign keys on orgId enforced where multi-tenant

MONITORING
  [ ] Alert on: spike in 401 responses (possible brute force)
  [ ] Alert on: refresh token reuse detected (token theft)
  [ ] Alert on: JWKS fetch failure (all auth will fail if JWKS unavailable)
  [ ] Log: successful auth events with userId (no PII beyond user ID)
  [ ] Log: failed auth attempts with IP and timestamp
```

### Mobile-Specific

```
  [ ] Tokens stored in SecureStore (Expo) / flutter_secure_storage (Flutter)
  [ ] NEVER AsyncStorage/SharedPreferences for tokens
  [ ] PKCE flow confirmed for all OAuth providers on mobile
  [ ] Biometric unlock configured for sensitive operations
  [ ] Certificate pinning considered for high-security apps
  [ ] Token refresh handled in background (user not interrupted)
  [ ] Apple Sign In included if targeting iOS App Store
```

---

## 19. ENVIRONMENT VARIABLES REFERENCE

```bash
# ─── CLERK ─────────────────────────────────────────────────────────────────
NEXT_PUBLIC_CLERK_PUBLISHABLE_KEY=pk_live_xxxxxxxxxxxxxxxxxxxx
CLERK_SECRET_KEY=sk_live_xxxxxxxxxxxxxxxxxxxx
CLERK_WEBHOOK_SECRET=whsec_xxxxxxxxxxxxxxxxxxxxxxxxxxxx
NEXT_PUBLIC_CLERK_SIGN_IN_URL=/sign-in
NEXT_PUBLIC_CLERK_SIGN_UP_URL=/sign-up
NEXT_PUBLIC_CLERK_SIGN_IN_FALLBACK_REDIRECT_URL=/dashboard
NEXT_PUBLIC_CLERK_SIGN_UP_FALLBACK_REDIRECT_URL=/onboarding

# React Native / Expo
EXPO_PUBLIC_CLERK_PUBLISHABLE_KEY=pk_live_xxxxxxxxxxxxxxxxxxxx

# Backend JWT verification (FastAPI, Axum, Go)
CLERK_JWKS_URL=https://your-app.clerk.accounts.dev/.well-known/jwks.json

# ─── AUTH0 ──────────────────────────────────────────────────────────────────
AUTH0_DOMAIN=your-app.us.auth0.com
AUTH0_CLIENT_ID=xxxxxxxxxxxxxxxxxxxxxxxxxxxx
AUTH0_CLIENT_SECRET=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
AUTH0_SECRET=64_char_random_string                # openssl rand -hex 32
AUTH0_AUDIENCE=https://api.artkins.dev

# Backend
AUTH0_JWKS_URL=https://your-app.us.auth0.com/.well-known/jwks.json

# ─── SUPABASE AUTH ──────────────────────────────────────────────────────────
NEXT_PUBLIC_SUPABASE_URL=https://xxxx.supabase.co
NEXT_PUBLIC_SUPABASE_ANON_KEY=eyJhbGciOiJIUzI1...    # Safe to expose
SUPABASE_SERVICE_ROLE_KEY=eyJhbGciOiJIUzI1...         # NEVER expose — full DB access
SUPABASE_JWT_SECRET=your_jwt_secret

# Backend
SUPABASE_JWKS_URL=https://xxxx.supabase.co/auth/v1/.well-known/jwks.json

# ─── AUTH.JS (NextAuth v5) ──────────────────────────────────────────────────
AUTH_SECRET=64_char_random_string                  # openssl rand -base64 32
AUTH_URL=https://yourdomain.com

# ─── KINDE ──────────────────────────────────────────────────────────────────
KINDE_CLIENT_ID=xxxxxxxxxxxxxxxxxxxx
KINDE_CLIENT_SECRET=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
KINDE_ISSUER_URL=https://your-app.kinde.com
KINDE_SITE_URL=https://yourdomain.com
KINDE_POST_LOGOUT_REDIRECT_URL=https://yourdomain.com
KINDE_POST_LOGIN_REDIRECT_URL=https://yourdomain.com/dashboard

# ─── BETTER AUTH ────────────────────────────────────────────────────────────
BETTER_AUTH_SECRET=64_char_random_string
BETTER_AUTH_URL=https://yourdomain.com

# ─── WORKOS ─────────────────────────────────────────────────────────────────
WORKOS_API_KEY=sk_live_xxxxxxxxxxxxxxxxxxxx
WORKOS_CLIENT_ID=client_xxxxxxxxxxxxxxxxxxxx
NEXT_PUBLIC_WORKOS_REDIRECT_URI=https://yourdomain.com/callback
WORKOS_COOKIE_PASSWORD=64_char_random_string

# ─── SOCIAL PROVIDERS ────────────────────────────────────────────────────────
GOOGLE_CLIENT_ID=xxxx.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=GOCSPX-xxxx
GITHUB_ID=Iv1.xxxx
GITHUB_SECRET=xxxx
APPLE_ID=com.artkins.yourapp                      # Service ID for web OAuth
APPLE_TEAM_ID=XXXXXXXXXX
APPLE_KEY_ID=XXXXXXXXXX
APPLE_PRIVATE_KEY="-----BEGIN PRIVATE KEY-----\n..."

# ─── DIY JWT (only if implementing yourself — see Section 3) ─────────────────
JWT_PRIVATE_KEY="-----BEGIN RSA PRIVATE KEY-----\n...\n-----END RSA PRIVATE KEY-----"
JWT_PUBLIC_KEY="-----BEGIN PUBLIC KEY-----\n...\n-----END PUBLIC KEY-----"
# Escape newlines: awk 'NF {sub(/\r/, ""); printf "%s\\n",$0;}' private.pem

# ─── SHARED ─────────────────────────────────────────────────────────────────
APP_URL=https://yourdomain.com
REDIS_URL=redis://localhost:6379              # Required for token blocklist (DIY JWT)
```

---

## SUMMARY — THE Lyncxs Industries AUTH DECISION TREE

```
Starting a new project?
  → Clerk (10K MAU free, best DX, polished components, webhooks included)

Already using Supabase as your database?
  → Supabase Auth (bundled, RLS integration, 50K MAU free)

Self-hosted / open source required?
  → Auth.js v5 (most mature, 70+ providers, framework-native)
  → Better Auth (TypeScript-native, plugin-based, modern API)

Expecting > 10K users fast, need to stay free?
  → WorkOS AuthKit (1M MAU free for social/email)

Need multi-tenant orgs, want Clerk-level DX, with self-host option?
  → Stack Auth (open-source Clerk alternative)

Building B2B SaaS where enterprise clients need SSO?
  → WorkOS (SSO is their core product; $149/connection/month)
  → Auth0 (full platform, established vendor)

Just need something running in 5 minutes for a new project?
  → Kinde (10,500 MAU free, 3 free orgs, simplest possible setup)

Need to implement custom JWT yourself (API-only service)?
  → See Section 3. RS256 + short-lived access + refresh rotation + Redis blocklist.

Python FastAPI / Rust Axum / Go backend?
  → Any managed provider works — verify JWT against their JWKS endpoint.
  → See Section 13 for language-specific implementations.

Mobile app (React Native, Flutter)?
  → Clerk (React Native: @clerk/clerk-expo)
  → Auth.js (no native SDK — backend handles OAuth, mobile calls backend)
  → Supabase (official Flutter SDK: supabase_flutter)
```

---

*Generated June 2026 · Lyncxs Industries / Lyncxs Industries · Nakuru, Kenya*\
*Cross-references: AGENTIC-SECURITY.md · EMAIL-SERVICES-GUIDE.md · MULTI-LANGUAGE-ARCHITECTURE-PROGRAMMING.md*\
*Covers: Clerk v6, Auth0, Supabase Auth, Auth.js v5, Stack Auth, Kinde, Better Auth, WorkOS*\
*JWT failure modes sourced from: OWASP JWT Security Cheatsheet, RFC 8725 (JWT Best Practices), PortSwigger Web Security Academy*

---

# VOLUME II — IDENTITY ENGINEERING

> **What Volume I covers**: Which auth service to pick, how JWT works, how to wire up Clerk/Auth0/Supabase in your stack.\
> **What Volume II covers**: How identity works at every layer of every system you will ever build — across platforms, token types, authorization models, secrets, PKI, and AI agents. This is the architectural reference.

---

## 20. AUTH BY PLATFORM — COMPLETE COVERAGE

Every system asks the same three questions differently:

```
Who are you?            → Authentication
What can you do?        → Authorization
How do we prove it?     → Identity & Trust model
```

The answers change completely depending on what you're building.

---

### 20.1 Web Applications

Already covered in depth in Sections 1–19. Quick summary:

```
Authentication:   Email+Password | Social OAuth | Passkeys | Magic Links | MFA
Session model:    httpOnly cookie (SameSite=Lax) OR in-memory access token
Token model:      JWT access token (15 min) + Refresh token (30 days, httpOnly)
Authorization:    RBAC → see Section 21
Security:         CSRF · XSS · Clickjacking · Open redirects · Cookie theft
```

| App Type | Auth Focus | Extra Concern |
|---|---|---|
| SaaS (B2C) | Smooth signup, social OAuth, magic links | Conversion rate |
| B2B SaaS | SSO/SAML, org-level permissions, provisioning | Enterprise compliance |
| E-commerce | Guest checkout, saved payment methods, SCA | Fraud prevention |
| Admin panel | 2FA mandatory, IP allowlist, audit logs | Insider threat |
| Public API | OAuth 2.0 authorization code flow | Developer UX |
| Marketplace | Two-sided identity, KYC | Trust & safety |

---

### 20.2 Mobile Apps — Android & iOS Deep Dive

Mobile auth is fundamentally different from web. No cookies. Hardware-backed secure storage. Biometrics. No browser.

```
Core Rules:
  1. PKCE mandatory — no client_secret on mobile (can be extracted from binary)
  2. Tokens in hardware-backed storage. NEVER plain SharedPreferences/AsyncStorage
  3. Access token: in memory only. Refresh token: in Keychain/Keystore
  4. Biometrics gate sensitive ops — do not replace auth
  5. Certificate pinning for high-security apps
  6. App attestation to prove the app is genuine and untampered
```

#### Android

**Android Keystore System**

```kotlin
val keyGenerator = KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, "AndroidKeyStore")
keyGenerator.init(
    KeyGenParameterSpec.Builder("artkins_auth_key",
        KeyProperties.PURPOSE_ENCRYPT or KeyProperties.PURPOSE_DECRYPT)
        .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
        .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
        .setUserAuthenticationRequired(true)
        .setUserAuthenticationParameters(300, KeyProperties.AUTH_BIOMETRIC_STRONG)
        .setKeySize(256)
        .build()
)
val secretKey = keyGenerator.generateKey()
// This key NEVER leaves the secure hardware element
```

**BiometricPrompt for Gating Sensitive Actions**

```kotlin
val biometricPrompt = BiometricPrompt(this, mainExecutor,
    object : BiometricPrompt.AuthenticationCallback() {
        override fun onAuthenticationSucceeded(result: BiometricPrompt.AuthenticationResult) {
            val cipher = result.cryptoObject?.cipher
            val decryptedToken = cipher?.doFinal(encryptedToken)
            performSensitiveAction(String(decryptedToken!!))
        }
    })

val promptInfo = BiometricPrompt.PromptInfo.Builder()
    .setTitle("Confirm your identity")
    .setSubtitle("Authorizing payment of KES 5,000")
    .setNegativeButtonText("Use PIN instead")
    .setAllowedAuthenticators(BiometricManager.Authenticators.BIOMETRIC_STRONG)
    .build()

biometricPrompt.authenticate(promptInfo, cryptoObject)
```

**EncryptedSharedPreferences (Refresh Token Storage)**

```kotlin
val masterKey = MasterKey.Builder(context).setKeyScheme(MasterKey.KeyScheme.AES256_GCM).build()
val encryptedPrefs = EncryptedSharedPreferences.create(
    context, "artkins_secure_prefs", masterKey,
    EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
    EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM
)
encryptedPrefs.edit().putString("refresh_token", refreshToken).apply()
```

**Play Integrity API (App Attestation) — replaces SafetyNet**

```kotlin
val integrityManager = IntegrityManagerFactory.create(context)
val nonce = Base64.encode(yourServerNonce.toByteArray(), Base64.URL_SAFE or Base64.NO_WRAP)
integrityManager.requestIntegrityToken(
    IntegrityTokenRequest.builder()
        .setNonce(String(nonce))
        .setCloudProjectNumber(YOUR_CLOUD_PROJECT_NUMBER)
        .build()
).addOnSuccessListener { response ->
    api.verifyDeviceIntegrity(response.token()) // Backend verifies via Google API
}
```

**Certificate Pinning (OkHttp)**

```kotlin
// Pin public key fingerprint — survives cert renewal
val client = OkHttpClient.Builder()
    .certificatePinner(CertificatePinner.Builder()
        .add("api.artkins.dev", "sha256/AAAA...=")  // Current cert
        .add("api.artkins.dev", "sha256/BBBB...=")  // Backup cert
        .build())
    .build()
// openssl s_client -connect api.artkins.dev:443 | openssl x509 -pubkey -noout |
// openssl pkey -pubin -outform der | openssl dgst -sha256 -binary | base64
```

#### iOS

**Keychain Services**

```swift
func storeToken(_ token: String, forKey key: String) throws {
    let tokenData = token.data(using: .utf8)!
    let access = SecAccessControlCreateWithFlags(nil,
        kSecAttrAccessibleWhenUnlockedThisDeviceOnly, .biometryAny, nil)!
    let query: [String: Any] = [
        kSecClass as String: kSecClassGenericPassword,
        kSecAttrAccount as String: key,
        kSecValueData as String: tokenData,
        kSecAttrAccessControl as String: access
    ]
    SecItemDelete(query as CFDictionary)
    let status = SecItemAdd(query as CFDictionary, nil)
    guard status == errSecSuccess else { throw KeychainError.failed(status) }
}
```

**LocalAuthentication (FaceID/TouchID)**

```swift
let context = LAContext()
let success = try await context.evaluatePolicy(
    .deviceOwnerAuthenticationWithBiometrics,
    localizedReason: "Confirm transfer of KES 10,000"
)
```

**DCAppAttest (App Attestation)**

```swift
let service = DCAppAttestService.shared
service.generateKey { keyId, error in
    guard let keyId = keyId else { return }
    let clientDataHash = Data(SHA256.hash(data: Data(yourNonce.utf8)))
    service.attestKey(keyId, clientDataHash: clientDataHash) { attestation, error in
        api.verifyAttestation(attestation: attestation)
    }
}
```

#### React Native / Expo (Cross-Platform)

```typescript
import * as SecureStore from 'expo-secure-store';
import * as LocalAuthentication from 'expo-local-authentication';

// Store refresh token — maps to Keychain (iOS) / EncryptedSharedPreferences (Android)
await SecureStore.setItemAsync('refresh_token', token, {
    requireAuthentication: true,
    keychainAccessible: SecureStore.WHEN_UNLOCKED_THIS_DEVICE_ONLY
});

// Gate sensitive action with biometrics
const result = await LocalAuthentication.authenticateAsync({
    promptMessage: 'Confirm transfer of KES 10,000',
    cancelLabel: 'Cancel',
    disableDeviceFallback: false
});
if (result.success) performTransfer();
```

**PKCE Flow in Expo — Clerk handles automatically**

```typescript
import { useOAuth } from '@clerk/clerk-expo';
import * as WebBrowser from 'expo-web-browser';
WebBrowser.maybeCompleteAuthSession();

function GoogleSignIn() {
    const { startOAuthFlow } = useOAuth({ strategy: 'oauth_google' });
    const onPress = async () => {
        const { createdSessionId, setActive } = await startOAuthFlow();
        // PKCE S256 generated and verified internally — you don't touch it
        if (createdSessionId) await setActive!({ session: createdSessionId });
    };
    return <Button onPress={onPress}>Sign in with Google</Button>;
}
```

**Offline Auth Pattern**

```typescript
interface OfflineAuthState {
    userId: string;
    displayName: string;
    offlineExpiry: number; // Hard timestamp limit
}

async function checkOfflineAuth(): Promise<boolean> {
    const stored = await SecureStore.getItemAsync('offline_auth');
    if (!stored) return false;
    const state: OfflineAuthState = JSON.parse(stored);
    if (Date.now() > state.offlineExpiry) {
        await SecureStore.deleteItemAsync('offline_auth');
        return false;
    }
    const bio = await LocalAuthentication.authenticateAsync({ promptMessage: 'Unlock your account' });
    return bio.success;
}
```

---

### 20.3 API Authentication

APIs authenticate **machines**, not humans.

#### API Keys — Design Them Properly

```
Format: {prefix}_{env}_{random_bytes}_{checksum}
Example: lnx_live_a8f2b9c4d1e3f7g8h9_ab12

Benefits:
  Prefix → grep-able in logs and code search
  Env    → prevents test keys hitting production
  Random → 32 bytes = 256 bits entropy
  Checksum → detects common typos before hitting API
```

```typescript
// NEVER store plain API keys — store the hash
import { createHash, randomBytes, timingSafeEqual } from 'crypto';

function hashApiKey(key: string): string {
    return createHash('sha256').update(key).digest('hex');
}

// On creation: return raw key ONCE, store hash + prefix
const rawKey = `lnx_live_${randomBytes(32).toString('hex')}`;
await db.apiKey.create({
    keyHash: hashApiKey(rawKey),
    keyPrefix: rawKey.slice(0, 12),
    scopes: ['read:transactions'],
    expiresAt: new Date(Date.now() + 365 * 24 * 60 * 60 * 1000),
});
return rawKey; // Show ONCE — never again

// On verification
async function verifyApiKey(rawKey: string) {
    return db.apiKey.findOne({ where: { keyHash: hashApiKey(rawKey), revokedAt: null } });
}
```

**Scope enforcement**

```typescript
function requireScope(requiredScope: string) {
    return async (req: Request, res: Response, next: NextFunction) => {
        const apiKey = await verifyApiKey(extractApiKey(req));
        if (!apiKey) return res.status(401).json({ error: 'Invalid API key' });
        if (!apiKey.scopes.includes(requiredScope)) {
            return res.status(403).json({ error: `Scope '${requiredScope}' required` });
        }
        req.apiKey = apiKey;
        next();
    };
}

app.get('/api/transactions', requireScope('read:transactions'), handler);
```

#### HMAC Signed Requests

Request integrity — tampering detection. Pattern used by AWS, Stripe, GitHub webhooks.

```typescript
// Sender signs
function signRequest(method: string, path: string, body: string, secret: string): string {
    const timestamp = Math.floor(Date.now() / 1000).toString();
    const payload = `${timestamp}\n${method.toUpperCase()}\n${path}\n${body}`;
    const sig = createHmac('sha256', secret).update(payload).digest('hex');
    return `t=${timestamp},v1=${sig}`;
}

// Receiver verifies
function verifyRequest(method: string, path: string, body: string,
    sigHeader: string, secret: string): boolean {
    const parts = Object.fromEntries(sigHeader.split(',').map(p => p.split('=') as [string, string]));
    const timestamp = parseInt(parts['t'], 10);
    // Replay protection: reject requests older than 5 minutes
    if (Math.abs(Date.now() / 1000 - timestamp) > 300) throw new Error('Replay attack detected');
    const payload = `${timestamp}\n${method.toUpperCase()}\n${path}\n${body}`;
    const expected = createHmac('sha256', secret).update(payload).digest('hex');
    return timingSafeEqual(Buffer.from(parts['v1']), Buffer.from(expected));
}
```

#### OAuth 2.0 Client Credentials — Machine-to-Machine

```typescript
class ServiceTokenCache {
    private token: string | null = null;
    private expiresAt: number = 0;

    async getToken(): Promise<string> {
        if (this.token && Date.now() < this.expiresAt - 60_000) return this.token;
        const resp = await fetch('https://auth.artkins.dev/oauth/token', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: new URLSearchParams({
                grant_type: 'client_credentials',
                client_id: CLIENT_ID,
                client_secret: CLIENT_SECRET,
                scope: 'read:transactions write:reports',
            }),
        });
        const { access_token, expires_in } = await resp.json();
        this.token = access_token;
        this.expiresAt = Date.now() + expires_in * 1000;
        return this.token;
    }
}
```

#### Mutual TLS (mTLS) for APIs

Both client and server present certificates. Neither can be impersonated.

```nginx
# Nginx: require client certificate
server {
    listen 443 ssl;
    ssl_certificate /etc/ssl/server.crt;
    ssl_certificate_key /etc/ssl/server.key;
    ssl_client_certificate /etc/ssl/ca.crt;
    ssl_verify_client on;

    location /api/internal/ {
        proxy_pass http://backend:3000;
        proxy_set_header X-Client-Cert-DN $ssl_client_s_dn;
    }
}
```

```typescript
// Node.js client with client certificate
import https from 'https';
import fs from 'fs';
const agent = new https.Agent({
    cert: fs.readFileSync('/etc/ssl/client.crt'),
    key: fs.readFileSync('/etc/ssl/client.key'),
    ca: fs.readFileSync('/etc/ssl/ca.crt'),
});
await fetch('https://internal-api.artkins.dev/api/internal/data', { agent });
```

---

### 20.4 Internal Microservices

**The critical rule: Never use user auth between services.**

Service A authenticates as Service A — not as the user who triggered the flow. User context is carried as a *claim*, not as the identity.

```
Architecture:
  User → API Gateway (validates user JWT)
    ↓
  Service A (user context in payload)
    ↓ (authenticates as Service A, passes userId in body)
  Service B (verifies Service A's identity, trusts userId claim)
```

**JWT Between Services**

```typescript
function issueServiceToken(caller: string, target: string): string {
    return sign({ iss: caller, aud: target, sub: caller },
        PRIVATE_KEY, { algorithm: 'RS256', expiresIn: '5m' });
}

function verifyServiceToken(token: string, expectedIssuer: string) {
    return verify(token, PUBLIC_KEY, {
        algorithms: ['RS256'],
        audience: 'notification-service',
        issuer: expectedIssuer,
    });
}
```

**SPIFFE / SPIRE (Production Grade)**

```
SPIFFE gives every service a cryptographic identity without shared secrets.

SPIFFE ID:     spiffe://artkins.dev/ns/production/sa/payment-service
SVID types:    X.509-SVID (short-lived TLS cert, hourly rotation)
               JWT-SVID (short-lived JWT, SPIFFE ID as subject)

SPIRE components:
  SPIRE Server → acts as CA, manages identity issuance
  SPIRE Agent  → runs on each node, attests workloads
  Workload     → gets SVID via SPIRE Agent UNIX socket
```

```bash
# Fetch JWT-SVID for a workload
spire-agent api fetch jwt --audience "notification-service" \
    --socketPath /tmp/spire-agent/public/api.sock
```

**Kubernetes Workload Identity**

```yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: payment-service
  namespace: production
  annotations:
    eks.amazonaws.com/role-arn: arn:aws:iam::123456789:role/payment-service-role
```

```typescript
// Token auto-projected at /var/run/secrets/kubernetes.io/serviceaccount/token
// AWS SDK uses IRSA to exchange SA token for AWS credentials automatically
```

---

### 20.5 CLI Tools

CLI auth: no browser session, terminal-only UX, tokens must survive restarts, secrets stored securely on dev machine.

#### Device Authorization Flow (RFC 8628)

The standard for CLIs that act on behalf of a user. GitHub CLI, Stripe CLI, Docker Desktop all use this.

```
Flow:
  1. CLI → POST /device/code → { device_code, user_code, verification_uri, interval }
  2. CLI → prints: "Visit https://auth.artkins.dev/activate  Code: ABCD-1234"
  3. User opens URL in browser, enters code, authorizes
  4. CLI polls POST /oauth/token every {interval} seconds
  5. Token returned once user approves
  6. CLI stores tokens in OS keychain
```

```typescript
async function authenticateCLI() {
    const { device_code, user_code, verification_uri, interval, expires_in } =
        await fetch('https://auth.artkins.dev/oauth/device/code', {
            method: 'POST',
            body: new URLSearchParams({ client_id: CLI_CLIENT_ID, scope: 'read:projects' }),
        }).then(r => r.json());

    console.log(`\nOpen: ${verification_uri}\nCode: ${user_code}\n`);

    const deadline = Date.now() + expires_in * 1000;
    let pollInterval = interval;

    while (Date.now() < deadline) {
        await sleep(pollInterval * 1000);
        const data = await fetch('https://auth.artkins.dev/oauth/token', {
            method: 'POST',
            body: new URLSearchParams({
                grant_type: 'urn:ietf:params:oauth:grant-type:device_code',
                client_id: CLI_CLIENT_ID, device_code,
            }),
        }).then(r => r.json());

        if (data.access_token) {
            await storeTokens(data.access_token, data.refresh_token);
            console.log('✓ Authenticated');
            return data;
        }
        if (data.error === 'slow_down') pollInterval += 5;
        if (data.error === 'access_denied') throw new Error('Authorization denied');
        if (data.error === 'expired_token') throw new Error('Code expired');
    }
}
```

**Secure CLI Token Storage**

```typescript
import keytar from 'keytar'; // Maps to: macOS Keychain, Windows Credential Manager, Linux Secret Service
const SERVICE = 'artkins-cli';
await keytar.setPassword(SERVICE, 'refresh_token', token);
const token = await keytar.getPassword(SERVICE, 'refresh_token');
await keytar.deletePassword(SERVICE, 'refresh_token');
```

**PAT (Personal Access Tokens) Design**

```
Format: lnx_pat_{random_hex}
Store:  SHA256 hash only — return raw once
Fields: keyHash, keyPrefix, lastFour, scopes[], expiresAt, lastUsedAt, lastUsedIp, revokedAt
```

**SSH Keys for CLI Auth**

```bash
# Generate ed25519 (preferred over RSA)
ssh-keygen -t ed25519 -C "don@artkins.dev" -f ~/.ssh/artkins

# ~/.ssh/config — multiple identities (GitSwitch pattern)
Host github.com-personal
    HostName github.com
    User git
    IdentityFile ~/.ssh/id_personal
    IdentitiesOnly yes

Host github.com-work
    HostName github.com
    User git
    IdentityFile ~/.ssh/id_work
    IdentitiesOnly yes
```

---

### 20.6 Desktop Applications

```
OS → Credential Manager:
  Windows → Windows Credential Manager (DPAPI)
  macOS   → macOS Keychain
  Linux   → GNOME Keyring / KWallet (libsecret)
  All     → keytar (Node.js cross-platform abstraction)
```

**Native Browser OAuth — The Correct Pattern**

```typescript
// Electron: NEVER use WebView for OAuth — use system browser
import { shell } from 'electron';

async function startOAuth() {
    const codeVerifier = generatePKCEVerifier();
    const codeChallenge = await generatePKCEChallenge(codeVerifier);
    const state = randomBytes(16).toString('hex');

    const authUrl = new URL('https://auth.artkins.dev/oauth/authorize');
    authUrl.searchParams.set('client_id', CLIENT_ID);
    authUrl.searchParams.set('redirect_uri', 'artkins://oauth/callback'); // Custom URI scheme
    authUrl.searchParams.set('response_type', 'code');
    authUrl.searchParams.set('code_challenge', codeChallenge);
    authUrl.searchParams.set('code_challenge_method', 'S256');
    authUrl.searchParams.set('state', state);

    await shell.openExternal(authUrl.toString()); // Opens SYSTEM browser — trusted
}

app.setAsDefaultProtocolClient('artkins');
app.on('open-url', async (_, url) => {
    const { code, state } = parseCallbackUrl(url);
    // Exchange code for tokens, store in keytar
});
```

---

### 20.7 AI Agents & MCP

> Expanded in full in Section 29. Summary:

```
Identity questions for agents:
  Who authorized this agent?
  What exactly can it do? (scope)
  For how long? (expiry)
  Can it delegate to sub-agents?
  How are all actions audited?

Token model:
  User grants scoped capability token to agent
  Scoped: only specific resources, specific actions
  Short-lived: 1–4 hours max
  Non-delegatable by default
  All actions logged with agentId, userId, timestamp, resourceId
```

---

### 20.8 Payment Systems & M-Pesa / Daraja API

#### M-Pesa Daraja API Authentication

Safaricom Daraja uses OAuth 2.0 Client Credentials. Required for every API call.

```typescript
const DARAJA_BASE = process.env.MPESA_ENV === 'production'
    ? 'https://api.safaricom.co.ke'
    : 'https://sandbox.safaricom.co.ke';

let tokenCache: { access_token: string; expires_in: number; cachedAt: number } | null = null;

async function getDarajaAccessToken(): Promise<string> {
    if (tokenCache && Date.now() < tokenCache.cachedAt + (tokenCache.expires_in - 60) * 1000) {
        return tokenCache.access_token; // Return cached (60s buffer)
    }

    const credentials = Buffer.from(
        `${process.env.MPESA_CONSUMER_KEY}:${process.env.MPESA_CONSUMER_SECRET}`
    ).toString('base64');

    const response = await fetch(
        `${DARAJA_BASE}/oauth/v1/generate?grant_type=client_credentials`,
        { method: 'GET', headers: { Authorization: `Basic ${credentials}` } }
    );
    if (!response.ok) throw new Error(`Daraja auth failed: ${response.status}`);

    const data = await response.json();
    tokenCache = { ...data, cachedAt: Date.now() };
    return tokenCache.access_token;
    // Token TTL: ~3599 seconds (just under 1 hour)
}
```

**STK Push Full Auth Flow**

```typescript
function getMpesaTimestamp(): string {
    return new Date().toISOString().replace(/[-T:.Z]/g, '').slice(0, 14);
    // Format: YYYYMMDDHHmmss
}

function getMpesaPassword(shortcode: string, passkey: string, timestamp: string): string {
    return Buffer.from(`${shortcode}${passkey}${timestamp}`).toString('base64');
}

async function initiateStkPush({ phone, amount, accountRef, transactionDesc }: {
    phone: string; amount: number; accountRef: string; transactionDesc: string;
}) {
    const token = await getDarajaAccessToken();
    const timestamp = getMpesaTimestamp();
    const password = getMpesaPassword(
        process.env.MPESA_SHORTCODE!, process.env.MPESA_PASSKEY!, timestamp
    );

    return fetch(`${DARAJA_BASE}/mpesa/stkpush/v1/processrequest`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
        body: JSON.stringify({
            BusinessShortCode: process.env.MPESA_SHORTCODE,
            Password: password,
            Timestamp: timestamp,
            TransactionType: 'CustomerPayBillOnline',
            Amount: amount,
            PartyA: phone,            // Format: 254XXXXXXXXX
            PartyB: process.env.MPESA_SHORTCODE,
            PhoneNumber: phone,
            CallBackURL: `${process.env.MPESA_CALLBACK_URL}/api/mpesa/callback`,
            AccountReference: accountRef,   // e.g. "FYN3SS3-INV-001"
            TransactionDesc: transactionDesc,
        }),
    }).then(r => r.json());
}
```

**Validating M-Pesa Callbacks**

Daraja callbacks are not cryptographically signed (unlike Stripe). Secure the endpoint instead:

```typescript
// Safaricom production IP allowlist (verify current list with Safaricom)
const SAFARICOM_IPS = ['196.201.214.200', '196.201.214.206', '196.201.213.114'];

async function handleMpesaCallback(req: Request, res: Response) {
    const clientIp = req.headers['x-forwarded-for'] || req.socket.remoteAddress;
    if (!SAFARICOM_IPS.includes(clientIp as string)) {
        return res.status(403).json({ error: 'Forbidden' });
    }

    const { Body: { stkCallback } } = req.body;

    if (stkCallback.ResultCode === 0) {
        const meta = stkCallback.CallbackMetadata.Item;
        const mpesaCode = meta.find((i: any) => i.Name === 'MpesaReceiptNumber')?.Value;
        const amount = meta.find((i: any) => i.Name === 'Amount')?.Value;
        await db.transaction.update({
            where: { checkoutRequestId: stkCallback.CheckoutRequestID },
            data: { status: 'completed', mpesaCode, confirmedAt: new Date() }
        });
    } else {
        await db.transaction.update({
            where: { checkoutRequestId: stkCallback.CheckoutRequestID },
            data: { status: 'failed', failureReason: stkCallback.ResultDesc }
        });
    }

    res.status(200).json({ ResultCode: 0, ResultDesc: 'Accepted' });
    // Always respond 200 — otherwise Daraja retries the callback
}
```

**C2B Registration**

```typescript
async function registerC2BUrls() {
    const token = await getDarajaAccessToken();
    await fetch(`${DARAJA_BASE}/mpesa/c2b/v1/registerurl`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
        body: JSON.stringify({
            ShortCode: process.env.MPESA_SHORTCODE,
            ResponseType: 'Completed',
            ConfirmationURL: `${process.env.MPESA_CALLBACK_URL}/api/mpesa/c2b/confirm`,
            ValidationURL: `${process.env.MPESA_CALLBACK_URL}/api/mpesa/c2b/validate`,
        }),
    });
}
```

**M-Pesa Environment Variables**

```bash
MPESA_CONSUMER_KEY=xxxxxxxxxxxxxxxx
MPESA_CONSUMER_SECRET=xxxxxxxxxxxxxxxx
MPESA_SHORTCODE=174379             # Use 174379 for sandbox
MPESA_PASSKEY=bfb279...            # From Daraja portal
MPESA_ENV=sandbox                  # sandbox | production
MPESA_CALLBACK_URL=https://api.fyn3ss3.co.ke
```

**PCI-DSS Auth Requirements (If Handling Card Data)**

```
Requirement 8 — Access Control:
  Unique ID per user (no shared accounts)
  Passwords: 12+ chars, complexity, 90-day rotation for admins
  MFA: required for all admin + remote access
  Failed attempts: lock after 10 failures, 30-min lockout
  Idle session timeout: 15 minutes
  Inactive accounts: disable after 90 days
  Audit log: all cardholder data access with timestamp + userId
```

---

### 20.9 Enterprise Systems — SAML 2.0 Deep Dive

**SP-Initiated Flow (Most Common)**

```
1. User → SP: accesses protected resource
2. SP → User: redirect to IdP with SAML AuthnRequest (XML)
3. User → IdP: authenticates (password + MFA)
4. IdP → User: redirect to SP's ACS URL with SAML Response (signed XML)
5. SP verifies Response signature against IdP's public certificate
6. SP extracts user attributes (email, name, groups) from assertion
7. SP creates session — optionally provisions user (JIT provisioning)
```

```typescript
import { ServiceProvider, IdentityProvider } from 'samlify';

const sp = ServiceProvider({
    entityID: 'https://app.artkins.dev',
    assertionConsumerService: [{
        Binding: 'urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST',
        Location: 'https://app.artkins.dev/auth/saml/callback',
    }],
});

const idp = IdentityProvider({
    entityID: 'https://enterprise-client.okta.com',
    singleSignOnService: [{
        Binding: 'urn:oasis:names:tc:SAML:2.0:bindings:HTTP-Redirect',
        Location: 'https://enterprise-client.okta.com/app/saml/sso',
    }],
    signingCert: enterpriseClientCert,
});

const { extract } = await sp.parseLoginResponse(idp, 'post', req);
const { email, displayName, groups } = extract.attributes;

// JIT Provisioning
const user = await db.user.upsert({
    where: { email },
    create: { email, name: displayName, orgId, provider: 'saml' },
    update: { lastLoginAt: new Date() },
});
```

**SAML vs OIDC**

```
SAML 2.0:  XML, enterprise, IdP-initiated possible, ADFS-compatible, older systems
OIDC:      JSON/JWT, modern, mobile-friendly, REST-native, developer-friendly

Use SAML when: client uses Microsoft ADFS, mandated by regulation, legacy Okta config
Use OIDC for: everything else (Okta, Auth0, Azure AD, Google all support OIDC)
```

**SCIM 2.0 — User Lifecycle Automation**

```typescript
// Your SaaS must expose SCIM endpoints for enterprise IdP integration
// WorkOS / Okta / Azure AD will call these to provision / deprovision users

// PATCH /scim/v2/Users/:id — deprovisioning (employee leaves)
app.patch('/scim/v2/Users/:id', requireScimBearer, async (req, res) => {
    for (const op of req.body.Operations) {
        if (op.path === 'active' && op.value === false) {
            await db.user.update({ where: { scimId: req.params.id }, data: { active: false } });
            await revokeAllSessions(req.params.id);
        }
    }
    res.json(toScimUser(await db.user.findUnique({ where: { scimId: req.params.id } })));
});
```

---

### 20.10 IoT & Embedded Devices

```
Rules for IoT auth:
  Devices cannot type passwords.
  Devices may run 10+ years without manual updates.
  Devices can be physically compromised.
  Secret keys cannot be changed without OTA capability.
```

**Device Certificates (X.509 — Issue at Manufacture)**

```bash
# Issue unique cert per device
openssl genrsa -out device-abc123.key 2048
openssl req -new -key device-abc123.key \
    -subj "/CN=device-abc123/O=LyncsIndustries/C=KE" -out device.csr
openssl x509 -req -CA device-ca.crt -CAkey device-ca.key -CAcreateserial \
    -in device.csr -days 3650 -out device-abc123.crt
# 10-year validity — devices have long lifecycles
```

**mTLS for IoT**

```python
import ssl, paho.mqtt.client as mqtt
context = ssl.create_default_context(ssl.Purpose.SERVER_AUTH)
context.load_cert_chain('/etc/device/cert.pem', '/etc/device/private.key')
context.load_verify_locations('/etc/device/ca.pem')
client = mqtt.Client(client_id='device-abc123')
client.tls_set_context(context)
client.connect('iot.artkins.dev', 8883) # MQTTS
```

**OTA Key Rotation Pattern**

```
Problem: Private key on device is compromised.
Solution: Device generates new keypair, signs CSR with current + new public key,
          backend issues new cert, device replaces keypair, old cert revoked.
Note:     Build rotation capability in from day 1. Impossible to add later at scale.
```

---

### 20.11 Browser Extensions

```typescript
// Store tokens in chrome.storage.session (cleared on browser close) — NOT chrome.storage.local
chrome.storage.session.set({ accessToken, refreshToken });
chrome.storage.session.get(['accessToken'], ({ accessToken }) => { /* use token */ });

// NEVER expose tokens to content scripts or page JS
// background.js — tokens live here only
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (message.type === 'GET_AUTH_HEADER') {
        chrome.storage.session.get(['accessToken'], ({ accessToken }) => {
            sendResponse({ header: `Bearer ${accessToken}` });
        });
        return true; // Keep channel open for async
    }
});
```

---

### 20.12 WebSockets & Real-Time Auth

WebSocket connections authenticate once at connection time — not per message.

```typescript
// Next.js + Pusher: server authorizes channel subscriptions
// pages/api/pusher/auth.ts
export async function POST(req: Request) {
    const session = await getServerSession();
    if (!session) return new Response('Unauthorized', { status: 401 });

    const { socket_id, channel_name } = await req.json();
    const authResponse = pusher.authorizeChannel(socket_id, channel_name, {
        user_id: session.userId,
        user_info: { name: session.userName },
    });
    return Response.json(authResponse);
}

// Socket.io — authenticate in handshake middleware
io.use(async (socket, next) => {
    const token = socket.handshake.auth.token;
    try {
        const decoded = await verifyJWT(token);
        socket.data.userId = decoded.sub;
        next();
    } catch { next(new Error('Unauthorized')); }
});

// Client sends token once at connection
const socket = io('wss://api.artkins.dev', { auth: { token: accessToken } });
```


---

## 21. AUTHORIZATION MODELS — FULL SPECTRUM

Authentication proves identity. Authorization controls permissions. Separate problems, separate systems.

```
Auth WITHOUT AuthZ = every logged-in user gets everything
AuthZ WITHOUT Auth = you don't know who is being checked
```

---

### 21.1 Role-Based Access Control (RBAC)

Most common model. Users → Roles → Permissions.

```typescript
const PERMISSIONS = {
    admin:   ['*'],
    editor:  ['post:read', 'post:write', 'comment:read', 'comment:write'],
    viewer:  ['post:read', 'comment:read'],
    billing: ['invoice:read', 'invoice:write', 'subscription:manage'],
} as const;

function hasPermission(userRoles: string[], required: string): boolean {
    return userRoles.some(role => {
        const perms = PERMISSIONS[role as keyof typeof PERMISSIONS] || [];
        return perms.includes('*') || perms.includes(required);
    });
}

function requirePermission(permission: string) {
    return (req: Request, res: Response, next: NextFunction) => {
        if (!hasPermission(req.user.roles, permission))
            return res.status(403).json({ error: 'Forbidden', required: permission });
        next();
    };
}
```

**Role Hierarchy**

```typescript
const ROLE_HIERARCHY: Record<string, string[]> = {
    'super-admin': ['admin'],
    'admin':       ['editor', 'billing'],
    'editor':      ['viewer'],
    'billing':     ['viewer'],
};

function resolveRoles(role: string, visited = new Set<string>()): string[] {
    if (visited.has(role)) return [];
    visited.add(role);
    return [role, ...(ROLE_HIERARCHY[role] || []).flatMap(p => resolveRoles(p, visited))];
}
// resolveRoles('admin') → ['admin', 'editor', 'billing', 'viewer']
```

**RBAC in Database**

```sql
CREATE TABLE roles (id UUID PRIMARY KEY, name VARCHAR(50) UNIQUE NOT NULL);
CREATE TABLE permissions (id UUID PRIMARY KEY, resource VARCHAR(100), action VARCHAR(50),
    UNIQUE(resource, action));
CREATE TABLE role_permissions (role_id UUID REFERENCES roles(id), permission_id UUID REFERENCES permissions(id),
    PRIMARY KEY(role_id, permission_id));
CREATE TABLE user_roles (user_id UUID, role_id UUID REFERENCES roles(id), org_id UUID,
    granted_at TIMESTAMP DEFAULT NOW(), granted_by UUID,
    PRIMARY KEY(user_id, role_id, org_id));

-- Permission check query
SELECT EXISTS (
    SELECT 1 FROM user_roles ur
    JOIN role_permissions rp ON rp.role_id = ur.role_id
    JOIN permissions p ON p.id = rp.permission_id
    WHERE ur.user_id = $1 AND ur.org_id = $2
      AND p.resource = $3 AND (p.action = $4 OR p.action = 'manage')
) AS has_permission;
```

---

### 21.2 Attribute-Based Access Control (ABAC)

Decisions based on user attributes + resource attributes + environment. More flexible, more complex.

```
Policy: ALLOW IF
  user.role = 'doctor'
  AND resource.type = 'patient-record'
  AND (resource.owner = user.id OR user.department = resource.department)
  AND environment.location = 'hospital-network'
  AND environment.time BETWEEN '09:00' AND '17:00'
```

```typescript
interface AccessContext {
    user: { id: string; roles: string[]; department: string; clearanceLevel: number };
    resource: { type: string; ownerId: string; orgId: string; classification: string };
    action: string;
    environment: { timestamp: Date; ipAddress: string; riskScore: number };
}

type Policy = (ctx: AccessContext) => boolean;

const patientRecordPolicy: Policy = (ctx) => {
    if (ctx.action === 'read') {
        return ctx.user.roles.includes('doctor') &&
            (ctx.resource.ownerId === ctx.user.id ||
             ctx.user.department === ctx.resource.department) &&
            ctx.user.clearanceLevel >= 2;
    }
    if (ctx.action === 'write') {
        return ctx.resource.ownerId === ctx.user.id && ctx.user.clearanceLevel >= 3;
    }
    return false;
};
```

---

### 21.3 Policy-Based Access Control (PBAC) — Open Policy Agent

OPA decouples policy from code. Write policy in Rego; query at runtime. Used at Netflix, Intuit, Twilio.

```rego
# policy.rego
package artkins.authz
default allow = false

allow { input.user.roles[_] == "admin" }

allow {
    input.action == "post:write"
    input.user.roles[_] == "editor"
    input.resource.orgId == input.user.orgId
}

# Time-based: no deletions outside 9am–8pm EAT (UTC+3)
deny {
    input.action == "post:delete"
    hour := time.clock(time.now_ns())[0]
    hour < 6
}
deny {
    input.action == "post:delete"
    hour := time.clock(time.now_ns())[0]
    hour >= 17
}
```

```typescript
async function checkOPA(input: object): Promise<boolean> {
    const { result } = await fetch('http://opa:8181/v1/data/artkins/authz/allow', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ input }),
    }).then(r => r.json());
    return result === true;
}
```

---

### 21.4 ReBAC — Relationship-Based Access Control (Google Zanzibar)

Authorization based on entity relationships. Used by Google (Drive/Docs), Airbnb, GitHub.

```
"Can user A read document D?"
→ user:alice → reader → document:d1               (direct)
→ user:alice → member → group:team-a → reader → document:d1  (via group)
→ user:alice → reader → folder:f1 → parent → document:d1     (via folder)

All three grant access. Zanzibar resolves these chains efficiently.
```

```typescript
// SpiceDB (open-source Google Zanzibar)
import { v1 } from '@authzed/authzed-node';
const client = v1.NewClient(process.env.SPICEDB_TOKEN!, process.env.SPICEDB_ENDPOINT!);

// Write relationship
await client.writeRelationships({ updates: [{
    operation: v1.RelationshipUpdate_Operation.CREATE,
    relationship: {
        resource: { objectType: 'document', objectId: 'fyn3ss3-report-q2' },
        relation: 'editor',
        subject: { object: { objectType: 'user', objectId: 'don123' } },
    },
}]});

// Check permission (editor implies reader in schema)
const { permissionship } = await client.checkPermission({
    resource: { objectType: 'document', objectId: 'fyn3ss3-report-q2' },
    permission: 'read',
    subject: { object: { objectType: 'user', objectId: 'alice456' } },
});
const allowed = permissionship === v1.CheckPermissionResponse_Permissionship.HAS_PERMISSION;
```

```
When to use ReBAC:
  ✓ File-sharing (any user shares any doc with any user)
  ✓ Collaborative platforms
  ✓ Social networks (follows, blocks, friends)
  ✓ GitHub-style (org → team → repo → branch)
  ✗ Simple role-based apps — massive overkill
  ✗ Adds latency (separate auth service per request)
```

---

### 21.5 Capability-Based Security

A capability token IS the permission. No identity check needed — holding the token proves authorization.
Used in: AI agents, object-capability systems, macaroons.

```typescript
interface CapabilityToken {
    id: string;
    issuedTo: string;            // 'agent:fyn3ss3-assistant'
    issuedBy: string;            // 'user:don123'
    capabilities: string[];      // ['transaction:read', 'budget:read'] — minimal set
    constraints: {
        maxCalls?: number;
        allowedResources?: string[];
        orgId: string;
    };
    expiresAt: string;
    nonDelegatable: boolean;     // Agent cannot re-issue this
}
// Agent presents token. Tool verifies scope + expiry + constraints. Identity is secondary.
```

---

### 21.6 Scope-Based Permissions (OAuth Scopes)

Controls what a third-party app can do on behalf of a user.

```
Good scope design (granular, human-readable):
  read:profile    read:transactions    write:transactions
  read:budgets    write:budgets        read:reports
  admin:users     billing:manage

Bad scope design (meaningless):
  full_access     all                  api:use
```

```typescript
const authUrl = new URL('https://accounts.google.com/o/oauth2/v2/auth');
authUrl.searchParams.set('scope', [
    'openid', 'profile', 'email',
    'https://www.googleapis.com/auth/gmail.readonly', // Specific — not gmail.*
].join(' '));
```

---

### 21.7 Row-Level Security (Supabase RLS)

Database-level authorization — every query automatically scoped to what the user can see.

```sql
ALTER TABLE transactions ENABLE ROW LEVEL SECURITY;

-- Users see only their own transactions
CREATE POLICY "own_transactions" ON transactions
    FOR ALL USING (auth.uid() = user_id);

-- Multi-tenant: members see their org's data
CREATE POLICY "org_transactions" ON transactions
    FOR SELECT USING (
        org_id IN (
            SELECT org_id FROM org_members
            WHERE user_id = auth.uid() AND status = 'active'
        )
    );

-- Admins see all their org's data
CREATE POLICY "admin_org_transactions" ON transactions
    FOR ALL USING (
        EXISTS (
            SELECT 1 FROM org_members
            WHERE user_id = auth.uid()
              AND org_id = transactions.org_id
              AND role = 'admin'
        )
    );

-- service_role key bypasses RLS — NEVER expose to client
```

---

### 21.8 Multi-Tenant Authorization Patterns

```typescript
// Pattern 1: Always filter by orgId (application-level)
async function getTransactions(userId: string, orgId: string) {
    return db.transaction.findMany({ where: { orgId, userId } }); // orgId NEVER optional
}

// Pattern 2: Middleware injects org context + verifies membership
async function orgMiddleware(req: Request, res: Response, next: NextFunction) {
    const { orgId } = req.params;
    const membership = await db.orgMember.findUnique({
        where: { userId_orgId: { userId: req.user.id, orgId } },
    });
    if (!membership) return res.status(403).json({ error: 'Not a member' });
    req.org = { id: orgId, userRole: membership.role };
    next();
}

// Pattern 3: Database-per-tenant (maximum isolation)
async function getTenantDb(orgId: string) {
    return createDbClient(await getOrgConnectionString(orgId));
}
```

---

### 21.9 Authorization in Next.js Middleware

```typescript
// middleware.ts — runs at the edge before every request
import { clerkMiddleware, createRouteMatcher } from '@clerk/nextjs/server';

const isPublicRoute = createRouteMatcher([
    '/', '/sign-in(.*)', '/sign-up(.*)',
    '/api/webhooks(.*)',
    '/api/mpesa/callback', // M-Pesa callbacks must be public
]);
const isAdminRoute = createRouteMatcher(['/admin(.*)']);

export default clerkMiddleware(async (auth, req) => {
    if (isPublicRoute(req)) return;

    const { userId, orgId, orgRole } = await auth.protect();

    if (isAdminRoute(req) && orgRole !== 'org:admin') {
        return Response.redirect(new URL('/dashboard', req.url));
    }

    // Inject context into headers for Server Components
    const headers = new Headers(req.headers);
    headers.set('x-user-id', userId);
    headers.set('x-org-id', orgId ?? '');
    headers.set('x-org-role', orgRole ?? '');
});
```

---

## 22. PASSKEYS, WEBAUTHN & FIDO2

Passkeys replace passwords. A cryptographic key pair stored on your device (synced to iCloud/Google). No password to phish. No server-side secret to leak.

### 22.1 How It Works

```
Registration:
  1. Server sends challenge
  2. Browser → authenticator (FaceID, TouchID, Windows Hello, security key)
  3. Authenticator generates key pair:
       Private key → stays on device, never leaves
       Public key  → sent to server and stored
  4. Server stores: public key + credential ID

Authentication:
  1. Server sends new challenge
  2. Browser → authenticator
  3. User verifies (FaceID, fingerprint, PIN)
  4. Authenticator signs challenge with private key
  5. Server verifies signature with stored public key
  ✓ No password ever transmitted. No secret ever stored on server.
```

### 22.2 SimpleWebAuthn in Next.js (Full Implementation)

```typescript
// app/api/auth/passkey/register/options/route.ts
import { generateRegistrationOptions } from '@simplewebauthn/server';

export async function GET() {
    const session = await getServerSession();
    const options = await generateRegistrationOptions({
        rpName: 'Fyn3ss3',
        rpID: 'fyn3ss3.co.ke',
        userID: new TextEncoder().encode(session.userId),
        userName: session.email,
        userDisplayName: session.name,
        attestationType: 'none',
        authenticatorSelection: {
            residentKey: 'required',      // Discoverable: username-less login
            userVerification: 'required', // Require biometric/PIN
            authenticatorAttachment: 'platform', // Built-in: FaceID, Windows Hello
        },
        excludeCredentials: (await db.passkey.findMany({
            where: { userId: session.userId }
        })).map(pk => ({ id: Buffer.from(pk.credentialId, 'base64url'), type: 'public-key' })),
    });
    await setSession({ registrationChallenge: options.challenge });
    return Response.json(options);
}

// app/api/auth/passkey/register/verify/route.ts
import { verifyRegistrationResponse } from '@simplewebauthn/server';

export async function POST(req: Request) {
    const session = await getServerSession();
    const body = await req.json();
    const verification = await verifyRegistrationResponse({
        response: body,
        expectedChallenge: session.registrationChallenge,
        expectedOrigin: 'https://fyn3ss3.co.ke',
        expectedRPID: 'fyn3ss3.co.ke',
    });
    if (!verification.verified) return Response.json({ error: 'Failed' }, { status: 400 });

    const { credentialID, credentialPublicKey, counter } = verification.registrationInfo!;
    await db.passkey.create({ data: {
        userId: session.userId,
        credentialId: Buffer.from(credentialID).toString('base64url'),
        publicKey: Buffer.from(credentialPublicKey).toString('base64url'),
        counter,
        deviceName: body.deviceName || 'My Device',
        createdAt: new Date(),
    }});
    return Response.json({ verified: true });
}

// app/api/auth/passkey/login/verify/route.ts
import { verifyAuthenticationResponse } from '@simplewebauthn/server';

export async function POST(req: Request) {
    const session = await getServerSession();
    const body = await req.json();
    const dbPasskey = await db.passkey.findUnique({ where: { credentialId: body.id } });
    if (!dbPasskey) return Response.json({ error: 'Passkey not found' }, { status: 404 });

    const verification = await verifyAuthenticationResponse({
        response: body,
        expectedChallenge: session.loginChallenge,
        expectedOrigin: 'https://fyn3ss3.co.ke',
        expectedRPID: 'fyn3ss3.co.ke',
        authenticator: {
            credentialID: Buffer.from(dbPasskey.credentialId, 'base64url'),
            credentialPublicKey: Buffer.from(dbPasskey.publicKey, 'base64url'),
            counter: dbPasskey.counter,
        },
    });
    if (!verification.verified) return Response.json({ error: 'Auth failed' }, { status: 401 });

    // MUST update counter — replay attack prevention
    await db.passkey.update({
        where: { credentialId: body.id },
        data: { counter: verification.authenticationInfo.newCounter, lastUsedAt: new Date() },
    });
    await createSession(dbPasskey.userId);
    return Response.json({ verified: true });
}
```

**Client-Side (React)**

```typescript
'use client';
import { startRegistration, startAuthentication } from '@simplewebauthn/browser';

async function registerPasskey() {
    const options = await fetch('/api/auth/passkey/register/options').then(r => r.json());
    const attestation = await startRegistration(options);
    // Browser prompts: "Save a passkey for fyn3ss3.co.ke?" → FaceID / TouchID / PIN
    const result = await fetch('/api/auth/passkey/register/verify', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ...attestation, deviceName: 'My MacBook' }),
    }).then(r => r.json());
    console.log(result.verified ? 'Passkey saved' : 'Registration failed');
}

async function loginWithPasskey() {
    const options = await fetch('/api/auth/passkey/login/options').then(r => r.json());
    const assertion = await startAuthentication(options);
    // Browser prompts user for biometric — no username or password typed
    return fetch('/api/auth/passkey/login/verify', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(assertion),
    }).then(r => r.json());
}
```

---

## 23. TOKEN SYSTEMS BEYOND JWT

### 23.1 PASETO (Platform-Agnostic Security Tokens)

PASETO fixes JWT's design mistakes. No algorithm confusion. No `alg:none`. No header manipulation.

```
JWT problems:                          PASETO solution:
  alg:none attack possible        →      No algorithm negotiation
  Algorithm confusion (RS256/HS256) →    Version + purpose baked in: v4.public
  Header modifiable without detection →  Authenticated encryption
  Too many config options         →      One right answer per version
```

```typescript
import { V4 } from 'paseto';

// Sign (v4.public = Ed25519 asymmetric signing)
const token = await V4.sign({
    sub: 'user_123',
    iat: new Date().toISOString(),
    exp: new Date(Date.now() + 15 * 60 * 1000).toISOString(),
    aud: 'fyn3ss3-api',
}, privateKey, { footer: JSON.stringify({ kid: 'key-2026-06' }) });
// → v4.public.xxxxxxx...

// Verify
const payload = await V4.verify(token, publicKey, {
    audience: 'fyn3ss3-api',
    clockTolerance: '30s',
});
```

```
Use JWT when:   Existing ecosystem expects it (Clerk/Auth0/Supabase all issue JWT)
Use PASETO when: Building your own token system. Internal microservices. New TypeScript projects.
```

---

### 23.2 Opaque Tokens

A random string. Meaning stored server-side. No claims in the token.

```
JWT:    eyJhbG...  ← payload visible → role:admin exposed to client
Opaque: artkins_tok_a8f2b9c4...  ← meaningless without DB lookup
```

```typescript
async function issueOpaqueToken(userId: string, scopes: string[]): Promise<string> {
    const raw = `artkins_tok_${randomBytes(32).toString('hex')}`;
    await db.token.create({
        data: { tokenHash: sha256(raw), userId, scopes,
                expiresAt: new Date(Date.now() + 3600_000) }
    });
    return raw; // Return once
}

async function verifyOpaqueToken(raw: string) {
    const record = await db.token.findUnique({ where: { tokenHash: sha256(raw) } });
    if (!record || record.expiresAt < new Date() || record.revokedAt) return null;
    await db.token.update({ where: { tokenHash: sha256(raw) }, data: { lastUsedAt: new Date() } });
    return record;
}
```

```
✓ Use opaque tokens when: immediate revocation needed, sensitive claims shouldn't be visible,
  token theft impact must be instantly containable
✗ Cost: DB lookup on every request (no stateless verification like JWT)
```

---

### 23.3 Macaroons

Tokens with unforgeable attenuation. A holder can restrict a token but never expand it.

```typescript
import Macaroon from 'macaroon';

const m = Macaroon.newMacaroon({
    identifier: 'access-token-001', location: 'https://api.artkins.dev',
    rootKey: randomBytes(32), version: 2,
});

// Anyone holding the token can narrow it further
m.addFirstPartyCaveat('time < 2026-06-28T00:00:00Z');
m.addFirstPartyCaveat('method = GET');
// Resulting token: GET only, expires midnight — cannot be un-restricted

m.verify(rootKey, (caveat) => {
    if (caveat.startsWith('time < ')) return new Date() < new Date(caveat.slice(7));
    if (caveat.startsWith('method = ')) return req.method === caveat.slice(9);
    return false;
});
```

---

### 23.4 Proof-of-Possession (DPoP)

Token bound to a key pair. Theft alone is useless without the private key.

```typescript
// Client generates ephemeral keypair per session
// Access token contains dpop_jkt claim (thumbprint of public key)
// Each request: client signs a DPoP proof with private key
// Server: verifies token's jkt matches DPoP proof's key

async function createDpopProof(url: string, method: string, privateKey: CryptoKey) {
    const header = { alg: 'ES256', typ: 'dpop+jwt', jwk: await exportPublicJwk(publicKey) };
    const payload = {
        jti: randomUUID(),           // Unique per request — prevents replay
        htm: method.toUpperCase(),   // HTTP method bound
        htu: url,                    // URL bound
        iat: Math.floor(Date.now() / 1000),
    };
    return signJWT(header, payload, privateKey);
}

fetch('https://api.artkins.dev/data', {
    headers: {
        'Authorization': `DPoP ${accessToken}`,   // DPoP, not Bearer
        'DPoP': await createDpopProof('https://api.artkins.dev/data', 'GET', privateKey),
    },
});
```

---

## 24. PASSWORD & CREDENTIAL STORAGE

### 24.1 The Hierarchy

```
NEVER use for passwords (too fast — GPUs crack in minutes):
  ❌ MD5, SHA-1, SHA-256, SHA-512

USE these (designed to be slow — parameterizable cost):
  ✅ Argon2id  — RECOMMENDED. PHC winner. Resistant to GPU, ASIC, side-channel.
  ✅ bcrypt    — Widely supported. Safe. Use cost factor ≥12.
  ✅ scrypt    — Good. Less supported than bcrypt.
  ✅ PBKDF2   — Acceptable. Required in some FIPS/regulated environments.
```

**Argon2id (Recommended)**

```typescript
import { hash, verify } from '@node-rs/argon2';

const passwordHash = await hash(password, {
    memoryCost: 65536,  // 64 MB — increase if server has spare RAM
    timeCost: 3,        // 3 iterations
    parallelism: 4,     // 4 parallel threads
});
// Target: hashing should take 100ms–1s on your server
// If <100ms: increase memoryCost. If >2s: reduce.

const isValid = await verify(passwordHash, inputPassword);
// Argon2id extracts salt from hash string automatically

// IMPORTANT: run on a worker thread — blocks event loop
import { Worker } from 'worker_threads';
// Or use @node-rs/argon2 which is a native module (non-blocking)
```

**bcrypt (if Argon2 unavailable)**

```typescript
import bcrypt from 'bcrypt';
const COST = 12; // Min 12. Each +1 doubles time. 14 ≈ 4s.

// CRITICAL: bcrypt silently truncates at 72 bytes
// Fix: pre-hash with SHA-512 before bcrypt
const preHashed = createHash('sha512').update(password).digest('base64');
const hash = await bcrypt.hash(preHashed, COST);
const isValid = await bcrypt.compare(createHash('sha512').update(input).digest('base64'), hash);
```

**NIST SP 800-63B — Current Password Policy Standard**

```
✓ Minimum 8 chars (recommend 15+), maximum 64+ chars
✓ Allow ALL Unicode characters including spaces
✓ Check against breached password databases
✓ Block context-specific passwords (app name, username)
✗ DO NOT force complexity rules (uppercase + special + numbers) — they reduce entropy
✗ DO NOT force periodic rotation — creates predictable patterns (Password1! → Password2!)
✗ DO NOT use security questions
✓ DO allow paste — password managers need it
```

**Breach Database Check (k-Anonymity via HIBP)**

```typescript
async function isPasswordBreached(password: string): Promise<boolean> {
    const sha1 = createHash('sha1').update(password).digest('hex').toUpperCase();
    const prefix = sha1.slice(0, 5);
    const suffix = sha1.slice(5);
    // Only 5 chars of SHA1 sent to HIBP — password stays private
    const hashes = await fetch(`https://api.pwnedpasswords.com/range/${prefix}`)
        .then(r => r.text());
    return hashes.split('\n').some(line => line.startsWith(suffix));
}

// Check on registration AND password change
if (await isPasswordBreached(newPassword)) {
    return { error: 'This password appears in a known data breach. Choose a different one.' };
}
```

---

## 25. IDENTITY FEDERATION — COMPLETE REFERENCE

### 25.1 OAuth 2.1

OAuth 2.1 = OAuth 2.0 + security best practices consolidated into one spec.

**Removed from 2.0:**

```
❌ Implicit flow     — tokens in URL fragment → exposed in browser history + referer headers
❌ ROPC flow         — user sends password to third-party app
❌ HTTP              — TLS required everywhere
```

**Required in 2.1:**

```
✓ PKCE for ALL clients — public AND confidential (was optional in 2.0)
✓ Exact redirect URI matching — no wildcards, no partial matches
✓ No tokens in URL query params
✓ Refresh token rotation by default
```

**Authorization Code + PKCE — The Only Web Flow**

```
1. Client generates:
     code_verifier  = randomBytes(32) → base64url (43–128 chars)
     code_challenge = SHA256(code_verifier) → base64url

2. GET /authorize?response_type=code&client_id=X&redirect_uri=Y
       &scope=openid profile email
       &state=RANDOM_STATE_VALUE        ← CSRF protection
       &code_challenge=CHALLENGE
       &code_challenge_method=S256

3. User authenticates at auth server

4. Auth server → GET your_redirect?code=AUTH_CODE&state=RANDOM_STATE_VALUE

5. Verify state matches (CSRF check)

6. POST /token
   grant_type=authorization_code&code=AUTH_CODE&redirect_uri=Y
   &client_id=X&code_verifier=VERIFIER  ← Server verifies SHA256(verifier)==challenge

7. Response: { access_token, refresh_token, id_token, expires_in, token_type: "Bearer" }
```

---

### 25.2 OpenID Connect (OIDC)

OIDC = OAuth 2.1 + identity layer. The `id_token` answers "who is this user?"

```typescript
interface OIDCClaims {
    sub: string;                // Stable user ID — never changes even if email changes
    iss: string;                // Issuer: 'https://accounts.google.com'
    aud: string | string[];     // Your client_id
    exp: number;                // Expiry
    iat: number;                // Issued at
    email?: string;
    email_verified?: boolean;
    name?: string;
    given_name?: string;
    family_name?: string;
    picture?: string;
    // Custom namespace claims
    'https://artkins.dev/role'?: string;
    'https://artkins.dev/orgId'?: string;
}
```

**OIDC Discovery (auto-configure from issuer)**

```typescript
// All OIDC providers expose this endpoint
const config = await fetch(`${issuer}/.well-known/openid-configuration`).then(r => r.json());
// Returns: authorization_endpoint, token_endpoint, jwks_uri, userinfo_endpoint, scopes_supported

// Canonical discovery URLs:
// Google:   https://accounts.google.com/.well-known/openid-configuration
// Clerk:    https://your-app.clerk.accounts.dev/.well-known/openid-configuration
// Auth0:    https://your-domain.us.auth0.com/.well-known/openid-configuration
// Keycloak: https://keycloak.artkins.dev/realms/artkins/.well-known/openid-configuration
```

---

### 25.3 SAML 2.0

Covered in detail in Section 20.9. Reference summary:

```
SAML:  XML, binary base64, enterprise IdPs, ADFS, PingFederate, legacy Okta
OIDC:  JSON/JWT, REST-native, mobile-friendly, modern, all major providers

Choose SAML when: enterprise client mandates it, using ADFS, regulated industry requirement
Choose OIDC for: everything else (Okta, Azure AD, Auth0, Google all support OIDC natively)
```

---

### 25.4 SCIM 2.0 — Automated User Provisioning

```typescript
// Minimum endpoints your SaaS must expose for enterprise clients
// Secured with a static bearer token (different from user tokens)

const SCIM_TOKEN = process.env.SCIM_BEARER_TOKEN;

function requireScimBearer(req: Request, res: Response, next: NextFunction) {
    const token = req.headers.authorization?.replace('Bearer ', '');
    if (!token || !timingSafeEqual(Buffer.from(token), Buffer.from(SCIM_TOKEN!)))
        return res.status(401).end();
    next();
}

// POST /scim/v2/Users — provision new employee
app.post('/scim/v2/Users', requireScimBearer, async (req, res) => {
    const { userName, name, active } = req.body;
    const user = await db.user.create({
        data: { email: userName, name: name.formatted, active, scimProvisioned: true }
    });
    res.status(201).json(toScimUser(user));
});

// PATCH /scim/v2/Users/:id — employee leaves → active: false
app.patch('/scim/v2/Users/:id', requireScimBearer, async (req, res) => {
    for (const op of req.body.Operations) {
        if (op.path === 'active' && op.value === false) {
            await db.user.update({ where: { scimId: req.params.id }, data: { active: false } });
            await revokeAllUserSessions(req.params.id);
        }
    }
    res.json(toScimUser(await db.user.findUnique({ where: { scimId: req.params.id } })));
});
```

---

### 25.5 LDAP & Active Directory

```
LDAP structure:
  dc=artkins,dc=dev                     ← Domain root
  └── ou=Users,dc=artkins,dc=dev        ← Organizational unit
      ├── cn=don,ou=Users               ← User
      └── cn=alice,ou=Users
  └── ou=Groups,dc=artkins,dc=dev
      └── cn=admins,ou=Groups

Modern approach: don't implement LDAP directly.
  → Keycloak: connects to LDAP/AD, exposes OIDC + SAML to your app
  → WorkOS / Auth0: enterprise connections, abstracts LDAP away
  → Azure AD Connect: syncs on-prem AD to Azure AD (then use OIDC)
```

```typescript
// If you must query LDAP directly (ldapts)
import { Client } from 'ldapts';
const client = new Client({ url: 'ldap://ad.enterprise-client.com:389' });
await client.bind('cn=svc-account,ou=Service,dc=enterprise,dc=com', password);
const { searchEntries } = await client.search('ou=Users,dc=enterprise,dc=com', {
    filter: `(mail=${userEmail})`,
    attributes: ['cn', 'mail', 'memberOf', 'sAMAccountName'],
});
const groups = (searchEntries[0].memberOf as string[])
    .map(dn => dn.split(',')[0].replace('CN=', ''));
await client.unbind();
```

---

## 26. PKI, MTLS & CERTIFICATE MANAGEMENT

### 26.1 Build an Internal CA

```bash
# Root CA (create once, store offline / in HSM)
openssl genrsa -out ca.key 4096
openssl req -new -x509 -days 3650 -key ca.key \
    -subj "/CN=Lyncs Industries CA/O=LyncsIndustries/C=KE" -out ca.crt

# Service certificate
openssl genrsa -out payment-service.key 2048
openssl req -new -key payment-service.key \
    -subj "/CN=payment-service/O=LyncsIndustries/C=KE" -out payment-service.csr
openssl x509 -req -CA ca.crt -CAkey ca.key -CAcreateserial \
    -in payment-service.csr -days 365 -out payment-service.crt

# Get public key fingerprint for certificate pinning
openssl s_client -connect api.artkins.dev:443 | \
    openssl x509 -pubkey -noout | openssl pkey -pubin -outform der | \
    openssl dgst -sha256 -binary | base64
```

### 26.2 cert-manager (Kubernetes — Automatic Rotation)

```yaml
apiVersion: cert-manager.io/v1
kind: Certificate
metadata:
  name: payment-service-cert
  namespace: production
spec:
  secretName: payment-service-tls
  duration: 24h          # Short-lived → automatic rotation
  renewBefore: 8h
  issuerRef:
    name: internal-ca
    kind: ClusterIssuer
  commonName: payment-service.production.svc.cluster.local
  dnsNames:
    - payment-service
    - payment-service.production.svc.cluster.local
```

### 26.3 Let's Encrypt (Public TLS)

```
Easiest: Use Caddy as reverse proxy
Caddyfile:
  api.artkins.dev {
      reverse_proxy localhost:3000
  }
  # Automatic HTTPS — Caddy handles ACME + Let's Encrypt + renewal
```

### 26.4 Certificate Rotation Best Practices

```
Pin public key, not certificate:
  ✓ Certificate changes on renewal but public key stays the same
  ✓ Pinning the cert breaks on every Let's Encrypt renewal (90 days)
  ✓ Pinning the public key survives renewals

Always pin TWO keys:
  Current cert + backup cert
  Prevents lockout if you need to rotate

Rotation procedure:
  1. Generate new keypair
  2. Get new cert from CA
  3. Deploy: configure both old + new cert accepted (dual-valid)
  4. Update all clients to pin new key
  5. Remove old cert + old pin from clients
  6. Revoke old cert
```

---

## 27. SECRET MANAGEMENT

### 27.1 Why .env Is Not Enough

```
.env in git:            ❌ Committed secrets are permanent (git history is forever)
.env on servers:        ⚠️  One compromised server leaks all secrets
Shared team .env:       ❌ No audit trail, no rotation, no fine-grained access, email → leak
Platform env vars:      ✅ OK for single-app — no team access, no history in git
Secret manager:         ✅ Audit logs, rotation, granular access, automatic injection
```

```bash
# Scan git history for leaked secrets
git log --all -S "CLERK_SECRET_KEY" --oneline
git log --all --full-history -- "**/.env"

# Better: install truffleHog or gitleaks as pre-commit hook
pip install trufflehog
trufflehog git file://. --only-verified
```

### 27.2 Infisical (Open-Source — Best for Lyncs/Self-Hosted)

```typescript
import InfisicalClient from '@infisical/sdk';

const client = new InfisicalClient({
    clientId: process.env.INFISICAL_CLIENT_ID!,
    clientSecret: process.env.INFISICAL_CLIENT_SECRET!,
    siteUrl: 'https://secrets.artkins.dev', // Self-hosted
});

const secret = await client.getSecret({
    secretName: 'MPESA_CONSUMER_SECRET',
    projectId: 'fyn3ss3-project-id',
    environment: 'production',
});
```

```bash
# CLI: inject secrets without .env files
infisical run --projectId xxx --env production -- node server.js
# → process.env.MPESA_CONSUMER_SECRET is populated at runtime
```

### 27.3 HashiCorp Vault

```typescript
import Vault from 'node-vault';
const vault = Vault({ endpoint: 'https://vault.artkins.dev', token: process.env.VAULT_TOKEN });

// Static secrets
const { data } = await vault.read('secret/data/fyn3ss3/production');
const mpesaSecret = data.data.MPESA_CONSUMER_SECRET;

// Dynamic secrets: Vault creates DB credentials on demand, auto-expires
const { data: creds } = await vault.read('database/creds/fyn3ss3-role');
// { username: 'v-app-xyz', password: 'A1B2...' } — expires in 1h, auto-deleted
```

### 27.4 SOPS — Encrypted Secrets in Git

```bash
# Encrypt secrets file with KMS key — safe to commit
sops --encrypt \
    --kms arn:aws:kms:af-south-1:123456789:key/xxx \
    --encrypted-regex '^(MPESA|CLERK|DATABASE).*' \
    .env.production > .env.production.enc

# Decrypt (only IAM roles with KMS access)
sops --decrypt .env.production.enc > .env.production

# Edit in-place (decrypts, opens editor, re-encrypts on save)
sops .env.production.enc
```

### 27.5 Secret Rotation Pattern (Zero-Downtime)

```typescript
async function rotateKey(serviceId: string): Promise<string> {
    const newKey = generateKey();
    const oldKeyId = await getCurrentKeyId(serviceId);

    // Store new key, mark old as "draining" with 24h window
    await setKeys(serviceId, {
        current: await hashAndStore(newKey),
        previous: oldKeyId,
        previousExpiresAt: new Date(Date.now() + 24 * 60 * 60 * 1000),
    });

    return newKey; // Deploy this to services
}

// Verification: accept both current and previous during rotation window
async function verifyKey(provided: string): Promise<boolean> {
    const state = await getKeyState();
    const hash = sha256(provided);
    if (hash === state.current) return true;
    if (hash === state.previous && new Date() < state.previousExpiresAt) return true;
    return false;
}
```

---

## 28. ZERO TRUST ARCHITECTURE

### 28.1 Principles

```
Traditional perimeter:          Zero Trust:
  Inside network = trusted        Never trust, always verify
  Outside network = untrusted     Assume breach at all times
  VPN in = full access            Per-request verification
  One breach = game over          Minimal access, logged everything
```

### 28.2 Google BeyondCorp Model

```
Traditional: User → VPN → Internal Network → Any Resource (one gate)

BeyondCorp:  User → Identity-Aware Proxy → Policy Engine → Specific Resource
                                                ↓
                                   Evaluates: user identity + device trust
                                   + location + time + risk score
                                   → per-resource, per-request
```

### 28.3 Continuous / Risk-Based Authentication

```typescript
interface RiskSignals {
    unusualLoginTime: boolean;  // 3 AM vs usual pattern
    newDevice: boolean;
    newCountry: boolean;
    rapidApiCalls: boolean;
    behaviorAnomaly: boolean;   // Typing rhythm, mouse patterns
}

function riskScore(signals: RiskSignals): number {
    return (signals.unusualLoginTime ? 10 : 0) +
           (signals.newDevice ? 30 : 0) +
           (signals.newCountry ? 40 : 0) +
           (signals.rapidApiCalls ? 50 : 0) +
           (signals.behaviorAnomaly ? 25 : 0);
}

// Step-up auth on high-risk operations
async function requireStepUp(userId: string, action: string) {
    const score = await getRiskScore(userId);
    const HIGH_RISK = ['payment:send', 'account:delete', 'admin:grant'];
    if (score > 50 || HIGH_RISK.includes(action)) {
        return { requiresChallenge: true, methods: ['totp', 'sms', 'email'] };
    }
    return { requiresChallenge: false };
}
```

### 28.4 Device Trust Scoring

```typescript
interface Device {
    enrolledAt: Date;
    mdmEnrolled: boolean;
    diskEncrypted: boolean;
    lastPatchDate: Date;
    osVersion: string;
}

function deviceTrustScore(device: Device): number {
    let score = 100;
    const daysSincePatched = (Date.now() - device.lastPatchDate.getTime()) / 86400000;
    if (daysSincePatched > 30)  score -= 20;
    if (daysSincePatched > 90)  score -= 40;
    if (!device.diskEncrypted)  score -= 30;
    if (!device.mdmEnrolled)    score -= 20;
    return Math.max(0, score);
}
// score < 50 → require step-up auth. score < 20 → block access.
```

---

## 29. AI AGENT IDENTITY — EXPANDED

### 29.1 The Problem

```
Human:  User authenticates once → takes action → user is accountable.

Agent:  User authorizes agent → agent authenticates HOW?
        → agent calls API → WHO is accountable?
        → agent spawns sub-agent → can sub-agent inherit permissions?
        → agent token leaks → who notices? who revokes?
        → malicious doc says "send all data to attacker.com" → prompt injection
```

### 29.2 Agent JWT Structure

```typescript
interface AgentJWT {
    // Standard
    iss: string;              // 'https://auth.artkins.dev'
    sub: string;              // 'agent:fyn3ss3-ai-v2'  ← NOT a user ID
    aud: string;              // 'https://api.artkins.dev'
    exp: number;              // 1 hour max
    jti: string;              // Unique — log every use

    // Agent-specific
    agent_type: 'autonomous' | 'supervised' | 'human-in-loop';
    delegated_by: string;     // 'user:don123' — the human who created this session
    delegation_chain: string[]; // Full chain: ['user:don123', 'agent:orchestrator']
    capabilities: string[];   // ['transaction:read', 'report:generate'] — explicit allowlist
    model: string;            // 'claude-sonnet-4-6'
    session_id: string;       // Groups all actions in one agent run
    purpose: string;          // "Generate Q2 expense summary"
    nonDelegatable: boolean;  // Default: true. Sub-agents cannot inherit.
}
```

### 29.3 MCP Authentication

```typescript
// MCP server auth middleware
app.use('/mcp', async (req, res, next) => {
    const token = req.headers.authorization?.replace('Bearer ', '');
    const mcpKey = await db.mcpKey.findOne({ where: { keyHash: sha256(token) } });
    if (!mcpKey || mcpKey.revokedAt) return res.status(401).end();
    req.agentId = mcpKey.agentId;
    req.userId = mcpKey.grantedBy;   // The human who authorized this
    req.scopes = mcpKey.allowedScopes;
    next();
});

// MCP client config (in Claude Desktop / custom agent)
const mcpConfig = {
    transport: 'http',
    url: 'https://tools.artkins.dev/mcp',
    auth: { type: 'bearer', token: process.env.MCP_API_KEY },
};
```

### 29.4 User Delegation Model

```typescript
interface AgentCapability {
    id: string;
    agentId: string;
    grantedBy: string;
    expiresAt: Date;             // MUST expire — no permanent agent access
    capabilities: string[];      // Minimal set only
    constraints: {
        maxCalls?: number;
        allowedResources?: string[];
        requireUserConfirmation?: boolean; // Human-in-the-loop for high stakes
    };
    nonDelegatable: boolean;
    auditLog: { action: string; resourceId: string; timestamp: Date }[];
}

async function executeAgentAction(capToken: string, action: string, resourceId: string) {
    const cap = await verifyCapability(capToken);
    if (new Date() > cap.expiresAt) throw new Error('Capability expired');
    if (!cap.capabilities.includes(action)) throw new Error(`Action '${action}' not in scope`);
    if (cap.constraints.allowedResources &&
        !cap.constraints.allowedResources.includes(resourceId))
        throw new Error('Resource not permitted');

    if (cap.constraints.requireUserConfirmation)
        await requestUserApproval(cap.grantedBy, action, resourceId);

    // Log EVERY action
    await db.agentAction.create({ data: {
        capabilityId: cap.id, agentId: cap.agentId,
        userId: cap.grantedBy, action, resourceId, timestamp: new Date(),
    }});

    return executeTool(action, resourceId);
}
```

### 29.5 Prompt Injection Defense

```typescript
// Auth decisions happen OUTSIDE the LLM context — never let the LLM modify its own permissions
const ALLOWED_TOOLS = ['read_transaction', 'generate_report', 'summarize_budget'];
const TOOL_SCOPE_MAP: Record<string, string> = {
    'read_transaction': 'transaction:read',
    'generate_report':  'report:generate',
    'summarize_budget': 'budget:read',
};

async function executeLLMTool(agentToken: string, toolName: string, input: unknown) {
    const cap = await verifyAgentJWT(agentToken);

    // Allowlist — LLM cannot invoke arbitrary tools
    if (!ALLOWED_TOOLS.includes(toolName))
        throw new Error(`Tool '${toolName}' not in agent allowlist`);

    // Scope — LLM cannot escalate permissions
    const required = TOOL_SCOPE_MAP[toolName];
    if (!cap.capabilities.includes(required))
        throw new Error(`Agent lacks scope '${required}'`);

    // Only execute after ALL auth checks pass in CODE — not based on LLM output
    return executeTool(toolName, input);
}
```

---

## 30. FULL IDENTITY ARCHITECTURE DECISION MATRIX

### 30.1 By System Type

| System | Authentication | Authorization | Token | Storage |
|---|---|---|---|---|
| **Next.js SaaS** | Clerk / Better Auth | RBAC | JWT 15min + Refresh | httpOnly cookie |
| **Mobile App** | Clerk Expo / Supabase | RBAC | JWT in memory + Refresh in Keychain | Keychain / Keystore |
| **REST API (public)** | API Keys + OAuth CC | Scopes | JWT / Opaque | Service accounts |
| **Internal Microservices** | mTLS / SPIFFE | ABAC / OPA | JWT-SVID / X.509 cert | Workload identity |
| **CLI Tool** | Device Auth Flow / PAT / SSH | Scopes | Opaque in OS keychain | keytar |
| **Desktop App** | OAuth PKCE via system browser | RBAC | JWT in memory + Refresh | Windows DPAPI / Keychain / libsecret |
| **AI Agent** | Capability token | Capability-based | Short-lived scoped JWT | Secret manager |
| **B2B SaaS** | SAML / OIDC | RBAC + ABAC | OIDC tokens | Enterprise IdP |
| **M-Pesa / Daraja** | OAuth CC (Safaricom) | N/A (Safaricom controls) | Short-lived (1h) | Encrypted env / Vault |
| **Payment (SCA/PSD2)** | SCA + 3DS + OTP | ABAC (amount/risk) | Short-lived | HSM-backed |
| **IoT Device** | mTLS + device cert | Resource policies | X.509 SVID | TPM / Secure Element |
| **Browser Extension** | OAuth PKCE | Scopes | JWT | chrome.storage.session |
| **WebSocket / Real-time** | JWT at handshake | Channel-level | JWT (short-lived) | In-memory |
| **Enterprise (AD/LDAP)** | SAML / LDAP bind | RBAC via group membership | OIDC via Keycloak | Enterprise IdP |

---

### 30.2 Threat → Defense Map

| Threat | Defense |
|---|---|
| Password breach | Argon2id + HIBP check + passkeys |
| Token theft (web) | httpOnly cookie + SameSite=Lax + 15min expiry |
| Token theft (mobile) | Keychain/Keystore + biometric gate |
| XSS → token extraction | httpOnly cookie — not localStorage |
| CSRF | SameSite=Lax + CSRF token on mutations |
| JWT algorithm confusion | Pin RS256; reject `alg:none` and HS256 |
| JWT key confusion | Separate key registry per issuer |
| Token replay | JTI + short expiry + Redis blocklist |
| Refresh token theft | Rotation + reuse detection → revoke all sessions |
| Brute force | Rate limit + lockout after 10 attempts + CAPTCHA |
| Credential stuffing | HIBP check + velocity detection |
| Session hijacking | Bind to IP/user-agent + revalidate on change |
| Man-in-the-middle | TLS + cert pinning on mobile |
| Prompt injection (agents) | Tool allowlist + auth OUTSIDE LLM context |
| Secret leak in git | SOPS + Infisical + gitleaks pre-commit hook |
| SIM swap → SMS 2FA bypass | App-based TOTP or hardware key instead of SMS |
| Phishing | Passkeys (phishing-resistant by design) |
| Insider threat | Least privilege + audit logs + MFA for admin |

---

### 30.3 Authorization Model Selection

| Scenario | Model | Reason |
|---|---|---|
| SaaS with fixed roles (admin/user/viewer) | RBAC | Simple, easy to understand |
| Healthcare (patient data across departments) | RBAC + ABAC | User + resource attributes both matter |
| File sharing (any user shares with any user) | ReBAC | Relationship graph — not roles |
| API gateway (rate limits, features per plan) | Scope-based | Per-token capability |
| Multi-cloud / regulatory compliance | PBAC / OPA | Externalize policy from code |
| AI agent permissions | Capability-based | Attenuation, delegation control, audit |
| Banking / high-stakes financial | ABAC + step-up | Context (time, location, amount) determines access |
| B2B enterprise | RBAC per org + SAML group sync | Group membership driven by customer's IdP |

---

### 30.4 Token Lifetime Reference

| Token | Lifetime | Notes |
|---|---|---|
| Access token (web) | 15 minutes | Short — limits breach window |
| Access token (mobile) | 15–30 min | Slightly longer OK (secure storage) |
| Service-to-service JWT | 5 minutes | Cache; don't re-fetch per request |
| Refresh token (web) | 7–30 days | Rotated on use, httpOnly cookie |
| Refresh token (mobile) | 30–90 days | Keychain/Keystore |
| API key | No expiry (rotatable) | Rotate on compromise or quarterly |
| PAT | User-set (default 90 days) | Warn 14 days before expiry |
| MCP capability token | 1–4 hours | Agent sessions must be short |
| SPIFFE SVID | 1 hour | SPIRE agent auto-rotates |
| Device auth code | 15 minutes | RFC 8628 |
| SAML assertion | 5 minutes | One-time use |
| OAuth auth code | 10 minutes | Must exchange immediately |
| M-Pesa access token | ~3599 seconds | Cache with 60s buffer |
| Session cookie (absolute) | 30 days | Hard limit regardless of activity |
| Daraja STK request | 60 seconds | User must respond to M-Pesa prompt within 1 min |

---

### 30.5 Quick Decision Tree

```
STARTING A NEW PROJECT?

├── Web App / SaaS?
│   ├── Using Supabase DB?             → Supabase Auth (50K MAU free, bundled)
│   ├── Best DX + polished UI?         → Clerk (10K MAU free)
│   ├── Self-hosted required?          → Better Auth / Auth.js v5
│   ├── Enterprise SSO from day 1?     → WorkOS
│   └── Scale early (>10K users)?      → WorkOS AuthKit (1M MAU free)
│
├── Mobile App?
│   ├── Expo / React Native?           → @clerk/clerk-expo + expo-secure-store
│   ├── Flutter?                       → supabase_flutter
│   └── High security?                 → Add biometrics + cert pinning + attestation
│
├── API / Backend?
│   ├── Public API for developers?     → API Keys + OAuth2 CC flow
│   ├── Internal microservice?         → mTLS / SPIFFE / SPIRE
│   ├── Third-party app integration?   → OAuth2 Authorization Code + PKCE
│   └── M-Pesa / Daraja?              → getDarajaAccessToken() → Section 20.8
│
├── CLI Tool?
│   └── Device Authorization Flow (RFC 8628) + keytar for storage
│
├── Desktop App?
│   └── OAuth2 PKCE via system browser (not WebView) + OS credential manager
│
├── AI Agent / MCP?
│   └── Capability tokens + delegation + audit log → Section 29
│
├── Enterprise / B2B?
│   ├── Client uses ADFS / old Okta?   → SAML 2.0 (WorkOS or Auth0)
│   └── Modern enterprise?             → OIDC + SCIM provisioning
│
└── Implementing JWT yourself?
    └── RS256 + 15min + 30d refresh + rotation + Redis blocklist → Section 3
```

---

*Volume II complete — June 2026 · Lyncxs Industries / Lyncxs Industries · Nakuru, Kenya*
*Platforms: Web · Android · iOS · Expo · CLI · Desktop (Electron) · Browser Extensions · WebSockets · IoT · M-Pesa/Daraja · Enterprise/SAML · AI Agents / MCP*
*Auth Models: RBAC · ABAC · PBAC/OPA · ReBAC/Zanzibar · Capability-based · Scope-based · RLS*
*Token Systems: JWT · PASETO · Opaque · Macaroons · DPoP/PoP*
*Infra: Argon2id · Passkeys/WebAuthn · PKI/mTLS/SPIFFE · Infisical · HashiCorp Vault · SOPS · Zero Trust*
*Standards: OAuth 2.1 · OIDC · SAML 2.0 · SCIM 2.0 · LDAP · RFC 8628 · NIST SP 800-63B · FIDO2*
*Cross-references: AGENTIC-SECURITY.md · EMAIL-SERVICES-GUIDE.md · MULTI-LANGUAGE-ARCHITECTURE-PROGRAMMING.md*

---

# ADDENDUM v1.1.0 (2026-08-25): PAYMENT PLATFORM AUTH ALIGNMENT

**Reference**: LPIP VI§31 (API Keys & Authentication), IV§19 (Onboarding), MASTER §50. Status: NORMATIVE for payment platform auth.

## K1. API Key Scheme (platform-standard)

1. Key formats: `pk_{live|test}_...` publishable, `sk_{live|test}_...` secret. Secret keys shown exactly once at creation; stored hashed (SHA-256+) with prefix hint for UI display.
2. Keys are scoped to Environment (sandbox vs production) and carry capability claims resolved server-side from merchant/provider-account state — keys never encode entitlements themselves.
3. Rotation: create-new → overlap window → revoke-old, fully self-service; rotation invalidates adapter credential caches cluster-wide within seconds.
4. Authentication middleware resolves: key → environment → project → organization(tenant); the resolved tenant context feeds RLS (`app.current_organization_id`) for every query in the request.

## K2. RBAC Alignment

Portal roles (OWNER/ADMIN/OPERATIONS/ANALYST) and admin roles (SUPERADMIN/OPERATIONS/SUPPORT/COMPLIANCE) map onto this guide's role primitives. Impersonation is consent-gated, read-only-by-default, and fully audited.

## K3. Change Report
- Added: K1–K3. No prior sections modified or removed.
