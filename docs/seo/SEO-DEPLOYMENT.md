# SEO & Public Deployment — Griot

> How to make the deployed Griot findable by Google, crawlable by LLM/bot crawlers, and production-clean for the public web. Follow this when Week-5 deploys.

---

## 1. Deploy-time SEO setup (web on Vercel)

### Meta/OG basics (in `web/index.html` + per-route)
- `<title>Griot — Project Management with an AI Copilot</title>`
- `<meta name="description" content="...">`
- Open Graph: `og:title`, `og:description`, `og:type=website`, `og:image` (OG PNG), `og:url`.
- Twitter card: `summary_large_image`.
- Canonical URL + favicon + `theme-color`.

### Sitemap + robots
- Generate `public/sitemap.xml` (static pages: `/`, `/pricing`, `/login`, `/signup`) + reference it in `robots.txt`.
- `public/robots.txt`:
```txt
User-agent: *
Allow: /
Sitemap: https://<YOUR-DOMAIN>/sitemap.xml
```
- App-shell `/app/*` is behind auth — do NOT list in sitemap; robots `Disallow: /app/` optional (it 401s anyway).

### LLM / bot crawling (the "AI can crawl my system" part)
Add a **`public/llms.txt`** (the emerging standard for LLM crawling — like robots.txt but for AI):
```txt
# Griot
> Project-management web app with an AI copilot.
> The accurate, shared record of what happened and what's next.

## Docs
- [Architecture](https://<domain>/docs/architecture): ...
- [API](https://<domain>/docs/api): ...
```
Also: expose **`/.well-known/`** as needed (ApplePay/SiteVerification are irrelevant; keep clean), and ensure **`/app`** pages return auth redirect rather than 404 so bots don't index login walls.

### Search Console (registering in Google)
1. Deploy the web app (Vercel) → get the production URL.
2. Go to [Google Search Console](https://search.google.com/search-console) → **Add property** → **URL prefix** → paste `https://<domain>`.
3. Verify: **HTML tag** (add `<meta name="google-site-verification" content="...">` to `web/index.html`) — simplest on Vercel; or DNS TXT record.
4. Submit **sitemap** (`/sitemap.xml`); Request indexing for `/`, `/pricing`.
5. Keep `robots.txt` reachable (Vercel serves `public/` automatically).

### Performance → SEO (Lighthouse)
- LCP < 2.5 s, CLS < 0.1 (NFR) — measure in Vercel Insights + Lighthouse CI (qa).
- Preload the hero image; static assets hashed by Vercel.

---

## 2. API / backend SEO-adjacent (not public-facing, but crawler-friendly)

- REST/GraphQL are behind auth — no public crawl (good). Public marketing only exposes static web.
- `/health` returns 200 quickly so uptime ping + crawlers don't see a broken root.

---

## 3. Post-deploy verification checklist

- [ ] Live URL loads; Lighthouse ≥ 90 perf/accessibility on `/` (qa gate)
- [ ] `/robots.txt` + `/sitemap.xml` + `/llms.txt` reachable (curl)
- [ ] OG image renders in a link preview (WhatsApp/Twitter/Facebook debugger)
- [ ] Search Console property verified + sitemap submitted; request indexing
- [ ] Googlebot + common AIs can fetch `/` (check in Search Console URL Inspection / a crawler tester)

---

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**