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

```
User → AuthController: POST /api/auth/login {email, password}
AuthController → AuthService: LoginAsync(dto)
AuthService → Users(DB): find by email
AuthService: Argon2 verify hash
AuthService: generate access JWT (15-min, sub/wid) + opaque refresh (256-bit)
AuthService → RefreshTokens(DB): store SHA-256 hash (rotation chain empty)
AuthService → Redis: record session + rate-limit window
AuthService → AuthController: { accessToken, refreshToken, user }
AuthController → User: 200 { accessToken, refreshToken }
```

## 3. The happy path (refresh rotation)

```
User → AuthController: POST /api/auth/refresh { refreshToken }
AuthController → AuthService: RefreshAsync(token)
AuthService → RefreshTokens(DB): load by TokenHash
alt token valid + not revoked + not expired
    AuthService: revoke old row; insert new hash (ReplacedByTokenId=old.Id)
    AuthService: issue new access JWT
    AuthService → Redis: update session
    AuthController → User: 200 { new accessToken, new refreshToken }
else invalid/revoked/expired
    AuthService: if token was already rotated → REVOKE ENTIRE FAMILY (replay!)
    AuthController → User: 401 (clear session)
```

## 4. The failure branch (replay race — the Week-6 OWASP case)

```
Attacker → AuthController: POST /api/auth/refresh { stolenRefresh }
AuthService → RefreshTokens(DB): hash not found (already rotated) OR found but ReplacedByTokenId set
AuthService: revoke all rows for family (same UserId + chain)
AuthController → User: 401
Note: the legitimate client's NEXT refresh also fails → must re-login. This is DESIRED: rotation is one-time-use.
```

## 5. Figma Make prompts (≤2000 chars)

### PROMPT A

```
UML sequence diagram: Griot login + refresh rotation. Lifelines L→R: User/Client, AuthController (/api/auth/*), AuthService (Griot.Application), SQL Server (Users/RefreshTokens), Redis.
Frame 1 LOGIN (happy): User→AuthController POST /api/auth/login {email,password}; AuthController→AuthService LoginAsync; AuthService→SQL Server find user by email; AuthService Argon2 verify; AuthService generate access JWT 15min(sub,wid)+opaque refresh; AuthService→SQL Server store SHA-256 hash; AuthService→Redis session+rate-limit; AuthController→User 200{accessToken,refreshToken}.
Frame 2 REFRESH (happy): User→AuthController POST /api/auth/refresh {refreshToken}; AuthController→AuthService RefreshAsync; AuthService→SQL Server load by hash; alt valid: revoke old + insert new (ReplacedByTokenId), issue new JWT, Redis update, 200; else invalid/revoked: 401.
Frame 3 REPLAY RACE (failure): attacker sends already-rotated token; AuthService revokes ENTIRE family; 401. Label the frame "replay → family revoke (OWASP case)".
```

### PROMPT B — failure branch emphasis

```
On the Griot login/refresh sequence diagram, make the replay-race frame visually distinct (red border): draw 'Attacker' lifeline separate from 'User'; show the second use of an already-rotated refresh token hitting RefreshTokens(DB), matching no active row, then AuthService revokes all rows for the family (same UserId), returns 401, and marks Redis session dead. Add an alt/else annotation: 'any rotated token reuse → whole family revoked'. Keep the diagram one page.
```

### Fix snippets

- "Add a self-arrow on AuthService: Argon2 verify (no timing leak)."
- "Rename the DB lifeline to 'SQL Server: Users + RefreshTokens'."
- "Move Redis calls below DB calls; keep ordering."

---

## Definition of Done

- [ ] Login + refresh + replay-race frames all present
- [ ] Rotation (revoke old → insert new → ReplacedByTokenId) explicit
- [ ] 401 + family-revoke on reuse; labels match `jwt-argon2-auth` skill
- [ ] Approved → PNG → `project-kit/diagrams/architecture/sequence-login-refresh.png`