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

## Multi-Tenant Update (2026-09-11 — PLANNED)

- Pricing page renders `OrganizationPlan` tiers (Free/Pro/Enterprise) as display/billing metadata only — no payments this wave (canonical guide §8 defers payments).
- Company signup stays off the public shell this wave: onboarding is SuperAdmin-only (`POST /api/organizations`, backend 32); self-serve company signup is a later option. Landing copy says the company is onboarded by the Griot operator rather than promising self-serve signup.
- Login returns the JWT v2 pair; a user with several org memberships (or none) routes to org selection (web 05) instead of straight into the app shell.
- Public signup remains personal registration (existing email-OTP verify flow); `Owner` assignment happens at company onboarding by the SuperAdmin, never from the public form.
- Marketing plan cards must not imply client-portal or platform-console features as purchasable add-ons this wave.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
