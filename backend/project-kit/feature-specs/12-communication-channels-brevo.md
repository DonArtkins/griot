# Backend Feature Spec 12 — Communication Channels (Brevo Email only) [own-stack]

## Goal

Transactional Email via Brevo with per-purpose sender identities, rate-guarded so Griot
never trips Brevo quota caps and never fails auth. SMS/WhatsApp/Contacts-automations were
explicitly **removed** (user decision: only Email needed) and the `ICommunicationService`
facade deleted in the same branch that shipped the full REST surface (specs 13-17).

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

1. `IEmailService.SendAsync(EmailMessage)`; `EmailMessage.SenderKey` resolves
   `Brevo:Senders:<Key>:Email/Name/ReplyTo` (fallback `Brevo:FromEmail/FromName`), sends
   `{sender, to, subject, htmlContent, textContent, replyTo}` to `POST /v3/smtp/email`.
2. AuthService: OTP email uses `SenderKey:"security"`, admin notice uses `"admin"`;
   register -> email-verify OTP to user + admin "New user registered" notice to
   `Brevo:ContactToEmail`. **Best-effort is scoped to registration only** — register
   always returns 201 and a Brevo failure there is logged, never surfaced; login sends
   no email. `/api/auth/otp/request` surfaces delivery failure as **HTTP 502 Bad
   Gateway** (202 on success, 401 unknown email, 429 rate-limited).
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

Backend env: `BREVO_API_KEY`, `BREVO_FROM_EMAIL`, `BREVO_SENDER_<KEY>_EMAIL/_NAME/_REPLYTO`, `SITE_URL`. Web
(Vercel) never holds Brevo keys (only `VITE_API_URL`).

## Sender validation & the Vercel domain question (2026-09-11 FAQ)

**Q: "Sending has been rejected because the sender you used noreply@griot.app is not valid. Validate your sender or authenticate your domain"**

**A:** The From-domain `griot.app` is **not verified in Brevo**, so Brevo rejects the send. Two remedies:

1. **Authenticate a domain you own** (recommended): Brevo → Senders → "I want to send from my own domain" → add the `brevo-code` TXT record (+ optional DKIM/SPF) → verify → all profile senders on that domain (e.g. `noreply@<your-domain>`) become valid and stop being rewritten to `brevosend.com`.
2. **Interim:** use Brevo's dashboard default sender (the `<account-id>@<account-id>.brevosend.com` address) — valid today, but not your brand.

**Will hosting the frontend on `griot.vercel.app` fix it?** **No.** Vercel owns `*.vercel.app` (a shared wildcard domain); you cannot add Brevo's TXT records to it and it is not a domain you control. Verify a domain **you own** (e.g. `griot.app` or `mail.<your-domain>`). Sending domains need no MX records — SPF/DKIM alignment (provided by Brevo's TXT records) is what mailboxes check. `SITE_URL=https://griot.vercel.app` stays correct as the email-footer origin; it is unrelated to the sender domain.

**Best sender format that works for ALL Griot email:** one authenticated domain + the six fixed identities (`Brevo:Senders:<Key>`): `security` → OTP/2FA (no reply-to), `noreply` → system notices, `admin` → ops notices (reply-to support), `support`/`info`/`team` per audience. Never introduce new From-domains per feature.

## Acceptance Criteria

- [x] Sender-identity map resolves per `SenderKey` with reply-to control (`BrevoEmailService.cs`)
- [x] OTP email + admin notice use their own identities (`AuthService.cs`)
- [x] SMS/WhatsApp/Contacts/automations code + `ICommunicationService` facade removed;
      all comm docs contract-synced to Email-only (AGENTS root/backend, stack-contract,
      integration-contracts, api-surface, README, COMMUNICATION-GUIDE, AUTHENTICATION-GUIDE)
- [x] Redis OTP window gates email sends before Brevo
- [x] Unit tests green (`dotnet test`)
- [ ] (operator) Verify a domain YOU own in Brevo (DNS TXT: `brevo-code`, optional DKIM/SPF) so `noreply@<your-domain>` profile senders deliver — `griot.vercel.app` cannot be verified (Vercel-owned wildcard; see FAQ above)
- [ ] In-app notifications (web/mobile + AI copilot) inside the Notifications domain (next spec)

## Verification

```bash
cd backend
dotnet build --no-incremental && dotnet test --no-build   # 0W/0E, all green
python3 ../scripts/check-contract-sync.py                 # exit 0
```

---
**Engineering Excellence. Production Mindset. Professional Impact.**
