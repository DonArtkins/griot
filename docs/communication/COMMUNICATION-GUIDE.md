# Griot Communication Guide — Email (Brevo) [own-stack]

Owner: `backend/project-kit/feature-specs/12-communication-channels-brevo.md`.
This is the single source of truth for how Griot talks to users and operators.

Outbound messaging is **Email only** (SMS/WhatsApp/Contacts/automations were removed from
code in the same branch — user decision: only transactional Email is needed). All email
runs through **Brevo** (`POST /v3/smtp/email`, one account/API key — 300 emails/day free
cap). Delivery is **best-effort for registration AND organization onboarding (spec 32)**:
register and org-onboarding owner-invite sends are fire-and-forget with structured logging
and never fail their 201 responses — in onboarding the owner invite is sent AFTER the durable
commit, so a Brevo delivery failure must NOT roll back or invalidate the committed onboarding.
`/api/auth/otp/request` is the
one caller that surfaces delivery failure (HTTP 502 on Brevo reject/outage). Login sends
no email.

---

## 1. Architecture

```
 AuthService ({RegisterAsync, RequestOtpAsync} + admin notice)
        |
        v
 IEmailService ----------> BrevoEmailService
 (Griot.Application)        (Griot.Infrastructure/Email)
                                   | POST https://api.brevo.com/v3/smtp/email
                                   v
                              Brevo (api-key)
```

- **Auto-callers today:** `AuthService` (`register` -> OTP `email_verify` email to the user +
  admin "New user registered" notice to `Brevo:ContactToEmail`; `/api/auth/otp/request` -> OTP email)
  and `OrganizationLifecycleService` (spec 32: `POST /api/organizations` -> branded company-owner
  invite email via `RenderOrganizationInviteEmail`, sent best-effort AFTER the durable onboarding
  commit). All send via the single sender (§2).
- **Redis gate before Brevo:** the OTP request route enforces `ratelimit:otp:request:{email}`
  3/15min; the API global rate limiter caps 100/min/caller. This slows abuse but does NOT
  make Brevo's 300/day cap mathematically unreachable — the cap is an operational budget
  to monitor, not a hard guarantee (see §4/§7).
- Future task/notification/agent callers add calls via `IEmailService` - never Brevo directly.

## 2. Sender identity (single `from:` — 2026-09-11 user decision)

Every Griot email is sent from **one** Brevo-verified sender — `Brevo:FromEmail`, currently
the Brevo dashboard-verified `info.donartkins.ke@gmail.com` (account id
`12095245`) with display name `Brevo:FromName` (default `Griot`). The former six-profile
map (`Brevo:Senders:Security/Admin/NoReply/Support/Info/Team`, e.g. `noreply@griot.app`) was
**removed** because `griot.app` was never verified — Brevo rejected those sends (§7b).
A per-call `message.ReplyTo` still controls the reply address (currently unused by callers).

| Caller | From | Reply-To | Used for |
|---|---|---|---|
| All email (OTP/verify, admin notice, future notification email) | `Brevo:FromEmail` as `Brevo:FromName` | *(none unless `message.ReplyTo` set)* | everything |

Config (git-ignored `appsettings.Local.json`) or env vars:
```jsonc
"Brevo": {
  "ApiKey": "xkeysib-...",
  "FromEmail": "info.donartkins.ke@gmail.com",  // the dashboard-verified sender
  "FromName": "Griot",
  "ContactToEmail": "info.donartkins.ke@gmail.com"           // admin new-user notice inbox
}
```
If a branded domain is verified later (§7b), ONLY `Brevo:FromEmail` changes — no code
change and no per-feature senders. Callers never pass a From; the transport owns it.

## 3. Brevo dashboard setup (one-time)

1. **API key** -- Settings -> SMTP & API -> API Keys -> `xkeysib-...` -> `Brevo:ApiKey`.
2. **Verify a domain** -- Settings -> Senders -> Add domain -> add the DNS records Brevo
   gives you (domain verification TXT + DKIM + DMARC; add SPF only when Brevo requires
   it for this specific account setup) -> wait for
   verification -> this is what kills the `@12095245.brevosend.com` rewrite.
3. **Senders** -- create `noreply@`, `support@`, `info@`, `team@` on your verified domain
   (or add the domain and use any mailbox -- Brevo sends from the verified domain).

## 4. Rate limit & budget (you will NOT hit limits)

Brevo free email cap = **300 emails/day**. Griot protects itself in TWO layers:

| Layer | Window | Key | Limits |
|---|---|---|---|
| AuthController (existing) | OTP request 3/15min per email | `ratelimit:otp:request:{email}` | per email |
| API global limiter (existing) | 100/min per caller | -- | HTTP layer |

The Redis window is checked **before** any Brevo HTTP call -- an exhausted window returns
429 with `Retry-After` without firing a request. Budget example: with OTP at most
3/15min/email and a 300/day account cap, one full OTP cycle per user (1 email) means
~300 users/day max; even at the 3-email window maximum, ~100 users doing 3 requests each
exhausts the day -- shared-IP test days hit this quickly (502s from
`/api/auth/otp/request` are the symptom). Plan a paid tier or spread signups before any
multi-user event.

## 5. Configuration summary

| Var | Notes |
|---|---|
| `BREVO_API_KEY` (`Brevo:ApiKey`) | `xkeysib-...` -- REQUIRED for delivery |
| `BREVO_FROM_EMAIL` (`Brevo:FromEmail`) | THE single sender — must be the Brevo dashboard-verified sender (currently `info.donartkins.ke@gmail.com`) |
| `BREVO_FROM_NAME` (`Brevo:FromName`) | display name (default `Griot`) |
| `CONTACT_TO_EMAIL` (`Brevo:ContactToEmail`) | admin inbox for register notices (canonical `info.donartkins.ke@gmail.com`) |
| `SITE_URL` | deployed origin, used in branded email footer links |

(All `Brevo:` keys accept `__` env form, e.g. `Brevo__ApiKey`.)

## 6. Deploying (Vercel frontend + Railway backend)

Backend env (Railway or compose -- `__` separator), NOT committed:

| Var | Value |
|---|---|
| `BREVO_API_KEY` | `xkeysib-...` |
| `BREVO_FROM_EMAIL` | the dashboard-verified sender, e.g. `info.donartkins.ke@gmail.com` |
| `BREVO_FROM_NAME` | `Griot` |
| `SITE_URL` | deployed app origin (used in email footers/links) |

Frontend (Vercel): the **web app never calls Brevo** -- it only calls the .NET API
(`VITE_API_URL`), which owns every channel. No Brevo keys in Vercel for the web app.

## 7. Observability & troubleshooting

- Every accepted/rejected send is logged with recipient + Brevo status/body in the API log.
- `502` from `POST /api/auth/otp/request` -> check log line "Brevo rejected email to ...:
  4xx ..." and Brevo dashboard -> Transactional -> Emails.
- Still seeing `brevosend.com`? That IS the configured sender — the dashboard-verified
  `<account-id>@<account-id>.brevosend.com` address (§2). A branded From requires domain
  verification (§7b), which changes only `Brevo:FromEmail`.
- In-app notifications (web push, mobile push, AI copilot messages) are the NEXT layer:
  they will ship on the Notifications domain + GraphQL (spec 16) without adding new channels.

## 7b. Q&A — "sender is not valid" and the Vercel domain question (2026-09-11)

**Q: "Sending has been rejected because the sender you used noreply@griot.app is not valid. Validate your sender or authenticate your domain"**

The From-domain (`griot.app`) is **not verified in Brevo**, so Brevo rejects the send at the API. Hosting the frontend on **`griot.vercel.app` does NOT fix it**: Vercel owns `*.vercel.app` (shared wildcard) — you cannot add Brevo's DKIM/SPF TXT records there, and it is not a domain you control. **This is why the `noreply@griot.app`-style profile senders were removed on 2026-09-11** — every email now sends from the single dashboard-verified sender.

**The sender that works (implemented):** `Brevo:FromEmail` = `info.donartkins.ke@gmail.com` — Brevo's dashboard-verified `<account-id>@<account-id>.brevosend.com` address (account id `12095245`). It delivers today to arbitrary recipients (300/day free cap); it just isn't the brand.

**If a branded From is wanted later (optional):** verify a domain **you own** (e.g. `griot.app` or `mail.griot.app`): Brevo dashboard → Senders → authenticate your domain → add the Brevo verification TXT plus the required DKIM and DMARC records shown for the domain → verify → change ONLY `Brevo:FromEmail` to an address on that domain. Outbound-only sending needs no MX records; reply-to mailboxes (e.g. support@...) require inbound mail hosting and MX records. SPF is added only when Brevo requires it for this account setup. Confirm the domain shows authenticated before switching the sender.

**Keep `SITE_URL=https://griot.vercel.app`** for email-footer links (separate concept; correct as-is).

## 8. Roadmap (email + in-app notifications)

| Form | Channel | Status |
|---|---|---|
| Email OTP/admin | Brevo Email (single verified sender) | implemented (Email only) |
| SMS transactional | Brevo SMS | removed (user decision -- Email only) |
| WhatsApp transactional | Brevo WhatsApp | removed (user decision -- Email only) |
| Brevo Contacts sync + automations | Brevo Contacts/Workflows | removed (user decision -- Email only) |
| In-app notifications (web/mobile/AI) | Notifications tables + GraphQL | next (DomainService + web + mobile + AI copilot) |

---
**Engineering Excellence. Production Mindset. Professional Impact.**

## Domain verification source

Follow [Brevo’s domain-authentication guide](https://help.brevo.com/hc/en-us/articles/12163873383186-Authenticate-your-domain-with-Brevo-Brevo-code-DKIM-DMARC) for the exact account-provided verification, DKIM and DMARC records. An example domain or sender in this repository is not proof that this account owns or has verified it.
