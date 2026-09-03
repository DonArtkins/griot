# Feature 06 — Public Shell (Landing / Pricing / Login)

## Type

NEW FEATURE

## What This Delivers

The **Public shell** from the Week-1 Figma design system: an Awwwards-grade landing page (hero with real product UI + GSAP/Lenis motion), pricing (3 tiers), login/signup forms — the conversion-facing surface.

## Dependencies

- Web features 01, 02, 05.
- Week-1 Public-shell Figma screens finalized.

## Context To Read First

- `web/project-kit/context/{architecture,design-system,code-standards}.md`
- `ui-rules.md` (Public shell budget)

## Agent Skills To Use

- `web/.agents/skills/material-ui-theme/SKILL.md` (+ motion conventions)
- Root `throttling-prevention` for long UI passes

## Files Owned

- `web/src/routes/public.tsx`, `web/src/features/marketing/**`
- `web/src/components/marketing/**`

## Implementation Notes

- Hero embeds the real dashboard screenshot (product-first).
- GSAP/Lenis lazy-loaded via dynamic import; `useGSAP()` StrictMode-safe.
- Login/signup wired to feature 05 auth flow.
- Lighthouse budget; axe-clean.

## Separation of Concerns

- Marketing components live in `features/marketing`; motion utilities a `lib/motion` concern used only here.

## Docker & Deploy

- No change (Vercel). Lighthouse recorded after deploy (qa/Week 6).

## Out of Scope

App-shell screens (feature 07).

## Acceptance Criteria

- [ ] `/` `/pricing` `/login` `/signup` render from Figma; auth works
- [ ] GSAP only in this subtree; `npm run build` green; axe + Lighthouse pass
