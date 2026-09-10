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
   `Brevo:ContactToEmail`. All best-effort (never fail auth).
3. Rate guard: `ratelimit:otp:request:{email}` 3/15min (AuthController, Redis) + API
   global limiter 100/min/caller. No per-recipient redis window for plain email beyond
   the route-level OTP window (facade removed with SMS/WhatsApp).

## Rate limits

| Layer | Window | Key |
|---|---|---|
| OTP request route | 3 / 15 min per email | `ratelimit:otp:request:{email}` |
| API global limiter | 100 / min per caller | -- |

## Docker & Deploy

Backend env: `BREVO_API_KEY`, `BREVO_FROM_EMAIL`, `BREVO_SENDER_<KEY>_EMAIL/_NAME/_REPLYTO`, `SITE_URL`. Web
(Vercel) never holds Brevo keys (only `VITE_API_URL`).

## Acceptance Criteria

- [x] Sender-identity map resolves per `SenderKey` with reply-to control (`BrevoEmailService.cs`)
- [x] OTP email + admin notice use their own identities (`AuthService.cs`)
- [x] SMS/WhatsApp/Contacts/automations code + `ICommunicationService` facade removed;
      all comm docs contract-synced to Email-only (AGENTS root/backend, stack-contract,
      integration-contracts, api-surface, README, COMMUNICATION-GUIDE, AUTHENTICATION-GUIDE)
- [x] Redis OTP window gates email sends before Brevo
- [x] Unit tests green (`dotnet test`)
- [ ] Verify a custom domain in Brevo dashboard -> sends with your own From-domain (no `brevosend.com`)
- [ ] In-app notifications (web/mobile + AI copilot) inside the Notifications domain (next spec)

## Verification

```bash
cd backend
dotnet build --no-incremental && dotnet test --no-build   # 0W/0E, all green
python3 ../scripts/check-contract-sync.py                 # exit 0
```

---
**Engineering Excellence. Production Mindset. Professional Impact.**
