# Backend Feature Spec 12 — Communication Channels (Brevo Email only) [own-stack]

## Goal

Transactional Email via Brevo with a **single Brevo-verified sender identity** (2026-09-11
user decision — the former domain-specific `Brevo:Senders:<Key>` profile map, e.g.
`noreply@griot.app`/`support@griot.app`, was removed because `griot.app` is not verified;
every email now sends from the one Brevo dashboard-verified sender `Brevo:FromEmail`),
rate-guarded so Griot never trips Brevo quota caps and never fails auth. SMS/WhatsApp/
Contacts-automations were explicitly **removed** (user decision: only Email needed) and the
`ICommunicationService` facade deleted in the same branch that shipped the full REST surface
(specs 13-17).

Canonical guide: `docs/communication/COMMUNICATION-GUIDE.md`.

## Dependencies

- Spec 07 (auth -- OTP delivery + admin notice) -- **completes its delivery path**
- Spec 08 (Postman) -- collection unaffected (no route changes)

## Separation of Concerns

- **Application** (`Griot.Application`) owns `IEmailService` + email message records.
  AuthService calls `IEmailService`-shaped paths only.
- **Infrastructure** (`Griot.Infrastructure/Email/`) owns the Brevo HTTP transport:
  `BrevoEmailService` (SSRF redirects disabled, never throws).
- **Api** (`Program.cs`) wires the typed `IHttpClientFactory` client (15 s timeout).

## Implementation

1. `IEmailService.SendAsync(EmailMessage)` — `{sender: {name, email}}` is resolved **solely**
   from `Brevo:FromEmail`/`BREVO_FROM_EMAIL` (required; the one Brevo dashboard-verified
   sender) + `Brevo:FromName`/`BREVO_FROM_NAME` (default `Griot`); optional
   `message.ReplyTo` overrides the reply address per call. Sends `{sender, to, subject,
   htmlContent, textContent, replyTo}` to `POST /v3/smtp/email`.
2. AuthService: both sends use the single sender — OTP email to the user, admin
   "New user registered" notice to `Brevo:ContactToEmail`. **Best-effort is scoped to
   registration only** — register always returns 201 and a Brevo failure there is logged,
   never surfaced; login sends no email. `/api/auth/otp/request` surfaces delivery failure
   as **HTTP 502 Bad Gateway** (202 on success, 401 unknown email, 429 rate-limited).
3. Rate guard: `ratelimit:otp:request:{email}` 3/15min (AuthController, Redis) + API
   global limiter 100/min/caller. No per-recipient redis window for plain email beyond
   the route-level OTP window (facade removed with SMS/WhatsApp).
4. Failure semantics: `BrevoEmailService` returns success/failure instead of throwing;
   AuthService maps an OTP-send failure to `OtpRequestResult { Success = false }` and
   AuthController returns 502 with a message. Registration emails have no such mapping —
   they are fire-and-forget with structured logs ("Brevo rejected email to ...").

## Rate limits

| Layer | Window | Key |
|---|---|---|
| OTP request route | 3 / 15 min per email | `ratelimit:otp:request:{email}` |
| API global limiter | 100 / min per caller | -- |

Brevo's **300 emails/day free cap is an operational budget, not a hard guarantee**:
the OTP window throttles abusive callers but a burst of distinct verified emails (or
shared-IP testing) can still exhaust it. When it does, `/api/auth/otp/request` returns
502 (delivery rejected) while registration stays 201 — monitor the API log line
"Brevo rejected email to ...".

## Docker & Deploy

Backend env: `BREVO_API_KEY`, `BREVO_FROM_EMAIL`, `BREVO_FROM_NAME`, `CONTACT_TO_EMAIL`, `SITE_URL`. Web
(Vercel) never holds Brevo keys (only `VITE_API_URL`). The `BREVO_SENDER_<KEY>_EMAIL/_NAME/_REPLYTO`
variables were removed with the profile map (2026-09-11).

## Sender validation & the Vercel domain question (2026-09-11 FAQ)

**Q: "Sending has been rejected because the sender you used noreply@griot.app is not valid. Validate your sender or authenticate your domain"**

**A:** The From-domain `griot.app` is **not verified in Brevo**, so Brevo rejects the send. **This is why the profile senders were removed on 2026-09-11** — the single `Brevo:FromEmail` is now the Brevo dashboard-verified `<account-id>@<account-id>.brevosend.com` sender, which is valid today.

1. **Interim (implemented):** use Brevo's dashboard default sender (the `<account-id>@<account-id>.brevosend.com` address) — valid today, but not the brand.
2. **When a domain is verified (optional, user decision):** Brevo → Senders → "I want to send from my own domain" → add the `brevo-code` TXT record plus the required DKIM and DMARC records displayed by Brevo → verify → change ONLY `Brevo:FromEmail` to an address on that domain. No code change; no per-feature senders return.

**Will hosting the frontend on `griot.vercel.app` fix it?** **No.** Vercel owns `*.vercel.app` (a shared wildcard domain); you cannot add Brevo's TXT records to it and it is not a domain you control. Verify a domain **you own** (e.g. `griot.app` or `mail.<your-domain>`). Outbound-only sending needs no MX records. A domain hosting reply-to mailboxes (for example support@...) needs inbound mail hosting and MX records. Add SPF only when Brevo identifies it as required for the account setup; confirm authenticated status before enabling production senders. `SITE_URL=https://griot.vercel.app` stays correct as the email-footer origin; it is unrelated to the sender domain.

**Sender format that works for ALL Griot email (2026-09-11):** the single Brevo
dashboard-verified sender `Brevo:FromEmail` = `info.donartkins.ke@gmail.com`
(one identity for every purpose: OTP/2FA, admin notice, future notification email; no
reply-to unless a caller sets `message.ReplyTo`). If a branded domain is authenticated
later, ONLY `Brevo:FromEmail` changes — no code change and no per-feature From-domains.

## Acceptance Criteria

- [x] Single sender resolved from `Brevo:FromEmail`/`Brevo:FromName` with per-call `ReplyTo` (`BrevoEmailService.cs`)
- [x] OTP email + admin notice both send via the single sender (`AuthService.cs`)
- [x] (2026-09-11) Domain-specific `Brevo:Senders:<Key>` profile map removed from code, config and all contract-synced docs (user decision: `griot.app` unverified → every email sends from the Brevo dashboard-verified `brevosend.com` sender, e.g. `info.donartkins.ke@gmail.com`)
- [x] SMS/WhatsApp/Contacts/automations code + `ICommunicationService` facade removed;
      all comm docs contract-synced to Email-only (AGENTS root/backend, stack-contract,
      integration-contracts, api-surface, README, COMMUNICATION-GUIDE, AUTHENTICATION-GUIDE)
- [x] Redis OTP window gates email sends before Brevo
- [x] Unit tests green (`dotnet test`)
- [ ] (operator, optional) Verify a domain YOU own in Brevo (DNS TXT: `brevo-code`, required DKIM and DMARC) and change ONLY `Brevo:FromEmail` to a branded address — `griot.vercel.app` cannot be verified (Vercel-owned wildcard; see FAQ above). Until then the dashboard-verified `brevosend.com` sender delivers every email.
- [ ] In-app notifications (web/mobile + AI copilot) inside the Notifications domain (next spec)

## Verification

```bash
cd backend
dotnet build --no-incremental && dotnet test --no-build   # 0W/0E, all green
python3 ../scripts/check-contract-sync.py                 # exit 0
```

---
**Engineering Excellence. Production Mindset. Professional Impact.**
