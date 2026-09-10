# Griot Communication Guide — Email (Brevo) [own-stack]

Owner: `backend/project-kit/feature-specs/12-communication-channels-brevo.md`.
This is the single source of truth for how Griot talks to users and operators.

Outbound messaging is **Email only** (SMS/WhatsApp/Contacts/automations were removed from
code in the same branch — user decision: only transactional Email is needed). All email
runs through **Brevo** (`POST /v3/smtp/email`, one account/API key — 300 emails/day free
cap). Delivery is **best-effort by design**: every send is fire-and-forget with structured
logging; a Brevo outage never fails registration, login or any other API call.

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

- **AuthService** is the only auto-caller today: `register` -> OTP `email_verify` email to
  the user (`security` sender) + admin "New user registered" notice to
  `Brevo:ContactToEmail` (`admin` sender); `/api/auth/otp/request` -> OTP email.
- **Redis gate before Brevo:** the OTP request route enforces `ratelimit:otp:request:{email}`
  3/15min; the API global rate limiter caps 100/min/caller. Brevo's 300/day cap is
  therefore never reachable from app code.
- Future task/notification/agent callers add calls via `IEmailService` - never Brevo directly.

## 2. Sender identities (your `from:` question)

Brevo rewrites unverified From-domains to their shared `<account-id>.brevosend.com`
(your case: `info.donartkins.ke@12095245.brevosend.com`, account id `12095245`). To send
as **your own brand** you must verify a custom domain in Brevo (free plan supports one
sender-domain per account) and use addresses on it. Public domains (gmail.com) can't be
verified. **Reply-to** is fully controlled per sender profile (empty = no reply expected).

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
  "ApiKey": "xkeysib-...",
  "Senders": {
    "Security": { "Email": "noreply@griot.app", "Name": "Griot Security", "ReplyTo": "" },
    "Admin":    { "Email": "noreply@griot.app", "Name": "Griot",        "ReplyTo": "support@griot.app" }
  }
}
```
Email `SenderKey` is passed by the caller (AuthService sends OTP with `security`, admin
notice with `admin`). Fallbacks: `Brevo:FromEmail/Brevo:FromName`, then "Griot".

## 3. Brevo dashboard setup (one-time)

1. **API key** -- Settings -> SMTP & API -> API Keys -> `xkeysib-...` -> `Brevo:ApiKey`.
2. **Verify a domain** -- Settings -> Senders -> Add domain -> add the DNS records Brevo
   gives you (domain verification TXT + SPF + DKIM; DMARC recommended) -> wait for
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
429 with `Retry-After` without firing a request. Budget: with 300/day and OTP at most
3/15min/email, daily testing of ~30 users x 10 emails each stays far inside the free cap.

## 5. Configuration summary

| Var | Notes |
|---|---|
| `BREVO_API_KEY` (`Brevo:ApiKey`) | `xkeysib-...` -- REQUIRED for delivery |
| `BREVO_FROM_EMAIL` (`Brevo:FromEmail`) | fallback sender when no `Senders` profile; must be verified |
| `BREVO_FROM_NAME` (`Brevo:FromName`) | fallback display name (default `Griot`) |
| `BREVO_SENDER_<KEY>_EMAIL/_NAME/_REPLYTO` | per-profile identities (KEY in SECURITY, ADMIN, NOREPLY, SUPPORT, INFO, TEAM) |
| `CONTACT_TO_EMAIL` (`Brevo:ContactToEmail`) | admin inbox for register notices (canonical `info.donartkins.ke@gmail.com`) |
| `SITE_URL` | deployed origin, used in branded email footer links |

(All `Brevo:` keys accept `__` env form, e.g. `Brevo__ApiKey`.)

## 6. Deploying (Vercel frontend + Railway backend)

Backend env (Railway or compose -- `__` separator), NOT committed:

| Var | Value |
|---|---|
| `BREVO_API_KEY` | `xkeysib-...` |
| `BREVO_FROM_EMAIL` | verified sender (only if no `Brevo:Senders:*` profiles) |
| `BREVO_SENDER_SECURITY_EMAIL` | `noreply@griot.app` |
| `BREVO_SENDER_ADMIN_EMAIL` / `_NAME` / `_REPLYTO` | admin profile |
| `SITE_URL` | deployed app origin (used in email footers/links) |

Frontend (Vercel): the **web app never calls Brevo** -- it only calls the .NET API
(`VITE_API_URL`), which owns every channel. No Brevo keys in Vercel for the web app.

## 7. Observability & troubleshooting

- Every accepted/rejected send is logged with recipient + Brevo status/body in the API log.
- `502` from `POST /api/auth/otp/request` -> check log line "Brevo rejected email to ...:
  4xx ..." and Brevo dashboard -> Transactional -> Emails.
- Still seeing `brevosend.com`? The domain isn't verified yet (see 3) -- the profile
  addresses exist but Brevo performs the rewrite until verification completes.
- In-app notifications (web push, mobile push, AI copilot messages) are the NEXT layer:
  they will ship on the Notifications domain + GraphQL (spec 16) without adding new channels.

## 8. Roadmap (email + in-app notifications)

| Form | Channel | Status |
|---|---|---|
| Email OTP/admin | Brevo Email (multi-sender) | implemented (Email only) |
| SMS transactional | Brevo SMS | removed (user decision -- Email only) |
| WhatsApp transactional | Brevo WhatsApp | removed (user decision -- Email only) |
| Brevo Contacts sync + automations | Brevo Contacts/Workflows | removed (user decision -- Email only) |
| In-app notifications (web/mobile/AI) | Notifications tables + GraphQL | next (DomainService + web + mobile + AI copilot) |

---
**Engineering Excellence. Production Mindset. Professional Impact.**
