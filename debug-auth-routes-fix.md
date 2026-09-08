# Debug Session: auth-routes-fix
Status: [OPEN]
Created: 2026-09-08
Session ID: auth-routes-fix

## Symptoms
1. POST /api/auth/register returns HTTP 200 with body `{"message": "Not implemented yet"}` instead of 201 Created with token pair
2. No user entry appears in database (DBeaver) after registration attempt
3. Same behavior for all auth routes

## Hypotheses (Falsifiable)
1. H1: Old compiled binaries are running — the source code has fixed controllers but a stale build is serving requests (rebuild needed)
2. H2: Missing/invalid ConnectionStrings:Default configuration → DbContext creation fails → fallback middleware returns old scaffold 200 response
3. H3: A catch-all exception handler or middleware intercepts the auth route and returns the old "Not implemented yet" placeholder instead of propagating the real error
4. H4: Environment variable loading fails — appsettings.Local.json / env vars aren't being picked up, so services fail silently
5. H5: Migrations haven't been applied — database schema is missing tables → SaveChangesAsync fails but error is swallowed

## Evidence Log
- [ ] Reproduction attempt
- [ ] Configuration validation
- [ ] Migration status check
- [ ] Source code grep for "Not implemented yet"
- [ ] Full rebuild + verification

## CodeRabbit Review Items (to verify)
| Item | Status | Evidence |
|------|--------|----------|
| Program.cs: Redis fallback URI format | Pending | Source check |
| Program.cs: IConnectionMultiplexer lifetime | Pending | Source check |
| AuthService: Login password timing oracle | Pending | Source check |
| AuthService: Register DbUpdateException → DuplicateEmailException | Pending | Source check |
| AuthService: Refresh token hex format validation (64 chars) | Pending | Source check |
| AuthRepository: FamilyId + atomic rotation | Pending | Source check |
| AuthRepository: Concurrent refresh test | Pending | Tests check |
| GRAPHQL-STATUS.md: Feature 07 status sync | Pending | Source check |
| DEPENDENCY-AUDIT.md: Next spec reference | Pending | Source check |
| Email-OTP 2FA: Resend + branded templates | Pending | Source check |
| ai-features-research.md: Feature specs | Pending | Specs check |
