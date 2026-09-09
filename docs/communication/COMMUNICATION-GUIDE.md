# Griot Communication Guide — Email, SMS, WhatsApp & Automations (Brevo) [own-stack]

Owner: `backend/project-kit/feature-specs/12-communication-channels-brevo.md`.
This is the single source of truth for how Griot talks to users and operators.

All channels run through **Brevo** (same account/API key — one quota pool). Delivery is
**best-effort by design**: every send is fire-and-forget with structured logging; a Brevo
outage never fails registration, login or any other API call.

---

## 1. Architecture

```
                    ┌─────────────────────────────────────────────────────────┐
                    │ ICommunicationService (CommunicationService)           │
                    │  per-channel, per-recipient REDIS sliding-window guard  │
                    └───────┬───────────────┬───────────────┬────────────────┘
                            │               │               │
                    IEmailService      ISmsService     IWhatsAppService
                    BrevoEmailService  BrevoSmsService  BrevoWhatsAppService
                    /v3/smtp/email     /v3/transactionalSMS/send  /v3/whatsapp/sendMessage
                            └───────────┴───────────────┴───────────┘
                                  ONE Brevo API key (xkeysib-…)
```

- **AuthService** (and future task/notification/agent services) calls ONLY
  `ICommunicationService` — never Brevo directly.
- **Auto-callers today:** register → OTP email (`security` sender) + admin notice (`admin`
  sender) + **contact upsert** (automation hook); OTP verify → **contact verified** signal.
- **IContactSynchronizer** (Brevo Contacts API) mirrors user lifecycle so Brevo
  **Automations** (welcome, onboarding, re-engagement) can fire without app involvement.

## 2. Sender identities (your `from:` question)

Brevo rewrites unverified From-domains to their shared `<account-id>.brevosend.com`
(your case: `info.donartkins.ke@12095245.brevosend.com`, account id `12095245`). To send
as **your own brand** you must verify a custom domain in Brevo (free plan supports one
sender-domain per account) and use addresses on it. Public domains (gmail.com) can't be
verified. **Reply-to** is now fully controlled per sender profile (empty = no reply expected).

| SenderKey | From (after domain verified) | Reply-To | Used for |
|---|---|---|---|
| `Security` | Griot Security <noreply@yourdomain> | *(none)* | OTP/verification codes (never reply) |
| `NoReply` | Griot <noreply@yourdomain> | *(none)* | system/automated notices |
| `Admin` | Griot <noreply@yourdomain> | support@yourdomain | ops "new user registered" |
| `Support` | Griot Support <support@yourdomain> | support@yourdomain | support communications |
| `Info` | Griot <info@yourdomain> | info@yourdomain | info/announcements |
| `Team` | Griot Team <team@yourdomain> | team@yourdomain | team/feature updates |

Config (git-ignored `appsettings.Local.json` under `Brevo:Senders:<Key>`) or env vars
`BREVO_SENDER_<KEY>_EMAIL/_NAME/_REPLYTO`, e.g.:
```jsonc
"Brevo": {
  "ApiKey": "xkeysib-…",
  "Senders": {
    "Security": { "Email": "noreply@griot.app", "Name": "Griot Security", "ReplyTo": "" },
    "Admin":    { "Email": "noreply@griot.app", "Name": "Griot",        "ReplyTo": "support@griot.app" }
  }
}
```
Email `SenderKey` is passed by the caller (AuthService sends OTP with `security`, admin
notice with `admin`). Fallbacks: `Brevo:FromEmail/Brevo:FromName`, then "Griot".

## 3. Brevo dashboard setup (one-time)

1. **API key** — Settings → SMTP & API → API Keys → `xkeysib-…` → `Brevo:ApiKey`.
2. **Verify a domain** — Settings → Senders → Add domain → add the DNS records Brevo
   gives you (domain verification TXT + SPF + DKIM; DMARC recommended) → wait for
   verification → this is what kills the `@12095245.brevosend.com` rewrite.
3. **Senders** — create `noreply@`, `support@`, `info@`, `team@` on your verified domain
   (or add the domain and use any mailbox — Brevo sends from the verified domain).
4. **SMS** — Settings → SMS → add credits (per-message pricing, not included in email
   free plan) → note your sender name (≤11 alphanumeric chars, e.g. `Griot`).
5. **WhatsApp** — Apps and integrations → enable **WhatsApp** → link WhatsApp Business
   Account (Facebook + WhatsApp Business) → approval of the business number → create a
   message template (Campaigns → WhatsApp) → copy `templateId`.

## 4. Channels & rate-limit budget (you will NOT hit limits)

Brevo free email cap = **300 emails/day**. Griot protects itself in TWO layers:

| Layer | Window | Key | Limits |
|---|---|---|---|
| AuthController (existing) | OTP request 3/15min | `ratelimit:otp:request:{email}` | per email |
| CommunicationService (new) | Email 10/15min per recipient | `ratelimit:comm:email:{to}` | per recipient |
| CommunicationService | SMS 5/hour per number | `ratelimit:comm:sms:{phone}` | per number |
| CommunicationService | WhatsApp 5/hour per number | `ratelimit:comm:whatsapp:{phone}` | per number |
| ApiController (existing) | 100/min per caller | — | HTTP layer |

The Redis window is checked **before** any Brevo HTTP call — an exhausted window returns
`false` without firing a request (logged). Brevo's own caps are therefore unreachable from
app code. Budget: with 300/day and 10/15min/recipient, daily testing of ~30 users × 10
emails each stays far inside the free cap. On the free plan, SMS/WhatsApp and >300
emails/day require the paid plans — see Brevo pricing.
## 5. Automations (Brevo dashboard) — triggers wired from Griot

The app emits these **contact lifecycle signals** (via `IContactSynchronizer`):
- **Contact created** — on register (attributes: FIRSTNAME, LASTNAME, ACCOUNT_STATUS=UNVERIFIED, EMAIL_VERIFIED=false).
- **Contact updated** — email verified (ACCOUNT_STATUS=VERIFIED, EMAIL_VERIFIED=true).

Recommended workflows (build in Brevo → Automations → Workflows):

| Automation | Trigger | Actions |
|---|---|---|
| Welcome | Contact is added / attribute update | send Welcome email (template) |
| Onboarding sequence | ACCOUNT_STATUS → VERIFIED | Day 0: "let's set up your workspace"; Day 2: conditional split on `EMAIL_VERIFIED`/login event → tips or reminder |
| Re-engagement | no login in 14 days (custom event) | win-back email |
| Password-reset follow-up | reset requested → 1h wait → conditional on completed | confirmation or security alert |

> Keep OTP and other **time-critical transactional emails on the Transactional API**
> (Griot's email path) — never route them through Automations (delivery latency).
> Automations serve the lifecycle emails above.

## 6. SMS & WhatsApp usage

```csharp
// SMS (transactional — never marketing; type:"transactional", no [STOP CODE] requirement)
await comm.SendSmsAsync(new SmsMessage("+2547XXXXXXX", "Griot code: 123456"), ct);

// WhatsApp (text or template)
await comm.SendWhatsAppAsync(new WhatsAppMessage("+2547XXXXXXX", "Griot code: 123456"), ct);  // text
await comm.SendWhatsAppAsync(new WhatsAppMessage("+2547XXXXXXX", TemplateId: 465118589032810), ct);
```
Config: `Brevo:Sms:Sender` (≤11 chars), `Brevo:Sms:Tag`, `Brevo:WhatsApp:SenderNumber`
(required — with country code, digits only). Add sender identity/recipient limits per
number are enforced by the facade windows in §4. Numbers: E.164 digits, no `+`/spaces.

## 7. Deploying (Vercel frontend + Railway backend)

Backend env (Railway or compose — `__` separator), NOT committed:

| Var | Value |
|---|---|
| `BREVO_API_KEY` | `xkeysib-…` |
| `BREVO_FROM_EMAIL` | verified sender (only if no `Brevo:Senders:*` profiles) |
| `BREVO_SENDER_SECURITY_EMAIL` | `noreply@griot.app` |
| `BREVO_SENDER_ADMIN_EMAIL` / `_NAME` / `_REPLYTO` | admin profile |
| `BREVO_SMS_SENDER`, `BREVO_SMS_TAG` | SMS defaults |
| `BREVO_WHATSAPP_SENDER_NUMBER` | WhatsApp business number |
| `SITE_URL` | deployed app origin (used in email footers/links) |

Frontend (Vercel): the **web app never calls Brevo** — it only calls the .NET API
(`VITE_API_URL`), which owns every channel. No Brevo keys in Vercel for the web app.

## 8. Observability & troubleshooting

- Every accepted/rejected send is logged with recipient + Brevo status/body in the API log.
- `502` from `POST /api/auth/otp/request` → check log line "Brevo rejected email to …:
  4xx …" and Brevo dashboard → Transactional → Emails.
- Still seeing `brevosend.com`? The domain isn't verified yet (see §3.2) — the profile
  addresses exist but Brevo performs the rewrite until verification completes.
- Contact not in Brevo / automation not firing → check the log for "Brevo contacted
  upserted" and confirm the workflow trigger matches contact-create/attribute-update.
- In-app notifications (web push, mobile push, AI copilot messages) are the NEXT layer
  (spec 12 §9): they will share the same `ICommunicationService` shape so call sites
  never change.

## 9. Roadmap (in-app notifications)

| Form | Channel | Status |
|---|---|---|
| Email OTP/admin | Brevo Email | ✅ implemented |
| SMS transactional | Brevo SMS | ✅ implemented (facade + limits) |
| WhatsApp transactional | Brevo WhatsApp | ✅ implemented (facade + limits) |
| Brevo contact sync + automations | Brevo Contacts/Workflows | ✅ hook implemented; workflows on dashboard |
| In-app notifications (web/mobile/AI) | Notifications tables + GraphQL | 🔜 next (NotificationService + web + mobile + AI copilot) |
| Webhooks (bounce/complaint) | `POST /api/webhooks/brevo` | 🔜 scaffold 501 today |

---
**Engineering Excellence. Production Mindset. Professional Impact. 🚀**