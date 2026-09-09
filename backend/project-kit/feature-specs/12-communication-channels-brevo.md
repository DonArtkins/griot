# Backend Feature Spec 12 — Communication Channels (Brevo Email / SMS / WhatsApp / Contacts) [own-stack]

## Goal

One rate-guarded communication facade (`ICommunicationService`) over Brevo so Griot
reaches users across Email, SMS, WhatsApp, and (via contact lifecycle signals) Brevo
Automations — without ever tripping Brevo rate/quota caps and without coupling business
services to an HTTP vendor.

Canonical guide: `docs/communication/COMMUNICATION-GUIDE.md`.

## Dependencies

- Spec 07 (auth — OTP delivery + admin notice) — **completes its delivery path**
- Spec 08 (Postman) — collection unaffected (no route changes)

## Separation of Concerns

- **Application** (`Griot.Application`) owns interfaces (`IEmailService`, `ISmsService`,
  `IWhatsAppService`, `IContactSynchronizer`, `ICommunicationService`) + message records.
  AuthService calls `ICommunicationService`-shaped paths only.
- **Infrastructure** (`Griot.Infrastructure/Communication/`) owns Brevo HTTP transports:
  `BrevoEmailService`, `BrevoSmsService`, `BrevoWhatsAppService`,
  `BrevoContactSynchronizer`, and the `CommunicationService` facade (Redis windows).
- **Api** (`Program.cs`) wires the HTTP clients (SSRF redirects disabled) + facade.

## Implementation

1. `IEmailService` gains `SenderKey` on `EmailMessage`; `BrevoEmailService` resolves
   `Brevo:Senders:<Key>:Email/Name/ReplyTo` (fallback `Brevo:FromEmail/FromName`), sends
   `{sender, to, subject, htmlContent, textContent, replyTo}`.
2. `BrevoSmsService` → `POST /v3/transactionalSMS/send` `{sender, recipient, content,
   type:"transactional", tag, unicodeEnabled}`; config `Brevo:Sms:Sender/Tag`.
3. `BrevoWhatsAppService` → `POST /v3/whatsapp/sendMessage` with text or `templateId`
   payloads; config `Brevo:WhatsApp:SenderNumber` (required).
4. `BrevoContactSynchronizer` → `POST /v3/contacts` (upsert: FIRSTNAME/LASTNAME/
   ACCOUNT_STATUS/EMAIL_VERIFIED) on register; `PUT /v3/contacts/{email}` on verify —
   the automation trigger hook.
5. `CommunicationService` facade: Redis sliding window per channel+recipient
   (`ratelimit:comm:email|sms|whatsapp:…`) checked BEFORE each Brevo call.
6. AuthService: OTP email uses `SenderKey:"security"`, admin notice `"admin"`; register →
   contact upsert; email-verified → contact verified. All best-effort (never fail auth).

## Rate limits (two layers, cannot hit Brevo caps)

| Channel | Facade window | AuthController (existing) |
|---|---|---|
| Email | 10 / 15 min per recipient | OTP request 3 / 15 min per email |
| SMS | 5 / hour per number | — |
| WhatsApp | 5 / hour per number | — |

## Docker & Deploy

Backend env: `BREVO_API_KEY`, `BREVO_FROM_EMAIL`, `BREVO_SENDER_<KEY>_EMAIL/_NAME/_REPLYTO`,
`BREVO_SMS_SENDER`, `BREVO_SMS_TAG`, `BREVO_WHATSAPP_SENDER_NUMBER`, `SITE_URL`. Web
(Vercel) never holds Brevo keys (only `VITE_API_URL`).

## Acceptance Criteria

- [x] Sender-identity map resolves per `SenderKey` with reply-to control (`BrevoEmailService.cs` L95–143)
- [x] OTP email + admin notice use their own identities (`AuthService.cs` L326–327, L344–345)
- [x] SMS transport implemented + wired (`BrevoSmsService.cs`; `Program.cs` L50)
- [x] WhatsApp text + template transport implemented + wired (`BrevoWhatsAppService.cs`; `Program.cs` L58)
- [x] Contact lifecycle sync on register + verify (`BrevoContactSynchronizer.cs`; `AuthService.cs` L111, L296)
- [x] Redis windows gate all three channels before Brevo (`CommunicationService.cs` L59–78)
- [x] Unit tests: `CommunicationServiceTests` (4) + AuthServiceTests updated — `dotnet test` green
- [ ] Verify a custom domain in Brevo dashboard → sends with your own From-domain (no `brevosend.com`)
- [ ] Configure SMS credits + WhatsApp Business in Brevo dashboard → set `SenderNumber`
- [ ] Build Welcome / Onboarding workflows in Brevo Automations on contact-create/attribute-update
- [ ] In-app notifications (web/mobile + AI copilot) on the same facade (next spec)
- [ ] Brevo bounce/complaint webhook → `POST /api/webhooks/brevo` (currently scaffold 501)

## Verification

```bash
cd backend
dotnet build --no-incremental && dotnet test --no-build   # 0W/0E, all green
python3 ../scripts/check-contract-sync.py                 # exit 0
```

---
**Engineering Excellence. Production Mindset. Professional Impact. 🚀**