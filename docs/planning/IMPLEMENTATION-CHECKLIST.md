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
**Details:** See CODERABBIT-REMAINING-ISSUES.md §1.1

#### 2. Atomic Quota Enforcement  
**Location:** `backend/project-kit/feature-specs/11-blob-storage-integration.md:308`  
**FIXME:** Implement WorkspaceStorageReservations table + atomic reservation  
**Effort:** 2 days  
**Details:** See CODERABBIT-REMAINING-ISSUES.md §1.2

#### 3. Metadata Persistence Compensation
**Location:** `backend/project-kit/feature-specs/11-blob-storage-integration.md:289-306`  
**FIXME:** Delete uploaded blob if database persistence fails  
**Effort:** 1 day  
**Details:** See CODERABBIT-REMAINING-ISSUES.md §1.3

#### 4. Deletion Failure Handling
**Location:** `backend/project-kit/feature-specs/11-blob-storage-integration.md:237-240`  
**FIXME:** Propagate blob deletion failures, don't remove metadata  
**Effort:** 1 day  
**Details:** See CODERABBIT-REMAINING-ISSUES.md §1.4

#### 5. Workspace Authorization
**Location:** `backend/project-kit/feature-specs/11-blob-storage-integration.md:301,347,354`  
**FIXME:** Add workspace membership checks in upload/list/delete  
**Effort:** 2 days  
**Details:** See CODERABBIT-REMAINING-ISSUES.md §1.5

**Blob Security Total:** 7 days

---

### Architecture & Code Quality (5 items)

#### 6. Layer Violations (DashboardService)
**Location:** `backend/project-kit/context/architecture.md:30-31`  
**Issue:** Application layer calling Redis/Dapper directly  
**Fix:** Create ICacheService, IDashboardRepository abstractions  
**Effort:** 2 days  
**Details:** See CODERABBIT-REMAINING-ISSUES.md §3.1

#### 7. Token Validation on Startup
**Location:** `backend/project-kit/feature-specs/11-blob-storage-integration.md:212-213`  
**Fix:** Add BlobStorageOptions validation with ValidateOnStart  
**Effort:** 0.5 days  
**Details:** See CODERABBIT-REMAINING-ISSUES.md §3.2

#### 8. Idempotency Keys Infrastructure
**Location:** `docs/planning/NFR.md:73`  
**Fix:** IdempotencyKeys table + middleware + 24h cache  
**Effort:** 3 days  
**Details:** See CODERABBIT-REMAINING-ISSUES.md §3.3

#### 9. Browser Refresh Serialization
**Location:** `docs/planning/CACHING-REFRESH-SYNC-STRATEGY.md:341-346`  
**Fix:** Share single in-flight refresh promise to prevent concurrent calls  
**Effort:** 1 day  
**Details:** See CODERABBIT-REMAINING-ISSUES.md (browser section)

#### 10. Concurrent Refresh Token Rotation
**Location:** `docs/planning/CACHING-REFRESH-SYNC-STRATEGY.md:733-735`  
**Fix:** Atomic claim with @@ROWCOUNT check in single transaction  
**Effort:** 2 days  
**Details:** See CODERABBIT-REMAINING-ISSUES.md (token rotation section)

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

**Complete implementation guidance with code examples:**  
See `docs/planning/CODERABBIT-REMAINING-ISSUES.md` (369 lines)

**Planning phase complete:** ✅  
**Implementation phase:** Ready to begin (15.5 days estimated)
