# Implementation Checklist - CodeRabbit Review Items

> **STATUS:** 32/42 issues resolved in SPECIFICATIONS. 10 issues require CODE IMPLEMENTATION (documented below).

---

## ✅ RESOLVED IN PLANNING PHASE (32/42)

All specification/documentation issues are complete. See commit history for details.

---

## 🔨 IMPLEMENTATION PHASE WORK (10/42)

**These are DOCUMENTED with FIXME/TODO markers. Implement during coding phase.**

### Blob Storage Security (5 items - CRITICAL)

#### 1. File Content Validation
**Location:** `backend/project-kit/feature-specs/11-blob-storage-integration.md:299`  
**FIXME:** Add extension + magic-byte validation  
**Effort:** 1 day  
**Details:** Implement extension + magic-byte validation on upload; block executable content types; enforce size limits per workspace quota.

#### 2. Atomic Quota Enforcement  
**Location:** `backend/project-kit/feature-specs/11-blob-storage-integration.md:308`  
**FIXME:** Implement WorkspaceStorageReservations table + atomic reservation  
**Effort:** 2 days  
**Details:** Implement WorkspaceStorageReservations table with transactional blob upload → reservation commit; rollback blob upload if reservation fails.

#### 3. Metadata Persistence Compensation
**Location:** `backend/project-kit/feature-specs/11-blob-storage-integration.md:289-306`  
**FIXME:** Delete uploaded blob if database persistence fails  
**Effort:** 1 day  
**Details:** If SaveChanges fails after successful blob storage upload, issue compensating blob DELETE to prevent orphaned storage content.

#### 4. Deletion Failure Handling
**Location:** `backend/project-kit/feature-specs/11-blob-storage-integration.md:237-240`  
**FIXME:** Propagate blob deletion failures, don't remove metadata  
**Effort:** 1 day  
**Details:** If underlying blob storage DELETE returns non-success, do NOT remove the metadata row; surface error to caller for retry; track orphaned blobs for periodic cleanup job.

#### 5. Workspace Authorization
**Location:** `backend/project-kit/feature-specs/11-blob-storage-integration.md:301,347,354`  
**FIXME:** Add workspace membership checks in upload/list/delete  
**Effort:** 2 days  
**Details:** Before upload/list/delete, verify caller's ClaimsPrincipal sub (userId) has a WorkspaceMembership row for the target workspaceId with at least Member role (defined workspace roles: Owner, Admin, Member).

**Blob Security Total:** 7 days

---

### Architecture & Code Quality (5 items)

#### 6. Layer Violations (DashboardService)
**Location:** `backend/project-kit/context/architecture.md:30-31`  
**Issue:** Application layer calling Redis/Dapper directly  
**Fix:** Create ICacheService, IDashboardRepository abstractions  
**Effort:** 2 days  
**Details:** Extract direct Redis/Dapper calls from DashboardService into IDashboardRepository (Infrastructure) and ICacheService abstraction; Application layer depends only on interfaces.

#### 7. Token Validation on Startup
**Location:** `backend/project-kit/feature-specs/11-blob-storage-integration.md:212-213`  
**Fix:** Add BlobStorageOptions validation with ValidateOnStart  
**Effort:** 0.5 days  
**Details:** Bind BlobStorageOptions with OptionsBuilder<BlobStorageOptions>, then call ValidateDataAnnotations().ValidateOnStart() to verify account name/container/SAS during host startup, not at first upload.

#### 8. Idempotency Keys Infrastructure
**Location:** `docs/planning/NFR.md:73`  
**Fix:** IdempotencyKeys table + middleware + 24h cache  
**Effort:** 3 days  
**Details:** Create IdempotencyKeys table (Key PK, RequestHash, ResponseBody, ExpiresAt); middleware checks Idempotency-Key header on write requests; cache GET in Redis 24h.

#### 9. Browser Refresh Serialization
**Location:** `docs/planning/CACHING-REFRESH-SYNC-STRATEGY.md:341-346`  
**Fix:** Share single in-flight refresh promise to prevent concurrent calls  
**Effort:** 1 day  
**Details:** Web (TanStack Query) and Mobile (GraphQL Flutter) each maintain one in-flight refresh-token Promise/Future; concurrent callers within each client await the same result to avoid double rotation.

#### 10. Concurrent Refresh Token Rotation
**Location:** `docs/planning/CACHING-REFRESH-SYNC-STRATEGY.md:733-735`  
**Fix:** Atomic claim with @@ROWCOUNT check in single transaction  
**Effort:** 2 days  
**Details:** AuthRepository.RotateRefreshTokenAsync uses provider-default transaction (parameterless BeginTransactionAsync) + conditional ExecuteUpdate WHERE RevokedAt IS NULL; 0 rows affected = reuse evidence = family-scoped RevokeFamilyAsync.

**Architecture Total:** 8.5 days

---

## 📋 Implementation Priority

**Week 6 (Security - CRITICAL):**
- [ ] Item 1: File validation (1d)
- [ ] Item 2: Quota atomicity (2d)
- [ ] Item 5: Workspace authorization (2d)
- [ ] Item 7: Token validation (0.5d)

**Week 7 (Reliability):**
- [ ] Item 3: Metadata compensation (1d)
- [ ] Item 4: Deletion failures (1d)
- [ ] Item 9: Browser refresh (1d)
- [ ] Item 10: Token rotation (2d)

**Week 8+ (Architecture):**
- [ ] Item 6: Layer violations (2d)
- [ ] Item 8: Idempotency (3d)

**Total Effort:** 15.5 days

---

## ✅ Verification

Before claiming complete, run:

```bash
# 1. All security FIXMEs documented
grep -r "FIXME (Security)" backend/project-kit/feature-specs/11-blob-storage-integration.md
# Should return 6 matches (lines 299, 300, 308, 347, 354, 301)

# 2. No implementation code added during review
find backend -name "*.cs" -o -name "*.csproj"
# Should return empty (planning phase only)

# 3. All TODOs documented
grep -r "TODO" backend/project-kit/ docs/planning/ | wc -l
# Should show count of documented work items
```

---

## 📖 Full Details

Each item above includes inline implementation details. For architectural decisions,
see `docs/decisions/ADR-003-auth-architecture-otp-security.md` (auth) and
related ADRs in `docs/decisions/`. For feature-specific guidance, see the
owning feature spec under `<system>/project-kit/feature-specs/`.

**Planning phase complete:** ✅  
**Implementation phase:** Ready to begin (15.5 days estimated)

## Implemented authentication contract (Feature 07)

Use the [auth contract](../api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.
