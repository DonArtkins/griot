# LYNCXS MULTI-TENANT SYSTEMS ENGINEERING
## Building Multi-Tenant Platforms: Tenancy, Onboarding, Offboarding, Auth, RBAC, and Payments Across Web, Mobile, MCP, and AI Surfaces

**CLASSIFICATION: HIGHLY CLASSIFIED PRIVATE PROPERTY OF LYNCXS INDUSTRIES**
**Status**: NEW RESEARCH VOLUME — v1.1.0 (2026-09-12)
**Companion documents**: `LYNCXS-AUTH-SERVICES-GUIDE.md` (Vol I–II), `LYNCXS-WORLD-CLASS-SOFTWARE-ENGINEERING-ARCHITECTURE(WCSEA).md` §9, `LYNCXS-PAYMENT-INFRASTRUCTURE-PLATFORM.md` (LPIP), `LYNCXS-PAYMENT-INTEGRATION-MASTER.md`, `LYNCXS-WORLD-CLASS-PRODUCTION-SECURITY-ENGINEERING(WCPSE).md`, `LYNCXS-ORM-DATA-ACCESS-ENGINEERING.md`, `LYNCXS-GLOBAL-REGULATORY&COMPLIANCE-ENGINEERING.md`, `LYNCXS-AI-SYSTEMS-ENGINEERING.md`

**v1.1.0 change note**: Added §5.6.1–5.7 (MCP tool-registry RBAC at scale, multi-agent orchestration within tenant boundaries, tenant-scoped AI memory taxonomy, AI-specific threat additions), drawing on `LYNCXS-AI-SYSTEMS-ENGINEERING.md`'s Tool Engineering, Multi-Agent Systems, Memory Engineering, and AI Security sections. Renumbered former §5.7 (Cross-surface summary) to §5.8. No prior sections modified or removed.

---

## WHY THIS DOCUMENT EXISTS

Every reusable SaaS you build at Lyncxs — a POS system sold to many merchants, a project-management tool sold to many companies, an internal tool exposed to AI agents over MCP — shares one underlying shape: **one codebase, many tenants, each believing they have the system to themselves.** The engineering problem is always the same five questions, asked in the same order:

1. How do we let a new company in? (**onboarding**)
2. How do we prove who's asking, on every surface — browser, phone, CLI, AI agent? (**authentication**)
3. How do we decide what they're allowed to do, and stop it leaking across tenants? (**authorization / RBAC**)
4. How do we take their money, or move money on their behalf, without becoming a bank? (**payments**)
5. How do we get a company out — cleanly, completely, provably — when they leave? (**offboarding**)

This document is the synthesis volume for those five questions. It doesn't repeat the Auth Services Guide's provider-by-provider walkthroughs or LPIP's full payment platform architecture — it shows how those existing volumes compose into one coherent multi-tenant system, adds the pieces neither document covers in depth (SCIM-based lifecycle automation, ReBAC/OpenFGA for complex permission graphs, MCP/AI-agent tenancy, Stripe-Connect-style embedded payments for platforms), and gives you the reference architecture to build **any** multi-tenant product — POS, project management, or otherwise — on top of the Lyncxs stack.

---

## TABLE OF CONTENTS

1. Foundations — What "Multi-Tenant" Actually Means
2. Tenancy Models — Choosing Your Isolation Boundary
3. The Canonical Domain Model
4. Onboarding — Getting a Tenant Into the System
5. Authentication Across Every Surface (Web, Mobile, API, MCP, AI Agents)
6. Authorization — RBAC, ABAC, ReBAC, and When to Use Each
7. Payments in a Multi-Tenant System
8. Offboarding — Getting a Tenant Out, Cleanly
9. Cross-Cutting Concerns: Rate Limiting, Caching, Storage, Async Work
10. Threat Model and Security Checklist
11. Reference Architecture Walkthroughs — POS and Project Management Tool
12. Decision Matrices and Quick Reference
13. Sources

---

## 1. FOUNDATIONS — WHAT "MULTI-TENANT" ACTUALLY MEANS

A **tenant** is the unit of isolation and billing in your system — usually a company, sometimes a single professional, occasionally a sub-organization inside a larger customer. Multi-tenancy means one running system (one codebase, one or a few deployed instances) serves many tenants, each of whom must experience the product as if it were built only for them, and none of whom can see, touch, or degrade another's data or performance.

Three properties define a healthy multi-tenant system, and they map directly onto the five questions above:

- **Isolation** — tenant A's data, files, cache entries, queue jobs, and audit trail are invisible to tenant B, enforced at a boundary that survives a bug in application code, not merely by convention.
- **Identity clarity** — at every point in the request lifecycle, the system knows *which human or agent* is acting and *on behalf of which tenant*, and never lets the caller simply assert the second half.
- **Lifecycle completeness** — a tenant can be created, have its membership and roles change continuously, and be fully and provably removed, with every stage audited.

Everything below is elaboration of these three properties for each of the five questions, plus the payments dimension, which has its own regulatory and financial-correctness requirements layered on top of ordinary multi-tenancy.

---

## 2. TENANCY MODELS — CHOOSING YOUR ISOLATION BOUNDARY

This document adopts the same three-model taxonomy used consistently across the Lyncxs knowledge base (WCSEA §9.1, LPIP §4.1, Auth Services Guide §16, and the OWASP Multi-Tenant Security Cheat Sheet all converge on this structure), because it is the industry-standard framing:

| Model | Boundary | Isolation | Operational cost | Use when |
|---|---|---|---|---|
| **Silo** (database-per-tenant) | Separate database + credentials | Maximum | High — N databases to patch, back up, migrate | Regulated industries, enterprise contracts demanding physical isolation, very large tenants |
| **Bridge** (schema-per-tenant) | Separate schema, shared server | Strong — namespace + role boundary | Medium — disciplined grants and `search_path` handling required | Dozens–hundreds of tenants, or automatic promotion target for tenants that outgrow Pool |
| **Pool** (shared tables, row-level) | `tenant_id` column + Postgres RLS policy | Policy-and-row boundary — as strong as your policy coverage and role discipline | Low — single schema, simple migrations | **The correct default.** Standard B2B SaaS, thousands of tenants, small-to-mid tenants |

A fourth pattern worth naming explicitly because it shows up constantly in production: **Hybrid tiered promotion** — start every tenant in Pool, and automatically (or contractually) promote a tenant to Bridge or Silo when it crosses a transaction-volume threshold or signs an enterprise contract requiring isolation. LPIP §4.2 formalizes exactly this for the Lyncxs payments platform: tenants under 10K transactions/month stay in Pool with RLS; tenants over 100K/month or with enterprise contracts get a dedicated schema or database. Apply the same rule to non-payment products — it is the standard way SaaS companies avoid choosing between "cheap to run" and "sells to the enterprise."

### 2.1 The Pool model, correctly implemented

Pool is the default, so it deserves the most rigor. The full pattern, synthesized from WCSEA §9.1, LPIP §4.3, the ORM engineering volume's Addendum L, and the OWASP Multi-Tenant Security Cheat Sheet:

```sql
-- Every tenant-owned table carries the tenant column
ALTER TABLE projects      ADD COLUMN organization_id uuid NOT NULL;
ALTER TABLE tasks         ADD COLUMN organization_id uuid NOT NULL;
ALTER TABLE invoices      ADD COLUMN organization_id uuid NOT NULL;

-- RLS is the enforcement layer, not the only layer
ALTER TABLE projects ENABLE ROW LEVEL SECURITY;
ALTER TABLE projects FORCE ROW LEVEL SECURITY;   -- applies even to the table owner

CREATE POLICY tenant_isolation ON projects
  USING (organization_id = current_setting('app.current_organization_id')::uuid);
```

Three details the Lyncxs KB and current OWASP guidance both insist on, because they are the actual points where RLS setups fail in production:

1. **`FORCE ROW LEVEL SECURITY` does not stop a superuser or a `BYPASSRLS` role.** Your ordinary application request path must run through a role that is neither. Reserve privileged connections for migrations only. Check the *deployed* role's `rolsuper`/`rolbypassrls` flags in `pg_roles` — not just what your config file says it should be.
2. **Tenant context must be transaction-local, not session-local.** Use `SET LOCAL app.current_organization_id = ...` or `set_config(..., true)` inside the same transaction as your queries, and re-set it on every transaction. Connection pooling (PgBouncer, Prisma's connection reuse, etc.) means a session can be handed to a different request without the tenant setting being cleared — a session-scoped `SET` is a cross-tenant leak waiting to happen.
3. **Application-level ORM scoping is defense in depth, not the boundary.** Prisma middleware, SQLAlchemy's `with_loader_criteria`, or a `TenantScopedRepository` wrapper (Auth Services Guide §21.8, OWASP cheat sheet §3) catch mistakes early and make code self-documenting, but raw SQL, bulk operations, and any code path that bypasses the ORM will not be covered by them. RLS at the database is the layer that survives an application bug; the ORM layer is the layer that catches the bug before it ships.

### 2.2 Tenant hierarchy shapes

Real products rarely have a flat Organization → Data shape. LPIP §4.4 documents three shapes that recur constantly and apply equally well outside payments:

```text
PATTERN A — Flat (simple B2B SaaS)
  Organization → Resources
  One company, one workspace. Most project-management tools start here.

PATTERN B — Hierarchical / Marketplace
  Platform
    └── Marketplace Organization (e.g. a multi-location POS franchise owner)
          └── Merchant/Location 1
          └── Merchant/Location 2
  Each location is semi-independent but rolls up to one paying customer.

PATTERN C — Multi-Project (internal platform pattern)
  Organization
    └── Project (e.g. "Acme Corp — Production")
          └── Environment (sandbox, production)
                └── Resources
  Used when the same organization needs isolated sandbox/production contexts,
  or when you sell your platform to other companies building their own products on it.
```

Pick the shape before writing your schema. A POS system for multi-location retailers is naturally Pattern B (Organization → Store). A project-management tool sold to companies is naturally Pattern A, possibly growing into "Organization → Workspace" as customers ask for department-level separation — which is Pattern A with one more layer, not a different pattern.

---

## 3. THE CANONICAL DOMAIN MODEL

Every multi-tenant product — POS, project management, or anything else — needs the same core entities, regardless of what it actually sells. This is the generalized version of LPIP §3's entity hierarchy, broadened beyond payments:

```text
Platform
  └── Organization (tenant_id)                    — the paying customer
        ├── Membership (User × Organization × Role) — who belongs, with what role
        │     └── Invitation (pending membership)
        ├── Project / Workspace (optional sub-tenant boundary)
        │     └── Environment (sandbox, production — if you expose an API)
        ├── API Key / Service Credential           — machine access, scoped to org
        ├── Domain Resources                       — the actual product data:
        │     (POS: Store, Till, SKU, Order, Payment, Inventory Count)
        │     (PM tool: Board, Task, Comment, Attachment, Automation Rule)
        ├── Webhook Endpoint                       — outbound events to the tenant
        ├── Audit Log                              — append-only, tenant-scoped
        └── Billing Subscription                   — Stripe Customer / Subscription
```

Two entities deserve individual definition because nearly every implementation mistake traces back to modeling them wrong:

**Organization** — the tenant. Carries `id`, `name`, `slug`, plan/tier, status (`active` | `suspended` | `offboarding` | `deleted`), and creation metadata. Never conflate Organization with User — a User can belong to multiple Organizations (this is the normal case for consultants, agencies, and anyone who works with more than one company), so membership is always its own join entity, never a foreign key on the user row.

**Membership** — the join between User and Organization, carrying the **role** (see §6) and status (`active` | `invited` | `suspended`). This is where RBAC actually lives. A common modeling mistake is putting `role` on the User table instead of the Membership table — that breaks the moment a user needs different roles in different organizations, which happens constantly (an agency's staff member is `admin` in their own org and `viewer` in three client orgs).

```typescript
// The membership join — this is the load-bearing table of the entire system
interface Membership {
  id: string;
  organization_id: string;
  user_id: string;
  role: string;                    // 'owner' | 'admin' | 'member' | 'viewer' | custom
  status: 'active' | 'invited' | 'suspended';
  invited_by?: string;
  invited_at?: Date;
  joined_at?: Date;
}
```

---

## 4. ONBOARDING — GETTING A TENANT INTO THE SYSTEM

Onboarding has two very different sub-problems that are easy to conflate: **tenant provisioning** (the system creates the organization, its isolated resources, and its first admin) and **member provisioning** (adding, updating, and removing individual users within an already-existing tenant, ideally automatically once the tenant is large enough to have an IT department).

### 4.1 Tenant provisioning — the first-run sequence

> **Griot delivery note (2026-09-12, backend 32):** this sequence now ships in Griot.
> `POST /api/organizations` (SuperAdmin-only) runs the provisioning step as ONE SQL
> transaction — `Organizations` row + the owner's `OrganizationMembers` row (Role
> Owner, status Invited) + the five seeded system `Roles` + the default `Workspaces`
> row + an `OrganizationLifecycleEvents(Onboarded)` row + the AuditLogs row — serialized
> on the unique Slug key; the branded Brevo owner invite (7-day token) is best-effort
> AFTER the durable commit, and `POST /api/organizations/invites/{token}/accept` flips
> the membership Invited → Active (the invited, authenticated account only). Suspend/
> reactivate/transfer-ownership/plan ride the same transaction pattern (`OrganizationStatus`
> maps 1:1 to the `TenantStatus` state machine below; offboarding stays backend 33).

```typescript
// Synthesized from OWASP Multi-Tenant Security Cheat Sheet §7 and
// LPIP's Merchant onboarding_status state machine (draft→submitted→
// under_review→verified→active), generalized beyond payments.

enum TenantStatus {
  PROVISIONING = 'provisioning',
  ACTIVE       = 'active',
  SUSPENDED    = 'suspended',
  OFFBOARDING  = 'offboarding',
  DELETED      = 'deleted',
}

async function provisionTenant(input: {
  name: string; adminEmail: string; tier: TenantTier;
}): Promise<TenantProvisioningResult> {
  const tenantId = generateTenantId();          // high-entropy, non-guessable
  await audit.log('tenant_provisioning_started', { tenantId, ...input });

  try {
    // 1. Create the tenant record in PROVISIONING state — nothing else
    //    should treat this tenant as real until it reaches ACTIVE.
    await db.createOrganization({ id: tenantId, name: input.name, status: TenantStatus.PROVISIONING, tier: input.tier });

    // 2. Provision the isolation boundary appropriate to the tenancy model.
    //    Pool model: nothing extra needed, RLS policies already cover new rows.
    //    Bridge model: CREATE SCHEMA tenant_xxx; run migrations against it.
    //    Silo model: provision a dedicated database/connection string.

    // 3. Create the first Membership as 'owner' — every tenant needs exactly
    //    one accountable human at creation time, even if they invite a team next.
    await db.createMembership({ organizationId: tenantId, userId: input.adminUserId, role: 'owner', status: 'active' });

    // 4. Issue platform API credentials if this product exposes an API
    //    (see §5.3 for the pk_/sk_ key scheme).

    // 5. Initialize tenant-scoped storage prefix / bucket policy (see §9.3).

    // 6. Flip to ACTIVE only after every step above succeeded.
    await db.updateOrganizationStatus(tenantId, TenantStatus.ACTIVE);
    await audit.log('tenant_provisioning_completed', { tenantId });
    return { tenantId, status: TenantStatus.ACTIVE };
  } catch (e) {
    await audit.log('tenant_provisioning_failed', { tenantId, error: String(e) });
    await cleanupFailedProvisioning(tenantId);   // don't leave half-built tenants
    throw e;
  }
}
```

The critical discipline here — easy to skip under deadline pressure, expensive to retrofit later — is **transactional provisioning with cleanup on failure**. A tenant stuck halfway between "created" and "active" (schema exists but no admin membership, or API keys issued but billing subscription never created) is a recurring source of production incidents in every multi-tenant system that skips this. Treat provisioning as a saga: each step logged, and a compensating cleanup path if any step fails.

### 4.2 Member provisioning — manual invite vs. SCIM automation

For small-to-mid tenants, manual invitation is the right and only necessary mechanism:

```typescript
// Clerk example — Auth Services Guide §16.2. The same shape applies
// regardless of auth provider: create a pending Membership, send an
// email with a signed invitation link, convert to 'active' on acceptance.
async function inviteOrgMember(orgId: string, email: string, role: string) {
  const client = await clerkClient();
  await client.organizations.createOrganizationInvitation({
    organizationId: orgId, emailAddress: email, role,
    redirectUrl: `${APP_URL}/accept-invite`,
  });
}
```

Once you sell to enterprise customers, manual invitation stops scaling — not technically, but organizationally. An enterprise customer's IT department adds and removes hundreds of employees through their own identity provider (Okta, Entra ID, Google Workspace) and expects every SaaS tool they use to stay in sync automatically. This is what **SCIM 2.0** (System for Cross-domain Identity Management, RFC 7643 + RFC 7644) solves, and current industry data makes clear it is not optional at enterprise scale: one widely cited 2024 report found that 63% of businesses have former employees retaining access somewhere, because the average employee touches around 29 different SaaS applications — no human offboarding checklist reliably covers that surface. <cite index="12-1">SCIM is the protocol enterprise identity providers use to automatically push user records into SaaS applications: when IT adds an employee in Okta or Entra, SCIM provisions them in the SaaS application; when IT removes the employee, SCIM deprovisions them.</cite> <cite index="12-1">By 2026 the protocol is fifteen years mature in production deployments, and every major workforce identity provider supports it — effectively every B2B SaaS product that lands enterprise customers eventually ships SCIM support.</cite>

The critical detail, and the one most first-time SCIM implementers get wrong: <cite index="5-1">deprovisioning in SCIM almost never means an actual DELETE — Okta and Microsoft Entra ID deactivate users by sending a PATCH request that sets `active` to `false`</cite>, preserving the audit trail and any data ownership rather than hard-deleting the account. SCIM and SSO are complementary, not interchangeable, and this distinction matters for how you design your auth flow: <cite index="9-1">SSO authenticates users at login, while SCIM manages which accounts exist and what state they're in — most platforms require you to configure an SSO enterprise connection before SCIM will apply, since SCIM has no login mechanism of its own.</cite>

The minimum SCIM server your product must expose (Auth Services Guide §25.4, matching the current RFC 7644 shape):

```typescript
const SCIM_TOKEN = process.env.SCIM_BEARER_TOKEN; // per-tenant, distinct from user tokens

function requireScimBearer(req: Request, res: Response, next: NextFunction) {
  const token = req.headers.authorization?.replace('Bearer ', '');
  if (!token || !timingSafeEqual(Buffer.from(token), Buffer.from(SCIM_TOKEN!)))
    return res.status(401).end();
  next();
}

// POST /scim/v2/Users — IdP provisions a new employee
app.post('/scim/v2/Users', requireScimBearer, async (req, res) => {
  const { userName, name, active } = req.body;
  const user = await db.user.create({ data: { email: userName, name: name.formatted, active, scimProvisioned: true } });
  res.status(201).json(toScimUser(user));
});

// PATCH /scim/v2/Users/:id — IdP deprovisions: set active=false, don't delete
app.patch('/scim/v2/Users/:id', requireScimBearer, async (req, res) => {
  for (const op of req.body.Operations) {
    if (op.path === 'active' && op.value === false) {
      await db.user.update({ where: { scimId: req.params.id }, data: { active: false } });
      await revokeAllUserSessions(req.params.id);   // this is the step that actually matters
    }
  }
  res.json(toScimUser(await db.user.findUnique({ where: { scimId: req.params.id } })));
});
```

<cite index="10-1">A well-built SCIM integration converts HR or directory events into these API calls to create, update, suspend, or remove accounts — delta syncs handle near-real-time changes, bulk operations handle large cohorts efficiently, and staged offboarding preserves data while still ensuring timely access revocation.</cite> The business case for building this before you think you need it: <cite index="6-1">SCIM support is increasingly a hard requirement for closing larger deals — without it, onboarding is slow, offboarding is risky, and enterprise IT teams see your app as a liability</cite> during security review. Most teams don't build SCIM servers from scratch — WorkOS, Okta's own tooling, and several other vendors sell "SCIM-as-a-middleware" specifically so a growing SaaS company doesn't have to become an identity-protocol expert to pass an enterprise security questionnaire.

### 4.3 Onboarding checklist (tenant-level)

Consolidated from OWASP §7 and the Lyncxs LPIP merchant-onboarding pattern, generalized to any product:

- [ ] Tenant record created in a non-active state until every provisioning step succeeds
- [ ] Isolation boundary provisioned (RLS coverage confirmed / schema created / database created, matching your tenancy model)
- [ ] First owner Membership created — never leave a tenant without an accountable human
- [ ] Tenant-scoped storage prefix and, if required by tier/compliance, tenant-specific encryption key provisioned
- [ ] API credentials issued (if applicable) — shown once, stored hashed
- [ ] Billing subscription created (Stripe Customer, or your billing provider's equivalent) — see §7
- [ ] Audit trail entry recorded for every step above
- [ ] Onboarding is idempotent and safe to retry on partial failure
## 5. AUTHENTICATION ACROSS EVERY SURFACE (WEB, MOBILE, API, MCP, AI AGENTS)

A multi-tenant product built "like a POS or project management tool that different companies use" is never single-surface. There's a web dashboard, almost certainly a mobile app (a POS especially — it runs on a tablet at the till), a public or partner-facing API, and increasingly an MCP server so AI agents (Claude, custom internal agents, a customer's own automation) can act on the tenant's behalf. Each surface authenticates differently, but all of them must resolve to the same two facts before any authorization decision is made: **who is this**, and **which tenant are they acting for**.

### 5.1 The core rule that survives every surface

Every authentication mechanism below exists to answer one question that must never be delegated to the client: *is the tenant identifier in this request one the caller is actually authorized to act in?* The OWASP Multi-Tenant Security Cheat Sheet states this as the load-bearing principle of the entire discipline: treat client-supplied tenant identifiers as selectors only, and verify server-side that the authenticated principal has active membership in the selected tenant before doing anything else.

```python
# WRONG — the header selects a tenant with no verification
def get_tenant_data(request):
    tenant_id = request.headers.get("X-Tenant-ID")   # attacker can set this to anything
    return db.execute("SELECT * FROM data WHERE tenant_id = :tid", {"tid": tenant_id})

# RIGHT — the tenant is derived from a verified token claim or session,
# then checked against a live membership record before being trusted
async def resolve_tenant_context(verified_claims, requested_org_id):
    membership = await db.get_active_membership(verified_claims["sub"], requested_org_id)
    if not membership:
        raise ForbiddenError("Not a member of this organization")
    return TenantContext(org_id=requested_org_id, user_id=verified_claims["sub"], role=membership.role)
```

Every surface below is a different way of getting to a verified identity; all of them then pass through this same membership check before a tenant context is trusted.

### 5.2 Web applications

Standard OAuth2/OIDC session via a managed provider — Clerk is the Lyncxs default (Auth Services Guide §4), with Auth0, WorkOS, Supabase Auth, Better Auth, and Kinde as situational alternatives (§5–§10). The pattern that matters for multi-tenancy specifically is **organization-aware middleware** that resolves org context before any route handler runs:

```typescript
// middleware.ts — runs at the edge before every request (Auth Services Guide §21.9)
export default clerkMiddleware(async (auth, req) => {
  if (isPublicRoute(req)) return;
  const { userId, orgId, orgRole } = await auth.protect();   // verified, not client-supplied

  if (isAdminRoute(req) && orgRole !== 'org:admin') {
    return Response.redirect(new URL('/dashboard', req.url));
  }
  const headers = new Headers(req.headers);
  headers.set('x-user-id', userId);
  headers.set('x-org-id', orgId ?? '');       // downstream trusts this because middleware verified it
  headers.set('x-org-role', orgRole ?? '');
});
```

Token storage: httpOnly, Secure, SameSite cookies for session tokens — never `localStorage`, which is readable by any injected script (Auth Services Guide §2.1 #4, #17.2).

### 5.3 Mobile apps (iOS / Android / React Native / Flutter)

Same identity provider, different token handling. Mobile apps cannot use httpOnly cookies the way a browser can, so the standard pattern is: access token in memory only (never persisted), refresh token in the platform secure enclave (iOS Keychain / Android Keystore, or Expo SecureStore in React Native). PKCE (Proof Key for Code Exchange) is mandatory for any OAuth flow on a public client — Auth Services Guide §2.1 #12 lists its absence as one of the twelve classic JWT/OAuth failure modes, and it remains the single most common mobile-auth vulnerability in real audits.

```typescript
// React Native / Expo — Clerk example (Auth Services Guide §4.5)
// Access token: memory only. Refresh token: SecureStore (Keychain/Keystore-backed).
import * as SecureStore from 'expo-secure-store';
const tokenCache = {
  getToken: (key: string) => SecureStore.getItemAsync(key),
  saveToken: (key: string, value: string) => SecureStore.setItemAsync(key, value),
};
```

Multi-tenant mobile apps have one extra UX obligation the web often skips: an explicit **organization switcher** must be reachable from anywhere in the app, and every cached screen must be invalidated on switch — a POS tablet that's switched from Store A to Store B and still shows Store A's till balance for even one frame is a real financial-reporting bug, not a cosmetic one.

### 5.4 API authentication (partner integrations, service-to-service)

Three mechanisms cover essentially every case (Auth Services Guide §20.3):

**API keys**, properly designed, are the default for anything a tenant's own developer will paste into a config file:

```typescript
// Platform-standard key scheme (Auth Services Guide Addendum K1 — this is
// NORMATIVE across Lyncxs and worth adopting verbatim for any new product)
// pk_{live|test}_...  → publishable, safe to expose client-side
// sk_{live|test}_...  → secret, shown exactly once at creation, stored hashed (SHA-256+)
//                        with a prefix hint for UI display (sk_live_ab12...redacted)
//
// Keys are scoped to Environment (sandbox/production) and carry NO entitlements
// of their own — capabilities are resolved server-side from the current
// organization/tenant state at request time. This means revoking a tenant's
// access doesn't require rotating every key; flip the tenant's status and
// every key referencing it stops working immediately.
```

Rotation is create-new → overlap window → revoke-old, fully self-service, invalidating adapter/credential caches cluster-wide within seconds — this exact rotation shape is what the Lyncxs payment platform standardizes on (Auth Services Guide Addendum K1) and it generalizes cleanly to any product's API keys.

**HMAC-signed requests** for webhooks and high-trust service-to-service calls where you need to verify the payload wasn't tampered with in transit, not just that the caller once had a valid key.

**OAuth 2.0 Client Credentials** for true machine-to-machine access where a third-party service needs to call your API on its own behalf (not a user's) — the standard shape for platform-to-platform integrations.

### 5.5 CLI tools and desktop applications

**Device Authorization Flow (RFC 8628)** — the same flow used by `gh auth login`, Docker CLI, and most modern developer CLIs: the CLI displays a code, the user authorizes it in a browser, the CLI polls for completion. This avoids ever needing a browser embedded in the CLI or a locally-listening redirect server, both of which are worse UX and worse security than the device flow.

### 5.6 AI agents and MCP — the newest surface, and the one with the least production history

This is the surface most likely to be under-specified in an existing multi-tenant design, because it's genuinely new. Two things changed recently and matter for anything built from September 2026 onward:

**MCP now has a real, mandatory authorization specification.** Early MCP deployments ran client and server together locally with no need for authorization at all, but as MCP servers moved to remote/hosted deployment the protocol formalized around OAuth. <cite index="16-1">The MCP Authorization Specification establishes a framework based on OAuth 2.1 to secure interactions between MCP clients and servers, and every revision since — especially the July 2026 release — has built on that separation rather than changing it.</cite> The most recent revision is a substantial hardening pass: <cite index="18-1">MCP servers are now formally OAuth 2.1 resource servers</cite> under the 2026-07-28 specification, and <cite index="21-1">authorization servers must now return the `iss` parameter per RFC 9207, with clients required to validate it before redeeming a code — closing an authorization-server mix-up vulnerability class</cite> that existed in earlier revisions. For local/stdio-transport MCP servers, the spec is explicit that this OAuth flow should *not* be used — credentials should come from the environment instead — but for any remote MCP server (which is what you'll be running if you're exposing your product to AI agents over the network), the OAuth 2.1 resource-server pattern is now mandatory, not optional.

Practically, this means an MCP server for a multi-tenant product looks like an OAuth-protected API with one addition — the token needs to carry *both* the human's identity and the fact that an agent is acting on their behalf, so every action remains attributable:

```typescript
// MCP server auth middleware — combining Auth Services Guide §29.2's
// agent-JWT structure with the 2026-07-28 MCP spec's OAuth 2.1 resource-server
// requirement. The server validates the OAuth token AND resolves it to a
// specific human-delegated capability grant before allowing any tool call.
app.use('/mcp', async (req, res, next) => {
  const token = req.headers.authorization?.replace('Bearer ', '');
  const grant = await db.mcpCapabilityGrant.findOne({ where: { tokenHash: sha256(token) } });
  if (!grant || grant.revokedAt || new Date() > grant.expiresAt) return res.status(401).end();

  req.agentId = grant.agentId;
  req.userId = grant.grantedBy;          // the human accountable for this session
  req.orgId = grant.organizationId;      // resolved server-side, never trusted from the request
  req.scopes = grant.allowedScopes;      // explicit allowlist, not "whatever the human can do"
  next();
});
```

The identity design that makes agent actions auditable — this is Auth Services Guide §29.2's agent-JWT shape, and it holds up well against the new spec:

```typescript
interface AgentJWT {
  iss: string;                 // your auth server
  sub: string;                 // 'agent:pos-assistant-v2' — NOT a user ID; agents are their own principal
  aud: string;                 // your API's resource identifier
  exp: number;                 // short-lived — 1 hour max, agent sessions should not be long-lived
  jti: string;                 // unique per token, log every use for audit

  agent_type: 'autonomous' | 'supervised' | 'human-in-loop';
  delegated_by: string;        // 'user:alice@acme.com' — the accountable human
  delegation_chain: string[];  // full chain if agents spawn sub-agents
  organization_id: string;     // which tenant this agent is scoped to — never broader than one org
  capabilities: string[];      // explicit allowlist: ['orders:read', 'inventory:write'] — never '*'
  nonDelegatable: boolean;     // default true — a sub-agent does not inherit this token's scope
}
```

Three rules for agent identity in a multi-tenant system, none of them optional:

1. **An agent token is scoped to exactly one organization, never to a user's full cross-org membership set.** A human who belongs to five organizations and authorizes an agent to "manage my POS inventory" must authorize it per-organization — the agent should never receive a token that silently spans all five.
2. **Every tool call is checked against an explicit capability allowlist resolved in code, never inferred from the LLM's own output.** The authorization decision happens outside the model's context window — nothing the agent says about its own permissions is trusted.
3. **High-stakes actions require human-in-the-loop confirmation**, gated by the `requireUserConfirmation` constraint on the capability grant, not by asking the model to "be careful."

```typescript
// Auth Services Guide §29.5 — auth decisions happen outside the LLM's context
const TOOL_SCOPE_MAP: Record<string, string> = {
  'read_orders':      'orders:read',
  'update_inventory': 'inventory:write',
  'issue_refund':     'payments:refund',   // this one should almost always require confirmation
};

async function executeAgentTool(agentToken: string, toolName: string, input: unknown) {
  const grant = await verifyAgentJWT(agentToken);
  const required = TOOL_SCOPE_MAP[toolName];
  if (!grant.capabilities.includes(required)) throw new Error(`Agent lacks scope '${required}'`);
  if (TOOLS_REQUIRING_CONFIRMATION.has(toolName)) await requestUserApproval(grant.delegated_by, toolName, input);
  await auditLog.record({ agentId: grant.sub, orgId: grant.organization_id, action: toolName, input });
  return executeTool(toolName, input);   // only reached after every check above passes
}
```

**Enterprise-Managed Authorization (EMA)** is the newest development worth tracking here: <cite index="23-1">in July 2026 MCP had its biggest update since launch, and with it Enterprise-Managed Authorization went from experimental to production-grade</cite> — the direction the protocol is heading is treating agents as first-class organizational entities with their own governed identity lifecycle, not as an extension of a human's session. For a multi-tenant product, this means the same lifecycle discipline you apply to human members (§4, §8) will increasingly need to apply to agent identities too: agents get provisioned, get scoped roles, and get deprovisioned when a tenant offboards or revokes a specific integration — not just when a human's session expires.

### 5.6.1 MCP at scale — the tool registry as an RBAC boundary

MCP has moved from novelty to default infrastructure faster than almost any protocol in recent memory: by mid-2026 it was seeing over 97 million monthly SDK downloads across languages, with more than 10,000 active MCP servers in production use, native support from every major model provider, and governance handed to the Agentic AI Foundation under the Linux Foundation as of December 2025 — the "any AI connects to any tool via one standard" framing has become a reasonably accurate description of the ecosystem, not just marketing language. That scale is exactly why a multi-tenant product exposing MCP tools needs the tool registry itself to be a first-class RBAC boundary, not an afterthought bolted onto individual tool handlers.

The production shape that holds up at real scale, drawn from Pinterest's own MCP deployment (reported April 2026: roughly 66,000 monthly tool invocations across 844 active users, saving an estimated 7,000 engineering hours a month) — domain-specific MCP servers rather than one monolithic server, a central registry for discovery, and mandatory security/legal/privacy/compliance review before any new server reaches production. Translated into the multi-tenant vocabulary from §5.6:

```typescript
// A tool registry lookup is itself a tenant + role scoped operation —
// an agent should discover only the tools its capability grant permits,
// not enumerate every tool the platform has ever registered.
async function listAvailableTools(agentToken: string): Promise<ToolDescriptor[]> {
  const grant = await verifyAgentJWT(agentToken);
  return TOOL_REGISTRY.filter(tool =>
    grant.capabilities.includes(tool.requiredScope) &&
    tool.availableToOrgTier.includes(grant.organizationTier)
  );
}
```

Five tool-engineering disciplines apply directly to a multi-tenant MCP surface, independent of the underlying business domain:

1. **Idempotency.** An agent may retry a call after a timeout without knowing whether the first attempt succeeded — a tool that isn't idempotent turns a retry into a duplicate side effect (a duplicate refund, a duplicate task). Apply the same `Idempotency-Key` discipline from §7.7's payment-API contract to any mutating MCP tool, not just payment endpoints.
2. **Confirmation gates on destructive or high-stakes tools.** Deleting records, sending bulk communications, or moving money should require human-in-the-loop confirmation by default — this is the same `requireUserConfirmation` constraint already shown in §5.6's capability-grant example, and it is worth encoding as a property of the *tool definition* itself (so a new destructive tool is confirmation-gated by default) rather than something each integrator has to remember to add.
3. **Structured, schema-validated responses in both directions.** Validate tool *input* against a schema before it reaches any handler, and validate tool *output* against an expected schema before an orchestrating agent acts on it — an agent parsing free-text tool output is a reliability problem even before it's a security one.
4. **Tool-level RBAC enforced at the MCP server, not inferred by the model.** Not every AI role should see every tool; a customer-support agent and a finance agent calling into the same multi-tenant platform should be issued different capability grants, each scoped to the organization they're acting for and the specific tool set their role needs — this is the same allowlist principle from §5.6, applied across an entire fleet of role-specific agents rather than one agent.
5. **Discoverability over pre-loading.** In a platform with a large tool surface, an agent should query the registry for what it can currently do rather than having every tool definition force-loaded into context regardless of relevance — this keeps context budgets sane and keeps the capability boundary enforced server-side rather than by convention.

### 5.6.2 Multi-agent orchestration inside a tenant boundary

Products mature enough to run multiple specialized agents — a support agent, a finance agent, an operations agent, each with its own tool set — need the orchestration layer itself to respect tenant boundaries, not just each individual agent. The common orchestration patterns (orchestrator-worker, hierarchical supervisor/worker, peer-to-peer mesh over a shared message bus, sequential pipeline, evaluator-optimizer) are architecture-neutral with respect to multi-tenancy, but the message bus or task queue connecting them is exactly the shared-infrastructure case §9.4 already covers: **a shared agent-to-agent message bus is not itself a tenant-isolation boundary.** Bind every inter-agent message to the organization it belongs to, and re-verify that scope at each agent that consumes it — a compromised or misconfigured agent in a multi-agent pipeline should never be able to pull another tenant's task off a shared queue simply because the queue itself has no tenant awareness.

The practical framework note worth carrying over is that the choice of orchestration framework (LangGraph, CrewAI, or a custom implementation) is rarely what determines whether a multi-agent system is production-ready — the gap between a reliable multi-agent deployment and an unreliable one is almost always the eval pipeline, the observability setup, and the failure-recovery logic, not the framework. For a multi-tenant product this translates directly: instrument every agent action with the same tenant-scoped audit logging from §10.1's threat table, regardless of which orchestration framework sits underneath.

### 5.6.3 Tenant-scoped AI memory

An AI feature that remembers things — user preferences, past conversations, organizational knowledge — is implicitly building a new data store, and that store needs the same tenant-isolation discipline as every other table in the system (§2, §9.2). The useful distinction here is that "AI memory" is not one thing but a taxonomy with genuinely different scope, storage, and retrieval characteristics per type, and the scope of each type maps directly onto the tenant hierarchy from §3:

| Memory type | Scope | Isolation implication |
|---|---|---|
| Conversation (working) memory | Current session only | Lives in-context; no persistent storage risk, but still must not leak across a multi-tenant agent's concurrent sessions |
| Long-term / personal memory | Per-user, cross-session | Tenant-scoped table, same RLS discipline as any other user data (§2.1) |
| Semantic memory (facts, policies, product catalog) | Organizational | Vector index partitioned by `organization_id` — a shared vector database without per-tenant filtering is a direct cross-tenant leak vector, since embedding similarity search has no inherent tenant awareness unless the filter is applied explicitly |
| Team memory | Team/department within an org | Filtered subset of organizational memory, scoped search with RBAC — this is the ReBAC case from §6.3 in miniature |
| Episodic memory (what happened, when) | Per-user or per-session, cross-session | Indexed conversation store; treat exactly like the audit log in §10.1 — tenant-scoped, append-heavy |

The concrete risk worth naming explicitly: a RAG pipeline backing a multi-tenant AI feature must filter retrieved documents by the requesting agent's organization *before* they reach the model, not rely on the model to decline to use information it shouldn't have seen. This is precisely the same principle as §5.6's "authorization happens outside the LLM's context" — retrieval-time filtering is an authorization control, and it belongs in the retrieval query (a `WHERE organization_id = $1` clause or an equivalent vector-index namespace/filter), not in a system-prompt instruction asking the model to ignore irrelevant results.

### 5.7 AI-specific threats layered onto the multi-tenant threat model

§10.1's threat catalog is written for the isolation, identity, and payments dimensions of a multi-tenant system; a product with meaningful AI/agent surface area needs five additional threats folded into the same model, all of them specific to how AI systems fail in ways traditional software doesn't:

| Threat | What it looks like | Primary control |
|---|---|---|
| Prompt injection | Malicious instructions embedded in user input, a retrieved document, or a tool's own output attempt to override system instructions or exfiltrate data | Treat all retrieved/tool-sourced content as untrusted input; authorization decisions never happen inside the model's context (§5.6) |
| Tool poisoning | A malicious or compromised MCP server, or a manipulated tool description, alters agent behavior | MCP server vetting before production (security/legal/privacy/compliance review) — non-negotiable at any real scale, per §5.6.1 |
| Cross-tenant data exfiltration via AI | An agent with broad knowledge access is manipulated into revealing another tenant's information | Retrieval-time tenant filtering (§5.6.3), never a model-level instruction to "not share" |
| Agent hijacking in a multi-agent pipeline | A compromised agent corrupts downstream agents' input in an orchestrated system | Validate every inter-agent output against a typed schema before passing it downstream — never assume inter-agent trust, even within one tenant (§5.6.2) |
| Unbounded agent action | An autonomous agent takes an irreversible action (payment, deletion, bulk send) without a human checkpoint | Human approval gates on irreversible actions, encoded as a property of the tool definition, not left to each integrator (§5.6.1) |

### 5.8 Cross-surface summary

| Surface | Mechanism | Token | Storage |
|---|---|---|---|
| Web app | OAuth2/OIDC via managed provider | Short JWT + refresh | httpOnly, Secure, SameSite cookie |
| Mobile app | OAuth2/OIDC + PKCE | Short JWT + refresh | Access: memory. Refresh: Keychain/Keystore |
| Public/partner API | API key (pk_/sk_) or OAuth Client Credentials | Long-lived key (hashed at rest) or short token | Secret manager on the caller's side |
| Webhooks (inbound) | HMAC signature verification | N/A — signature, not a token | N/A |
| CLI | Device Authorization Flow (RFC 8628) | Short JWT + refresh | OS credential store (`~/.config`, keychain) |
| Internal microservices | mTLS or workload identity (SPIFFE/SPIRE) | Short-lived X.509 or SVID | In-memory, auto-rotated |
| MCP server (remote) | OAuth 2.1 resource server (MCP spec 2026-07-28) | Short-lived agent JWT, capability-scoped | Server-side grant record, not a bearer secret alone |
| MCP server (stdio/local) | Environment-provided credential | N/A | Local process environment |
## 6. AUTHORIZATION — RBAC, ABAC, REBAC, AND WHEN TO USE EACH

Authentication proves identity; authorization decides what that identity may do. They are separate systems, and conflating them is the source of a large fraction of real-world access-control bugs — "the user is logged in" and "the user is allowed to do this" are different questions, and a system that only asks the first one grants every logged-in user everything.

### 6.1 Role-Based Access Control (RBAC) — the default, start here

RBAC is right for the overwhelming majority of multi-tenant products, including both examples in this document's brief (a POS and a project-management tool). Users get roles; roles get permissions; the check is a lookup, not a computation.

```typescript
const PERMISSIONS = {
  owner:  ['*'],
  admin:  ['orders:read', 'orders:write', 'inventory:read', 'inventory:write', 'staff:manage', 'billing:manage'],
  staff:  ['orders:read', 'orders:write', 'inventory:read'],
  viewer: ['orders:read', 'inventory:read'],
} as const;

function hasPermission(role: string, required: string): boolean {
  const perms = PERMISSIONS[role as keyof typeof PERMISSIONS] || [];
  return perms.includes('*') || perms.includes(required);
}
```

The standard hierarchy for a multi-tenant SaaS (WCSEA §9.2, Auth Services Guide §21.1) — adapt the names to your product, keep the shape:

```text
super_admin      → All permissions across all tenants (your internal ops team only)
org_admin/owner  → All permissions within their organization
workspace_admin  → All permissions within a sub-workspace, if your hierarchy has one
member/staff     → Read + limited write in their scope
viewer           → Read only
billing_admin    → Billing only — a genuinely useful narrow role, since "who can change
                    the payment method" is a different question from "who can edit tasks"
                    and conflating them is a common source of accidental over-privilege
```

Store roles relationally, not just as a string on the membership row, once you need custom roles or an audit trail of grants:

```sql
CREATE TABLE roles (id uuid PRIMARY KEY, name varchar(50) UNIQUE NOT NULL);
CREATE TABLE permissions (id uuid PRIMARY KEY, resource varchar(100), action varchar(50), UNIQUE(resource, action));
CREATE TABLE role_permissions (role_id uuid REFERENCES roles(id), permission_id uuid REFERENCES permissions(id), PRIMARY KEY(role_id, permission_id));
CREATE TABLE user_roles (user_id uuid, role_id uuid REFERENCES roles(id), org_id uuid,
    granted_at timestamptz DEFAULT NOW(), granted_by uuid, PRIMARY KEY(user_id, role_id, org_id));

-- The actual permission check, run on every protected request
SELECT EXISTS (
    SELECT 1 FROM user_roles ur
    JOIN role_permissions rp ON rp.role_id = ur.role_id
    JOIN permissions p ON p.id = rp.permission_id
    WHERE ur.user_id = $1 AND ur.org_id = $2
      AND p.resource = $3 AND (p.action = $4 OR p.action = 'manage')
) AS has_permission;
```

Notice `org_id` is part of the primary key on `user_roles` — this is what makes a single user's role correctly differ per organization, matching §3's Membership design.

### 6.2 Attribute-Based Access Control (ABAC) — when the rule depends on context, not just role

ABAC decides access from a combination of user attributes, resource attributes, and environmental context — useful when "can you do this" depends on more than a static role. A POS example: a shift supervisor can void a transaction, but only during their own shift and only for transactions under a certain value; anything larger needs a manager override regardless of role.

```typescript
interface AccessContext {
  user: { id: string; roles: string[]; shiftId?: string };
  resource: { type: string; ownerId: string; orgId: string; amount?: number };
  action: string;
  environment: { timestamp: Date; ipAddress: string };
}

const voidTransactionPolicy = (ctx: AccessContext): boolean => {
  if (ctx.action !== 'transaction:void') return false;
  if (ctx.resource.amount! > 5000 && !ctx.user.roles.includes('manager')) return false;
  return ctx.user.roles.includes('staff') || ctx.user.roles.includes('manager');
};
```

For larger rule sets, externalize the policy instead of scattering conditionals through application code — **Open Policy Agent (OPA)** with Rego is the production-proven pattern, used at Netflix, Intuit, and Twilio, and it decouples policy changes from code deployments:

```rego
package pos.authz
default allow = false

allow { input.user.roles[_] == "manager" }
allow {
    input.action == "transaction:void"
    input.user.roles[_] == "staff"
    input.resource.amount <= 5000
    input.resource.orgId == input.user.orgId
}
```

### 6.3 Relationship-Based Access Control (ReBAC) — when permissions form a graph, not a hierarchy

ReBAC decides access from chains of relationships rather than static roles — "can Alice read this document" resolves by checking whether she has a direct grant, is a member of a group that has a grant, or is in a folder whose parent has a grant. This is the model behind Google Drive, GitHub's org→team→repo→branch permission structure, and Airbnb's authorization system, and it derives from Google's internal **Zanzibar** paper.

**OpenFGA** is the open-source, production-proven implementation of this model, and it has moved decisively from "interesting research pattern" to "standard infrastructure component" over the past year: <cite index="28-1">originally developed by Auth0/Okta and open-sourced in mid-2022, OpenFGA joined the Cloud Native Computing Foundation as a Sandbox project and was promoted to Incubation maturity in October 2025</cite>, with adopters including <cite index="28-1">Auth0, Grafana Labs, Canonical, Docker, and Headspace</cite>. The core value proposition, directly from the project's own framing: <cite index="33-1">OpenFGA moves authorization logic outside application code, making it easier to write, change, and audit, and centralizes authorization decisions and audit logs in a way that's simpler to align with security and compliance requirements.</cite>

```typescript
// SpiceDB/OpenFGA-style relationship write and check
await client.writeRelationships({ updates: [{
  operation: 'CREATE',
  relationship: {
    resource: { objectType: 'board', objectId: 'roadmap-2026' },
    relation: 'editor',
    subject: { object: { objectType: 'user', objectId: 'alice' } },
  },
}]});

const { permissionship } = await client.checkPermission({
  resource: { objectType: 'board', objectId: 'roadmap-2026' },
  permission: 'view',   // schema defines editor implies view
  subject: { object: { objectType: 'user', objectId: 'alice' } },
});
```

Reach for ReBAC specifically when your product needs **per-resource sharing that role-only systems can't express cleanly** — this is exactly the shape a project-management tool needs once customers ask for "share this one board with an external contractor who isn't otherwise a member of our org," or "let this team see project X but not project Y even though they're both in the same workspace." A recent OpenFGA industry guide makes the multi-store distinction concrete for e-commerce/POS-style platforms too: <cite index="29-1">a merchant typically has one organization but many stores, and staff often have different permissions in different stores — a category manager might be a manager in the apparel store and just a staff member in the home-goods store</cite> — a shape plain RBAC (one role per org membership) cannot express without ReBAC-style per-resource relationships layered on top.

```text
When to reach for ReBAC:
  ✓ Per-resource sharing (share this doc/board/project with a specific person or group)
  ✓ Nested containment where permission should flow down (org → team → project → task)
  ✓ Multi-store / multi-location staff permissions that vary per location
  ✗ Simple "one role per org membership" apps — this is RBAC, and ReBAC is overkill
  ✗ Latency-sensitive hot paths without a caching strategy — a separate authorization
    service call per request adds real latency; cache decisions where correctness allows it
```

OpenFGA explicitly documents production patterns for the exact combination this document is about — <cite index="26-1">AI agent authorization (modeling agents as principals, delegating user permissions, and bounding what an autonomous agent can do), RAG authorization (filtering retrieved documents by the user's permissions before they reach the model), and multi-tenant SaaS and microservices authorization</cite> — which makes it a natural fit if your product's permission model grows complex enough to need it, including the AI-agent dimension from §5.6.

### 6.4 Row-Level Security as an authorization layer, not just an isolation layer

Postgres RLS (§2.1) is usually framed as a tenant-isolation tool, but the same mechanism does double duty as fine-grained authorization within a tenant:

```sql
-- Members see only their org's data (isolation)
CREATE POLICY org_scoped ON transactions FOR SELECT
    USING (org_id IN (SELECT org_id FROM org_members WHERE user_id = auth.uid() AND status = 'active'));

-- Admins additionally get write access within their org (authorization)
CREATE POLICY admin_write ON transactions FOR ALL
    USING (EXISTS (SELECT 1 FROM org_members
        WHERE user_id = auth.uid() AND org_id = transactions.org_id AND role = 'admin'));
```

The `service_role` / bypass-RLS key that most managed-Postgres providers offer must never reach client code or an ordinary request path — see §2.1's discipline on privileged roles.

### 6.5 Model selection

| Need | Model | Why |
|---|---|---|
| "Members of an org have one of a few fixed roles" | RBAC | Simplest to build, audit, and explain to customers |
| "Access depends on time of day, transaction size, ownership, or other context" | ABAC (OPA/Rego for complex rule sets) | Rules as data, not scattered conditionals |
| "Users share specific resources with specific other users/groups, permissions nest" | ReBAC (OpenFGA/SpiceDB) | Graph-shaped permissions; per-resource sharing |
| "An AI agent needs a bounded, revocable, auditable capability set" | Capability tokens (§5.6) + RBAC/ReBAC underneath | Explicit allowlist beats inferred trust |
| "Third-party app should act on a user's behalf, narrowly" | OAuth scopes | `read:orders` not `full_access` |

Most real products end up layering these: RBAC as the default for org membership, RLS enforcing it at the database, and ReBAC bolted on specifically for the "share this one item externally" feature once customers ask for it. Building RBAC + solid RLS first and adding ReBAC only when a concrete feature demands it is the right sequencing — introducing a relationship-graph authorization service on day one for a product that only ever needed four roles is unnecessary complexity.
## 7. PAYMENTS IN A MULTI-TENANT SYSTEM

Payments is the domain where "multi-tenant" stops being purely an engineering pattern and starts being a regulatory one. The moment your product moves money — a POS taking card payments for many merchants, a marketplace splitting funds between buyers and sellers, a project-management tool billing its own customers via subscription — you are dealing with at least one of two distinct problems, and conflating them is the single most common design mistake in this area:

1. **You are billing your tenants** (SaaS subscription revenue — the project-management tool charging companies a monthly fee). This is comparatively simple: you are the merchant, your tenants are your customers, and you own one Stripe/payment-provider relationship.
2. **Your tenants are taking payments from *their* customers, through your platform** (a POS processing a merchant's card sales; a marketplace splitting a sale between a buyer and multiple sellers). This is a fundamentally different, harder problem: you are not the merchant of record for most of these transactions, you may be moving money you don't own, and depending on how you architect it you can accidentally take on payment-service-provider regulatory obligations you never intended to.

### 7.1 The core abstraction: Method ≠ Provider ≠ Rail

LPIP's foundational design principle, and the single most important idea to carry into any new multi-tenant payments feature, is the strict separation of three concepts that are easy to conflate:

```text
Payment Method  — what the customer wants to use (M-Pesa, card, bank transfer, USDC)
Payment Provider — who actually executes it (Paystack, Daraja, IntaSend, Stripe, Circle)
Payment Rail    — the underlying network (card networks, mobile money, ACH, blockchain)

Applications declare WHAT they want: { method: 'mpesa', amount: 5000, currency: 'KES' }
The orchestration layer decides WHICH provider executes it, and can fail over to a
different provider without the application code — or the tenant — ever knowing.
```

This is why LPIP's application-code rule is absolute: **application code references payment methods and capabilities, never provider names.** A tenant's checkout flow asks for `mpesa` or `card`, not `daraja` or `paystack` — the provider is an implementation detail the orchestration layer owns, which means you can swap, add, or fail over providers without touching every tenant's integration.

### 7.2 Your own tenant/entity hierarchy for money movement

LPIP §3's domain model is the reference shape for any multi-tenant payments feature, generalized here beyond the Lyncxs payments platform itself:

```text
Organization (your tenant — the company using your product)
  └── Merchant (the onboarded entity actually authorized to receive money —
      for a simple SaaS-billing case this collapses into the Organization itself;
      for a POS or marketplace, one Organization can have several Merchants,
      e.g. a franchise owner with multiple store locations)
        ├── Provider Account (credentials for the actual PSP — Paystack, Stripe, etc.,
        │     stored as a vault reference, never as raw secrets in your database)
        ├── Payment Intent (what should happen — amount, currency, method)
        │     └── Payment Attempt (try 1, try 2 — retries and failover live here)
        │           └── Transaction (the actual provider execution record)
        ├── Wallet (available / pending / reserved balance)
        └── Ledger Entry (immutable, double-entry, the source of truth for balances)
```

Two entities are worth calling out because they are the two things new implementers most often skip and then have to retrofit under pressure:

**Wallets and the ledger are not the same thing.** A wallet's balance fields (`available_balance`, `pending_balance`, `reserved_balance`) are a *cached, derived* view for fast reads — the actual source of truth is the append-only ledger. Never let application code write directly to a wallet balance column; every balance change is the result of a ledger entry, and the wallet balance is recomputed or incrementally maintained from the ledger, never the other way around.

**Merchant onboarding status is a real state machine, not a boolean.** `draft → submitted → under_review → verified/rejected → active → suspended` (LPIP §3.2.4, operationalized further in the Global Regulatory volume's KYC pipeline addendum as `DRAFT→SUBMITTED→UNDER_REVIEW→ACTION_REQUIRED→VERIFIED/REJECTED`). Production capabilities — actually taking a live payment — must be gated server-side on this status, not inferred from "the merchant filled out a form."

### 7.3 Multi-tenant isolation applied to money — higher stakes, same mechanisms

Everything in §2 about Pool/Bridge/Silo and RLS applies to payment tables, with one addition specific to this domain, taken from the ORM engineering volume's payment-data addendum: **ledger immutability is enforced at three layers simultaneously** — the application service refuses updates/deletes, a database trigger raises an exception on any attempted `UPDATE`/`DELETE`, and the application database role has those grants revoked entirely. Corrections happen only as compensating entry pairs (a reversing entry plus a new correct entry), never as edits to history. This triple-layer redundancy exists because a single point of enforcement — application code alone, or a database constraint alone — has historically proven insufficient once a system is under real production pressure to "just fix this one row."

```sql
-- Layer 2 of 3: the database itself refuses to let ledger history be rewritten
REVOKE UPDATE, DELETE ON ledger_entries FROM application_role;
-- Layer 3: a trigger as defense in depth even against roles that shouldn't have the grant
CREATE OR REPLACE FUNCTION prevent_ledger_mutation() RETURNS trigger AS $$
BEGIN RAISE EXCEPTION 'ledger_entries is append-only'; END;
$$ LANGUAGE plpgsql;
CREATE TRIGGER no_ledger_update BEFORE UPDATE OR DELETE ON ledger_entries
  FOR EACH ROW EXECUTE FUNCTION prevent_ledger_mutation();
```

### 7.4 When your tenants ARE the merchants: embedded payments for platforms

This is the POS case specifically, and the marketplace/project-tool-with-payouts case generally. Building your own PSP relationship management from scratch is what LPIP exists to abstract for internal Lyncxs products — but the same problem, solved by a third party, is what **Stripe Connect** (and equivalent offerings from Paystack, Adyen for Platforms, etc.) exists for if you don't want to build and operate the orchestration layer yourself.

The account-type decision is the first and most consequential choice, and it has changed shape recently: <cite index="34-1">the Standard/Express/Custom account-type split is a legacy concept — in the current Accounts API, you no longer pick a fixed account type up front; instead you create a single Account and attach configurations to it (merchant, to accept payments; customer, to be billed by your platform; and/or recipient, to receive transfers), which lets one underlying account represent more than one of these roles simultaneously as your product's needs evolve.</cite> The practical trade-off underneath the new API is the same one that existed under the old names: <cite index="34-1">a configuration that keeps compliance and payout management with the connected account (closer to legacy "Standard") means your tenants manage their own payouts, disputes, and compliance while you just create charges on their account — versus a fully white-labeled flow where you build the entire onboarding UI yourself and carry the compliance burden, which is only worth taking on if complete white-labeling is a hard product requirement.</cite> For most SaaS platforms building a first payments feature, the lighter-compliance-burden option is the right default; move toward the heavier, more-owned option only when a specific product requirement (typically white-labeling for a large customer) demands it.

The routing pattern for a single-seller checkout — the common POS case — versus a true multi-seller marketplace cart are genuinely different implementations, not a config flag:

```text
SINGLE-SELLER (POS, most SaaS-with-payments cases):
  Customer's card is charged, with an application_fee_amount specified —
  your platform's cut routes to your balance automatically, the remainder
  settles directly in the connected merchant's balance. One charge, one destination.

MULTI-SELLER (marketplace cart with items from several independent sellers):
  A single charge can only have one transfer destination, so this breaks the
  single-charge model. The platform charges the customer's card in full on
  its own account, then programmatically creates separate transfers to each
  seller, grouped under one transfer_group identifier for reconciliation.
  This makes the platform the merchant of record for the full transaction,
  which increases liability for chargebacks and disputes — a real trade-off,
  not just an implementation detail.
```

<cite index="39-1">Managing marketplace payouts also requires configuring automated or manual payout schedules while maintaining a rolling reserve balance to cover refunds and chargebacks</cite> — build the reserve-balance mechanic in from the start rather than retrofitting it after the first serious dispute.

### 7.5 Compliance boundary — the question every multi-tenant payments feature must answer before writing code

The Lyncxs regulatory volume's operationalized standard (Global Regulatory Addendum R1, itself derived from MASTER §43–44) is the right question to ask of *any* multi-tenant payments feature, not just Lyncxs's own platform:

```text
Level 1–2 (software abstraction / orchestration over licensed PSPs):
  You integrate with licensed providers; they hold the regulatory licenses;
  you never custody funds yourself. This is where nearly every product should
  start and, for most products, where it should stay.

Level 3 (holding/routing merchant funds as a payment service yourself):
  You are now doing something that in most jurisdictions requires its own
  licensing (in Kenya, CBK PSP authorization; equivalent regimes exist
  everywhere). This requires, BEFORE activating any such feature: legal
  counsel review against the applicable regulator's checklist, a named
  executive owner of that review, and a registered review date that
  escalates automatically if it goes stale.
```

Practically: a POS that routes card payments through Paystack/Stripe/Daraja on behalf of merchants, where the provider settles directly to each merchant, is Level 1–2 — this is the overwhelmingly common and correct default. A POS that pools all merchants' money into one platform-owned account and pays merchants out itself on its own schedule has moved to Level 3, and that move should be a deliberate, counsel-reviewed decision, never an accidental consequence of "it was easier to build the ledger that way."

### 7.6 PCI DSS scope discipline

The standing rule from the Lyncxs security volume's payment threat-model addendum applies to any product handling card data, not just Lyncxs's own platform: **no PAN (primary account number) or CVV field may exist in any schema or log, CI-enforced.** Tokenize immediately at the provider/SDK boundary (Stripe Elements, Paystack's client-side tokenization, etc.) so raw card data never transits your servers — this is what keeps most SaaS platforms at the simplest PCI DSS self-assessment tier (SAQ-A) by construction, rather than the substantially heavier tiers that apply once your own infrastructure touches raw card data.

### 7.7 API contract standards for payment endpoints

Regardless of whether you build custom orchestration or wrap Stripe Connect, the API-Design volume's payment addendum (Addendum A1) sets contract standards worth adopting for any multi-tenant payments API:

- **Idempotency**: every mutating endpoint accepts an `Idempotency-Key` header; replays return the original response, and key reuse with a different body returns a distinct `422 IDEMPOTENCY_KEY_REUSED` rather than silently succeeding or double-charging.
- **Structured error catalog**: `(code, http_status, retryable, docs_url)` — never free-text-only errors on a payment surface, since a client's retry logic needs to programmatically know whether an error is safe to retry.
- **Cursor pagination only** on transaction-class resources; offset pagination is explicitly prohibited, because offset pagination on a high-write table produces skipped or duplicated rows as new transactions are inserted mid-page.
- **No provider names in request contracts** — clients express `payment_method` and required capabilities only, preserving the Method ≠ Provider separation from §7.1 all the way to the public API surface.

### 7.8 Payments feature checklist for any multi-tenant product

- [ ] Decide explicitly: are you billing tenants, or are tenants taking payments through you? (Often both — treat them as separate features with separate architectures.)
- [ ] Method/Provider/Rail separation maintained in application code and API contracts
- [ ] Tenant/Merchant hierarchy modeled explicitly, even if it collapses to 1:1 for simple cases today
- [ ] Ledger is append-only, enforced at three layers (app, trigger, revoked grants)
- [ ] Wallet balances are derived from the ledger, never written directly
- [ ] Webhook signature verification (HMAC, timing-safe compare) on every inbound provider callback
- [ ] Idempotency keys on every mutating payment endpoint
- [ ] PCI scope minimized by construction — no PAN/CVV fields anywhere, tokenize at the edge
- [ ] Regulatory boundary (Level 1–2 vs Level 3) explicitly decided and, if Level 3, counsel-reviewed before activation
- [ ] Reserve balance / dispute handling designed in from the start if operating a multi-seller marketplace
## 8. OFFBOARDING — GETTING A TENANT OUT, CLEANLY

Offboarding gets far less design attention than onboarding in most systems, which is exactly why it is where the most damaging bugs live. OWASP's current multi-tenant guidance lists "insecure tenant onboarding/offboarding" as a named top-level risk in its own right — specifically **incomplete provisioning, unauthorized residual access, or data retention beyond policy** — distinct from generic cross-tenant leakage, and the SCIM research in §4.2 already established the scale of the problem for individual member offboarding: roughly two-thirds of businesses in a widely cited industry report retained access for former employees somewhere in their SaaS footprint. The tenant-level version of this problem — an entire company leaving your product — carries higher stakes, because getting it wrong means either a data breach (residual access lingers) or a compliance failure (data isn't actually deleted when it should be, or is deleted when it legally needed to be retained).

### 8.1 Two distinct offboarding events, two distinct mechanisms

Just as with onboarding (§4), offboarding splits into **member offboarding** (one person leaves the tenant, tenant continues) and **tenant offboarding** (the whole organization leaves your product).

**Member offboarding** is the SCIM deprovisioning flow from §4.2, and the detail worth re-emphasizing: it is a deactivation (`active: false`), not a deletion, in essentially every real implementation. The step that actually matters for security — and the one manual, human-driven offboarding checklists most reliably miss — is **session revocation**: deactivating the account in your database does nothing if the person's existing JWT/session token remains valid until its natural expiry. Revoke all active sessions and tokens as an explicit, immediate step, not something that happens implicitly when the token would have expired anyway.

**Tenant offboarding** is a full lifecycle transition, and it deserves its own explicit state, not just a boolean `is_active` flag on the Organization row:

```typescript
enum TenantStatus {
  PROVISIONING = 'provisioning',
  ACTIVE       = 'active',
  SUSPENDED    = 'suspended',    // temporary — e.g. payment failure, contract lapse
  OFFBOARDING  = 'offboarding',  // irreversible process has begun
  DELETED      = 'deleted',      // terminal
}
```

### 8.2 The tenant offboarding sequence

Synthesized from the OWASP Multi-Tenant Security Cheat Sheet's tenant lifecycle pattern (§7 of that document) together with the Lyncxs regulatory volume's retention standards:

```typescript
async function offboardTenant(tenantId: string, retainDays: number, exportRequired: boolean) {
  await audit.log('tenant_offboarding_started', { tenantId });

  // 1. Mark OFFBOARDING immediately — this must block new writes and new
  //    logins before anything else happens, so the tenant can't keep
  //    operating mid-teardown.
  await db.updateTenantStatus(tenantId, TenantStatus.OFFBOARDING);

  // 2. Revoke every active session and every API key belonging to this
  //    tenant, not just the account that initiated offboarding.
  await revokeAllTenantAccess(tenantId);

  // 3. Export data ONLY when contract, regulation, or explicit tenant
  //    request requires it — don't build a silent default export of
  //    a departing customer's data that nobody asked for.
  const exportLocation = exportRequired ? await exportTenantData(tenantId) : null;

  // 4. Schedule the actual deletion after the documented retention window,
  //    not immediately — most contracts and several regulatory regimes
  //    require a grace period before irreversible deletion.
  const deletionDate = addDays(new Date(), retainDays);
  await db.scheduleTenantDeletion(tenantId, deletionDate);

  await audit.log('tenant_offboarding_completed', { tenantId, exportLocation, deletionDate });
  return { status: 'offboarding_complete', exportLocation, scheduledDeletion: deletionDate };
}

async function executeTenantDeletion(tenantId: string) {
  await audit.log('tenant_deletion_started', { tenantId });

  // Delete in the order that matches your tenancy model:
  // Bridge/Silo: DROP SCHEMA / decommission database.
  // Pool: DELETE FROM every tenant-scoped table WHERE organization_id = tenantId
  //       — derive this table list from schema classification (§2.1's RLS
  //       inventory), not a hand-maintained list that drifts as tables are added.
  await deleteAllTenantRows(tenantId);

  await cache.invalidateTenant(tenantId);          // every cache key namespaced by tenant (§9.2)
  await storage.deleteTenantPrefix(tenantId);       // every object under tenants/{tenantId}/ (§9.3)
  await deleteTenantEncryptionKey(tenantId);        // if tenant-specific keys were provisioned (§4.3)

  // Keep a minimal, permanent record that this tenant existed and was
  // deleted on this date — this is itself often a compliance requirement,
  // and it's what makes "deleted" auditable rather than just "gone."
  await db.updateTenantStatus(tenantId, TenantStatus.DELETED);

  // Explicitly verify: replicas, point-in-time backups, object storage
  // versioning, and any downstream export all follow their OWN retention
  // policies — deleting the primary store does not delete these, and each
  // needs its own scheduled purge or documented exception (e.g. immutable
  // backups may legitimately retain deleted data until their retention
  // window lapses, provided it's not actively used and is purged on schedule).

  await audit.log('tenant_deletion_completed', { tenantId });
}
```

### 8.3 GDPR and the right to erasure, applied at tenant scale

Article 17's right to erasure is usually discussed at the individual-user level, but the same mechanics apply when an entire tenant requests deletion, and the same practical challenges apply at greater scale: <cite index="46-1">cascading deletes across related records, data that persists in backups after the primary deletion, and derived data such as identifiers embedded in analytics tables all complicate what "delete the data" actually requires</cite>. The standard resolution for the audit-log tension specifically — you need audit history for accountability, but that history may name the tenant or its users you're supposed to erase — is anonymization rather than deletion of the record itself: <cite index="47-1">strip or irreversibly hash the identifiers so the record can no longer be linked to the individual or organization, and keep the now-anonymous event; genuinely anonymized data falls outside GDPR's scope, while pseudonymization — where a mapping could still re-identify the party — does not qualify and remains personal data requiring erasure treatment</cite>.

A practical deletion pipeline, regardless of company size: <cite index="47-1">verify the requester's authority before deleting anything, scope the request across every store that holds the tenant's or user's data — primary database, per-tenant data, caches, search indexes, logs, and each downstream sub-processor — then execute and log the deletion</cite>. Treat this as infrastructure you build once and reuse, not a bespoke one-off script each time a deletion request arrives — <cite index="48-1">the discipline that actually holds up under audit is treating data deletion as a continuous, auditable process: map your data stores, define retention rules explicitly, handle backups deliberately rather than by accident, automate the erasure workflow, and log every action taken</cite>.

### 8.4 Choosing your isolation model with offboarding in mind

This is a case where the tenancy-model choice from §2 has a direct, sometimes underappreciated consequence for how clean offboarding can be. Schema-per-tenant and database-per-tenant models make deletion nearly trivial — drop the schema or decommission the database — where a shared-table Pool model requires the `DELETE FROM ... WHERE organization_id = X` sweep across every tenant-scoped table shown above, with correspondingly more surface area for a forgotten table to leave residual data behind. This is a legitimate factor, not just isolation strength, in deciding whether a given tier of tenant (especially regulated-industry or enterprise tenants who negotiate explicit data-handling terms) should be promoted out of Pool.

### 8.5 Offboarding checklist

- [ ] Tenant status transitions to `OFFBOARDING` and blocks new writes/logins before any data is touched
- [ ] All active sessions and API keys revoked immediately, not left to expire naturally
- [ ] Data export produced only when contractually/legally required, delivered before deletion
- [ ] Deletion scheduled after a documented retention window, not executed instantly
- [ ] Deletion sweep covers every tenant-scoped table/schema/database, every cache namespace, every storage prefix, and any tenant-specific encryption key
- [ ] Backups, replicas, and object-storage versions are explicitly accounted for under their own retention policy — not assumed to be handled by the primary-store deletion
- [ ] A minimal permanent audit record survives deletion itself, proving the tenant existed and was properly removed
- [ ] Legal holds or regulatory retention requirements are checked before any hard deletion executes

---

## 9. CROSS-CUTTING CONCERNS: RATE LIMITING, CACHING, STORAGE, ASYNC WORK

These four concerns are easy to design correctly for a single tenant and easy to get subtly wrong the moment a second tenant shares the same infrastructure. Each has the same underlying shape: **classify every piece of shared infrastructure as global, tenant-scoped, or explicitly cross-tenant, and never let a tenant-scoped resource be addressed without a verified tenant identifier as part of its key.**

### 9.1 Rate limiting and the "noisy neighbor" problem

A shared worker pool, database connection pool, or API gateway that isn't rate-limited per tenant lets one tenant's traffic spike degrade service for every other tenant on the same infrastructure — this is a genuine availability risk, not just a fairness concern, and it's named explicitly in current OWASP guidance as "Noisy Neighbor Attacks: one tenant exhausting shared resources, impacting others (DoS)."

```typescript
const TIER_LIMITS: Record<TenantTier, RateLimitConfig> = {
  free:       { requestsPerMinute: 60,   requestsPerDay: 1_000 },
  starter:    { requestsPerMinute: 300,  requestsPerDay: 10_000 },
  business:   { requestsPerMinute: 1000, requestsPerDay: 100_000 },
  enterprise: { requestsPerMinute: 5000, requestsPerDay: 1_000_000 },
};
```

The rule that matters beyond the HTTP-layer limit above: rate limiting at the API boundary does not, by itself, constrain every shared bottleneck downstream. If tenant load can affect other tenants through a shared queue, worker pool, or database connection pool, apply tenant-aware concurrency and throughput controls at *that* bottleneck specifically, in addition to the request-level limit — retain global safety limits as a backstop, and isolate a dedicated worker or resource pool for tenants whose contractual service commitment justifies the operational cost.

### 9.2 Cache isolation

Classify every cached value as global, tenant-scoped, or user-scoped, and always include the tenant identifier as part of the cache key for anything in the second or third category:

```typescript
// Wrong — user IDs are not guaranteed unique across tenants in most schemas,
// and this places tenant-scoped data in a global namespace regardless
function badKey(userId: string) { return `global:user-preferences:${userId}`; }

// Right — tenant-scoped data gets a tenant-scoped key; genuinely shared,
// tenant-independent reference data gets an explicit global namespace
function tenantKey(tenantId: string, resource: string, id: string) { return `tenant:${tenantId}:${resource}:${id}`; }
function globalKey(resource: string, version: string) { return `global:${resource}:${version}`; }
```

Cache-key separation is not a substitute for authorization — authorize the request before reading a protected cached value, every time, even when the key structure already implies tenant scoping.

### 9.3 Storage/blob isolation

Partition tenant-owned files with a tenant-aware key prefix or bucket, and — this is the detail that's easy to skip — guard the prefix-building function against path traversal and prefix-collision, not just against obviously malicious tenant IDs:

```python
def _build_key(tenant_id: str, file_path: str) -> str:
    # Reject anything that isn't the canonical tenant-ID format — naming
    # alone is not authorization, but a malformed ID must never be allowed
    # to alter the object-key structure (e.g. path traversal via "../").
    if not re.fullmatch(r"[A-Za-z0-9_-]{1,128}", tenant_id):
        raise ValueError("Invalid tenant identifier")
    path = PurePosixPath(file_path)
    if not path.parts or path.is_absolute() or ".." in path.parts:
        raise ValueError("Invalid object path")
    return f"tenants/{tenant_id}/{path.as_posix()}"
```

Note also the delimiter in `tenants/{tenant_id}/` — without the trailing slash, a prefix match on tenant `"acme"` would also match another tenant's `"acme-west"`, a subtle but real cross-tenant leak in naive prefix-based storage isolation.

Authorize the exact object and operation *before* generating a pre-signed URL, and scope the URL's lifetime to the operation — the tenant identifier doesn't need to appear in the URL itself, since authorization already happened before signing.

### 9.4 Tenant-aware asynchronous work

A shared queue or topic is not itself a tenant-isolation boundary — it's transport, and isolation has to be enforced at both ends. At the producer, derive tenant context from the authenticated caller and bind it to the message through trusted routing or an integrity-protected payload, never an unverified message field the consumer will simply trust. At the consumer, re-authenticate the producer/broker path, re-establish tenant context, and re-authorize the operation — and if execution can be delayed (a queued job processed minutes or hours later), re-check any time-sensitive membership or permission rather than trusting a decision made when the message was first enqueued, since the person's access may have changed in the interim.

---

## 10. THREAT MODEL AND SECURITY CHECKLIST

Consolidated from the OWASP Multi-Tenant Security Cheat Sheet's key-risks catalog and the Lyncxs security volume's payment threat-model addendum, generalized across both domains.

### 10.1 Threat catalog

| Threat | What it looks like | Primary control |
|---|---|---|
| Cross-tenant data leakage | A bug or misconfiguration exposes tenant A's data to tenant B | RLS (fail-closed) + application-layer scoping as defense in depth |
| Tenant context injection | Attacker manipulates a tenant ID in a header, token, or request body | Never trust client-supplied tenant IDs; verify against server-side membership |
| IDOR (insecure direct object reference) | Resource accessed by ID without checking tenant ownership | Every lookup scoped by `(tenant_id, resource_id)`, not `resource_id` alone |
| Noisy neighbor / resource exhaustion | One tenant's load degrades service for others | Per-tenant rate limits + isolated worker/connection pools at real bottlenecks |
| Privilege escalation across tenants | Admin functionality exploited to reach another tenant's data | Explicit, separately-authorized cross-tenant admin paths; never implicit |
| Shared resource poisoning | Cache, queue, or storage pollution affecting other tenants | Tenant-scoped keys everywhere; explicit global namespace for shared data |
| Insecure onboarding/offboarding | Incomplete provisioning or residual access after deletion | Transactional provisioning with cleanup; explicit offboarding state machine |
| Webhook forgery/replay | Fake or duplicated provider callbacks | HMAC verification, timing-safe compare, idempotency keys |
| Wallet/ledger race conditions | Concurrent operations corrupt a balance | Row locks (`FOR UPDATE`) inside the same transaction as balance-affecting writes |
| Credential leakage | Provider secrets or API keys exposed | Vault references only; zero raw secrets in application database or logs |
| Admin/impersonation abuse | Internal staff misuse elevated access | Allowlist + MFA + explicit elevation + full audit trail; consent-gated impersonation |

### 10.2 Do's and don'ts, consolidated

**Do:**
- Derive tenant context from a server-verified identity and current membership — every time, on every surface (§5.1)
- Enforce isolation at a boundary that survives an application bug (RLS with `FORCE`, correct role, transaction-local context)
- Include tenant identity in every cache key, storage prefix, and rate-limit dimension where the resource varies by tenant
- Apply and verify the documented retention and deletion policy at offboarding, including backups and replicas
- Log server-verified tenant context on every tenant-scoped security and audit event
- Scope AI agent tokens to exactly one organization, with an explicit capability allowlist, never `*`
- Treat SCIM/automated member lifecycle as a near-term requirement once you have any enterprise customer, not a someday feature

**Don't:**
- Trust a tenant ID from a client header, request body, or unverified token claim as authorization proof
- Let an ordinary tenant-scoped request path run through a database role that can bypass RLS
- Use a session-scoped (rather than transaction-scoped) tenant setting with a connection pool
- Store payment provider secrets anywhere but a secret manager, referenced by path
- Let an LLM's own output determine what actions it's permitted to take — authorization happens in code, outside the model's context
- Skip the regulatory-boundary decision (§7.5) for a payments feature just because "it's easier to build the ledger that way"
- Delete a tenant instantly on request without a documented retention window and legal-hold check
## 11. REFERENCE ARCHITECTURE WALKTHROUGHS

Two concrete applications of everything above, matching the examples given for this research: a multi-tenant POS and a multi-tenant project management tool. Both assume the Lyncxs default stack (WCSEA: TypeScript/Next.js, Postgres via Neon, Clerk for auth, Stripe for billing) unless a step-specific reason says otherwise.

### 11.1 Multi-tenant POS

**Tenancy shape**: Pattern B (hierarchical/marketplace) from §2.2 — `Organization → Store/Merchant`. A single-location retailer's Organization has one Store; a franchise owner's Organization has many.

**Tenancy model**: Pool with RLS as the default; promote a Store to Bridge/Silo automatically once it crosses a transaction-volume threshold or the Organization negotiates an enterprise contract requiring isolation (§2, following LPIP §4.2's exact promotion pattern).

**Onboarding**:
1. Organization owner signs up, provisions the Organization in `PROVISIONING` state (§4.1).
2. Owner adds their first Store/Merchant — this is where payment-provider onboarding begins (KYC state machine, §7.2), separately from the Organization's own account creation, since a Store can't take live payments until its merchant verification completes.
3. Owner invites staff via manual invitation (§4.2) — SCIM is unlikely to matter for a typical POS customer's headcount, but keep the door open architecturally (membership + role join table, not staff hardcoded to Organization) since larger retail chains do run SCIM from their own HR systems.

**Authentication**: Web dashboard (owner/manager back-office) uses standard Clerk OAuth session (§5.2). The actual till — the surface staff use all day — is almost always a tablet or fixed terminal: use the mobile/device pattern from §5.3, with a fast local PIN or badge-tap re-auth layered on top of a longer-lived device session, since requiring full OAuth re-login between every customer transaction is both bad UX and unnecessary given the device itself is already authenticated to the Store.

**Authorization**: RBAC is sufficient for the overwhelming majority of POS deployments — `owner / manager / staff` roles, ABAC layered in specifically for shift- and amount-based rules (§6.2's void-transaction example is drawn directly from this domain). Reach for ReBAC only if you build multi-location staff-sharing (§6.3's "manager in the apparel store, staff in home goods" pattern) as an explicit product feature.

**Payments**: This is the domain where the POS case is hardest, because the POS is the merchant-facing surface for §7.4's "tenants ARE the merchants" problem. Model Store as LPIP's Merchant entity; use the Method/Provider/Rail separation so a Store's checkout flow says `card` or `mpesa`, never `stripe` or `daraja`, directly. Decide the account-configuration question from §7.4 early — most POS products should default to the lighter-compliance-burden configuration (merchant manages their own payout relationship) and only build the fully-owned/white-labeled flow if a specific large customer requires it.

**Offboarding**: A Store closing (one location shuts down within a still-active Organization) is different from an Organization leaving entirely — model both. Store closure needs final settlement/reconciliation completed before deactivation; Organization offboarding follows the full sequence in §8.2, with particular attention to the payments-specific retention requirement (financial records typically must be retained substantially longer — 7 years is the figure used elsewhere in the Lyncxs regulatory volume for Kenya — than the general data-deletion grace period you'd apply to, say, a project-management tool's task data).

### 11.2 Multi-tenant project management tool

**Tenancy shape**: Pattern A (flat) initially — `Organization → Workspace → Board/Project → Task`. Nearly every project-management SaaS ends up adding the Workspace layer once customers with multiple departments ask for it; model it from the start even if v1 only ever creates one Workspace per Organization, since retrofitting an extra hierarchy layer under live customer data is materially more painful than including it in the initial schema.

**Tenancy model**: Pool with RLS — this product's data (tasks, comments, boards) rarely has the same physical-isolation demands as a POS's financial data, so Pool is very likely the permanent model for the large majority of customers, with Bridge/Silo reserved for genuine enterprise-contract edge cases.

**Onboarding**: Standard tenant provisioning (§4.1). This is the canonical case for SCIM mattering early — project management tools routinely land enterprise customers whose IT departments expect automated provisioning from day one of a larger contract, so budget for the SCIM server (§4.2) sooner than you would for a smaller-business-focused product like a single-location POS.

**Authentication**: Web is primary; mobile is a real but secondary surface (viewing/commenting on tasks on the go, not full project administration); a public API for integrations (Slack, GitHub, Zapier-style connectors) needs the pk_/sk_ key scheme from §5.4; and — increasingly the differentiator worth investing in — an MCP server so a customer's AI agents can query and update tasks directly. This last surface is exactly the case §5.6 was written for: scope agent tokens to one Organization, give them an explicit capability allowlist (`tasks:read`, `tasks:write`, but not `billing:manage`), and require human confirmation for destructive actions like bulk task deletion.

**Authorization**: Start with RBAC (`owner / admin / member / viewer` per Organization). The feature that reliably pushes a project-management tool toward ReBAC is per-board or per-project sharing with external collaborators who aren't full Organization members — "share this one board with our client's contractor" is precisely the per-resource-sharing case §6.3 describes, and it genuinely cannot be expressed cleanly with role-only RBAC once a user needs `editor` on one board and no access at all to everything else in the same Workspace.

**Payments**: This product is almost always in the "billing tenants" case from §7.1, not the "tenants take payments through us" case — a standard Stripe Subscriptions integration (WCSEA §9.3: Products, Prices, Customers, Subscriptions, Webhooks) is the entire payments surface for most project-management SaaS. Handle the standard webhook set (`checkout.session.completed`, `customer.subscription.updated`, `customer.subscription.deleted`, `invoice.payment_failed`) and the LPIP-derived idempotency discipline from §7.7 even though the payment domain here is much smaller than a POS's — a duplicate-subscription bug from a missed idempotency key is just as real a problem at small scale.

**Offboarding**: This is usually the more straightforward offboarding case of the two, since there's no separate financial-record retention obligation beyond ordinary contractual/GDPR requirements (§8.3) — export the Organization's boards/tasks/comments as structured data (the right to portability, not just erasure), then follow the standard sequence in §8.2.

---

## 12. DECISION MATRICES AND QUICK REFERENCE

### 12.1 Tenancy model selection

| Signal | Choose |
|---|---|
| Standard B2B SaaS, thousands of small-mid tenants expected | Pool (default) |
| Dozens to low hundreds of tenants, moderate isolation need | Bridge |
| Regulated industry, enterprise contract demands physical isolation, very large single tenant | Silo |
| Mixed tenant sizes across the same product | Hybrid — start Pool, promote automatically on volume/contract threshold |

### 12.2 Authorization model selection

| Signal | Choose |
|---|---|
| Fixed set of roles per org membership | RBAC |
| Access depends on context (time, amount, ownership, shift) beyond static role | ABAC (OPA/Rego for complex rule sets) |
| Users share individual resources with specific other users/groups; permission needs to nest | ReBAC (OpenFGA/SpiceDB) |
| Third-party app acting on a user's behalf | OAuth scopes |
| AI agent acting on a tenant's behalf | Capability tokens, RBAC/ReBAC underneath, explicit allowlist |

### 12.3 Payments architecture selection

| Signal | Choose |
|---|---|
| You bill your own tenants for subscription access | Stripe (or equivalent) Subscriptions — you are the sole merchant |
| Tenants take payments from their own customers, single-seller per transaction | Stripe Connect (or Paystack/Adyen equivalent) with a lighter-compliance-burden account configuration as the default |
| Multi-seller carts, marketplace splitting funds among several sellers per transaction | Separate charges + transfers, `transfer_group` reconciliation, platform as merchant of record, reserve balance for disputes |
| You're building the orchestration/ledger layer yourself rather than using a PSP's built-in platform tooling | LPIP's Method/Provider/Rail separation, immutable double-entry ledger, provider-adapter pattern |

### 12.4 Onboarding automation threshold

| Signal | Approach |
|---|---|
| Small business customers, low headcount per tenant | Manual invitation is sufficient |
| Any enterprise customer, or IT-department-managed customer base | Build SCIM 2.0 support before it blocks a deal, not after |

### 12.5 Authentication surface reference (recap of §5.7)

| Surface | Mechanism | Token storage |
|---|---|---|
| Web | OAuth2/OIDC via managed provider | httpOnly, Secure, SameSite cookie |
| Mobile | OAuth2/OIDC + PKCE | Access in memory; refresh in Keychain/Keystore |
| Public API | API keys (pk_/sk_) or OAuth Client Credentials | Hashed at rest; secret manager on caller's side |
| CLI | Device Authorization Flow (RFC 8628) | OS credential store |
| MCP (remote) | OAuth 2.1 resource server, per 2026-07-28 MCP spec | Short-lived, capability-scoped agent JWT |
| MCP (stdio/local) | Environment-provided credential | Local process environment |

---

## 13. SOURCES

**Internal Lyncxs Knowledge Base:**
- `LYNCXS-WORLD-CLASS-SOFTWARE-ENGINEERING-ARCHITECTURE(WCSEA).md` §9 (Enterprise Engineering: multi-tenancy, RBAC, billing, audit, compliance, feature flags)
- `LYNCXS-AUTH-SERVICES-GUIDE.md` §16 (Multi-Tenant/Organization Auth), §20.7–20.8 (AI Agents & MCP, Payment Systems), §21 (Authorization Models — RBAC/ABAC/PBAC/ReBAC/Capability/Scope-based/RLS), §25.4 (SCIM 2.0), §29 (AI Agent Identity), Addendum K (Payment Platform Auth Alignment)
- `LYNCXS-PAYMENT-INFRASTRUCTURE-PLATFORM.md` (LPIP) §1–7 (Platform Architecture, Domain Model, Multi-Tenant Architecture Patterns, Orchestration Engine, Provider Abstraction)
- `LYNCXS-PAYMENT-INTEGRATION-MASTER.md` §27–29 (Client Payment Onboarding Model, Gateway Strategy)
- `LYNCXS-WORLD-CLASS-PRODUCTION-SECURITY-ENGINEERING(WCPSE).md`, Addendum S (Payment Security Threat Model Adoption)
- `LYNCXS-ORM-DATA-ACCESS-ENGINEERING.md`, Addendum L (Payment Data Access Rules — tenant scoping, ledger immutability)
- `LYNCXS-API-DESIGN-ENGINEERING.md`, Addendum A (Payment Platform API Alignment)
- `LYNCXS-GLOBAL-REGULATORY&COMPLIANCE-ENGINEERING.md`, Addendum R (Kenya PSP Boundary Operationalized, KYC Pipeline States)
- `LYNCXS-AI-SYSTEMS-ENGINEERING.md` — Tool Engineering §§(The MCP Standard, Tool Categories, Tool Engineering Principles), AI Maturity Levels §5 (Multi-Agent Systems), Memory Engineering (full taxonomy: conversation/long-term/semantic/episodic/procedural/organizational/team/personal), Human Escalation (HITL patterns, escalation ladder), AI Security (AI-specific threats and controls)
- `upgrade.md`, `README.md`, `UPGRADE-LOG.md` (platform layering and strategic rationale for LPIP)

**External research (current as of September 2026):**
- OWASP Cheat Sheet Series — *Multi-Tenant Security Cheat Sheet*: tenant isolation strategies, RLS implementation and pitfalls, IDOR prevention, cache/storage/async isolation, tenant lifecycle management — cheatsheetseries.owasp.org
- Model Context Protocol specification (2026-07-28 revision) and MCP blog — OAuth 2.1 resource-server requirements, authorization hardening, RFC 9207 `iss` validation — modelcontextprotocol.io, blog.modelcontextprotocol.io
- WorkOS — *The biggest MCP spec update ships July 28: what changes for AI agent authentication* — workos.com/blog
- Aembit — *MCP, OAuth 2.1, PKCE, and the Future of AI Authorization* — aembit.io/blog
- OpenFGA project documentation and use-case guides (AI agent authorization, multi-tenant SaaS, e-commerce) — openfga.dev, CNCF Incubation status
- Clerk, WorkOS, SSOJet, Torii, Authgear, CIAM Compass — SCIM 2.0 (RFC 7643/7644) implementation guides and enterprise-adoption data
- Stripe — Connect documentation, Accounts V2 API, multivendor marketplace payment architecture — stripe.com/connect, stripe.com/resources
- DEV Community, No7 Software — Stripe Connect 2026 implementation patterns (Accounts V2, destination charges vs. separate charges and transfers)
- Drata, DCHost, ValidonX, Reform, archman.dev — GDPR right to erasure and data portability at SaaS/multi-tenant scale

---

*Document maintained by Lyncxs Industries. Part of the LYNCXS Knowledge Base. Companion volumes listed above. This is a synthesis and extension document — where it conflicts with LPIP or the Auth Services Guide on payment- or auth-specific normative rules, those source volumes govern; this document governs the cross-cutting multi-tenant architecture that composes them.*
