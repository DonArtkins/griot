# Feature 06 — Fully Responsive Cross-Device UI

## Type

NEW FEATURE

## What This Delivers

The bootcamp deliverable **"Fully responsive cross-device UI"**: layouts that adapt across phone/tablet/landscape using `LayoutBuilder`/`MediaQuery`, the theme from tokens, and the Week-1 Figma mobile frames.

## Dependencies

- Mobile features 04 + 05 (data + state).

## Context To Read First

- `mobile/project-kit/context/{design-system,architecture}.md`

## Files Owned

- `mobile/lib/core/theme/theme.dart`
- `mobile/lib/features/**/widgets/**` (responsive variants)

## Implementation Notes

- Breakpoints: phone (<600), tablet (600–1024), large (>1024).
- Board on phone: horizontally scrollable columns; on tablet: 2–3 columns visible.
- Use the token theme; status colors match the enum map.

## Separation of Concerns

- Responsive layout lives in feature widgets; theme central in `core/theme`.

## Docker & Deploy

- Verified on emulator (phone + tablet profiles) and a physical device.

## Out of Scope

Web parity pixel-matching.

## Acceptance Criteria

- [ ] UI adapts at all breakpoints without overflow errors
- [ ] Board usable on phone + tablet; status picker accessible
