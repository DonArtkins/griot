# Week 01 · Prompt 02 — Figma Make: Public Shell (Landing / Pricing / Login) — EXTENSIVE MASTER

**Tool:** Figma Make (single-shot generation with Plan). **When:** Week 1, after the App Shell.

> **No character limit.** The single authoritative prompt. The Public shell is the Awwwards face of Griot: high-conversion marketing + auth. Different design goal from the App shell — spectacle is welcome here (GSAP/Lenis motion in Week 3) — but it must *show the real product*, not describe it.

---

## 1. Master spec

### Design language (carries over from the App Shell)
- Dark theme matching the product's app shell (same `ui-tokens.md` palette; raised surfaces; saturated accents only for priority/status).
- Typography: large display type for hero/pricing; Inter; clean whitespace rhythm.
- Motion (Week-3 build): GSAP/Lenis reveals; sub-2-second-clean visual weight.
- A11y: WCAG AA contrast, keyboard navigable, axe-clean at the Week-6 gate.

### Page 1 — Landing `/`
- **Navigation (sticky)**: logo → Features · Pricing · Sign in (secondary) → Get started (primary).
- **Hero**: headline + subhead, primary CTA "Get started" + secondary "View demo"; **real screenshot of the Griot Kanban board** (import from the App Shell) as the hero visual — product-first, not abstract art. Trust strip under the CTA.
- **Feature grid (bento, 3×2)**: Boards & drag-drop · Comments & Attachments · Notifications · Roles & Invites · Activity feed · **AI Copilot** ("Ask Griot what's blocked this week"). Each: icon + one-liner; the Copilot + Boards cards carry product screenshots.
- **Social proof row**: testimonials/logos placeholder.
- **CTA band**: headline + email capture or "Get started free" button.
- **Footer**: logo, Product/Company/Legal links, copyright.
- Hero must stay LCP < 2.5 s — the above-the-fold board screenshot is the LCP candidate, so **load it eagerly** (`loading="eager"`, `fetchpriority="high"`) with explicit responsive `width`/`height` attributes (or `aspect-ratio` CSS) to prevent CLS; do NOT lazy-load the hero. Lazy-load only below-the-fold screenshots (bento cards, feature screenshots).

### Page 2 — Pricing `/pricing`
- **Header**: title + Monthly/Yearly toggle (-12.5%).
- **3 tiers**:
  - **Free** — 1 workspace · 3 projects · 5 members · core boards. CTA: Get started.
  - **Team** (featured, Most popular) — unlimited projects · roles + invites · notifications · activity feed · AI Copilot (limited) · email support. CTA: Start 14-day trial.
  - **Enterprise** — everything in Team · audit logs · SLA · priority support · custom onboarding. CTA: Contact sales.
- **Feature comparison mini-table** (rows = features, columns = tiers, check / dash).
- **FAQ** 5-6 accordions: Free vs Team, data/limits, AI limits, cancel anytime.
- **CTA band** after the FAQ.

### Page 3 — Login / Signup `/login`, `/signup`
- **Split layout**: left = product visual + brand quote; right = form card.
- **Login**: email + password (show/hide), Forgot?, submit; divider or continue with (oAuth placeholders — own-stack auth in v1, so keep the social row visually muted); link to Signup.
- **Signup**: name, email, password (strength meter), accept terms, Create workspace; link to Login.
- **Validation states**: empty, invalid email, short password, error banner (invalid creds).
- **Security cue**: Secure sign-in — Argon2 hashing + rotated sessions (confidence builder).

---

## 2. THE PROMPT — paste into Figma Make (no length limit)

```text
Design the Public marketing site for Griot, a project-management web app for small teams, in the same dark design language as its app shell. Spectacle is allowed (showcase face) but must remain clean, sub-2-second, and WCAG-AA accessible. Three pages, persistent sticky nav (logo to Features, Pricing, Sign in (secondary), Get started (primary)). All copy uses real product names (Boards, Tasks, Notifications, Copilot).

PAGE 1 — LANDING (/):
- Hero: headline + subhead, primary Get started + secondary View demo. REAL screenshot of the Griot kanban board as the hero visual (product-first). Trust strip below the CTA.
- Bento feature grid (3 cols x 2 rows): Boards & drag-drop, Comments & Attachments, Notifications, Roles & Invites, Activity feed, AI Copilot (Ask Griot what's blocked this week). Icons + one-liners; the Boards and Copilot cards carry product screenshots.
- Social-proof row (testimonials/logos placeholder).
- CTA band with email capture or Get started free.
- Footer: Product / Company / Legal links + copyright.
- LCP under 2.5s: load the hero screenshot EAGERLY (loading="eager", fetchpriority="high") with explicit responsive `width` and `height` attributes (or `aspect-ratio` CSS) to prevent CLS — do NOT add lazy loading to the above-the-fold hero image; lazy-load ONLY card screenshots and feature-bento screenshots that are below the fold.

PAGE 2 — PRICING (/pricing):
- Header + Monthly/Yearly toggle (-12.5%).
- 3 tiers: Free (1 workspace, 3 projects, 5 members, core boards; CTA Get started) ; Team (featured, Most popular; unlimited projects, roles + invites, notifications, activity feed, AI Copilot limited, email support; CTA Start 14-day trial) ; Enterprise (everything in Team + audit logs + SLA + priority support + custom onboarding; CTA Contact sales).
- Feature comparison mini-table (rows features, cols tiers, check / dash).
- FAQ accordion 5-6 items (Free vs Team, data, AI limits, cancel anytime).
- CTA band after FAQ.

PAGE 3 — LOGIN / SIGNUP (/login, /signup):
- Split layout: left = product screenshot + brand quote; right = form card.
- Login: email + password (show/hide), Forgot?, submit, link to Signup. Social row visually muted (own-stack auth, no working oAuth in v1).
- Signup: name, email, password with strength meter, accept terms, Create workspace button, link to Login.
- Validation states: empty / invalid email / short password / error banner.
- Security cue near submit: Secure sign-in — Argon2 hashing + rotated sessions.

CROSS-CUTTING:
- Dark theme matching app-shell tokens; large display typography; clean whitespace.
- No dashboards or kanban functionality beyond the hero + card screenshots (images only).
- A11y: AA contrast, visible focus, 44px touch, keyboard-nav. Lighthouse LCP under 2.5s.
```

---

## 3. Refine (at least 1 round)

- Make the hero product screenshot the real App-Shell board export (not a placeholder).
- Keep above-the-fold visual weight lean; preserve the LCP target.
- Tighten the 6-pack bento grid to exactly 3 cols x 2 rows.
- Pricing: ensure only the Team tier is highlighted with Most popular.

## 4. Done → handoff

- [ ] 3 pages rendered in one Figma Make file; sticky nav consistent
- [ ] Hero uses the actual board screenshot; CTA path to /signup
- [ ] Pricing Team tier featured; Monthly/Yearly toggle visible
- [ ] Login/signup forms complete with validation + security cue
- [ ] Refine at least 1 round; export to Figma components; tokens carried over from the App Shell
- [ ] Lighthouse + axe captured at week-5/6 deploy
