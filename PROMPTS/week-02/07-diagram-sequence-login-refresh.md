# Week 02 · Diagram 05A — Sequence: Login & Refresh Rotation

**Master spec + Figma Make paste prompts.** One sequence diagram per critical flow. This one covers **credentials → access token issuance → refresh token stored → rotation (incl. the replay/race failure branch)** — the riskiest auth flow.

---

## 1. Lifelines (actor / component verticals, left→right)

1. **User / Client** (web or mobile)
2. **AuthController** (`/api/auth/*`)
3. **AuthService** (Griot.Application)
4. **Users / RefreshTokens (DB)**
5. **Redis**

## 2. The happy path (login)

STYLE: light canvas (#F7F8FA), white boxes with 1px hairlines, token-named fills only (per docs/design/MASTER-DESIGN-SYSTEM.md), readable at 100% zoom, one page.
```
User → AuthController: POST /api/auth/login {email, password}
AuthController → AuthService: LoginAsync(dto)
AuthService → Users(DB): find by email
AuthService: Argon2 verify hash
AuthService: generate access JWT (15-min, sub/email/jti) + opaque refresh (256-bit)
AuthService: derive FamilyId = new GUID (root of a new rotation chain)
AuthService → RefreshTokens(DB): store { TokenHash, FamilyId, UserId,
    ReplacedByTokenId=NULL, ExpiresAt }
AuthService → Redis: record session + rate-limit window
AuthService → AuthController: { accessToken, refreshToken, user }
AuthController → User: 200 { accessToken, user }
    [web: refreshToken in Set-Cookie httpOnly Secure SameSite=Strict
     mobile/Postman: JSON body { refreshToken }]
STYLE: light canvas (#F7F8FA), white boxes with 1px hairlines, token-named fills only (per docs/design/MASTER-DESIGN-SYSTEM.md), readable at 100% zoom, one page.
```

## 3. The happy path (refresh rotation)

STYLE: light canvas (#F7F8FA), white boxes with 1px hairlines, token-named fills only (per docs/design/MASTER-DESIGN-SYSTEM.md), readable at 100% zoom, one page.
```
User → AuthController: POST /api/auth/refresh
    [web: httpOnly cookie | mobile: secure storage | Postman: JSON body { refreshToken }]
AuthController → AuthService: RefreshAsync(token)
AuthService → RefreshTokens(DB): BEGIN TRANSACTION
    DECLARE @foundId uniqueidentifier, @familyId uniqueidentifier, @userId uniqueidentifier
    SELECT @foundId = Id, @familyId = FamilyId, @userId = UserId
        FROM RefreshTokens 
        WHERE TokenHash = @hash 
          AND RevokedAt IS NULL 
          AND ExpiresAt > SYSUTCDATETIME()  -- Security: reject expired tokens
    IF @foundId IS NULL
        → EXPIRED or REPLAY detected (not found OR already revoked OR expired) → ROLLBACK, return 401
    UPDATE RefreshTokens SET RevokedAt = SYSUTCDATETIME(), ReplacedByTokenId = @newId
        WHERE Id = @foundId AND RevokedAt IS NULL   -- atomic guard: only one caller succeeds
    IF @@ROWCOUNT = 0
        → race lost (another caller revoked first) → REPLAY path
    INSERT RefreshTokens (Id = @newId, TokenHash = @newHash, FamilyId = @familyId,
        UserId = @userId, ReplacedByTokenId = NULL, ExpiresAt = @exp, CreatedAt = SYSUTCDATETIME())
    COMMIT
alt row found + RevokedAt IS NULL + ExpiresAt > SYSUTCDATETIME() (within transaction)
    AuthService: issue new access JWT
    AuthService → Redis: update session
    AuthController → User: 200 { new accessToken }
        [web: new refresh in Set-Cookie httpOnly; mobile: JSON { refreshToken }; Postman: JSON { refreshToken }]
else row not found OR RevokedAt already set
    AuthService: REVOKE ENTIRE FAMILY (same FamilyId) — replay detected
    AuthController → User: 401 (clear session)
STYLE: light canvas (#F7F8FA), white boxes with 1px hairlines, token-named fills only (per docs/design/MASTER-DESIGN-SYSTEM.md), readable at 100% zoom, one page.
```

> **Transport rule** (aligned with `SECURITY.md`): The refresh token is *never* returned in a `localStorage`-accessible field on web. Web clients receive it via `Set-Cookie: refreshToken=…; HttpOnly; Secure; SameSite=Strict`. Mobile clients receive it in the JSON response body and store it in platform secure storage (Android Keystore / iOS Secure Enclave). Postman / API testing uses the JSON body field `refreshToken` directly.

> **Redis contract** (matches `integration-contracts.md`, `sababisha-redis`): keys `sess:{userId}` (session + refresh family metadata), `rtm:{tokenHash}` (rotation metadata), `rl:{ip}:{route}` (rate-limit windows), `budget:{workspaceId}` (AI token budgets). Metadata only — SQL Server `RefreshTokens` stays the source of truth. Mobile stores the refresh token in `flutter_secure_storage` (Android Keystore / iOS Secure Enclave).

> **Transaction invariant**: The `WHERE RevokedAt IS NULL` predicate inside the transaction is the sole gate. Any path that does *not* match this predicate — whether the row is missing entirely or already has `RevokedAt` set — is treated identically as a replay attempt and triggers full family revocation. There is no "soft miss" case.

## 4. The failure branch (replay race — the Week-6 OWASP case)

STYLE: light canvas (#F7F8FA), white boxes with 1px hairlines, token-named fills only (per docs/design/MASTER-DESIGN-SYSTEM.md), readable at 100% zoom, one page.
```
Attacker → AuthController: POST /api/auth/refresh
    [presents stolenRefresh via cookie or body]
AuthService → RefreshTokens(DB): BEGIN TRANSACTION
    -- Security fix: Recover FamilyId by TokenHash without RevokedAt filter
    DECLARE @familyId uniqueidentifier
    SELECT @familyId = FamilyId FROM RefreshTokens WHERE TokenHash = @hash
    
    IF @familyId IS NULL
        → token hash not found (invalid token) → ROLLBACK, return 401
    
    -- Now check if already rotated/revoked
    IF NOT EXISTS (SELECT 1 FROM RefreshTokens WHERE TokenHash = @hash AND RevokedAt IS NULL)
        → REPLAY detected (already rotated OR already revoked)
        UPDATE RefreshTokens SET RevokedAt = SYSUTCDATETIME()
            WHERE FamilyId = @familyId AND RevokedAt IS NULL  -- revoke entire family
        COMMIT
        → return 401 with replay indicator
    ELSE
        → normal rotation path (should not reach here if initial SELECT failed)
    
AuthController → Attacker: 401
Note: the legitimate client's NEXT refresh also fails (same FamilyId revoked)
      → must re-login. This is DESIRED: FamilyId scopes the blast radius; rotation is one-time-use.
STYLE: light canvas (#F7F8FA), white boxes with 1px hairlines, token-named fills only (per docs/design/MASTER-DESIGN-SYSTEM.md), readable at 100% zoom, one page.
```

> **FamilyId invariant**: Every row in `RefreshTokens` carries the `FamilyId` of the issuance event. On replay the revocation `UPDATE` targets only `WHERE FamilyId = @familyId` — it does not revoke tokens in other families (other sessions) for the same user. This bounds the impact to the compromised session while still invalidating every token that traces back to the leaked root.

> **RefreshTokens schema** (from ERD): `Id` (PK), `TokenHash` (SHA-256 of opaque token), `FamilyId` (GUID, assigned at login, propagated on rotation), `UserId` (FK), `ReplacedByTokenId` (FK, self-ref), `ExpiresAt`, `RevokedAt`, `CreatedAt`. Index on `(TokenHash, RevokedAt)` for `WHERE TokenHash = @hash AND RevokedAt IS NULL` query. Index on `FamilyId` for family-scoped revocation on replay.

## 5. The prompt (single, extensive — no length limit)

Paste the full prompt below into Figma Make. It draws all three sequence frames in one pass.

```text
UML sequence diagram: Griot login + refresh rotation. Lifelines left→right: User/Client,
AuthController (/api/auth/*), AuthService (Griot.Application),
SQL Server (Users/RefreshTokens), Redis. Use three labeled frames.

FRAME 1 — LOGIN (happy path):
User → AuthController: POST /api/auth/login {email, password}
AuthController → AuthService: LoginAsync(dto)
AuthService → SQL Server: find user by email
AuthService [self]: Argon2 verify hash (no timing leak)
AuthService [self]: generate access JWT (15-min, claims sub/email/jti) + opaque refresh (256-bit)
AuthService [self]: FamilyId = new GUID (root of rotation chain)
AuthService → SQL Server: INSERT RefreshTokens
    { TokenHash=SHA256(token), FamilyId, UserId, ReplacedByTokenId=NULL, ExpiresAt }
AuthService → Redis: record session + rate-limit window
AuthController → User: 200 { accessToken, user }
Note over AuthController,User: Transport: web → Set-Cookie refreshToken HttpOnly Secure SameSite=Strict
    mobile/Postman → JSON body { refreshToken }

FRAME 2 — REFRESH (happy path):
User → AuthController: POST /api/auth/refresh
Note over User,AuthController: web: httpOnly cookie | mobile/Postman: JSON body { refreshToken }
AuthController → AuthService: RefreshAsync(token)
AuthService → SQL Server: BEGIN TRANSACTION
    SELECT ... WHERE TokenHash = @hash AND RevokedAt IS NULL AND ExpiresAt > SYSUTCDATETIME()
AuthService [self]: if no row → treat as EXPIRED or REPLAY → return 401 (GOTO FRAME 3 if replay suspected)
    UPDATE RefreshTokens SET RevokedAt=NOW(), ReplacedByTokenId=@newId
        WHERE Id=@foundId AND RevokedAt IS NULL  [race guard]
    INSERT RefreshTokens { TokenHash=newHash, FamilyId=same, UserId, ReplacedByTokenId=oldId, ExpiresAt }
    COMMIT
AuthService [self]: issue new access JWT
AuthService → Redis: update session
AuthController → User: 200 { accessToken }
Note over AuthController,User: Transport: web → Set-Cookie refreshToken HttpOnly Secure SameSite=Strict
    mobile/Postman → JSON body { refreshToken }

FRAME 3 — REPLAY RACE (failure branch, red border):
Attacker [separate lifeline] → AuthController: POST /api/auth/refresh (stolen token)
AuthService → SQL Server: BEGIN TRANSACTION
    SELECT ... WHERE TokenHash = @hash AND RevokedAt IS NULL → NOT FOUND or already revoked
    UPDATE RefreshTokens SET RevokedAt=NOW() WHERE FamilyId=@familyId  [scoped to family only]
    COMMIT
AuthController → Attacker: 401
Annotation: "Miss on WHERE RevokedAt IS NULL = replay. FamilyId scopes revocation to the
    compromised session — other user sessions are unaffected. Legitimate client's next
    refresh also 401s → must re-login. Desired: rotation is one-time-use. (OWASP A07)"
STYLE: light canvas (#F7F8FA), white boxes with 1px hairlines, token-named fills only (per docs/design/MASTER-DESIGN-SYSTEM.md), readable at 100% zoom, one page.
```

### Refine

- "Add a self-arrow on AuthService: Argon2 verify (no timing leak)."
- "Rename the DB lifeline to 'SQL Server: Users + RefreshTokens'."
- "Make the replay-race frame red-bordered."
- "Annotate the FamilyId field on the INSERT arrow in Frame 1 and 2."

---

## Definition of Done

- [ ] Login + refresh + replay-race frames all present
- [ ] Rotation (revoke old → insert new → ReplacedByTokenId) explicit and inside a single transaction
- [ ] `FamilyId` column present in `RefreshTokens`; assigned at login; propagated on rotation; used for scoped revocation on replay
- [ ] `WHERE RevokedAt IS NULL` is the sole gate; a miss (row absent or already revoked) triggers family revocation — no soft-miss path
- [ ] 401 + family-revoke (`WHERE FamilyId = @familyId`) on replay; other user sessions unaffected
- [ ] Transport documented on every auth response: web = httpOnly Secure cookie; mobile = JSON body + secure storage; Postman = JSON body
- [ ] Labels match `jwt-argon2-auth` skill
- [ ] Approved → PNG → `diagrams/architecture/sequence-login-refresh.png`

## Implemented authentication contract (Feature 07)

Use the [auth contract](../../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

The current REST transport uses JSON refresh tokens for Postman/mobile. Web
HttpOnly cookie transport in the design remains a backend prerequisite for web
Feature 05; do not treat the cookie diagrams as live behavior or store tokens in
localStorage. SQL Server owns refresh rows; Redis currently owns login limits.
