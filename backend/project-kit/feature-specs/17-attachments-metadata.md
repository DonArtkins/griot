# Backend Feature Spec 17 — Attachments (metadata) & Webhooks readiness

## Goal
Attachment metadata list/create/delete (blob upload is spec 11) and the signed webhook
trigger endpoint (HMAC X-Trigger-Signature) — spec 09 dependency.

## Routes
- `GET|POST /api/tasks/{id}/attachments` (metadata create — blob upload is spec 11) · `DELETE /api/tasks/{id}/attachments/{attachmentId}`
- `POST /api/webhooks/trigger` (HMAC)

## Acceptance (implemented)
- [x] Attachment metadata listing + delete scoped to task visibility
- [ ] Blob upload (spec 11)
- [x] Webhook validates `sha256=` signature against `Webhook:Secret`; 202 on valid
- [x] No 501 across attachments/webhooks
